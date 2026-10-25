using RimWorld;
using RimWorld.Planet;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using Verse;
using WNA.WNADefOf;
using static WNA.WNAUtility.General;

namespace WNA.WNAWorldComp
{
    public class WorldComp_WNAGenerate : WorldComponent
    {
        private const int initialDelay = 37;
        private const int refillDelay = 173205;
        private const float refillMultiplier = 1.414214f;
        private static readonly MethodInfo RemoveFactionMethod =
            typeof(FactionManager).GetMethod("Remove", BindingFlags.Instance | BindingFlags.NonPublic);
        private static readonly List<PlanetTile> tmpNeighbors = new List<PlanetTile>();
        private bool initialized = false;
        private int primalCount = -1;
        private int nextFilltick = -1;
        private Faction wna = WNAFaction;
        public WorldComp_WNAGenerate(World world) : base(world) { }
        public override void ExposeData()
        {
            Scribe_Values.Look(ref initialized, "initialized", false);
            Scribe_Values.Look(ref primalCount, "primalCount", 0);
            Scribe_Values.Look(ref nextFilltick, "nextFilltick", -1);
            Scribe_References.Look(ref wna, "wna");
        }
        private Faction GetOrCreateFaction(FactionDef def)
        {
            var existing = Find.FactionManager.AllFactionsListForReading
                .FirstOrDefault(f => f.def == def);
            if (existing != null)
            {
                wna = existing;
                existing.defeated = false;
                existing.hidden = false;
                return existing;
            }
            var ideoParms = new IdeoGenerationParms(
                def,
                name: def.ideoName,
                styles: def.styles,
                deities: def.deityPresets,
                hidden: def.hiddenIdeo,
                description: def.ideoDescription,
                forcedMemes: def.forcedMemes,
                fixedIdeo: def.fixedIdeo,
                requiredPreceptsOnly: def.requiredPreceptsOnly);
            var parms = new FactionGeneratorParms(def, ideoParms, hidden: false);
            Faction fac = FactionGenerator.NewGeneratedFaction(Find.WorldGrid.Surface, parms);
            Find.FactionManager.Add(fac);
            wna = fac;
            return fac;
        }
        public override void FinalizeInit(bool fromLoad)
        {
            base.FinalizeInit(fromLoad);
            if (!initialized)
                nextFilltick = Find.TickManager.TicksGame + initialDelay;
        }
        public override void WorldComponentTick()
        {
            int now = Find.TickManager.TicksGame;
            if (!initialized && now >= nextFilltick)
            {
                initialized = true;
                nextFilltick = now + refillDelay;
                InitialSpawn();
                return;
            }
            if (initialized && now >= nextFilltick)
            {
                RefillSettlementsIfNeeded();
                nextFilltick = now + refillDelay;
            }
        }
        private void InitialSpawn()
        {
            FactionDef def = WNAMainDefOf.WNA_FactionWNA;
            Faction owner = EnsureFaction(def);
            if (owner == null) return;
            var allowedLayers = GetAllowedLayers(def);
            if (allowedLayers.Count == 0) return;
            primalCount = GetRecommendedFactionCount();
            int target = GetTargetSettlementCount(def, allowedLayers.Count);
            int need = GetNeededSettlements(def, allowedLayers, target);
            int spawnedTotal = SpawnSettlements(owner, def, allowedLayers, need);
            Find.LetterStack.ReceiveLetter(
                "WNA_InitialSpawn_Label".Translate(),
                "WNA_InitialSpawn_Desc".Translate(target, spawnedTotal),
                Faction.OfPlayer.HostileTo(owner) ? LetterDefOf.ThreatBig : LetterDefOf.NeutralEvent);
        }
        private void RefillSettlementsIfNeeded()
        {
            FactionDef def = WNAMainDefOf.WNA_FactionWNA;
            Faction owner = EnsureFaction(def);
            if (owner == null) return;
            var allowedLayers = GetAllowedLayers(def);
            if (allowedLayers.Count == 0) return;
            int target = GetTargetSettlementCount(def, allowedLayers.Count);
            int need = GetNeededSettlements(def, allowedLayers, target);
            int spawnedTotal = SpawnSettlements(owner, def, allowedLayers, Mathf.CeilToInt(need * refillMultiplier));
            if (spawnedTotal > 0)
            {
                Find.LetterStack.ReceiveLetter(
                    "WNA_Refill_Label".Translate(),
                    "WNA_Refill_Desc".Translate(spawnedTotal, target),
                    Faction.OfPlayer.HostileTo(owner) ? LetterDefOf.ThreatBig : LetterDefOf.NeutralEvent);
            }
        }
        private int SpawnSettlements(Faction owner, FactionDef def, List<PlanetLayer> layers, int amount)
        {
            if (owner == null || amount <= 0 || layers.NullOrEmpty())
                return 0;
            var unavailableLayers = new HashSet<PlanetLayer>();
            int spawned = 0;
            while (spawned < amount)
            {
                PlanetLayer layer = layers
                    .Where(l => !unavailableLayers.Contains(l))
                    .OrderBy(l => CountSettlementsOnLayer(def, l))
                    .ThenBy(l => Rand.Value)
                    .FirstOrDefault();
                if (layer == null)
                    break;
                if (TrySpawnSettlement(owner, layer))
                    spawned++;
                else
                    unavailableLayers.Add(layer);
            }
            return spawned;
        }
        private bool TrySpawnSettlement(Faction owner, PlanetLayer layer)
        {
            if (!TryFindSettlementTile(layer, out PlanetTile tile))
                return false;
            WorldObjectDef objDef = layer.Def.SettlementWorldObjectDef
                ?? WorldObjectDefOf.Settlement;
            WorldObject wo = WorldObjectMaker.MakeWorldObject(objDef);
            if (wo == null)
                return false;
            wo.SetFaction(owner);
            wo.Tile = tile;
            if (wo is Settlement settlement)
                settlement.Name = SettlementNameGenerator.GenerateSettlementName(settlement);
            Find.WorldObjects.Add(wo);
            return true;
        }
        private bool TryFindSettlementTile(PlanetLayer layer, out PlanetTile tile)
        {
            int tilesCount = layer.TilesCount;
            if (tilesCount <= 0)
            {
                tile = PlanetTile.Invalid;
                return false;
            }
            for (int attempt = 0; attempt < 256; attempt++)
            {
                PlanetTile candidate = new PlanetTile(Rand.Range(0, tilesCount), layer);
                if (IsSettlementTileSuitable(candidate))
                {
                    tile = candidate;
                    return true;
                }
            }
            int start = Rand.Range(0, tilesCount);
            for (int i = 0; i < tilesCount; i++)
            {
                PlanetTile candidate = new PlanetTile((start + i) % tilesCount, layer);
                if (IsSettlementTileSuitable(candidate))
                {
                    tile = candidate;
                    if (Find.WorldGrid[tile].hilliness == Hilliness.Impassable)
                        Find.WorldGrid[tile].hilliness = Hilliness.Mountainous;
                    return true;
                }
            }
            tile = PlanetTile.Invalid;
            return false;
        }
        private bool IsSettlementTileSuitable(PlanetTile tile)
        {
            if (!tile.Valid || tile.Tile == null)
                return false;
            if (!Find.WorldGrid[tile].PrimaryBiome.canBuildBase)
                return false;
            if (!Find.WorldGrid[tile].PrimaryBiome.implemented)
                return false;
            if (Find.WorldObjects.AnySettlementBaseAt(tile) || Find.WorldObjects.AnyMapParentAt(tile))
                return false;
            tile.Layer.GetTileNeighbors(tile, tmpNeighbors);
            if (tmpNeighbors.Count == 0)
                return false;
            for (int i = 0; i < tmpNeighbors.Count; i++)
            {
                PlanetTile neighbor = tmpNeighbors[i];
                if (!neighbor.Valid || neighbor.Tile == null)
                    return false;
            }
            return true;
        }
        private Faction EnsureFaction(FactionDef def)
        {
            List<Faction> all = Find.FactionManager.AllFactionsListForReading
                .Where(f => f.def == def)
                .ToList();
            if (all.Count == 0)
                return GetOrCreateFaction(def);
            Faction primary = ChoosePrimaryFaction(all);
            primary.defeated = false;
            primary.hidden = false;
            foreach (Faction duplicate in all)
            {
                if (duplicate != primary)
                    MergeFaction(duplicate, primary);
            }
            wna = primary;
            return primary;
        }
        private Faction ChoosePrimaryFaction(List<Faction> factions)
        {
            List<Faction> candidates = factions
                .Where(f => !Find.QuestManager.IsReservedByAnyQuest(f))
                .ToList();
            if (candidates.Count == 0)
                return factions[0];
            Faction current = candidates.FirstOrDefault(f => f == wna && !f.defeated);
            if (current != null)
                return current;
            return candidates
                .OrderByDescending(f => !f.defeated)
                .ThenByDescending(CountSettlements)
                .First();
        }
        private void MergeFaction(Faction source, Faction target)
        {
            if (source.temporary && source.defeated && source.hidden == true)
                return;
            if (Find.QuestManager.IsReservedByAnyQuest(source))
            {
                Log.Warning("[WNA] Skipped merging faction " + source.Name + " into " + target.Name + " because a quest reserves it.");
                return;
            }
            try
            {
                source.temporary = true;
                foreach (WorldObject worldObject in Find.WorldObjects.AllWorldObjects
                    .Where(wo => wo.Faction == source).ToList())
                {
                    worldObject.SetFaction(target);
                }
                foreach (Pawn pawn in PawnsFinder.All_AliveOrDead.ToList())
                {
                    if (pawn.Faction == source)
                        pawn.SetFaction(target);
                }
                RemoveFactionMethod.Invoke(Find.FactionManager, new object[] { source });
                Log.Message("[WNA] Merged duplicate faction " + source.Name + " into " + target.Name + ".");
            }
            catch (Exception ex)
            {
                source.defeated = true;
                source.hidden = true;
                Log.Warning("[WNA] Could not remove duplicate faction " + source.Name + "; marked it defeated and hidden instead. " + ex);
            }
        }
        private int CountSettlements(Faction faction)
        {
            return Find.WorldObjects.Settlements.Count(s => s.Faction == faction);
        }
        private int CountSettlements(FactionDef def)
        {
            return Find.WorldObjects.Settlements.Count(s => s.Faction != null && s.Faction.def == def);
        }
        private int GetTargetSettlementCount(FactionDef def, int allowedLayerCount)
        {
            int baseTarget = Mathf.Max(1, primalCount) * Mathf.Max(1, allowedLayerCount);
            int rivalMax = GetMaxRivalSettlementCount(def);
            return Mathf.Max(baseTarget, Mathf.CeilToInt(rivalMax * 1.5f));
        }
        private int GetNeededSettlements(FactionDef def, List<PlanetLayer> layers, int target)
        {
            int globalNeed = Mathf.Max(0, target - CountSettlements(def));
            int perLayerTarget = Mathf.Max(1, primalCount);
            int layerNeed = 0;
            foreach (PlanetLayer layer in layers)
                layerNeed += Mathf.Max(0, perLayerTarget - CountSettlementsOnLayer(def, layer));
            return Mathf.Max(globalNeed, layerNeed);
        }
        private int GetMaxRivalSettlementCount(FactionDef def)
        {
            return Find.WorldObjects.Settlements
                .Where(s => s.Faction != null && s.Faction.def != def)
                .GroupBy(s => s.Faction)
                .Select(g => g.Count())
                .DefaultIfEmpty(0)
                .Max();
        }
        private int CountSettlementsOnLayer(FactionDef def, PlanetLayer layer)
        {
            return Find.WorldObjects.Settlements
                .Count(s => s.Faction != null && s.Faction.def == def && s.Tile.Layer == layer);
        }
        private List<PlanetLayer> GetAllowedLayers(FactionDef def)
        {
            return Find.WorldGrid.PlanetLayers.Values
                .Where(layer => CanExistOnLayer(layer, def))
                .ToList();
        }
        private bool CanExistOnLayer(PlanetLayer layer, FactionDef f)
        {
            if (!f.layerBlacklist.NullOrEmpty() && f.layerBlacklist.Contains(layer.Def))
                return false;
            if (!f.layerWhitelist.NullOrEmpty() && f.layerWhitelist.Contains(layer.Def))
                return true;
            return layer.Def.GenStepsInOrder.Contains(DefDatabase<WorldGenStepDef>.GetNamed("Factions"));
        }
        private int GetRecommendedFactionCount()
        {
            float baseVal = (Find.WorldGrid.TilesCount * 0.7f) / 100000f;
            return Mathf.Max(1, GenMath.RoundRandom(baseVal));
        }
    }
}
