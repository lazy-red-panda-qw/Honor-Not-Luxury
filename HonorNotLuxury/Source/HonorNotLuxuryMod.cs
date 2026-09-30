using System;
using System.Collections.Generic;
using UnityEngine;
using Verse;

namespace HonorNotLuxury
{
    public sealed class HonorNotLuxuryMod : Mod
    {
        public const string HarmonyId = "local.honornotluxury";
        public static HonorNotLuxurySettings Settings = new HonorNotLuxurySettings();
        private Vector2 scrollPosition;

        public HonorNotLuxuryMod(ModContentPack content) : base(content)
        {
            Settings = GetSettings<HonorNotLuxurySettings>();
            PatchBootstrap.Apply();
        }

        public override string SettingsCategory() => "HNL_Name".Translate();

        public override void DoSettingsWindowContents(Rect inRect)
        {
            float width = inRect.width - 20f;
            float introHeight = Text.CalcHeight("HNL_Intro".Translate(), width) + 12f;
            float footerHeight = Text.CalcHeight("HNL_Footer".Translate(), width) + 16f;
            float buttonWidth = Math.Min(250f, width * 0.48f);
            float labelWidth = width - buttonWidth - 12f;
            var heights = new float[7];
            float contentHeight = introHeight + footerHeight + 45f;
            foreach (Requirement requirement in Enum.GetValues(typeof(Requirement)))
            {
                heights[(int)requirement] = Math.Max(42f,
                    Text.CalcHeight(("HNL_" + requirement).Translate(), labelWidth) + 12f);
                contentHeight += heights[(int)requirement];
            }
            if (!PatchBootstrap.Healthy) contentHeight += 65f;
            Rect view = new Rect(0, 0, width, Math.Max(contentHeight, inRect.height));
            Widgets.BeginScrollView(inRect, ref scrollPosition, view);
            float y = 0;
            if (!PatchBootstrap.Healthy)
            {
                Widgets.Label(new Rect(0, y, width, 60f), "HNL_PatchFailure".Translate());
                y += 65f;
            }
            Widgets.Label(new Rect(0, y, width, introHeight), "HNL_Intro".Translate());
            y += introHeight;
            foreach (Requirement requirement in Enum.GetValues(typeof(Requirement)))
            {
                float height = heights[(int)requirement];
                Widgets.Label(new Rect(0, y + 6f, labelWidth, height), ("HNL_" + requirement).Translate());
                TooltipHandler.TipRegion(new Rect(0, y, width, height), ("HNL_" + requirement + "Tip").Translate());
                if (Widgets.ButtonText(new Rect(width - buttonWidth, y, buttonWidth, height - 6f), ScopeLabel(Settings.Get(requirement))))
                {
                    var options = new List<FloatMenuOption>();
                    foreach (RequirementScope scope in Enum.GetValues(typeof(RequirementScope)))
                    {
                        Requirement selectedRequirement = requirement;
                        RequirementScope selectedScope = scope;
                        options.Add(new FloatMenuOption(ScopeLabel(scope), () =>
                        {
                            Settings.Set(selectedRequirement, selectedScope);
                            WriteSettings();
                        }));
                    }
                    Find.WindowStack.Add(new FloatMenu(options));
                }
                y += height;
            }
            Widgets.Label(new Rect(0, y, width, footerHeight), "HNL_Footer".Translate());
            y += footerHeight;
            if (Widgets.ButtonText(new Rect(0, y, Math.Min(230f, width), 32f), "HNL_Reset".Translate()))
            {
                Settings.ResetDefaults();
                WriteSettings();
            }
            Widgets.EndScrollView();
        }

        public override void WriteSettings()
        {
            base.WriteSettings();
            RuntimeRefresh.RefreshAll();
        }

        private static string ScopeLabel(RequirementScope scope) => ("HNL_Scope_" + scope).Translate();
    }
}
