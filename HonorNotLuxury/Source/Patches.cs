using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using RimWorld;
using Verse;

namespace HonorNotLuxury
{
    public static class PatchBootstrap
    {
        public static bool Healthy { get; private set; }
        public static bool Apply()
        {
            if (Healthy) return true;
            var harmony = new Harmony(HonorNotLuxuryMod.HarmonyId);
            try
            {
                harmony.PatchAll(typeof(PatchBootstrap).Assembly);
                Healthy = true;
                Log.Message("[Honor, Not Luxury] 2.0 active: seven scoped policies; bestowing requirements preserved.");
            }
            catch (Exception ex)
            {
                Healthy = false;
                harmony.UnpatchAll(HonorNotLuxuryMod.HarmonyId);
                Log.Error("[Honor, Not Luxury] Patches disabled; original rules retained.\n" + ex);
            }
            return Healthy;
        }
    }

    [HarmonyPatch(typeof(Pawn_RoyaltyTracker), nameof(Pawn_RoyaltyTracker.CanRequireBedroom))]
    public static class Patch_Bedroom
    {
        public static void Postfix(Pawn_RoyaltyTracker __instance, ref bool __result)
        {
            if (RequirementPolicy.Applies(__instance.pawn, Requirement.Bedroom)) __result = false;
        }
    }

    [HarmonyPatch(typeof(Pawn_RoyaltyTracker), nameof(Pawn_RoyaltyTracker.CanRequireThroneroom))]
    public static class Patch_Throneroom
    {
        public static void Postfix(Pawn_RoyaltyTracker __instance, ref bool __result)
        {
            if (RequirementPolicy.Applies(__instance.pawn, Requirement.Throneroom)) __result = false;
        }
    }

    [HarmonyPatch(typeof(ThoughtWorker_RoyalTitleApparelRequirementNotMet), "CurrentStateInternal")]
    public static class Patch_ApparelStyle
    {
        public static bool Prefix(Pawn p, ref ThoughtState __result)
        {
            if (!RequirementPolicy.Applies(p, Requirement.ApparelStyle)) return true;
            __result = ThoughtState.Inactive;
            return false;
        }
    }

    [HarmonyPatch(typeof(ThoughtWorker_RoyalTitleApparelMinQualityNotMet), "CurrentStateInternal")]
    public static class Patch_ApparelQuality
    {
        public static bool Prefix(Pawn p, ref ThoughtState __result)
        {
            if (!RequirementPolicy.Applies(p, Requirement.ApparelQuality)) return true;
            __result = ThoughtState.Inactive;
            return false;
        }
    }

    [HarmonyPatch(typeof(FoodUtility), nameof(FoodUtility.InappropriateForTitle), new[] { typeof(ThingDef), typeof(Pawn), typeof(bool) })]
    public static class Patch_Food
    {
        public static void Postfix(Pawn p, ref bool __result)
        {
            if (RequirementPolicy.Applies(p, Requirement.Food)) __result = false;
        }
    }

