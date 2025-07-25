using RimWorld;
using System.Collections.Generic;
using UnityEngine;
using Verse;
using Verse.AI;
using WNA.WNADefOf;

namespace WNA.ThingCompProp
{
    public class CompIsDevourer : CompProperties
    {
        public string messageDevoured;
        public string messageReleased;
        public string messageDigested;
        public string inspector;
        public SimpleCurve bodySizeDigestTimeCurve = new SimpleCurve
        {
            new CurvePoint(0.2f, 35f),
            new CurvePoint(1f, 60f),
            new CurvePoint(4f, 95f),
            new CurvePoint(10f, 129f)
        };
        public CompIsDevourer()
        {
            compClass = typeof(IsDevourer);
        }
    }
    public class IsDevourer : ThingComp, IThingHolder
    {
        public CompIsDevourer Props => (CompIsDevourer)props;
        private ThingOwner<Thing> innerContainer;
        private int ticksDigesting;
        private int ticksFullyDigest;
        public Pawn victim
        {
            get
            {
                if (innerContainer.InnerListForReading.Count <= 0)
                {
                    return null;
                }
                var digestingThing = innerContainer.InnerListForReading[0];
                return digestingThing as Pawn;
            }
        }
        public bool Digesting => victim != null;
        public Pawn Pawn => parent as Pawn;
        public ThingOwner GetDirectlyHeldThings() => innerContainer;
        public IsDevourer()
        {
            innerContainer = new ThingOwner<Thing>(this);
        }
        public override void CompTick()
        {
            if (Digesting) ticksDigesting++;
            if (Digesting && victim.Dead) CompleteDigestion();
        }
        public override string CompInspectStringExtra()
        {
            if (Digesting)
            {
                int digestionTicks = GetDigestionTicks();
                int ticksLeftThisToil = Pawn.jobs.curDriver.ticksLeftThisToil;
                int num = ((ticksLeftThisToil < 0) ? digestionTicks : ticksLeftThisToil);
                float num2 = (float)(digestionTicks - (digestionTicks - num)) / 60f;
                return Props.inspector.Formatted(victim.Named("PAWN"), num2.Named("SECONDS"));
            }
            return null;
        }
        public void StartDigesting(Pawn victim)
        {
            if (victim == null || !victim.Spawned || victim.Dead) return;
            if (innerContainer.Contains(victim)) return;
            victim.DeSpawn();
            ticksDigesting = 0;
            innerContainer.TryAdd(victim);
            ticksFullyDigest = GetDigestionTicks();
            Pawn.jobs.StartJob(JobMaker.MakeJob(JobDefOf.DevourerDigest), JobCondition.InterruptForced);
            if (!Props.messageDevoured.NullOrEmpty() && victim.Faction == Faction.OfPlayer)
            {
                Messages.Message(Props.messageDevoured.Formatted(victim.Named("PAWN")), Pawn, MessageTypeDefOf.NegativeEvent);
            }
        }
        private void CompleteDigestion()
        {
            if (victim == null) return;
            Find.BattleLog.Add(new BattleLogEntry_Event(victim, RulePackDefOf.Event_DevourerDigestionCompleted, Pawn));
            DamageInfo damageInfo = new DamageInfo(WNAMainDefOf.WNA_CastMelee, float.PositiveInfinity);
            victim.TakeDamage(damageInfo);
            if (!Props.messageDigested.NullOrEmpty() && victim.Faction == Faction.OfPlayer)
            {
                Messages.Message(Props.messageDigested.Formatted(victim.Named("PAWN")), Pawn, MessageTypeDefOf.NegativeEvent);
            }
            innerContainer.Clear();
        }
        public override void Notify_Killed(Map prevMap, DamageInfo? _ = null)
        {
            AbortDigestion(prevMap);
        }
        private void AbortDigestion(Map map)
        {
            if (!Digesting) return;
            if (victim == null) return;
            victim.DeSpawn();
            innerContainer.TryDrop(victim, Pawn.PositionHeld, map, ThingPlaceMode.Near, out var _);
            GenSpawn.Spawn(victim, Pawn.PositionHeld, map);
            if (victim.Faction == Faction.OfPlayer && !Props.messageReleased.NullOrEmpty())
            {
                Messages.Message(Props.messageReleased.Formatted(victim.Named("PAWN")), Pawn, MessageTypeDefOf.NeutralEvent);
            }
            innerContainer.Clear();
        }
        private int GetDigestionTicks()
        {
            if (victim == null)
            {
                return 0;
            }
            return Mathf.CeilToInt(Props.bodySizeDigestTimeCurve.Evaluate(victim.BodySize));
        }
        public override void PostExposeData()
        {
            base.PostExposeData();
            Scribe_Values.Look(ref ticksDigesting, "ticksDigesting", 0);
            Scribe_Values.Look(ref ticksFullyDigest, "ticksFullyDigest", 0);
            Scribe_Deep.Look(ref innerContainer, "innerContainer", this);
        }
        public void EndDigestingJob()
        {
            if (!Pawn.Dead && Pawn.CurJobDef == JobDefOf.DevourerDigest && Pawn.jobs.curDriver != null && !Pawn.jobs.curDriver.ended)
            {
                Pawn.jobs.EndCurrentJob(JobCondition.InterruptForced);
            }
        }
        public void GetChildHolders(List<IThingHolder> outChildren)
        {
            ThingOwnerUtility.AppendThingHoldersFromThings(outChildren, GetDirectlyHeldThings());
        }
    }
}
