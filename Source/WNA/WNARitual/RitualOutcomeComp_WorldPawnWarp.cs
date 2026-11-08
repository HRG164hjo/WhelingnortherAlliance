using RimWorld;

namespace WNA.WNARitual
{
    public class RitualOutcomeComp_WorldPawnWarp : RitualOutcomeComp
    {
        public override bool DataRequired => false;

        public override bool Applies(LordJob_Ritual ritual)
        {
            return true;
        }
    }
}
