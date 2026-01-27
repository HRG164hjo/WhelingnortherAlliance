using RimWorld;
using Verse;

namespace WNA.GameCondition
{
    public class GameCond_ClimateControl : RimWorld.GameCondition
    {
        public float targetTemperature = 21f;
        public WeatherDef forcedWeather = WeatherDefOf.Clear;
        public ThingClass.ClimateControl controller;
        public void SetTargetTemperature(float temp)
        {
            targetTemperature = temp;
        }
        public void SetForcedWeather(WeatherDef weather)
        {
            forcedWeather = weather;
        }
        public override WeatherDef ForcedWeather()
        {
            return forcedWeather;
        }
        public override void GameConditionTick()
        {
            base.GameConditionTick();
            if (controller == null || !controller.Spawned)
            {
                base.End();
                return;
            }
            if (Find.TickManager.TicksGame % 250 != 0) return;
            foreach (Map map in AffectedMaps)
            {
                if (!map.Biome.inVacuum) continue;
                VacuumComponent vacComp = map.GetComponent<VacuumComponent>();
                vacComp?.Dirty();
                foreach (Room room in map.regionGrid.AllRooms)
                {
                    room.Vacuum = 0f;
                }
            }
        }
        public override void Init()
        {
            base.Init();
        }
    }
}
