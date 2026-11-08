using System.Collections.Generic;
using RimWorld;
using RimWorld.Planet;
using Verse;
using WNA.WNAWorldObject;

namespace WNA.WNAUtility
{
    public static class WNACapturedSettlementUtility
    {
        private const string CapturedSettlementDefName = "WNA_CapturedSettlement";
        private const string CapturedSpaceSettlementDefName = "WNA_CapturedSpaceSettlement";

        private static bool retiringRuin;

        public static void ReviveDestroyedSettlements()
        {
            if (WNAMod.Settings == null || !WNAMod.Settings.reviveDestroyedSettlements)
                return;
            List<DestroyedSettlement> destroyed = Find.WorldObjects.DestroyedSettlements;
            if (destroyed.Count == 0)
                return;
            // Destroy() mutates Find.WorldObjects.DestroyedSettlements, so walk a snapshot.
            List<DestroyedSettlement> targets = new List<DestroyedSettlement>(destroyed);
            for (int i = 0; i < targets.Count; i++)
            {
                DestroyedSettlement ruined = targets[i];
                if (ruined == null || ruined.Destroyed || ruined.HasMap || !ruined.Tile.Valid)
                    continue;
                PlanetTile tile = ruined.Tile;
                if (!TryPlaceCaptured(tile))
                    continue;
                retiringRuin = true;
                try
                {
                    ruined.Destroy();
                }
                finally
                {
                    retiringRuin = false;
                }
            }
        }
        // Any removal of a destroyed settlement becomes a capture, whatever deleted it: the vanilla empty-map
        // cleanup (MapParent.CheckRemoveMapNow) or another mod's own marker sweep. The player can never be the
        // one losing out here, because a colonist, a colonist corpse or an occupied pod on the ruin's map
        // already blocks the vanilla deletion.
        // Outposts sit outside the destroyed-settlement hierarchy so no sweep should reach them, but a deletion
        // that happens anyway hands the tile to the Whelingnorther Alliance instead of the Permaconst Command -
        // losing a second outpost to the same cleaner would otherwise go unnoticed.
        public static void Notify_WorldObjectRemoved(WorldObject removed)
        {
            bool capturedOutpost = removed is WorldObject_WNACapturedSettlement;
            if (!capturedOutpost && !(removed is DestroyedSettlement))
                return;
            if (retiringRuin || Current.ProgramState != ProgramState.Playing)
                return;
            if (WNAMod.Settings == null || !WNAMod.Settings.reviveDestroyedSettlements)
                return;
            PlanetTile tile = removed.Tile;
            if (!tile.Valid)
                return;
            if (Find.WorldObjects.AnyWorldObjectAt<WorldObject_WNACapturedSettlement>(tile))
                return;
            if (Find.WorldObjects.AnySettlementAt(tile) || Find.WorldObjects.AnyDestroyedSettlementAt(tile)
                || Find.WorldObjects.AnySiteAt(tile))
                return;
            if (removed.Faction != null && Find.QuestManager.IsReservedByAnyQuest(removed.Faction))
                return;
            if (capturedOutpost)
            {
                TryPlaceWNASettlement(tile);
                return;
            }
            TryPlaceCaptured(tile);
        }
        private static bool TryPlaceCaptured(PlanetTile tile)
        {
            Faction pcc = General.PCCFaction;
            Faction wna = General.WNAFaction;
            if (pcc == null && wna == null)
                return false;
            bool space = tile.LayerDef.isSpace;
            if (wna != null && (pcc == null || Rand.Chance(WNAMod.Settings.capturedSettlementWNAChance)))
                return TryPlaceWNASettlement(tile);
            WorldObjectDef capturedDef = DefDatabase<WorldObjectDef>.GetNamedSilentFail(space ? CapturedSpaceSettlementDefName : CapturedSettlementDefName);
            if (capturedDef == null)
                return false;
            WorldObject captured = WorldObjectMaker.MakeWorldObject(capturedDef);
            if (captured == null)
                return false;
            captured.Tile = tile;
            captured.SetFaction(pcc);
            Find.WorldObjects.Add(captured);
            return true;
        }
        private static bool TryPlaceWNASettlement(PlanetTile tile)
        {
            Faction wna = General.WNAFaction;
            if (wna == null)
                return false;
            WorldObjectDef def = DefDatabase<WorldObjectDef>.GetNamedSilentFail(tile.LayerDef.isSpace ? "SpaceSettlement" : "Settlement");
            if (def == null || !(WorldObjectMaker.MakeWorldObject(def) is Settlement settlement))
                return false;
            settlement.Tile = tile;
            settlement.SetFaction(wna);
            settlement.Name = SettlementNameGenerator.GenerateSettlementName(settlement);
            Find.WorldObjects.Add(settlement);
            return true;
        }
    }
}
