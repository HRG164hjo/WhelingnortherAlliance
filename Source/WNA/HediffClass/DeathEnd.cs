using RimWorld;
using Verse;
using Verse.AI;
using WNA.WNADefOf;

namespace WNA.HediffClass
{
    public class DeathEnd : HediffWithComps
    {
        private bool resurrecting;
        public bool PlayerControlled
        {
            get
            {
                if (pawn.IsColonist)
                {
                    if (pawn.HostFaction != null)
                    {
                        return pawn.IsSlave;
                    }
                    return true;
                }
                return false;
            }
        }
        public override void PostAdd(DamageInfo? dinfo)
        {
            base.PostAdd(dinfo);
			if (pawn.ageTracker.AgeBiologicalTicks >= (int)(24 * 3600000f))
                pawn.ageTracker.AgeBiologicalTicks = (int)(24 * 3600000f);
        }
        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref resurrecting, "resurrecting", defaultValue: false);
            if (Scribe.mode == LoadSaveMode.PostLoadInit)
            {
                if (!resurrecting && pawn.Dead)
                    Resurrect();
            }
        }
        public override void Notify_PawnDied(DamageInfo? dinfo, Hediff culprit = null)
        {
            base.Notify_PawnDied(dinfo, culprit);
            Resurrect();
            if (PlayerControlled && PawnUtility.ShouldSendNotificationAbout(pawn))
                Messages.Message("SelfResurrectText".Translate(pawn.Named("PAWN")), pawn, MessageTypeDefOf.NeutralEvent);
        }
        private void Resurrect()
        {
            resurrecting = false;
            pawn.Drawer.renderer.SetAnimation(null);
            ResurrectionUtility.TryResurrect(pawn, new ResurrectionParams
            {
                gettingScarsChance = 0f,
                canKidnap = false,
                canTimeoutOrFlee = false,
                useAvoidGridSmart = true,
                canSteal = false,
                invisibleStun = false
            });
            if (pawn.Faction != Faction.OfPlayer && !pawn.Downed)
            {
                Thing thing = GenClosest.ClosestThingReachable(pawn.Position, pawn.Map, ThingRequest.ForGroup(ThingRequestGroup.Weapon), PathEndMode.OnCell, TraverseParms.For(pawn), 5f);
                if (thing != null)
                {
                    Job job = JobGiver_PickupDroppedWeapon.PickupWeaponJob(pawn, thing, ignoreForbidden: true);
                    if (job != null)
                        pawn.jobs.StartJob(job, JobCondition.InterruptForced);
                }
            }
            pawn.health.AddHediff(WNAMainDefOf.sWNA_DeathEnd);
        }
    }
}
