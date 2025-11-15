using RimWorld;
using UnityEngine;
using Verse;

namespace WNA.ThingCompProp
{
    public class CompCamoPillbox : CompProperties
    {
        public bool visibleToPlayer = false;
        public int fadeDurationTicks = 27;
        public int recoverFromDisruptedTicks = 250;
        public bool affectedByDisruptor = true;
        public CompCamoPillbox()
        {
            compClass = typeof(CamoPillbox);
        }
    }
    public class CamoPillbox : ThingComp
    {
        public CompCamoPillbox Props => (CompCamoPillbox)props;
        private int lastDisrupted = -99999;
        private bool wasForcedVisibleLastTick;
        private bool everVisible;
        private static readonly SimpleCurve RevealCurve = new SimpleCurve
        {
            new CurvePoint(0f, 0f),
            new CurvePoint(0.1f, 0.5f),
            new CurvePoint(1f, 1f)
        };
        private int LastBecameVisibleTick;
        private int LastBecameInvisibleTick;
        private bool ShouldBeVisible => LastBecameVisibleTick >= LastBecameInvisibleTick;
        private float FadeIn => Mathf.Clamp01((float)(Find.TickManager.TicksGame - LastBecameVisibleTick) / Props.fadeDurationTicks);
        private float FadeOut => 1f - Mathf.Clamp01((float)(Find.TickManager.TicksGame - LastBecameInvisibleTick) / Props.fadeDurationTicks);
        private float FadePct => ShouldBeVisible ? FadeIn : FadeOut;
        public bool ForcedVisible
        {
            get
            {
                if (parent.IsBurning())
                    return true;
                if (Find.TickManager.TicksGame < lastDisrupted + Props.recoverFromDisruptedTicks)
                    return true;
                return false;
            }
        }
        public override void PostExposeData()
        {
            Scribe_Values.Look(ref wasForcedVisibleLastTick, "wasForcedVisibleLastTick", false);
            Scribe_Values.Look(ref lastDisrupted, "lastDisrupted", -99999);
            Scribe_Values.Look(ref everVisible, "everVisible", false);
            Scribe_Values.Look(ref LastBecameVisibleTick, "LastBecameVisibleTick", -99999);
            Scribe_Values.Look(ref LastBecameInvisibleTick, "LastBecameInvisibleTick", -99999);
        }
        public override void PostSpawnSetup(bool respawningAfterLoad)
        {
            base.PostSpawnSetup(respawningAfterLoad);
            BecomeInvisible(instant: true);
        }
        public override void CompTick()
        {
            base.CompTick();
            if (!ShouldBeVisible)
            {
                if (!wasForcedVisibleLastTick && ForcedVisible && FadePct == 0f)
                    FleckMaker.Static(parent.TrueCenter(), parent.Map, FleckDefOf.PsycastAreaEffect, 1.5f);
                if (wasForcedVisibleLastTick && !ForcedVisible)
                    LastBecameInvisibleTick = Find.TickManager.TicksGame;
            }
            wasForcedVisibleLastTick = ForcedVisible;
            if (!everVisible && GetAlpha() > 0f)
                everVisible = true;
        }
        public void BecomeVisible(bool instant = false)
        {
            if (!ShouldBeVisible)
            {
                if (instant)
                    LastBecameVisibleTick = Find.TickManager.TicksGame - Props.fadeDurationTicks;
                else
                    LastBecameVisibleTick = Find.TickManager.TicksGame;
                if (!ForcedVisible)
                    RefreshVisuals();
            }
        }
        public void BecomeInvisible(bool instant = false)
        {
            if (ShouldBeVisible)
            {
                if (instant)
                    LastBecameInvisibleTick = Find.TickManager.TicksGame - Props.fadeDurationTicks;
                else
                    LastBecameInvisibleTick = Find.TickManager.TicksGame;
                if (!ForcedVisible)
                    RefreshVisuals();
            }
        }
        public void DisruptInvisibility()
        {
            lastDisrupted = Find.TickManager.TicksGame;
        }
        public float GetAlpha()
        {
            if (Props.visibleToPlayer)
                return 1f;
            if (ForcedVisible)
                return 1f;
            return RevealCurve.Evaluate(FadePct);
        }

        private void RefreshVisuals()
        {
            parent.Map.mapDrawer.MapMeshDirty(parent.Position, MapMeshFlagDefOf.Things);
        }
        public override void PostDraw()
        {
            base.PostDraw();
            if (DebugSettings.godMode)
                GenDraw.DrawFieldEdges(new System.Collections.Generic.List<IntVec3> { parent.Position }, new Color(0f, 1f, 1f, 0.5f));
        }
        public override void PostPostApplyDamage(DamageInfo dinfo, float totalDamageDealt)
        {
            DisruptInvisibility();
        }
    }
}
