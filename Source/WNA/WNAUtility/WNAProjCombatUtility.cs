using RimWorld;
using System.Collections.Generic;
using UnityEngine;
using Verse;
using WNA.WNAMiscs;

namespace WNA.WNAUtility
{
    internal static class WNAProjCombatUtility
    {
        private static readonly HashSet<Thing> areaAffected = new HashSet<Thing>();
        private static readonly List<Thing> areaCellBuffer = new List<Thing>(16);
        private static readonly HashSet<Thing> clusterAffected = new HashSet<Thing>();
        private static readonly List<Thing> clusterCellBuffer = new List<Thing>(16);
        private static readonly List<IntVec3> lineCells = new List<IntVec3>(64);
        private static readonly HashSet<IntVec3> lineCellSet = new HashSet<IntVec3>();
        private static readonly List<Thing> lineCellBuffer = new List<Thing>(16);
        public static bool IsValidTarget(Thing thing, Map map, Thing instigator, bool affectNonPawns)
        {
            if (thing == null || thing == instigator)
            {
                return false;
            }
            if (thing.Destroyed || !thing.Spawned || thing.Map != map)
            {
                return false;
            }
            ThingDef def = thing.def;
            if (!def.useHitPoints || def.category == ThingCategory.Mote || def.category == ThingCategory.Ethereal)
            {
                return false;
            }
            if (thing is Projectile)
            {
                return false;
            }
            if (!affectNonPawns && !(thing is Pawn))
            {
                return false;
            }
            return true;
        }
        public static void ApplyToThing(Thing target, in WNAProjDamageContext ctx, float amountFactor, ThingDef projectileDef, ThingDef coverDef)
        {
            if (target == null || ctx.damageDef == null || amountFactor <= 0f)
            {
                return;
            }
            BattleLogEntry_RangedImpact log = ctx.MakeBattleLog(target, projectileDef, coverDef);
            Find.BattleLog.Add(log);
            target.TakeDamage(ctx.ToDamageInfo(amountFactor)).AssociateWithLog(log);
        }
        public static void ApplyInRadius(Map map, IntVec3 center, float radius, in WNAProjDamageContext ctx, ThingDef projectileDef, ThingDef coverDef, bool requireLineOfSight, float falloffMin, Thing exclude, bool affectNonPawns = true)
        {
            if (map == null || radius <= 0f || ctx.damageDef == null)
            {
                return;
            }
            radius = Mathf.Min(radius, GenRadial.MaxRadialPatternRadius);
            falloffMin = Mathf.Clamp01(falloffMin);
            areaAffected.Clear();
            if (exclude != null)
            {
                areaAffected.Add(exclude);
            }
            int numCells = GenRadial.NumCellsInRadius(radius);
            for (int i = 0; i < numCells; i++)
            {
                IntVec3 c = center + GenRadial.RadialPattern[i];
                if (!c.InBounds(map))
                {
                    continue;
                }
                if (requireLineOfSight && i > 0 && !GenSight.LineOfSight(center, c, map, skipFirstCell: true))
                {
                    continue;
                }
                float t = radius > 0f ? Mathf.Clamp01(GenRadial.RadialPatternRadii[i] / radius) : 0f;
                float factor = Mathf.Lerp(1f, falloffMin, t);
                DamageThingsAtCell(map, c, ctx, factor, projectileDef, coverDef, affectNonPawns, areaAffected, areaCellBuffer, dedupeAll: true);
            }
            areaAffected.Clear();
        }
        public static void ApplyClusters(Map map, IntVec3 center, float radius, int amount, in WNAProjDamageContext ctx, ThingDef projectileDef, ThingDef coverDef, bool requireLineOfSight, bool affectNonPawns = true, FleckDef fleck = null)
        {
            if (map == null || amount <= 0 || ctx.damageDef == null)
            {
                return;
            }
            radius = Mathf.Clamp(radius, 0f, GenRadial.MaxRadialPatternRadius);
            clusterAffected.Clear();
            int numCells = radius > 0f ? GenRadial.NumCellsInRadius(radius) : 1;
            for (int i = 0; i < amount; i++)
            {
                IntVec3 c = numCells <= 1 ? center : center + GenRadial.RadialPattern[Rand.Range(0, numCells)];
                if (!c.InBounds(map))
                {
                    continue;
                }
                if (requireLineOfSight && c != center && !GenSight.LineOfSight(center, c, map, skipFirstCell: true))
                {
                    continue;
                }
                if (fleck != null)
                {
                    FleckMaker.Static(c.ToVector3Shifted(), map, fleck, 1f);
                }
                DamageThingsAtCell(map, c, ctx, 1f, projectileDef, coverDef, affectNonPawns, clusterAffected, clusterCellBuffer, dedupeAll: false);
            }
            clusterAffected.Clear();
        }
        public static void CollectLineTargets(Map map, Vector3 from, Vector3 to, Thing instigator, Thing finalTarget, bool affectNonPawns, float extraRadius, int maxTargets, List<Thing> result)
        {
            result.Clear();
            if (map == null)
            {
                return;
            }
            WNABeamLineUtility.CellsAlongSegment(from, to, lineCells);
            if (extraRadius > 0f)
            {
                ExpandCells(map, extraRadius);
            }
            for (int i = 0; i < lineCells.Count; i++)
            {
                IntVec3 c = lineCells[i];
                if (!c.InBounds(map))
                {
                    continue;
                }
                List<Thing> things = c.GetThingList(map);
                lineCellBuffer.Clear();
                for (int j = 0; j < things.Count; j++)
                {
                    lineCellBuffer.Add(things[j]);
                }
                for (int j = 0; j < lineCellBuffer.Count; j++)
                {
                    Thing thing = lineCellBuffer[j];
                    if (thing == finalTarget || result.Contains(thing))
                    {
                        continue;
                    }
                    if (!IsValidTarget(thing, map, instigator, affectNonPawns))
                    {
                        continue;
                    }
                    result.Add(thing);
                    if (maxTargets > 0 && result.Count >= maxTargets)
                    {
                        lineCellBuffer.Clear();
                        lineCells.Clear();
                        lineCellSet.Clear();
                        return;
                    }
                }
                lineCellBuffer.Clear();
            }
            lineCells.Clear();
            lineCellSet.Clear();
        }
        private static void ExpandCells(Map map, float extraRadius)
        {
            lineCellSet.Clear();
            for (int i = 0; i < lineCells.Count; i++)
            {
                lineCellSet.Add(lineCells[i]);
            }
            int original = lineCells.Count;
            int ring = GenRadial.NumCellsInRadius(extraRadius);
            for (int i = 0; i < original; i++)
            {
                IntVec3 c = lineCells[i];
                for (int j = 1; j < ring; j++)
                {
                    IntVec3 n = c + GenRadial.RadialPattern[j];
                    if (n.InBounds(map) && lineCellSet.Add(n))
                    {
                        lineCells.Add(n);
                    }
                }
            }
        }
        private static void DamageThingsAtCell(Map map, IntVec3 cell, in WNAProjDamageContext ctx, float amountFactor, ThingDef projectileDef, ThingDef coverDef, bool affectNonPawns, HashSet<Thing> affected, List<Thing> buffer, bool dedupeAll)
        {
            List<Thing> things = cell.GetThingList(map);
            buffer.Clear();
            for (int i = 0; i < things.Count; i++)
            {
                buffer.Add(things[i]);
            }
            for (int i = 0; i < buffer.Count; i++)
            {
                Thing thing = buffer[i];
                if (!IsValidTarget(thing, map, ctx.instigator, affectNonPawns))
                {
                    continue;
                }
                bool multiCell = thing.def.size.x > 1 || thing.def.size.z > 1;
                if ((dedupeAll || multiCell) && !affected.Add(thing))
                {
                    continue;
                }
                ApplyToThing(thing, ctx, amountFactor, projectileDef, coverDef);
            }
            buffer.Clear();
        }
    }
}
