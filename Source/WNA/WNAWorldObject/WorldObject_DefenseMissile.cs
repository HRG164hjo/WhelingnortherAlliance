using System.Collections.Generic;
using RimWorld;
using RimWorld.Planet;
using UnityEngine;
using Verse;
using WNA.WNAGameComp;
using WNA.WNAUtility;
namespace WNA.WNAWorldObject
{
    public class WorldObject_DefenseMissile : WorldObject
    {
        private const float TransporterRadPerTick = 0.00025f;
        private const float SpeedMultiplierVsTransporters = 6f;
        private static readonly HashSet<WorldObject> TargetedObjects = new HashSet<WorldObject>();
        private static MaterialPropertyBlock propertyBlock;
        private WorldObject target;
        private WorldObject source;
        private PlanetTile startTile = PlanetTile.Invalid;
        private Vector3 startPos;
        private int ageTicks;
        private int flightTicks = 60;
        private bool verticalDrop;
        private bool resolved;
        public override Vector3 DrawPos => Tile.Layer.Origin + CurrentLocalPos();
        protected override int UpdateRateTicks => 1;
        public static void PruneTargeted()
        {
            TargetedObjects.RemoveWhere(x => x == null || x.Destroyed);
        }
        public static bool IsTargeted(WorldObject o)
        {
            return o != null && TargetedObjects.Contains(o);
        }
        public void Setup(WorldObject sourceSettlement, WorldObject targetObject, float distanceTiles)
        {
            source = sourceSettlement;
            target = targetObject;
            startTile = Tile;
            startPos = startTile.Valid ? startTile.Layer.GetTileCenter(startTile) : Vector3.zero;
            if (targetObject != null)
                TargetedObjects.Add(targetObject);
            PlanetLayer layer = startTile.Layer;
            // Transporter pods travel at 0.00025 rad/tick; the missile keeps a fixed multiple of that angular speed.
            float tilesPerTick = SpeedMultiplierVsTransporters * TransporterRadPerTick * layer.Radius / Mathf.Max(0.01f, layer.AverageTileSize);
            if (distanceTiles < 0.5f)
            {
                verticalDrop = true;
                flightTicks = 20;
            }
            else
                flightTicks = Mathf.Clamp(Mathf.CeilToInt(distanceTiles / Mathf.Max(0.0001f, tilesPerTick)), 10, 300);
        }
        protected override void TickInterval(int delta)
        {
            base.TickInterval(delta);
            if (resolved)
                return;
            ageTicks += delta;
            if (target == null || target.Destroyed)
            {
                Finish(null);
                return;
            }
            if (ageTicks >= flightTicks)
                Finish(target);
        }
        private void Finish(WorldObject hitTarget)
        {
            if (resolved)
                return;
            resolved = true;
            if (target != null)
                TargetedObjects.Remove(target);
            if (hitTarget != null && !hitTarget.Destroyed)
            {
                if (hitTarget is MapParent mapParent && mapParent.HasMap && mapParent.Map != null && Find.Maps.Contains(mapParent.Map))
                {
                    if (WNAMod.Settings != null && WNAMod.Settings.attackGeneratedMaps)
                    {
                        CameraJumper.TryJump(mapParent.Map.Center, mapParent.Map);
                        GameComp_MapStrike.ScheduleStrike(mapParent.Map, source);
                    }
                }
                else
                    WNASettlementDefenseUtility.DestroyTarget(hitTarget, source);
            }
            Destroy();
        }
        private Vector3 CurrentLocalPos()
        {
            float progress = flightTicks <= 0 ? 1f : Mathf.Clamp01((float)ageTicks / flightTicks);
            progress = Mathf.SmoothStep(0f, 1f, progress);
            Vector3 end = target != null && !target.Destroyed ? WNASettlementDefenseUtility.LocalPosOf(target) : startPos;
            return Vector3.Slerp(startPos, end, progress);
        }
        public override void Draw()
        {
            if (resolved)
                return;
            Material material = Material;
            if (material == null)
                return;
            PlanetLayer layer = Tile.Layer;
            float averageTileSize = Mathf.Max(0.01f, layer.AverageTileSize);
            float progress = flightTicks <= 0 ? 1f : Mathf.Clamp01((float)ageTicks / flightTicks);
            Vector3 localPos = CurrentLocalPos();
            float altitude = verticalDrop ? Mathf.Lerp(1.5f, 0.05f, Mathf.SmoothStep(0f, 1f, progress)) : 0.08f;
            float rotation = 0f;
            if (!verticalDrop && (localPos - startPos).sqrMagnitude > 0.0001f)
                rotation = layer.GetHeadingFromTo(startPos, localPos);
            float alpha = Mathf.Clamp01(Mathf.Min(ageTicks / 6f, (flightTicks - ageTicks) / 6f));
            if (propertyBlock == null)
                propertyBlock = new MaterialPropertyBlock();
            propertyBlock.SetColor(ShaderPropertyIDs.Color, new Color(1f, 1f, 1f, alpha));
            WorldRendererUtility.DrawQuadTangentialToPlanet(layer.Origin + localPos, 0.9f * averageTileSize, altitude, material, rotation, counterClockwise: false, useSkyboxLayer: false, propertyBlock: propertyBlock);
        }
        public override void PostRemove()
        {
            base.PostRemove();
            if (target != null)
                TargetedObjects.Remove(target);
        }
        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_References.Look(ref target, "target");
            Scribe_References.Look(ref source, "source");
            Scribe_Values.Look(ref startTile, "startTile", PlanetTile.Invalid, forceSave: true);
            Scribe_Values.Look(ref ageTicks, "ageTicks", 0);
            Scribe_Values.Look(ref flightTicks, "flightTicks", 60);
            Scribe_Values.Look(ref verticalDrop, "verticalDrop", false);
            if (Scribe.mode == LoadSaveMode.PostLoadInit)
            {
                startPos = startTile.Valid ? startTile.Layer.GetTileCenter(startTile) : Vector3.zero;
                if (target != null && !target.Destroyed)
                    TargetedObjects.Add(target);
            }
        }
    }
}
