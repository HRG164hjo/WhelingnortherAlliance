using RimWorld;

namespace WNA.WNARitual
{
    public class RitualOutcomeComp_Uplifting : RitualOutcomeComp
    {
        public override bool DataRequired => false;

        public override bool Applies(LordJob_Ritual ritual)
        {
            return true;
        }
    }
}
