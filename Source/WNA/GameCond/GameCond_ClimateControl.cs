using RimWorld;
using Verse;

namespace WNA.GameCond
{
    public class GameCond_ClimateControl : GameCondition
    {
        public float offset;
        public WeatherDef weather;
        public override void Init()
        {
            base.Init();
            offset = def.temperatureOffset;
            if (weather == null)
                weather = def.weatherDef;
        }
        public override float TemperatureOffset()
        {
            return offset;
        }
        public override WeatherDef ForcedWeather()
        {
            return weather;
        }
        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref offset, "offset", 0f);
            Scribe_Defs.Look(ref weather, "weather");
        }
    }
}
