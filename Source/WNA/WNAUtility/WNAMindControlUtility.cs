using RimWorld;
using Verse;
using WNA.WNADefOf;
using WNA.WNAGameComp;
using WNA.WNAHediffClass;
using WNA.WNAModExtension;

namespace WNA.WNAUtility
{
    internal class WNAMindControlUtility
    {
        internal static bool MindControlled(Pawn pawn)
        {
            return pawn?.health?.hediffSet?.HasHediff(WNAMainDefOf.WNA_MindControlEffect) == true;
        }

        internal static bool PermaconstControlled(Pawn pawn)
        {
            return pawn?.health?.hediffSet?.HasHediff(WNAMainDefOf.WNA_PermaconstActive) == true;
        }

        internal static bool CanBeControlled(Thing controller, Thing victim)
        {
            return CanBeControlled(controller, victim, null, null, false, out _);
        }

        internal static bool CanBeControlled(Thing controller, Thing victim, MindControlConfig config, out string reason)
        {
            return CanBeControlled(controller, victim, null, config, config?.permanent ?? false, out reason);
        }

        internal static bool CanBeControlled(Thing controller, Thing victim, Def sourceDef, MindControlConfig config, out string reason)
        {
            return CanBeControlled(controller, victim, sourceDef, config, config?.permanent ?? false, out reason);
        }

        private static bool CanBeControlled(Thing controller, Thing victim, Def sourceDef, MindControlConfig config, bool permanent, out string reason)
        {
            config ??= MindControlConfig.Default;
            reason = null;

            if (controller == null || victim == null)
            {
                reason = "WNA_MindControl_InvalidTarget".Translate();
                return false;
            }
            if (controller.DestroyedOrNull() || (controller is Pawn controllerPawn && controllerPawn.Dead))
            {
                reason = "WNA_MindControl_InvalidTarget".Translate();
                return false;
            }

            Faction controllerFaction = controller.Faction;
            if (controllerFaction == null || controllerFaction == victim.Faction)
            {
                reason = "WNA_MindControl_SameFaction".Translate();
                return false;
            }

            TechnoConfig defConfig = TechnoConfig.Get(victim.def);
            if (defConfig != null && defConfig.immuneToMindControl == true)
            {
                reason = "WNA_MindControl_Immune".Translate();
                return false;
            }

            if (!(victim is Pawn pawn))
                return false;

            if (pawn.Dead)
            {
                reason = "WNA_MindControl_InvalidTarget".Translate();
                return false;
            }
            if (pawn.RaceProps.intelligence != Intelligence.Humanlike)
            {
                reason = "WNA_MindControl_NotHumanlike".Translate();
                return false;
            }
            if (General.IsWNARace(pawn)
                || pawn.story?.traits?.HasTrait(WNAMainDefOf.WNA_Trait_Unshakable) == true)
            {
                reason = "WNA_MindControl_Immune".Translate();
                return false;
            }

            if (pawn.health != null && pawn.health.hediffSet != null
                && (MindControlled(pawn) || PermaconstControlled(pawn)))
            {
                reason = "WNA_MindControl_AlreadyControlled".Translate();
                return false;
            }

            if (TechnoConfigUtility.PawnHasFlag(pawn, TechnoFlag.ImmuneToMindControl))
            {
                reason = "WNA_MindControl_Immune".Translate();
                return false;
            }

            if (!permanent && config.maxTargets >= 0)
            {
                int controlled = GameComp_MindControl.CountControlledBy(controller, sourceDef);
                if (controlled >= config.maxTargets)
                {
                    reason = "WNA_MindControl_Full".Translate();
                    return false;
                }
            }

            return true;
        }

        internal static bool TryControl(Thing controller, Thing victim, MindControlConfig config = null, bool? permanentOverride = null)
        {
            return TryControl(controller, victim, null, config, permanentOverride);
        }

        internal static bool TryControl(Thing controller, Thing victim, Def sourceDef, MindControlConfig config = null, bool? permanentOverride = null)
        {
            config ??= MindControlConfig.Default;
            bool permanent = permanentOverride ?? config.permanent;

            if (victim is Pawn nonHumanlike && nonHumanlike.RaceProps.intelligence != Intelligence.Humanlike)
            {
                BurnBrain(nonHumanlike, controller, config);
                return false;
            }

            if (!CanBeControlled(controller, victim, sourceDef, config, permanent, out _))
                return false;

            if (!(victim is Pawn pawn))
                return false;

            Faction controllerFaction = controller.Faction;
            Faction oldFaction = pawn.Faction;

            if (config.goodwillChange != 0 && oldFaction != null && controllerFaction != null && oldFaction != controllerFaction)
            {
                oldFaction.TryAffectGoodwillWith(
                    controllerFaction,
                    config.goodwillChange,
                    reason: config.goodwillReason ?? WNAMainDefOf.WNA_HE_MemberControlled);
            }

            HdfClass_MindControl hediff = (HdfClass_MindControl)HediffMaker.MakeHediff(WNAMainDefOf.WNA_MindControlEffect, pawn);
            hediff.yrFac = controllerFaction;
            hediff.source = controller;
            hediff.sourceFaction = controllerFaction;
            hediff.permanent = permanent;
            hediff.appliedTick = Find.TickManager.TicksGame;
            hediff.releaseRange = config.releaseWhenOutOfRange ? config.range : -1f;
            hediff.releaseOnSourceLost = config.releaseOnControllerDeath && !permanent;
            hediff.releaseOnFactionChange = config.releaseOnControllerFactionChange && !permanent;
            hediff.requireSourcePower = controller is Building building && building.GetComp<CompPowerTrader>() != null;
            pawn.health.AddHediff(hediff);

            if (!permanent)
                Current.Game?.GetComponent<GameComp_MindControl>()?.Register(controller, pawn, sourceDef, config.maxTargets);

            pawn.jobs?.StopAll();
            pawn.pather?.StopDead();
            pawn.mindState?.Reset(true, true);

            if (config.sendMessage && IsPlayerAffiliated(pawn, oldFaction))
            {
                string controllerLabel = controller is Pawn controllerPawn
                    ? controllerPawn.LabelShort
                    : (controllerFaction?.Name ?? controller.LabelShort);
                Messages.Message(
                    "WNA_MindControl_Controlled_Message".Translate(pawn.LabelShort, controllerLabel),
                    pawn,
                    MessageTypeDefOf.NegativeEvent,
                    false);
            }
            return true;
        }

        internal static void Release(Pawn pawn)
        {
            Hediff hediff = pawn?.health?.hediffSet?.GetFirstHediffOfDef(WNAMainDefOf.WNA_MindControlEffect);
            if (hediff != null)
                pawn.health.RemoveHediff(hediff);
        }

        private static bool IsPlayerAffiliated(Pawn pawn, Faction oldFaction)
        {
            if (oldFaction == Faction.OfPlayer)
                return true;
            return pawn.IsPrisonerOfColony
                || pawn.IsSlaveOfColony
                || pawn.IsQuestLodger();
        }

        private static void BurnBrain(Pawn pawn, Thing controller, MindControlConfig config)
        {
            if (!config.brainBurn || pawn == null || pawn.Dead || pawn.Destroyed)
                return;
            BodyPartRecord brain = pawn.health?.hediffSet?.GetBrain();
            if (brain == null)
                return;
            int hp = brain.def.hitPoints;
            float factor = Rand.Range(config.brainBurnHpFactor.min, config.brainBurnHpFactor.max);
            pawn.TakeDamage(new DamageInfo(DamageDefOf.Flame, factor * hp, float.MaxValue, -1f, controller, brain));
        }
    }
}
