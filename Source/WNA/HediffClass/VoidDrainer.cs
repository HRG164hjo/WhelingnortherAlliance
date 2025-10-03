using RimWorld;
using System;
using UnityEngine;
using Verse;
using WNA.WNADefOf;

namespace WNA.HediffClass
{
    public class VoidDrainer : HediffWithComps
    {
        private int spawnInterval = 40000;
        private int spawnTimes = 0;
        private const float maxSeverity = 10f;
        private float severityLevel;
        public override void PostMake()
        {
            base.PostMake();
            if (this.Severity < 0.01f) this.Severity = 0.01f;
            ForcePawnDowned();
        }
        public override void PostTickInterval(int delta)
        {
            base.PostTickInterval(delta);
            if (pawn.Destroyed || pawn.Dead) return;
            spawnInterval -= delta;
            if (spawnInterval <= 0)
            {
                SpawnResource();
                spawnTimes++;
                severityLevel = Rand.Range(0.3f, 0.5f);
                this.Severity = Math.Min(this.Severity + severityLevel, maxSeverity);
                pawn.health.capacities.Notify_CapacityLevelsDirty();
                spawnInterval = Rand.RangeInclusive(30000, 50000);
            }
            ForcePawnDowned();
            DeathCheck();
        }
        private void ForcePawnDowned()
        {
            if (!pawn.Downed && !pawn.Dead)
            {
                pawn.health.forceDowned = true;
                if (pawn.CurJobDef != JobDefOf.Wait_AsleepDormancy) pawn.jobs.StartJob(JobMaker.MakeJob(JobDefOf.Wait_AsleepDormancy, pawn.Position));
            }
        }
        private void DeathCheck()
        {
            if (pawn.Dead || pawn.Destroyed || pawn.Map == null) return;
            float currConsc = pawn.health.capacities.GetLevel(PawnCapacityDefOf.Consciousness);
            if ((currConsc <= 0.01f) || (this.Severity >= maxSeverity - float.Epsilon))
            {
                pawn.health.RemoveHediff(this);
                pawn.apparel?.DropAll(pawn.Position);
                pawn.inventory?.DropAllNearPawn(pawn.Position);
                pawn.equipment?.DropAllEquipment(pawn.Position);
                pawn.health.SetDead();
                pawn.Destroy(DestroyMode.Vanish);
                Messages.Message($"{pawn.LabelShort} vanished into the void.", pawn, MessageTypeDefOf.NeutralEvent);
            }
        }
        private void SpawnResource()
        {
            if (pawn.Dead || pawn.Map == null) return;
            float currConsc = pawn.health.capacities.GetLevel(PawnCapacityDefOf.Consciousness);
            float scaled = currConsc * 10f;
            int basev = 5;
            int total = basev * Math.Max(1, Mathf.CeilToInt(scaled));
            if (total > 0)
            {
                Thing raw = ThingMaker.MakeThing(WNAMainDefOf.WNA_Voidsteel);
                raw.stackCount = total;
                GenSpawn.Spawn(raw, pawn.Position, pawn.Map);
                Messages.Message($"{total} units of voidsteel is drained through {pawn.LabelShort}.", pawn, MessageTypeDefOf.PositiveEvent);
            }
        }
        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref spawnInterval, "voidDrainer_spawnInterval", 40000);
            Scribe_Values.Look(ref spawnTimes, "voidDrainer_spawnTimes", 0);
        }
    }
}
