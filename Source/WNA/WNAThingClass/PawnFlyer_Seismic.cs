using RimWorld;
using Verse;
using WNA.WNADamageWorker;

namespace WNA.WNAThingClass
{
    internal class PawnFlyer_Seismic : PawnFlyer
    {
        internal float landingDamage;
        internal float landingArmorPenetration;
        internal bool lethalEdge;
        internal float impactAngle;
        internal Thing instigator;

        protected override void RespawnPawn()
        {
            Pawn pawn = FlyingPawn;
            base.RespawnPawn();
            if (pawn == null || pawn.Dead || pawn.Destroyed || !pawn.Spawned)
                return;
            if (lethalEdge && !DmgWorker_Seismic.IsEdgeKillImmune(pawn))
            {
                pawn.Kill(new DamageInfo(DamageDefOf.Crush, 99999f, 0f, impactAngle, instigator));
                return;
            }
            if (landingDamage > 0f)
                pawn.TakeDamage(new DamageInfo(DamageDefOf.Crush, landingDamage, landingArmorPenetration, impactAngle, instigator));
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref landingDamage, "landingDamage", 0f);
            Scribe_Values.Look(ref landingArmorPenetration, "landingArmorPenetration", 0f);
            Scribe_Values.Look(ref lethalEdge, "lethalEdge", false);
            Scribe_Values.Look(ref impactAngle, "impactAngle", 0f);
            Scribe_References.Look(ref instigator, "instigator");
        }
    }
}
