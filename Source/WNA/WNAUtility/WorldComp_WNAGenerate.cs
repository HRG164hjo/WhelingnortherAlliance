using RimWorld;
using RimWorld.Planet;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;
using WNA.WNADefOf;

namespace WNA.WNAUtility
{
    public class WorldComp_WNAGenerate : WorldComponent
    {
        private const int InitialDelayTicks = 37;
        private const int RefillIntervalTicks = 173205;
        private bool initialized;
        private int targetSettlementCount;
        private int nextRefillTick = -1;
        private Faction wna;
        public WorldComp_WNAGenerate(World world) : base(world) { }
        public override void ExposeData()
        {
            Scribe_Values.Look(ref initialized, "initialized", false);
            Scribe_Values.Look(ref targetSettlementCount, "targetSettlementCount", 0);
            Scribe_Values.Look(ref nextRefillTick, "nextRefillTick", -1);
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
            var parms = new FactionGeneratorParms(def, default, hidden: false);
            Faction fac = FactionGenerator.NewGeneratedFaction(Find.WorldGrid.Surface, parms);
            Find.FactionManager.Add(fac);
            wna = fac;
            return fac;
        }
        public override void FinalizeInit(bool fromLoad)
        {
            base.FinalizeInit(fromLoad);
            if (!initialized)
                nextRefillTick = Find.TickManager.TicksGame + InitialDelayTicks;
        }
        public override void WorldComponentTick()
        {
            int now = Find.TickManager.TicksGame;
            if (!initialized && now >= nextRefillTick)
            {
                InitialSpawn();
                initialized = true;
                nextRefillTick = now + RefillIntervalTicks;
                return;
            }
            if (initialized && now >= nextRefillTick)
            {
                RefillSettlementsIfNeeded();
                nextRefillTick = now + RefillIntervalTicks;
            }
        }
        private void InitialSpawn()
        {
            FactionDef def = WNAMainDefOf.WNA_FactionWNA;
            var allowedLayers = GetAllowedLayers(def);
            if (allowedLayers.Count == 0) return;
            targetSettlementCount = GetRecommendedFactionCount();
            int spawnedTotal = 0;
            foreach (var layer in allowedLayers)
            {
                int current = CountSettlementsOnLayer(def, layer);
                int needOnLayer = Mathf.Max(0, targetSettlementCount - current);
                spawnedTotal += SpawnSettlementsOnLayer(def, layer, needOnLayer);
            }
            Faction pl = Faction.OfPlayer;
            Find.LetterStack.ReceiveLetter(
                "WNA_InitialSpawn_Label".Translate(),
                "WNA_InitialSpawn_Desc".Translate(targetSettlementCount, spawnedTotal),
                Faction.OfPlayer.HostileTo(wna) ? LetterDefOf.ThreatBig : LetterDefOf.NeutralEvent);
        }
        private void RefillSettlementsIfNeeded()
        {
            FactionDef def = WNAMainDefOf.WNA_FactionWNA;
            var allowedLayers = GetAllowedLayers(def);
            if (allowedLayers.Count == 0 || targetSettlementCount <= 0)
                return;
            int refillTotal = 0;
            foreach (var layer in allowedLayers)
            {
                int current = CountSettlementsOnLayer(def, layer);
                int need = Mathf.Max(0, targetSettlementCount - current);
                if (current <= 0 || need > 0)
                    refillTotal += Mathf.CeilToInt(SpawnSettlementsOnLayer(def, layer, need) * 1.414214f);
            }
            if (refillTotal > 0)
            {
                Find.LetterStack.ReceiveLetter(
                    "WNA_Refill_Label".Translate(),
                    "WNA_Refill_Desc".Translate(refillTotal, targetSettlementCount),
                    Faction.OfPlayer.HostileTo(wna) ? LetterDefOf.ThreatBig : LetterDefOf.NeutralEvent);
            }
        }
        private int SpawnSettlementsOnLayer(FactionDef def, PlanetLayer layer, int amount)
        {
            EnsureFaction(def);
            if (amount <= 0)
                return 0;
            int spawned = 0;
            Faction owner = GetOrCreateFaction(def);
            for (int i = 0; i < amount; i++)
            {
                PlanetTile tile = TileFinder.RandomSettlementTileFor(layer, owner);
                if (!tile.Valid || !TileFinder.IsValidTileForNewSettlement(tile)) continue;
                WorldObjectDef objDef = layer.Def.SettlementWorldObjectDef
                    ?? WorldObjectDefOf.Settlement;
                WorldObject wo = WorldObjectMaker.MakeWorldObject(objDef);
                if (wo == null)
                    continue;
                wo.SetFaction(owner);
                wo.Tile = tile;
                if (wo is Settlement settlement)
                    settlement.Name = SettlementNameGenerator.GenerateSettlementName(settlement);
                Find.WorldObjects.Add(wo);
                spawned++;
            }
            return spawned;
        }
        private void EnsureFaction(FactionDef def)
        {
            var all = Find.FactionManager.AllFactionsListForReading
                .Where(f => f.def == def && !f.defeated)
                .ToList();
            if (all.Count > 0)
            {
                wna = all[0];
                wna.defeated = false;
                wna.hidden = false;
                for (int i = 1; i < all.Count; i++)
                    Log.Warning("Duplicate factions found for def: " + def.defName);
                return;
            }
            GetOrCreateFaction(def);
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
            //int visible = Mathf.Max(1, Find.FactionManager.AllFactionsVisible.Count());
            float baseVal = (Find.WorldGrid.TilesCount * 0.7f) / 100000f;
            return Mathf.Max(1, GenMath.RoundRandom(baseVal));
        }
    }
}
