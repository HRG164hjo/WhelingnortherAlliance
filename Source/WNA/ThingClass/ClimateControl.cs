using RimWorld;
using Verse;
using WNA.ThingCompProp;

namespace WNA.ThingClass
{
    public class ClimateControl : Building_TempControl
    {
        public CompClimateControl ClimateComp => GetComp<CompClimateControl>();

        public override void Destroy(DestroyMode mode)
        {
            CompPowerTrader powerComp = GetComp<CompPowerTrader>();
            if (powerComp?.PowerOn == true)
                ClimateComp.CompTickRare();
            base.Destroy(mode);
        }
    }
}
