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
        public static bool IsValidTarget(Thing thing, Map map, Thing instigator, bool affectNonPawns, bool affectsAllies = false)
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
            if (def.category == ThingCategory.Mote || def.category == ThingCategory.Ethereal)
            {
                return false;
            }
            // Pawns keep their health in Pawn_HealthTracker and inherit useHitPoints=false from
            // BasePawn, so useHitPoints alone must not be used to reject them.
            if (!def.useHitPoints && !(thing is Pawn))
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
            if (affectsAllies)
            {
                Faction ownFaction = instigator?.Faction;
                Faction targetFaction = thing.Faction;
                if (ownFaction != null && targetFaction != null && !targetFaction.HostileTo(ownFaction))
                {
                    return false;
                }
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
        public static void ApplyInRadius(Map map, IntVec3 center, float radius, in WNAProjDamageContext ctx, ThingDef projectileDef, ThingDef coverDef, bool requireLineOfSight, float falloffMin, Thing exclude, bool affectNonPawns = true, bool affectsAllies = false)
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
                DamageThingsAtCell(map, c, ctx, factor, projectileDef, coverDef, affectNonPawns, areaAffected, areaCellBuffer, dedupeAll: true, affectsAllies: affectsAllies);
            }
            areaAffected.Clear();
        }
        public static void ApplyClusters(Map map, IntVec3 center, float radius, int amount, in WNAProjDamageContext ctx, ThingDef projectileDef, ThingDef coverDef, bool requireLineOfSight, bool affectNonPawns = true, Thing exclude = null, FleckDef fleck = null, bool affectsAllies = false)
        {
            if (map == null || amount <= 0 || ctx.damageDef == null)
            {
                return;
            }
            radius = Mathf.Clamp(radius, 0f, GenRadial.MaxRadialPatternRadius);
            clusterAffected.Clear();
            if (exclude != null)
            {
                clusterAffected.Add(exclude);
            }
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
                DamageThingsAtCell(map, c, ctx, 1f, projectileDef, coverDef, affectNonPawns, clusterAffected, clusterCellBuffer, dedupeAll: false, affectsAllies: affectsAllies);
            }
            clusterAffected.Clear();
        }
        public static void CollectLineTargets(Map map, Vector3 from, Vector3 to, Thing instigator, Thing finalTarget, bool affectNonPawns, float width, int maxTargets, List<Thing> result, bool affectsAllies = false)
        {
            result.Clear();
            if (map == null)
            {
                return;
            }
            WNABeamLineUtility.CellsAlongSegment(from, to, lineCells);
            // The beam starts at the launcher's draw position, which sits inside its own footprint
            // (a 2x2 turret's center is a real cell). Drop those cells so the shot can't gouge
            // structures the turret is built against.
            if (instigator != null)
            {
                CellRect occupied = instigator.OccupiedRect();
                for (int i = lineCells.Count - 1; i >= 0; i--)
                {
                    if (occupied.Contains(lineCells[i]))
                    {
                        lineCells.RemoveAt(i);
                    }
                }
            }
            // width is the beam's total width in cells (1 = a single-cell line); ExpandCells wants
            // a sideways radius, so halve it. Treating width as a radius made width 1 a 3-cell swath.
            if (width > 1f)
            {
                ExpandCells(map, width * 0.5f);
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
                    if (!IsValidTarget(thing, map, instigator, affectNonPawns, affectsAllies))
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
        private static void DamageThingsAtCell(Map map, IntVec3 cell, in WNAProjDamageContext ctx, float amountFactor, ThingDef projectileDef, ThingDef coverDef, bool affectNonPawns, HashSet<Thing> affected, List<Thing> buffer, bool dedupeAll, bool affectsAllies = false)
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
                if (!IsValidTarget(thing, map, ctx.instigator, affectNonPawns, affectsAllies))
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
