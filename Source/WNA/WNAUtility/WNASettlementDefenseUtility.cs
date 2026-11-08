using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using RimWorld.Planet;
using UnityEngine;
using Verse;
using WNA.WNADefOf;
using WNA.WNAWorldObject;

namespace WNA.WNAUtility
{
    public static class WNASettlementDefenseUtility
    {
        private const float TransporterRadPerTick = 0.00025f;
        private const float SweepTicks = 45f;
        private const float SweepChordMargin = 0.02f;
        private const float ForeignKindPenalty = 1000f;
        private const int CandidateRefreshTicks = 250;
        // Shared across settlements: rebuilt every CandidateRefreshTicks, positions kept on the unit sphere so
        // range checks are plain squared-distance comparisons (no acos/sqrt per candidate).
        private static readonly List<WorldObject> candidateCache = new List<WorldObject>();
        private static readonly List<Vector3> candidateUnitPos = new List<Vector3>();
        private static Game candidateCacheGame;
        private static int candidateCacheTick;
        public static WorldObject FindTarget(WorldObject settlement, float rangeTiles)
        {
            if (settlement == null || settlement.Destroyed || settlement.Faction == null || !settlement.Tile.Valid)
                return null;
            RefreshCandidates();
            if (candidateCache.Count == 0)
                return null;
            WorldObject_DefenseMissile.PruneTargeted();
            PlanetLayer homeLayer = settlement.Tile.Layer;
            bool crossLayer = WNAMod.Settings != null && WNAMod.Settings.enableCrossLayerDefense;
            Vector3 homePos = homeLayer.GetTileCenter(settlement.Tile).normalized;
            float homeMaxChordSq = MaxChordSq(rangeTiles, homeLayer.Radius / Mathf.Max(0.01f, homeLayer.AverageTileSize));
            Dictionary<PlanetLayer, Vector3> layerHomePos = null;
            Dictionary<PlanetLayer, float> layerMaxChordSq = null;
            WorldObject best = null;
            float bestScore = float.MaxValue;
            for (int i = 0; i < candidateCache.Count; i++)
            {
                WorldObject candidate = candidateCache[i];
                if (candidate == settlement || candidate.Destroyed || !candidate.Spawned || !candidate.Tile.Valid)
                    continue;
                Vector3 from = homePos;
                float maxChordSq = homeMaxChordSq;
                PlanetLayer layer = candidate.Tile.Layer;
                if (layer != homeLayer)
                {
                    if (!crossLayer)
                        continue;
                    if (layerHomePos == null)
                    {
                        layerHomePos = new Dictionary<PlanetLayer, Vector3>();
                        layerMaxChordSq = new Dictionary<PlanetLayer, float>();
                    }
                    if (!layerHomePos.TryGetValue(layer, out from))
                    {
                        PlanetTile projected = layer.GetClosestTile_NewTemp(settlement.Tile);
                        from = projected.Valid ? layer.GetTileCenter(projected).normalized : Vector3.zero;
                        layerHomePos[layer] = from;
                    }
                    if (from == Vector3.zero)
                        continue;
                    if (!layerMaxChordSq.TryGetValue(layer, out maxChordSq))
                    {
                        maxChordSq = MaxChordSq(rangeTiles, layer.Radius / Mathf.Max(0.01f, layer.AverageTileSize));
                        layerMaxChordSq[layer] = maxChordSq;
                    }
                }
                bool movingShip = IsMovingShip(candidate);
                float chordSq;
                Vector3 cachedPos = candidateUnitPos[i];
                if (cachedPos == Vector3.zero)
                {
                    Vector3 current = LocalPosOf(candidate);
                    if (current.sqrMagnitude < 0.0001f)
                        continue;
                    current.Normalize();
                    chordSq = (current - from).sqrMagnitude;
                    if (chordSq > maxChordSq
                        && (!movingShip || Mathf.Sqrt(chordSq) > Mathf.Sqrt(maxChordSq) + SweepChordMargin))
                        continue;
                    if (!IsValidTarget(settlement, candidate) || WorldObject_DefenseMissile.IsTargeted(candidate))
                        continue;
                    if (movingShip)
                        chordSq = Mathf.Min(chordSq, MovingTargetMinChordSq(from, current, candidate, layer));
                }
                else
                {
                    chordSq = (cachedPos - from).sqrMagnitude;
                    if (chordSq > maxChordSq)
                        continue;
                    if (!IsValidTarget(settlement, candidate) || WorldObject_DefenseMissile.IsTargeted(candidate))
                        continue;
                }
                if (chordSq > maxChordSq)
                    continue;
                float score = IsFastMover(candidate) ? chordSq - 10000f : chordSq;
                if (IsForeignKind(candidate))
                    score += ForeignKindPenalty;
                if (score < bestScore)
                {
                    bestScore = score;
                    best = candidate;
                }
            }
            return best;
        }
        private static void RefreshCandidates()
        {
            Game game = Current.Game;
            int now = Find.TickManager.TicksGame;
            if (game == candidateCacheGame && now >= candidateCacheTick && now - candidateCacheTick < CandidateRefreshTicks)
                return;
            candidateCacheGame = game;
            candidateCacheTick = now;
            candidateCache.Clear();
            candidateUnitPos.Clear();
            List<WorldObject> all = Find.WorldObjects.AllWorldObjects;
            for (int i = 0; i < all.Count; i++)
            {
                WorldObject o = all[i];
                if (o == null || o.Destroyed || !o.Spawned || o.Faction == null || !o.Tile.Valid)
                    continue;
                if (o.questTags != null && o.questTags.Count > 0)
                    continue;
                if (!IsCandidateKind(o))
                    continue;
                candidateCache.Add(o);
                bool moves = IsMovingShip(o) || o is Caravan;
                candidateUnitPos.Add(moves ? Vector3.zero : o.Tile.Layer.GetTileCenter(o.Tile).normalized);
            }
        }
        private static bool IsCandidateKind(WorldObject o)
        {
            if (IsInertDebris(o))
                return false;
            if (o is Settlement || o is Caravan || o is TravellingTransporters || o is Camp)
                return true;
            if (o is Site || o is Gravship)
                return WNAMod.Settings != null && WNAMod.Settings.attackSitesAndGravships;
            return true;
        }
        private static bool IsInertDebris(WorldObject o)
        {
            return o is DestroyedSettlement || o is AbandonedSettlement || o is GravshipLaunch || o is PeaceTalks
                || o is WorldObject_WNACapturedSettlement;
        }
        private static bool IsForeignKind(WorldObject o)
        {
            return !(o is Settlement || o is Caravan || o is TravellingTransporters || o is Camp
                || o is Site || o is Gravship);
        }
        private static float MaxChordSq(float rangeTiles, float tilesPerRadian)
        {
            float angle = rangeTiles / Mathf.Max(0.0001f, tilesPerRadian);
            if (angle >= Mathf.PI)
                return 4f;
            float chord = 2f * Mathf.Sin(angle * 0.5f);
            return chord * chord;
        }
        private static float MovingTargetMinChordSq(Vector3 from, Vector3 current, WorldObject target, PlanetLayer layer)
        {
            float best = (current - from).sqrMagnitude;
            PlanetTile destination = PlanetTile.Invalid;
            if (target is TravellingTransporters transporters)
                destination = transporters.destinationTile;
            else if (target is Gravship gravship)
                destination = gravship.destinationTile;
            if (!destination.Valid || destination.Layer != layer)
                return best;
            Vector3 start = layer.GetTileCenter(target.Tile).normalized;
            Vector3 end = layer.GetTileCenter(destination).normalized;
            float total = GenMath.SphericalDistance(start, end);
            if (total < 0.00001f)
                return best;
            float travelled = GenMath.SphericalDistance(start, current);
            float pct = Mathf.Clamp01(travelled / total);
            float pctAhead = Mathf.Min(1f, pct + TransporterRadPerTick * SweepTicks / total);
            Vector3 fromPoint = Vector3.Slerp(start, end, pct);
            Vector3 toPoint = Vector3.Slerp(start, end, pctAhead);
            for (int i = 0; i <= 8; i++)
            {
                Vector3 point = Vector3.Slerp(fromPoint, toPoint, i / 8f);
                if (point.sqrMagnitude < 0.0001f)
                    continue;
                point.Normalize();
                best = Mathf.Min(best, (point - from).sqrMagnitude);
            }
            return best;
        }
        public static bool IsValidTarget(WorldObject settlement, WorldObject target)
        {
            if (settlement == null || target == null || target == settlement)
                return false;
            if (target.Destroyed || !target.Spawned || !target.Tile.Valid)
                return false;
            if (IsInertDebris(target))
                return false;
            if (!IsHostile(settlement.Faction, target.Faction))
                return false;
            if (target.questTags != null && target.questTags.Count > 0)
                return false;
            bool allowGeneratedMaps = WNAMod.Settings != null && WNAMod.Settings.attackGeneratedMaps;
            if (target is MapParent mapParent && mapParent.HasMap && !allowGeneratedMaps)
                return false;
            if (target is Settlement || target is Caravan || target is TravellingTransporters)
                return true;
            if (target is Site || target is Gravship)
                return WNAMod.Settings != null && WNAMod.Settings.attackSitesAndGravships;
            if (target is Camp camp)
                return allowGeneratedMaps
                    && settlement.Faction != null
                    && settlement.Faction.def == WNAMainDefOf.WNA_FactionWNA
                    && IsDetectedCamp(camp);
            // Unrecognized world object types from other mods are destroyed on sight.
            return true;
        }
        private static bool IsDetectedCamp(Camp camp)
        {
            TimedDetectionRaids detection = camp.GetComponent<TimedDetectionRaids>();
            return detection != null && (detection.alertRaidsArrivingIn || detection.NextRaidCountdownActiveAndVisible);
        }
        public static bool IsHostile(Faction a, Faction b)
        {
            return a != null && b != null && a != b && a.HostileTo(b);
        }
        public static bool IsFastMover(WorldObject o)
        {
            return o is TravellingTransporters;
        }
        private static bool IsMovingShip(WorldObject o)
        {
            return o is TravellingTransporters || o is Gravship;
        }
        public static Vector3 LocalPosOf(WorldObject o)
        {
            if (o == null || o.Destroyed)
                return Vector3.zero;
            if (IsMovingShip(o))
                return o.DrawPos;
            return o.Tile.Valid ? o.Tile.Layer.GetTileCenter(o.Tile) : Vector3.zero;
        }