    [HarmonyPatch(typeof(Alert_RoyalNoAcceptableFood), nameof(Alert_RoyalNoAcceptableFood.Targets), MethodType.Getter)]
    public static class Patch_FoodAlert
    {
        // Skip exempt candidates BEFORE vanilla fills its shared food-search cache.
        // Filtering results afterwards could let a colonist's ordinary meal satisfy
        // the cached search for an unexempt visitor with the same title requirements.
        public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions, MethodBase __originalMethod) =>
            TitleGateTranspiler.Replace(instructions, __originalMethod, nameof(RequirementPolicy.TitleNeedsFoodAlert));
    }

    public static class TitleGateTranspiler
    {
        public static IEnumerable<CodeInstruction> Replace(IEnumerable<CodeInstruction> instructions,
            MethodBase original, string replacement)
        {
            var codes = instructions.ToList();
            var field = AccessTools.Field(typeof(RoyalTitle), nameof(RoyalTitle.conceited));
            var matches = codes.Where(c => c.opcode == OpCodes.Ldfld && Equals(c.operand, field)).ToList();
            if (matches.Count != 1)
                throw new InvalidOperationException("Expected exactly one RoyalTitle.conceited read in "
                    + original.FullDescription() + "; found " + matches.Count);
            // Same stack shape: RoyalTitle -> bool. Preserve labels and exception blocks.
            matches[0].opcode = OpCodes.Call;
            matches[0].operand = AccessTools.Method(typeof(RequirementPolicy), replacement);
            return codes;
        }
    }

    [HarmonyPatch(typeof(ExpectationsUtility), nameof(ExpectationsUtility.CurrentExpectationFor), new[] { typeof(Pawn) })]
    public static class Patch_Expectations
    {
        public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions, MethodBase __originalMethod) =>
            TitleGateTranspiler.Replace(instructions, __originalMethod, nameof(RequirementPolicy.TitleAffectsExpectations));
    }

    [HarmonyPatch]
    public static class Patch_Work
    {
        public static IEnumerable<MethodBase> TargetMethods()
        {
            yield return AccessTools.PropertyGetter(typeof(Pawn), nameof(Pawn.CombinedDisabledWorkTags));
            var builders = typeof(Pawn).GetMethods(AccessTools.allDeclared)
                .Where(m => m.Name.StartsWith("<GetDisabledWorkTypes>g__FillList|", StringComparison.Ordinal)).ToList();
            if (builders.Count != 1) throw new MissingMethodException("Pawn.GetDisabledWorkTypes FillList: expected one builder.");
            yield return builders[0];
            yield return AccessTools.Method(typeof(Pawn), nameof(Pawn.GetReasonsForDisabledWorkType));
            yield return AccessTools.Method(typeof(CharacterCardUtility), "GetWorkTypeDisableCauses");
        }

        public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions, MethodBase __originalMethod) =>
            TitleGateTranspiler.Replace(instructions, __originalMethod, nameof(RequirementPolicy.TitleRestrictsWork));
    }

    [HarmonyPatch]
    public static class Patch_WorkCache
    {
        public static IEnumerable<MethodBase> TargetMethods()
        {
            yield return AccessTools.Method(typeof(Pawn), nameof(Pawn.GetDisabledWorkTypes));
            yield return AccessTools.Method(typeof(Pawn), nameof(Pawn.GetReasonsForDisabledWorkType));
        }
        public static void Prefix(Pawn __instance) => RuntimeRefresh.EnsureWorkPolicy(__instance);
    }

    [HarmonyPatch]
    public static class Patch_WorkGiverCache
    {
        public static IEnumerable<MethodBase> TargetMethods()
        {
            yield return AccessTools.PropertyGetter(typeof(Pawn_WorkSettings), nameof(Pawn_WorkSettings.WorkGiversInOrderNormal));
            yield return AccessTools.PropertyGetter(typeof(Pawn_WorkSettings), nameof(Pawn_WorkSettings.WorkGiversInOrderEmergency));
        }
        public static void Prefix(Pawn ___pawn) => RuntimeRefresh.EnsureWorkPolicy(___pawn);
    }

    [HarmonyPatch(typeof(Pawn), nameof(Pawn.SetFaction), new[] { typeof(Faction), typeof(Pawn) })]
    public static class Patch_FactionChanged
    {
        public static void Postfix(Pawn __instance) => RuntimeRefresh.RefreshPawn(__instance);
    }

    [HarmonyPatch(typeof(Pawn_GuestTracker), nameof(Pawn_GuestTracker.SetGuestStatus), new[] { typeof(Faction), typeof(GuestStatus) })]
    public static class Patch_GuestChanged
    {
        public static void Postfix(Pawn ___pawn) => RuntimeRefresh.RefreshPawn(___pawn);
    }
}
