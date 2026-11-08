using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;

namespace WNA.WNAThingComp
{
    internal class PropSensorArray : CompProperties
    {
        public float sensorSight = 19.9f;
        public int checkInterval = 30;
        public int stunTicks = 300;
        public bool requiresPower = true;
        public PropSensorArray()
        {
            compClass = typeof(CompSensorArray);
        }
    }
    internal class CompSensorArray : ThingComp
    {
        private PropSensorArray Props => (PropSensorArray)props;
        private CompPowerTrader powerComp;
        private bool powerResolved;
        private bool Powered
        {
            get
            {
                if (!Props.requiresPower)
                {
                    return true;
                }
                if (!powerResolved)
                {
                    powerComp = parent.TryGetComp<CompPowerTrader>();
                    powerResolved = true;
                }
                return powerComp == null || powerComp.PowerOn;
            }
        }
        public override void CompTick()
        {
            base.CompTick();
            if (!parent.Spawned)
            {
                return;
            }
            if (!parent.IsHashIntervalTick(Mathf.Max(1, Props.checkInterval)))
            {
                return;
            }
            if (!Powered)
            {
                return;
            }
            Scan(parent.Map);
        }
        private void Scan(Map map)
        {
            IReadOnlyList<Pawn> pawns = map.mapPawns.AllPawnsSpawned;
            IntVec3 center = parent.Position;
            float sensorSight = Props.sensorSight;
            for (int i = 0; i < pawns.Count; i++)
            {
                Pawn pawn = pawns[i];
                // Cheapest rejection first: distance before touching the pawn's hediff set.
                if (!pawn.Position.InHorDistOf(center, sensorSight))
                {
                    continue;
                }
                if (TryReveal(pawn))
                {
                    Stun(pawn);
                }
            }
        }
        private static bool TryReveal(Pawn pawn)
        {
            List<Hediff> hediffs = pawn.health.hediffSet.hediffs;
            bool revealed = false;
            for (int i = 0; i < hediffs.Count; i++)
            {
                HediffComp_Invisibility invisibility = hediffs[i].TryGetComp<HediffComp_Invisibility>();
                if (invisibility == null || invisibility.PsychologicallyVisible)
                {
                    continue;
                }
                invisibility.DisruptInvisibility();
                invisibility.BecomeVisible(instant: true);
                revealed = true;
            }
            return revealed;
        }
        private void Stun(Pawn pawn)
        {
            StunHandler stunner = pawn.stances?.stunner;
            int ticks = Props.stunTicks;
            if (stunner == null || ticks <= 0 || stunner.Stunned)
            {
                return;
            }
            stunner.StunFor(ticks, parent, addBattleLog: false, showMote: true);
        }
    }
}
