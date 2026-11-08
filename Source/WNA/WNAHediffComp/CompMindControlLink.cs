using RimWorld;
using UnityEngine;
using Verse;
using WNA.WNAHediffClass;

namespace WNA.WNAHediffComp
{
    internal class PropHediffComp_MindControlLink : HediffCompProperties
    {
        public ThingDef linkMote = null;
        public ThingDef overheadMote = null;
        public ThingDef overheadMotePermanent = null;
        public float overheadOffset = 1.4f;

        public PropHediffComp_MindControlLink()
        {
            compClass = typeof(HediffComp_MindControlLink);
        }
    }

    internal class HediffComp_MindControlLink : HediffComp
    {
        private Mote linkMote;
        private Mote overheadMote;

        public PropHediffComp_MindControlLink Props => (PropHediffComp_MindControlLink)props;

        private HdfClass_MindControl MindControl => parent as HdfClass_MindControl;

        public override bool CompShouldRemove
        {
            get
            {
                HdfClass_MindControl mc = MindControl;
                if (mc == null || mc.IsPermanent)
                    return false;
                if (mc.source == null)
                    return mc.releaseOnSourceLost && mc.sourceFaction != null;
                if (mc.source.DestroyedOrNull())
                    return mc.releaseOnSourceLost;
                if (mc.source is Building && !mc.source.Spawned)
                    return mc.releaseOnSourceLost;
                if (mc.source is Pawn sourcePawn && sourcePawn.Dead)
                    return mc.releaseOnSourceLost;
                if (mc.releaseOnFactionChange && mc.sourceFaction != null && mc.source.Faction != mc.sourceFaction)
                    return true;
                return false;
            }
        }

        public override void CompPostTick(ref float severityAdjustment)
        {
            base.CompPostTick(ref severityAdjustment);
            if (Pawn == null || !Pawn.Spawned || Pawn.Map == null)
            {
                CleanupMotes();
                return;
            }
            if (!Pawn.IsHashIntervalTick(30))
                return;

            HdfClass_MindControl mc = MindControl;
            if (mc == null)
                return;

            MaintainOverheadMote(mc);
            MaintainLinkMote(mc);
        }

        public override void CompPostTickInterval(ref float severityAdjustment, int delta)
        {
            base.CompPostTickInterval(ref severityAdjustment, delta);
            HdfClass_MindControl mc = MindControl;
            if (mc == null || mc.IsPermanent || mc.source == null || Pawn == null)
                return;

            if (mc.requireSourcePower && mc.source is Building building)
            {
                CompPowerTrader power = building.GetComp<CompPowerTrader>();
                if (power != null && !power.PowerOn)
                {
                    Pawn.health.RemoveHediff(parent);
                    return;
                }
            }

            if (mc.releaseRange > 0f && mc.source.Spawned && Pawn.Spawned
                && mc.source.MapHeld == Pawn.MapHeld
                && !Pawn.PositionHeld.InHorDistOf(mc.source.PositionHeld, mc.releaseRange))
            {
                Pawn.health.RemoveHediff(parent);
            }
        }

        public override void CompPostPostRemoved()
        {
            base.CompPostPostRemoved();
            CleanupMotes();
        }

        private void MaintainOverheadMote(HdfClass_MindControl mc)
        {
            if (overheadMote == null || overheadMote.Destroyed)
            {
                ThingDef moteDef = mc.IsPermanent && Props.overheadMotePermanent != null
                    ? Props.overheadMotePermanent
                    : Props.overheadMote;
                if (moteDef == null)
                    return;
                overheadMote = MoteMaker.MakeAttachedOverlay(
                    Pawn,
                    moteDef,
                    new Vector3(0f, 0f, Props.overheadOffset));
            }
            overheadMote?.Maintain();
        }

        private void MaintainLinkMote(HdfClass_MindControl mc)
        {
            Thing source = mc.source;
            bool valid = source != null
                && !source.DestroyedOrNull()
                && !(source is Pawn sourcePawn && sourcePawn.Dead)
                && source.Spawned
                && source.MapHeld == Pawn.MapHeld
                && Props.linkMote != null;
            if (!valid)
            {
                DestroyLinkMote();
                return;
            }
            if (linkMote == null || linkMote.Destroyed)
                linkMote = MoteMaker.MakeInteractionOverlay(Props.linkMote, Pawn, source);
            linkMote?.Maintain();
        }

        private void CleanupMotes()
        {
            DestroyLinkMote();
            if (overheadMote != null && !overheadMote.Destroyed)
                overheadMote.Destroy();
            overheadMote = null;
        }

        private void DestroyLinkMote()
        {
            if (linkMote != null && !linkMote.Destroyed)
                linkMote.Destroy();
            linkMote = null;
        }
    }
}
