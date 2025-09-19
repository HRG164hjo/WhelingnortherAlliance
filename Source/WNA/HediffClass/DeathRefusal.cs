using RimWorld;
using System.Collections.Generic;
using UnityEngine;
using Verse;
using Verse.AI;
using WNA.WNADefOf;

namespace WNA.HediffClass
{
    public class DeathRefusal : HediffWithComps
    {
    	protected int usesLeft;
    
        private readonly TickTimer resurrectTimer = new TickTimer();
    
        private bool resurrecting;
    
        private bool aiEnabled = true;
    
        private Effecter resurrectAvailableEffecter;
    
        private static readonly CachedTexture Icon = new CachedTexture("UI/Abilities/SelfResurrect");
    
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
    
        public int UsesLeft => usesLeft;
    
        public bool AIEnabled
        {
            get
            {
                return aiEnabled;
            }
            set
            {
                aiEnabled = value;
            }
        }
    
        public virtual int MaxUses => 7;
    
        public override string LabelInBrackets => UsesLeft + " " + ((UsesLeft > 1) ? "DeathRefusalUsePlural".Translate() : "DeathRefusalUseSingular".Translate()).ToString();
    
        public override void PostAdd(DamageInfo? dinfo)
        {
            if (!ModLister.CheckAnomaly("Death refusal"))
            {
                pawn.health.RemoveHediff(this);
                return;
            }
            base.PostAdd(dinfo);
            usesLeft = MaxUses;
        }
    
        public override IEnumerable<Gizmo> GetGizmos()
        {
            foreach (Gizmo gizmo in base.GetGizmos())
            {
                yield return gizmo;
            }
            if (!pawn.Dead || !PlayerControlled)
            {
                yield break;
            }
            Command_ActionWithLimitedUseCount cmdSelfResurrect = new Command_ActionWithLimitedUseCount
            {
                defaultLabel = "CommandSelfResurrect".Translate(),
                defaultDesc = "CommandSelfResurrectDesc".Translate(),
                usesLeftGetter = () => usesLeft,
                maxUsesGetter = () => MaxUses
            };
            cmdSelfResurrect.UpdateUsesLeft();
            cmdSelfResurrect.icon = Icon.Texture;
            cmdSelfResurrect.action = delegate
            {
                if (resurrectTimer.Finished)
                {
                    Use();
                    cmdSelfResurrect.UpdateUsesLeft();
                }
            };
            yield return cmdSelfResurrect;
        }
    
        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref usesLeft, "usesLeft", 0);
            Scribe_Values.Look(ref resurrecting, "resurrecting", defaultValue: false);
            Scribe_Values.Look(ref aiEnabled, "aiEnabled", defaultValue: false);
            if (Scribe.mode == LoadSaveMode.PostLoadInit)
            {
                if (!resurrecting && pawn.Dead)
                {
                    TryTriggerAIWarmupResurrection();
                }
            }
        }
    
        public override void Notify_PawnDied(DamageInfo? dinfo, Hediff culprit = null)
        {
            base.Notify_PawnDied(dinfo, culprit);
            if (PlayerControlled && PawnUtility.ShouldSendNotificationAbout(pawn))
            {
                Messages.Message("SelfResurrectText".Translate(pawn.Named("PAWN")), pawn, MessageTypeDefOf.PositiveEvent);
            }
            TryTriggerAIWarmupResurrection();
            }
    
            private void TryTriggerAIWarmupResurrection()
        {
            if (!PlayerControlled && !resurrecting && AIEnabled && usesLeft > 0)
            {
                Use();
            }
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
            if (usesLeft == 0)
            {
                Severity = 0f;
            }
            if (pawn.Faction != Faction.OfPlayer && !pawn.Downed)
            {
                Thing thing = GenClosest.ClosestThingReachable(pawn.Position, pawn.Map, ThingRequest.ForGroup(ThingRequestGroup.Weapon), PathEndMode.OnCell, TraverseParms.For(pawn), 5f);
                if (thing != null)
                {
                    Job job = JobGiver_PickupDroppedWeapon.PickupWeaponJob(pawn, thing, ignoreForbidden: true);
                    if (job != null)
                    {
                        pawn.jobs.StartJob(job, JobCondition.InterruptForced);
                    }
                }
            }
            pawn.health.AddHediff(WNAMainDefOf.sWNA_DeathRefusal);
        }
    
        private void Use()
        {
            Messages.Message("MessageUsingSelfResurrection".Translate(pawn), pawn, MessageTypeDefOf.NeutralEvent);
            resurrecting = true;
            if (pawn.health.hediffSet.HasHediff(WNAMainDefOf.sWNA_DeathRefusal))
            {
                usesLeft++;
            }
            else usesLeft = Mathf.Max(usesLeft - 1, 0);
            Resurrect();
            resurrectAvailableEffecter?.ForceEnd();
            resurrectAvailableEffecter = null;
        }
    }
}
