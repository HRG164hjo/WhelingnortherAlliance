using RimWorld;
using Verse;

namespace WNA.WNARitual
{
    public class RitualOutcomeComp_AbilityGrant : RitualOutcomeComp
    {
        public override bool DataRequired => false;

        public override bool Applies(LordJob_Ritual ritual)
        {
            return true;
        }
    }
}
