using System;
using Verse;

namespace HonorNotLuxury
{
    public enum RequirementScope { Unchanged, Colonists, Everyone }
    public enum Requirement { Bedroom, Throneroom, ApparelStyle, ApparelQuality, Food, Expectations, Work }

    // Stored in normal mod configuration, never on a pawn or title.
    public sealed class HonorNotLuxurySettings : ModSettings
    {
        public RequirementScope Bedroom = RequirementScope.Colonists;
        public RequirementScope Throneroom = RequirementScope.Colonists;
        public RequirementScope ApparelStyle = RequirementScope.Colonists;
        public RequirementScope ApparelQuality = RequirementScope.Colonists;
        public RequirementScope Food = RequirementScope.Colonists;
        public RequirementScope Expectations = RequirementScope.Colonists;
        public RequirementScope Work = RequirementScope.Unchanged;

        public override void ExposeData()
        {
            Scribe_Values.Look(ref Bedroom, "bedroom", RequirementScope.Colonists);
            Scribe_Values.Look(ref Throneroom, "throneroom", RequirementScope.Colonists);
            Scribe_Values.Look(ref ApparelStyle, "apparelStyle", RequirementScope.Colonists);
            Scribe_Values.Look(ref ApparelQuality, "apparelQuality", RequirementScope.Colonists);
            Scribe_Values.Look(ref Food, "food", RequirementScope.Colonists);
            Scribe_Values.Look(ref Expectations, "expectations", RequirementScope.Colonists);
            Scribe_Values.Look(ref Work, "work", RequirementScope.Unchanged);
            if (Scribe.mode == LoadSaveMode.PostLoadInit)
                foreach (Requirement requirement in Enum.GetValues(typeof(Requirement)))
                    if (!Enum.IsDefined(typeof(RequirementScope), Get(requirement)))
                        Set(requirement, requirement == Requirement.Work ? RequirementScope.Unchanged : RequirementScope.Colonists);
        }

        public RequirementScope Get(Requirement requirement)
        {
            switch (requirement)
            {
                case Requirement.Bedroom: return Bedroom;
                case Requirement.Throneroom: return Throneroom;
                case Requirement.ApparelStyle: return ApparelStyle;
                case Requirement.ApparelQuality: return ApparelQuality;
                case Requirement.Food: return Food;
                case Requirement.Expectations: return Expectations;
                case Requirement.Work: return Work;
                default: throw new ArgumentOutOfRangeException(nameof(requirement));
            }
        }

        public void Set(Requirement requirement, RequirementScope scope)
        {
            if (!Enum.IsDefined(typeof(RequirementScope), scope)) throw new ArgumentOutOfRangeException(nameof(scope));
            switch (requirement)
            {
                case Requirement.Bedroom: Bedroom = scope; break;
                case Requirement.Throneroom: Throneroom = scope; break;
                case Requirement.ApparelStyle: ApparelStyle = scope; break;
                case Requirement.ApparelQuality: ApparelQuality = scope; break;
                case Requirement.Food: Food = scope; break;
                case Requirement.Expectations: Expectations = scope; break;
                case Requirement.Work: Work = scope; break;
                default: throw new ArgumentOutOfRangeException(nameof(requirement));
            }
        }

        public void ResetDefaults()
        {
            foreach (Requirement requirement in Enum.GetValues(typeof(Requirement)))
                Set(requirement, requirement == Requirement.Work ? RequirementScope.Unchanged : RequirementScope.Colonists);
        }
    }
}
