using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.Sound;

namespace WNA.WNAThingComp
{
    internal class PropTempControl : CompProperties
    {
        public float defaultTargetTemperature = 20f;
        public float energyPerSecond = 30f;
        public float maxTempChangePerInterval = 2f;
        public float smoothing = 0.5f;
        public float deadband = 0.5f;

        public float minTargetTemperature = -50f;

        public float maxTargetTemperature = 100f;
        public PropTempControl()
        {
            compClass = typeof(CompTempControl);
        }
    }
    internal class CompTempControl : ThingComp
    {
        private const int IntervalTicks = 250;

        private const float TicksPerSecond = 60f;

        private readonly List<Room> targetRooms = new List<Room>();

        public bool enabled = true;

        public float targetTemperature = -99999f;

        public PropTempControl Props => (PropTempControl)props;

        public virtual float TargetTemperature
        {
            get
            {
                return targetTemperature;
            }
            set
            {
                targetTemperature = Mathf.Clamp(value, Props.minTargetTemperature, Props.maxTargetTemperature);
            }
        }
        public override void PostSpawnSetup(bool respawningAfterLoad)
        {
            base.PostSpawnSetup(respawningAfterLoad);
            if (TargetTemperature < -2000f)
                TargetTemperature = Props.defaultTargetTemperature;
        }
        public override void CompTick()
        {
            base.CompTick();
            if (parent.IsHashIntervalTick(IntervalTicks))
                ApplyTemperatureControl();
        }
        public override void CompTickRare()
        {
            base.CompTickRare();
            ApplyTemperatureControl();
        }
        public override void PostExposeData()
        {
            base.PostExposeData();
            Scribe_Values.Look(ref enabled, "enabled", defaultValue: true);
            Scribe_Values.Look(ref targetTemperature, "targetTemperature", -99999f);
        }
        private void ApplyTemperatureControl()
        {
            if (!enabled || !parent.SpawnedOrAnyParentSpawned)
                return;
            CollectTargetRooms(targetRooms);
            if (targetRooms.Count == 0)
                return;
            int totalCells = 0;
            for (int i = 0; i < targetRooms.Count; i++)
                totalCells += targetRooms[i].CellCount;
            float maxDelta = Mathf.Min(Props.energyPerSecond * IntervalTicks / TicksPerSecond / totalCells, Props.maxTempChangePerInterval);
            float smoothing = Mathf.Clamp01(Props.smoothing);
            for (int j = 0; j < targetRooms.Count; j++)
            {
                Room room = targetRooms[j];
                float gap = TargetTemperature - room.Temperature;
                if (Mathf.Abs(gap) <= Props.deadband)
                    continue;
                float delta = Mathf.Clamp(gap * smoothing, 0f - maxDelta, maxDelta);
                if (Mathf.Abs(delta) > 0.0001f)
                    room.PushHeat(delta * room.CellCount);
            }
            targetRooms.Clear();
        }
        private void CollectTargetRooms(List<Room> rooms)
        {
            rooms.Clear();
            Room room = parent.GetRoom();
            if (room != null)
            {
                if (!room.UsesOutdoorTemperature)
                    rooms.Add(room);
                return;
            }
            Map map = parent.MapHeld;
            if (map == null)
                return;
            foreach (IntVec3 cell in GenAdj.CellsAdjacent8Way(parent.PositionHeld, parent.Rotation, parent.def.size))
            {
                if (!cell.InBounds(map))
                    continue;
                room = cell.GetRoom(map);
                if (room != null && !room.UsesOutdoorTemperature && !rooms.Contains(room))
                    rooms.Add(room);
            }
        }
        protected float RoundedToCurrentTempModeOffset(float celsiusTemp)
        {
            return GenTemperature.ConvertTemperatureOffset(Mathf.RoundToInt(GenTemperature.CelsiusToOffset(celsiusTemp, Prefs.TemperatureMode)), Prefs.TemperatureMode, TemperatureDisplayMode.Celsius);
        }
        public override IEnumerable<Gizmo> CompGetGizmosExtra()
        {
            foreach (Gizmo item in base.CompGetGizmosExtra())
                yield return item;
            float offset = RoundedToCurrentTempModeOffset(-10f);
            Command_Action lowerFast = new Command_Action();
            lowerFast.action = delegate { InterfaceChangeTargetTemperature(offset); };
            lowerFast.defaultLabel = offset.ToStringTemperatureOffset("F0");
            lowerFast.defaultDesc = "CommandLowerTempDesc".Translate();
            lowerFast.hotKey = KeyBindingDefOf.Misc5;
            lowerFast.icon = ContentFinder<Texture2D>.Get("UI/Commands/TempLower");
            yield return lowerFast;
            float offset2 = RoundedToCurrentTempModeOffset(-1f);
            Command_Action lower = new Command_Action();
            lower.action = delegate { InterfaceChangeTargetTemperature(offset2); };
            lower.defaultLabel = offset2.ToStringTemperatureOffset("F0");
            lower.defaultDesc = "CommandLowerTempDesc".Translate();
            lower.hotKey = KeyBindingDefOf.Misc4;
            lower.icon = ContentFinder<Texture2D>.Get("UI/Commands/TempLower");
            yield return lower;
            Command_Action reset = new Command_Action();
            reset.action = delegate
            {
                TargetTemperature = Props.defaultTargetTemperature;
                SoundDefOf.Tick_Tiny.PlayOneShotOnCamera();
                ThrowCurrentTemperatureText();
            };
            reset.defaultLabel = "CommandResetTemp".Translate();
            reset.defaultDesc = "CommandResetTempDesc".Translate();
            reset.hotKey = KeyBindingDefOf.Misc1;
            reset.icon = ContentFinder<Texture2D>.Get("UI/Commands/TempReset");
            yield return reset;
            float offset3 = RoundedToCurrentTempModeOffset(1f);
            Command_Action raise = new Command_Action();
            raise.action = delegate { InterfaceChangeTargetTemperature(offset3); };
            raise.defaultLabel = "+" + offset3.ToStringTemperatureOffset("F0");
            raise.defaultDesc = "CommandRaiseTempDesc".Translate();
            raise.hotKey = KeyBindingDefOf.Misc2;
            raise.icon = ContentFinder<Texture2D>.Get("UI/Commands/TempRaise");
            yield return raise;
            float offset4 = RoundedToCurrentTempModeOffset(10f);
            Command_Action raiseFast = new Command_Action();
            raiseFast.action = delegate { InterfaceChangeTargetTemperature(offset4); };
            raiseFast.defaultLabel = "+" + offset4.ToStringTemperatureOffset("F0");
            raiseFast.defaultDesc = "CommandRaiseTempDesc".Translate();
            raiseFast.hotKey = KeyBindingDefOf.Misc3;
            raiseFast.icon = ContentFinder<Texture2D>.Get("UI/Commands/TempRaise");
            yield return raiseFast;
        }
        protected void InterfaceChangeTargetTemperature(float offset)
        {
            SoundDefOf.DragSlider.PlayOneShotOnCamera();
            TargetTemperature += offset;
            ThrowCurrentTemperatureText();
        }
        protected void ThrowCurrentTemperatureText()
        {
            MoteMaker.ThrowText(parent.TrueCenter() + new Vector3(0.5f, 0f, 0.5f), parent.Map, TargetTemperature.ToStringTemperature("F0"), Color.white);
        }
    }
}
