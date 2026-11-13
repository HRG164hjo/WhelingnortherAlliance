using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI;
using WNA.WNADamageWorker;
using WNA.WNADefOf;
using WNA.WNAModExtension;
using WNA.WNAThingClass;

namespace WNA.WNAGameComp
{
    internal class GameComp_SeismicLaunch : GameComponent
    {
        private List<SeismicLaunchRequest> pending = new List<SeismicLaunchRequest>();

        public GameComp_SeismicLaunch(Game game) { }

        private static GameComp_SeismicLaunch Instance =>
            Current.Game?.GetComponent<GameComp_SeismicLaunch>();

        internal static void Enqueue(Pawn pawn, DamageDef def, float angle, float damageDealt, Thing instigator)
        {
            Instance?.EnqueueInternal(pawn, def, angle, damageDealt, instigator);
        }

        private void EnqueueInternal(Pawn pawn, DamageDef def, float angle, float damageDealt, Thing instigator)
        {
            int dueTick = Find.TickManager.TicksGame + 1;
            SeismicLaunchRequest request = null;
            for (int i = 0; i < pending.Count; i++)
            {
                if (pending[i].pawn == pawn && pending[i].dueTick == dueTick)
                {
                    request = pending[i];
                    break;
                }
            }
            if (request == null)
            {
                request = new SeismicLaunchRequest { pawn = pawn, dueTick = dueTick };
                pending.Add(request);
            }
            request.AddHit(def, angle, damageDealt, instigator);
        }

        public override void GameComponentTick()
        {
            if (pending.Count == 0)
                return;
            int now = Find.TickManager.TicksGame;
            for (int i = pending.Count - 1; i >= 0; i--)
            {
                SeismicLaunchRequest request = pending[i];
                if (request.dueTick > now)
                    continue;
                pending.RemoveAt(i);
                TryLaunch(request);
            }
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Collections.Look(ref pending, "pending", LookMode.Deep);
            if (pending == null)
                pending = new List<SeismicLaunchRequest>();
        }

        private static void TryLaunch(SeismicLaunchRequest request)
        {
            Pawn pawn = request.pawn;
            if (pawn == null || pawn.Dead || pawn.Destroyed || request.damageDef == null)
                return;
            SeismicConfig config = request.damageDef.GetModExtension<SeismicConfig>();
            if (config == null || !TryPreparePawn(pawn))
                return;

            Map map = pawn.Map;
            Vector3 direction = request.Direction;
            float distance = Mathf.Max(1f,
                config.EvalDistance(request.totalDamage) * config.EvalBodyDistanceFactor(pawn.BodySize));
            int maxCells = Mathf.Max(1, Mathf.RoundToInt(distance));
            IntVec3 start = pawn.Position;
            IntVec3 destination = start;
            bool found = false;
            for (int steps = maxCells; steps >= 1; steps--)
            {
                IntVec3 cell = start + (direction * steps).ToIntVec3();
                if (!cell.InBounds(map) || !JumpUtility.ValidJumpTarget(pawn, map, cell))
                    continue;
                destination = cell;
                found = true;
                break;
            }
            IntVec3 idealCell = start + (direction * maxCells).ToIntVec3();
            bool projectedOffMap = !idealCell.InBounds(map);

            float travelled = found ? start.DistanceTo(destination) : 0f;
            float landingDamage = config.EvalLandingDamage(Mathf.Max(1f, travelled))
                * config.EvalBodyDamageFactor(pawn.BodySize);
            bool hitEdge = found
                ? IsNearEdge(map, destination, config.edgeKillMargin)
                : projectedOffMap && IsNearEdge(map, start, config.edgeKillMargin);
            bool lethalEdge = config.edgeKill && hitEdge && !DmgWorker_Seismic.IsEdgeKillImmune(pawn);

            ThingDef flyerDef = config.flyerDef ?? WNAMainDefOf.WNA_PawnFlyer_Seismic;
            if (flyerDef == null)
            {
                Log.ErrorOnce("WNA: seismic damage def has no flyer def configured.", 885139321);
                return;
            }
            PawnFlyer flyer = PawnFlyer.MakeFlyer(flyerDef, pawn, destination,
                config.flightEffecter, config.landingSound);
            if (flyer == null)
                return;
            if (flyer is PawnFlyer_Seismic seismicFlyer)
            {
                seismicFlyer.landingDamage = landingDamage;
                seismicFlyer.landingArmorPenetration = config.landingArmorPenetration;
                seismicFlyer.lethalEdge = lethalEdge;
                seismicFlyer.impactAngle = request.fallbackAngle;
                seismicFlyer.instigator = request.instigator;
            }
            GenSpawn.Spawn(flyer, destination, map);
        }

