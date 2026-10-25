using RimWorld;
using System.Collections.Generic;
using System.Linq;
using Verse;

namespace WNA.WNAThingComp
{
    internal class PropAOEDamage : CompProperties
    {
        public DamageDef damageDef = null;
        public int damage = 60;
        public float ap = 200;
        public float radius = 2;
        public int rof = 60;
        public int update = 60;
        public bool affectArea = false;
        public bool affectRoof = false;
        public bool affectPrisoner = false;

        public PropAOEDamage()
        {
            this.compClass = typeof(CompAOEDamage);
        }
    }
    internal class CompAOEDamage : ThingComp
    {
        public PropAOEDamage Props => (PropAOEDamage)props;
        protected virtual bool Active => parent.Spawned;
        protected virtual IntVec3 EffectCenter => parent.Position;
        protected virtual Map EffectMap => parent.Map;
        protected virtual Faction EffectFaction => parent.Faction;
        private int nextTickEffect;
        private int nextScanTick;
        private int NextTickEffect => Find.TickManager.TicksGame + Props.rof;
        private List<Thing> cachedTargets = new List<Thing>();
        private Faction lastRecordedFaction;
        private static readonly HashSet<string> roofToRemove = new HashSet<string>
        {
            "RoofConstructed",
            "RoofRockThin"
        };
        public override void CompTick()
        {
            base.CompTick();
            if (Active)
            {
                if (ShouldResetCache(lastRecordedFaction))
                {
                    cachedTargets.Clear();
                    nextScanTick = 0;
                    lastRecordedFaction = EffectFaction;
                }
                if (nextTickEffect == 0) nextTickEffect = NextTickEffect;
                if (nextScanTick == 0) nextScanTick = Find.TickManager.TicksGame + Props.update;
                if (Find.TickManager.TicksGame >= nextScanTick)
                {
                    cachedTargets = GetThings();
                    nextScanTick = Find.TickManager.TicksGame + Props.update;
                }
                if (Find.TickManager.TicksGame >= nextTickEffect)
                {
                    if (Props.affectRoof) RemoveRoofs();
                    if (Props.affectArea)
                        foreach (var target in cachedTargets) DoEffect(target);
                    else if (cachedTargets.Count > 0 && cachedTargets.TryRandomElement(out Thing target)) DoEffect(target);
                    nextTickEffect = NextTickEffect;
                }
            }
            else nextTickEffect++;
        }
        protected virtual bool ShouldResetCache(Faction recordedFaction)
        {
            return EffectFaction != recordedFaction;
        }
        protected List<Thing> GetThings()
        {
            return GenRadial.RadialDistinctThingsAround(EffectCenter, EffectMap, Props.radius, useCenter: true).Where(t => IfThingValid(t)).ToList();
        }
        protected bool IfThingValid(Thing thing)
        {
            if (!(thing.def.useHitPoints || thing is Pawn)) return false;
            Faction sourceFaction = EffectFaction;
            if (sourceFaction == null || thing.Faction == null) return false;
            bool isHostile = thing.Faction.HostileTo(sourceFaction);
            if (thing is Pawn pawn && !Props.affectPrisoner)
                if (pawn.IsPrisonerOfColony) return false;
            return isHostile;
        }
        protected void DoEffect(Thing thing)
        {
            if (thing.Destroyed) return;
            if (thing is Pawn pawn && pawn.Dead) return;
            DamageInfo dinfo = new DamageInfo(Props.damageDef, Props.damage, Props.ap,
                -1, parent, null, parent.def, DamageInfo.SourceCategory.ThingOrUnknown);
            thing.TakeDamage(dinfo);
        }
        private void RemoveRoofs()
        {
            Map map = EffectMap;
            if (!Props.affectRoof || map == null) return;

            foreach (IntVec3 cell in GenRadial.RadialCellsAround(EffectCenter, Props.radius, true))
            {
                if (!cell.InBounds(map)) continue;

                RoofDef roof = map.roofGrid.RoofAt(cell);
                if (roof != null && roofToRemove.Contains(roof.defName))
                {
                    map.roofGrid.SetRoof(cell, null);
                    RoofCollapserImmediate.DropRoofInCells(cell, map);
                }
            }
        }
        public override void PostExposeData()
        {
            base.PostExposeData();
            Scribe_Values.Look(ref nextTickEffect, "nextTickEffect", 0);
            Scribe_Values.Look(ref nextScanTick, "nextScanTick", 0);
            Scribe_Collections.Look(ref cachedTargets, "cachedTargets", LookMode.Reference);
            if (Scribe.mode == LoadSaveMode.PostLoadInit)
            {
                cachedTargets?.Clear();
                nextScanTick = 0;
            }
        }
    }
}
