using RimWorld;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;
using WNA.GameCond;

namespace WNA.ThingCompProp
{
    public class CompClimateControl : CompCauseGameCondition
    {
        public float offset;
        public WeatherDef weather;
        public GameCond_ClimateControl cond;
        public override void Initialize(CompProperties props)
        {
            base.Initialize(props);
            offset = Props.conditionDef.temperatureOffset;
            weather = Props.conditionDef.weatherDef;
        }
        public override void PostExposeData()
        {
            base.PostExposeData();
            Scribe_Values.Look(ref offset, "temp", 0f);
            Scribe_Defs.Look(ref weather, "weather");
        }
        public override IEnumerable<Gizmo> CompGetGizmosExtra()
        {
            Command_Action command_Action = new Command_Action
            {
                defaultLabel = "-10",
                icon = ContentFinder<Texture2D>.Get("UI/Commands/TempLower"),
                action = delegate
                {
                    offset -= 10f;
                    offset = Mathf.Clamp(offset, -273.15f, 1000f);
                    ReSetupAllConditions();
                }
            };
            yield return command_Action;
            Command_Action command_Action2 = new Command_Action
            {
                defaultLabel = "-1",
                icon = ContentFinder<Texture2D>.Get("UI/Commands/TempLower"),
                action = delegate
                {
                    offset -= 1f;
                    offset = Mathf.Clamp(offset, -273.15f, 1000f);
                    ReSetupAllConditions();
                }
            };
            yield return command_Action2;
            Command_Action command_Action3 = new Command_Action
            {
                defaultLabel = "+1",
                icon = ContentFinder<Texture2D>.Get("UI/Commands/TempRaise"),
                action = delegate
                {
                    offset += 1f;
                    offset = Mathf.Clamp(offset, -273.15f, 1000f);
                    ReSetupAllConditions();
                }
            };
            yield return command_Action3;
            Command_Action command_Action4 = new Command_Action
            {
                defaultLabel = "+10",
                icon = ContentFinder<Texture2D>.Get("UI/Commands/TempRaise"),
                action = delegate
                {
                    offset += 10f;
                    offset = Mathf.Clamp(offset, -273.15f, 1000f);
                    ReSetupAllConditions();
                }
            };
            yield return command_Action4;
            Command_Action command_Action5 = new Command_Action
            {
                defaultLabel = weather.LabelCap,
                icon = ContentFinder<Texture2D>.Get("UI/Commands/TempReset"),
                action = delegate
                {
                    List<FloatMenuOption> options = new List<FloatMenuOption>();
                    var sortedWeathers = DefDatabase<WeatherDef>.
                        AllDefsListForReading.OrderBy(w => w.label).ToList();
                    foreach (WeatherDef wli in sortedWeathers)
                    {
                        string text = wli.LabelCap + "\n(" + wli.defName + ")";
                        options.Add(new FloatMenuOption(text, delegate
                        {
                            weather = wli;
                            ReSetupAllConditions();
                        }));
                    }
                    if (options.Any())
                        Find.WindowStack.Add(new FloatMenu(options));
                }
            };
            yield return command_Action5;
        }
        protected override void SetupCondition(GameCondition condition, Map map)
        {
            base.SetupCondition(condition, map);
            ((GameCond_ClimateControl)condition).offset = offset;
            ((GameCond_ClimateControl)condition).weather = weather;
        }
        public override string CompInspectStringExtra()
        {
            string text = base.CompInspectStringExtra();
            if (!text.NullOrEmpty())
                text += "\n";
            return text + ("Temperature".Translate() + ": " + offset + "\n"
                 + "Weather".Translate() + ": " + weather.LabelCap);
        }
    }
}