        private static bool TryPreparePawn(Pawn pawn)
        {
            if (pawn.BeingTransportedOnGravship)
                return false;
            IThingHolder holder = pawn.ParentHolder;
            if (holder is Pawn_CarryTracker || holder is PawnFlyer)
                return false;
            if (holder != null)
            {
                Map map = ThingOwnerUtility.GetRootMap(holder);
                IntVec3 position = ThingOwnerUtility.GetRootPosition(holder);
                ThingOwner owner = holder.GetDirectlyHeldThings();
                if (map == null || !position.IsValid || owner == null || !owner.Contains(pawn))
                    return false;
                if (!owner.TryDrop(pawn, position, map, ThingPlaceMode.Near, out _))
                    return false;
            }
            if (pawn.CurrentBed() != null)
                pawn.jobs?.StopAll();
            return pawn.Spawned && pawn.Map != null;
        }

        private static bool IsNearEdge(Map map, IntVec3 cell, float margin)
        {
            return cell.x < margin || cell.z < margin
                || cell.x >= map.Size.x - margin || cell.z >= map.Size.z - margin;
        }
    }

    internal class SeismicLaunchRequest : IExposable
    {
        internal Pawn pawn;
        internal int dueTick;
        internal float totalDamage;
        internal float strongestDamage;
        internal float fallbackAngle;
        internal DamageDef damageDef;
        internal Thing instigator;
        private float dirX;
        private float dirZ;

        internal Vector3 Direction
        {
            get
            {
                float magnitude = Mathf.Sqrt(dirX * dirX + dirZ * dirZ);
                if (magnitude < 0.0001f)
                    return Quaternion.Euler(0f, fallbackAngle, 0f) * Vector3.forward;
                return new Vector3(dirX / magnitude, 0f, dirZ / magnitude);
            }
        }

        internal void AddHit(DamageDef def, float angle, float damageDealt, Thing source)
        {
            float weight = Mathf.Max(damageDealt, 0f);
            Vector3 dir = Quaternion.Euler(0f, angle, 0f) * Vector3.forward;
            dirX += dir.x * weight;
            dirZ += dir.z * weight;
            totalDamage += weight;
            if (weight >= strongestDamage)
            {
                strongestDamage = weight;
                fallbackAngle = angle;
                damageDef = def;
                instigator = source;
            }
        }

        public void ExposeData()
        {
            Scribe_References.Look(ref pawn, "pawn");
            Scribe_Values.Look(ref dueTick, "dueTick", 0);
            Scribe_Values.Look(ref totalDamage, "totalDamage", 0f);
            Scribe_Values.Look(ref strongestDamage, "strongestDamage", 0f);
            Scribe_Values.Look(ref fallbackAngle, "fallbackAngle", 0f);
            Scribe_Values.Look(ref dirX, "dirX", 0f);
            Scribe_Values.Look(ref dirZ, "dirZ", 0f);
            Scribe_Defs.Look(ref damageDef, "damageDef");
            Scribe_References.Look(ref instigator, "instigator");
        }
    }
}
