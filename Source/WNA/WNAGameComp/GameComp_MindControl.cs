using RimWorld;
using System.Collections.Generic;
using System.Linq;
using Verse;
using WNA.WNADefOf;

namespace WNA.WNAGameComp
{
    internal class MindControlRecord : IExposable
    {
        public Thing controller;
        public Def capacityDef;
        public int maxTargets = -1;
        public List<Pawn> victims = new List<Pawn>();

        public void ExposeData()
        {
            Scribe_References.Look(ref controller, "controller");
            Scribe_Defs.Look(ref capacityDef, "capacityDef");
            Scribe_Values.Look(ref maxTargets, "maxTargets", -1);
            Scribe_Collections.Look(ref victims, "victims", LookMode.Reference);
            if (Scribe.mode == LoadSaveMode.PostLoadInit)
            {
                if (victims == null)
                    victims = new List<Pawn>();
                victims.RemoveAll(v => v == null || v.DestroyedOrNull());
            }
        }
    }

    internal class GameComp_MindControl : GameComponent
    {
        private List<MindControlRecord> records = new List<MindControlRecord>();

        public GameComp_MindControl(Game game) { }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Collections.Look(ref records, "mindControlRecords", LookMode.Deep);
            if (Scribe.mode == LoadSaveMode.PostLoadInit)
            {
                if (records == null)
                    records = new List<MindControlRecord>();
                records.RemoveAll(r => r == null);
                foreach (MindControlRecord record in records)
                {
                    if (record.victims == null)
                        record.victims = new List<Pawn>();
                    record.victims.RemoveAll(v => v == null || v.Dead || v.DestroyedOrNull());
                }
                records.RemoveAll(r => IsControllerGone(r.controller) || r.victims.Count == 0);
            }
        }

        public override void GameComponentTick()
        {
            if (Find.TickManager.TicksGame % 60 != 0)
                return;
            foreach (MindControlRecord record in records.ToList())
            {
                if (record == null)
                    continue;
                record.victims.RemoveAll(v => v == null || v.Dead || v.DestroyedOrNull() || !MindControlled(v));
            }
            records.RemoveAll(r => r == null || IsControllerGone(r.controller) || r.victims == null || r.victims.Count == 0);
        }

        internal void Register(Thing controller, Pawn victim, Def capacityDef, int maxTargets)
        {
            if (controller == null || victim == null)
                return;
            MindControlRecord record = records.FirstOrDefault(r => r != null && r.controller == controller && r.capacityDef == capacityDef);
            if (record == null)
            {
                record = new MindControlRecord
                {
                    controller = controller,
                    capacityDef = capacityDef,
                    maxTargets = maxTargets
                };
                records.Add(record);
            }
            if (!record.victims.Contains(victim))
                record.victims.Add(victim);
        }

        internal void Unregister(Pawn victim)
        {
            if (victim == null)
                return;
            foreach (MindControlRecord record in records)
                record.victims.Remove(victim);
            records.RemoveAll(r => r.victims == null || r.victims.Count == 0);
        }

        internal int CountControlled(Thing controller, Def capacityDef)
        {
            if (controller == null)
                return 0;
            MindControlRecord record = records.FirstOrDefault(r => r != null && r.controller == controller && r.capacityDef == capacityDef);
            if (record == null)
                return 0;
            return record.victims.Count(v => v != null && !v.Dead && !v.DestroyedOrNull());
        }

        internal static int CountControlledBy(Thing controller, Def capacityDef)
        {
            return Current.Game?.GetComponent<GameComp_MindControl>()?.CountControlled(controller, capacityDef) ?? 0;
        }

        private static bool MindControlled(Pawn pawn)
        {
            return pawn?.health?.hediffSet?.HasHediff(WNAMainDefOf.WNA_MindControlEffect) == true;
        }

        private static bool IsControllerGone(Thing controller)
        {
            if (controller == null || controller.DestroyedOrNull())
                return true;
            return controller is Pawn pawn && pawn.Dead;
        }
    }
}