        public static float DistanceInTiles(WorldObject settlement, WorldObject target)
        {
            if (settlement == null || target == null || !settlement.Tile.Valid || !target.Tile.Valid)
                return float.MaxValue;
            PlanetLayer layer = target.Tile.Layer;
            bool sameLayer = settlement.Tile.Layer == layer;
            if (!sameLayer && (WNAMod.Settings == null || !WNAMod.Settings.enableCrossLayerDefense))
                return float.MaxValue;
            PlanetTile settlementTile = sameLayer ? settlement.Tile : layer.GetClosestTile_NewTemp(settlement.Tile);
            if (!settlementTile.Valid)
                return float.MaxValue;
            Vector3 settlementPos = layer.GetTileCenter(settlementTile).normalized;
            if (IsMovingShip(target))
            {
                Vector3 current = LocalPosOf(target);
                if (current.sqrMagnitude < 0.0001f)
                    return float.MaxValue;
                current.Normalize();
                float chord = Mathf.Sqrt(MovingTargetMinChordSq(settlementPos, current, target, layer));
                return layer.ApproxDistanceInTiles(2f * Mathf.Asin(Mathf.Clamp(chord * 0.5f, 0f, 1f)));
            }
            Vector3 targetPos = layer.GetTileCenter(target.Tile).normalized;
            return layer.ApproxDistanceInTiles(GenMath.SphericalDistance(settlementPos, targetPos));
        }
        public static bool LaunchMissile(WorldObject settlement, WorldObject target)
        {
            if (settlement == null || target == null || target.Destroyed)
                return false;
            WorldObjectDef def = WNAMainDefOf.WNA_DefenseMissile;
            if (def == null)
            {
                Log.Error("[WNA] Missing WorldObjectDef WNA_DefenseMissile.");
                return false;
            }
            PlanetLayer layer = target.Tile.Layer;
            bool sameLayer = settlement.Tile.Layer == layer;
            PlanetTile startTile = sameLayer ? settlement.Tile : layer.GetClosestTile_NewTemp(settlement.Tile);
            if (!startTile.Valid)
                return false;
            float distance = DistanceInTiles(settlement, target);
            if (distance > 10000f)
                return false;
            WorldObject_DefenseMissile missile = WorldObjectMaker.MakeWorldObject(def) as WorldObject_DefenseMissile;
            if (missile == null)
            {
                Log.Error("[WNA] WNA_DefenseMissile did not create a WorldObject_DefenseMissile.");
                return false;
            }
            missile.Tile = startTile;
            missile.Setup(settlement, target, distance);
            Find.WorldObjects.Add(missile);
            return true;
        }
        public static void DestroyTarget(WorldObject target, WorldObject source)
        {
            if (target == null || target.Destroyed)
                return;
            PlanetTile tile = target.Tile;
            string label = target.LabelCap;
            Faction targetFaction = target.Faction;
            try
            {
                if (target is Settlement settlement)
                    DestroySettlement(settlement);
                else if (target is Caravan caravan)
                    DestroyCaravan(caravan);
                else if (target is TravellingTransporters transporters)
                    DestroyTransporters(transporters);
                else if (target is Gravship gravship)
                    DestroyGravship(gravship);
                else
                    target.Destroy();
            }
            catch (Exception ex)
            {
                Log.Error("[WNA] Failed to destroy auto-defense target " + label + ": " + ex);
            }
            NotifyDestruction(source, targetFaction, label, tile);
        }
        private static void DestroySettlement(Settlement settlement)
        {
            Faction faction = settlement.Faction;
            PlanetTile tile = settlement.Tile;
            WorldObjectDef destroyedDef = tile.LayerDef.DestroyedSettlementWorldObjectDef;
            if (destroyedDef != null)
            {
                WorldObject destroyed = WorldObjectMaker.MakeWorldObject(destroyedDef);
                destroyed.Tile = tile;
                destroyed.SetFaction(faction);
                Find.WorldObjects.Add(destroyed);
            }
            settlement.Destroy();
            WNAFactionDefeatUtility.TryMarkDefeated(faction, tile);
        }
        private static void DestroyCaravan(Caravan caravan)
        {
            List<Pawn> pawns = caravan.PawnsListForReading.ToList();
            for (int i = 0; i < pawns.Count; i++)
                KillPawn(pawns[i]);
            if (!caravan.Destroyed)
                caravan.Destroy();
        }
        private static void DestroyTransporters(TravellingTransporters transporters)
        {
            List<IThingHolder> holders = new List<IThingHolder>();
            transporters.GetChildHolders(holders);
            for (int i = 0; i < holders.Count; i++)
            {
                ThingOwner owner = holders[i].GetDirectlyHeldThings();
                if (owner == null)
                    continue;
                for (int j = owner.Count - 1; j >= 0; j--)
                {
                    if (owner[j] is Pawn pawn)
                    {
                        owner.Remove(pawn);
                        KillPawn(pawn);
                    }
                }
                owner.ClearAndDestroyContentsOrPassToWorld();
            }
            transporters.Destroy();
        }
        private static void DestroyGravship(Gravship gravship)
        {
            List<Pawn> pawns = gravship.Pawns.ToList();
            for (int i = 0; i < pawns.Count; i++)
                KillPawn(pawns[i]);
            gravship.Destroy();
        }
        public static void KillPawn(Pawn pawn)
        {
            if (pawn == null || pawn.Dead || pawn.Destroyed)
                return;
            if (!pawn.Spawned && !pawn.IsWorldPawn())
                Find.WorldPawns.PassToWorld(pawn);
            pawn.Kill(new DamageInfo(WNAMainDefOf.WNA_ChimingBlaze, 9999999f, 9999999f));
        }
        public static void NotifyDestruction(WorldObject source, Faction targetFaction, string targetLabel, PlanetTile tile)
        {
            string sourceName = source != null ? source.LabelCap : "WNA_AutoDefense_UnknownSource".Translate().Resolve();
            string factionName = targetFaction != null ? targetFaction.Name : "WNA_AutoDefense_UnknownFaction".Translate().Resolve();
            TaggedString text = "WNA_AutoDefense_LetterText".Translate(sourceName, factionName, targetLabel);
            if (tile.Valid)
                Find.LetterStack.ReceiveLetter("WNA_AutoDefense_LetterLabel".Translate(), text, LetterDefOf.NegativeEvent, new LookTargets(tile), source != null ? source.Faction : null);
            else
                Find.LetterStack.ReceiveLetter("WNA_AutoDefense_LetterLabel".Translate(), text, LetterDefOf.NegativeEvent, null, source != null ? source.Faction : null);
        }
        public static void NotifyStrikeIncoming(Map map, WorldObject source)
        {
            string sourceName = source != null ? source.LabelCap : "WNA_AutoDefense_UnknownSource".Translate().Resolve();
            string targetName = map.Parent != null ? map.Parent.LabelCap : map.Tile.ToString();
            TaggedString text = "WNA_AutoDefense_MapStrikeText".Translate(sourceName, targetName);
            Find.LetterStack.ReceiveLetter("WNA_AutoDefense_MapStrikeLabel".Translate(), text, LetterDefOf.ThreatBig, new LookTargets(map.Center, map), source != null ? source.Faction : null);
        }
        public static void RemoveStruckMap(Map map)
        {
            if (map == null || !Find.Maps.Contains(map))
                return;
            List<Pawn> survivors = new List<Pawn>();
            AddDistinct(survivors, map.mapPawns.FreeColonistsSpawned);
            AddDistinct(survivors, map.mapPawns.PrisonersOfColonySpawned);
            AddDistinct(survivors, map.mapPawns.SlavesOfColonySpawned);
            List<Corpse> corpses = new List<Corpse>();
            List<Thing> corpseThings = map.listerThings.ThingsInGroup(ThingRequestGroup.Corpse);
            for (int i = 0; i < corpseThings.Count; i++)
            {
                if (corpseThings[i] is Corpse corpse && corpse.InnerPawn != null && corpse.InnerPawn.Faction == Faction.OfPlayer)
                    corpses.Add(corpse);
            }
            bool canFormCaravans = map.Tile.LayerDef.canFormCaravans;
            Caravan caravan = null;
            if (survivors.Count > 0)
            {
                if (canFormCaravans)
                {
                    caravan = CaravanExitMapUtility.ExitMapAndCreateCaravan(survivors, Faction.OfPlayer, map.Tile, map.Tile, PlanetTile.Invalid, sendMessage: false);
                    if (caravan != null)
                    {
                        for (int i = 0; i < corpses.Count; i++)
                        {
                            Corpse corpse = corpses[i];
                            if (corpse == null || corpse.Destroyed)
                                continue;
                            corpse.DeSpawn(DestroyMode.Vanish);
                            caravan.AddPawnOrItem(corpse, addCarriedPawnToWorldPawnsIfAny: true);
                        }
                    }
                }
                else
                {
                    for (int i = 0; i < survivors.Count; i++)
                    {
                        Pawn pawn = survivors[i];
                        if (pawn == null || pawn.Dead || !pawn.Spawned)
                            continue;
                        pawn.ExitMap(allowedToJoinOrCreateCaravan: false, Rot4.Invalid);
                        if (!pawn.IsWorldPawn())
                            Find.WorldPawns.PassToWorld(pawn);
                    }
                }
            }

            Current.Game.DeinitAndRemoveMap(map, notifyPlayer: false);
            Faction struckFaction = map.Parent?.Faction;
            if (map.Parent != null)
                map.Parent.Destroy();
            WNAFactionDefeatUtility.TryMarkDefeated(struckFaction, map.Tile);
            for (int i = 0; i < survivors.Count; i++)
            {
                Pawn pawn = survivors[i];
                if (pawn != null && pawn.playerSettings != null)
                    pawn.playerSettings.Notify_MapRemoved(map);
            }
            if (caravan != null)
                CameraJumper.TryJump(new GlobalTargetInfo(caravan));
            ShowStrikeDialog(survivors.Count, caravan != null);
        }
        private static void AddDistinct(List<Pawn> list, List<Pawn> source)
        {
            for (int i = 0; i < source.Count; i++)
            {
                Pawn pawn = source[i];
                if (pawn != null && !pawn.Dead && !list.Contains(pawn))
                    list.Add(pawn);
            }
        }
        private static void ShowStrikeDialog(int survivorCount, bool caravanCreated)
        {
            TaggedString text;
            if (survivorCount <= 0)
                text = "WNA_AutoDefense_NoSurvivors".Translate();
            else if (caravanCreated)
                text = "WNA_AutoDefense_ColonistsEscaped".Translate();
            else
                text = "WNA_AutoDefense_OrbitEscaped".Translate();
            Find.WindowStack.Add(new Dialog_MessageBox(text, "OK".Translate().Resolve()));
        }
    }
}
