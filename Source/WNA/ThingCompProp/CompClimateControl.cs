using RimWorld;
using Verse;
using WNA.GameCondition;

namespace WNA.ThingCompProp
{
    public class PropClimateControl : CompProperties
    {
        public PropClimateControl()
        {
            compClass = typeof(CompClimateControl);
        }
    }
    public class CompClimateControl : ThingComp
    {
        public GameCond_ClimateControl condition;
        public float targetTemperature = 21f;
        public WeatherDef forcedWeather = WeatherDefOf.Clear;
        private bool lastPoweredOn = false;
        private Map Map => parent.Map;
        public override void PostExposeData()
        {
            base.PostExposeData();
            Scribe_Values.Look(ref targetTemperature, "targetTemperature", 21f);
            Scribe_Values.Look(ref forcedWeather, "forcedWeather");
            Scribe_References.Look(ref condition, "condition");
        }
        public override void CompTickRare()
        {
            base.CompTickRare();
            CompPowerTrader powerComp = parent.TryGetComp<CompPowerTrader>();
            bool poweredOn = powerComp != null && powerComp.PowerOn;
            if (poweredOn != lastPoweredOn)
            {
                if (poweredOn)
                {
                    RimWorld.GameCondition cond = GameConditionMaker.MakeCondition(DefDatabase<GameConditionDef>.GetNamedSilentFail("WNA_GameCond_ClimateControl"));
                    if (cond is GameCond_ClimateControl climateCond)
                    {
                        Map.gameConditionManager.RegisterCondition(climateCond);
                        climateCond.controller = (ThingClass.ClimateControl)parent;
                        condition = climateCond;
                    }
                }
                else
                {
                    if (condition != null)
                    {
                        condition.End();
                        condition = null;
                    }
                }
                lastPoweredOn = poweredOn;
            }
            if (poweredOn && condition != null)
            {
                CompTempControl tempComp = parent.TryGetComp<CompTempControl>();
                if (tempComp != null)
                    targetTemperature = tempComp.targetTemperature;
                else
                    targetTemperature = 21f;
                condition.SetTargetTemperature(targetTemperature);
                condition.SetForcedWeather(forcedWeather);
            }
        }
    }
}
