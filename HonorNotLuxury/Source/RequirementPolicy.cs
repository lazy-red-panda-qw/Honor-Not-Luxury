using System.Linq;
using System.Runtime.CompilerServices;
using RimWorld;
using Verse;

namespace HonorNotLuxury
{
    public static class RequirementPolicy
    {
        public static bool Applies(Pawn pawn, Requirement requirement)
        {
            if (!PatchBootstrap.Healthy || pawn == null || pawn.royalty == null) return false;
            switch (HonorNotLuxuryMod.Settings.Get(requirement))
            {
                case RequirementScope.Everyone: return true;
                case RequirementScope.Colonists: return IsPermanentColonist(pawn);
                default: return false;
            }
        }

        public static bool IsPermanentColonist(Pawn pawn)
        {
            // IsColonistPlayerControlled excludes caravans and mental states.
            // Quest lodgers may temporarily belong to the player faction.
            return pawn != null && pawn.Faction != null && pawn.Faction.IsPlayer
                && pawn.RaceProps.Humanlike && !pawn.IsSubhuman
                && !pawn.IsPrisoner && !pawn.IsSlave && pawn.HostFaction == null
                && !pawn.IsQuestLodger();
        }

        // Replace only READS at selected calculation sites, never the persisted field.
        // Vanilla assigns RoyalTitle.pawn when creating a title and at post-load init.
        public static bool TitleAffectsExpectations(RoyalTitle title) =>
            title.conceited && !Applies(title.pawn, Requirement.Expectations);

        public static bool TitleRestrictsWork(RoyalTitle title) =>
            title.conceited && !Applies(title.pawn, Requirement.Work);

        public static bool TitleNeedsFoodAlert(RoyalTitle title) =>
            title.conceited && !Applies(title.pawn, Requirement.Food);
    }

    public static class RuntimeRefresh
    {
        private sealed class WorkState { public bool Removed; }
        private static readonly ConditionalWeakTable<Pawn, WorkState> WorkStates = new ConditionalWeakTable<Pawn, WorkState>();

        public static void EnsureWorkPolicy(Pawn pawn)
        {
            if (pawn?.royalty == null) return;
            WorkState state = WorkStates.GetOrCreateValue(pawn);
            bool removed = RequirementPolicy.Applies(pawn, Requirement.Work);
            if (state.Removed == removed) return;
            state.Removed = removed; // Refresh invokes GetDisabledWorkTypes again.
            RefreshPawn(pawn);
        }

        public static void RefreshPawn(Pawn pawn)
        {
            if (pawn?.royalty == null) return;
            WorkStates.GetOrCreateValue(pawn).Removed = RequirementPolicy.Applies(pawn, Requirement.Work);
            pawn.Notify_DisabledWorkTypesChanged();
            pawn.needs?.mood?.thoughts?.situational?.Notify_SituationalThoughtsDirty();
        }

        public static void RefreshAll()
        {
            if (Current.Game == null || Current.ProgramState != ProgramState.Playing) return;
            foreach (Pawn pawn in PawnsFinder.AllMapsWorldAndTemporary_Alive.ToList()) RefreshPawn(pawn);
        }
    }
}
