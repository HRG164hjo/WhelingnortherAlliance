using RimWorld;
using Verse;
using WNA.WNADefOf;
using WNA.WNAHediffClass;
using WNA.WNAModExtension;

namespace WNA.WNAUtility
{
    public static class MindControlUtility
    {
        internal static bool MindControlled(Pawn pawn) => pawn.health.hediffSet.HasHediff(WNAMainDefOf.WNA_MindControlEffect);
        internal static bool PermaconstControlled(Pawn pawn) => pawn.health.hediffSet.HasHediff(WNAMainDefOf.WNA_PermaconstActive);
        internal static bool CanBeControlled(Thing controller, Thing victim)
        {
            TechnoConfig config = TechnoConfig.Get(victim.def);
            if (config != null && config.immuneToMindControl == true)
                return false;
            if (controller.Faction == victim.Faction || controller.Faction == null)
                return false;
            if (victim is Pawn pawn)
            {
                if (pawn.RaceProps.intelligence != Intelligence.Humanlike)
                    return false;
                if (pawn.def == WNAMainDefOf.WNA_WNThan
                    || pawn.def == WNAMainDefOf.WNA_Human
                    || pawn.story.traits.HasTrait(WNAMainDefOf.WNA_Trait_Unshakable))
                    return false;
                if (pawn.apparel != null)
                {
                    foreach (Apparel apparel in pawn.apparel.WornApparel)
                    {
                        TechnoConfig ac = TechnoConfig.Get(apparel.def);
                        if (ac != null && ac.immuneToMindControl == true)
                            return false;
                    }
                }
                if (pawn.health != null && pawn.health.hediffSet != null)
                {
                    if (pawn.Dead || MindControlled(pawn) || PermaconstControlled(pawn))
                        return false;
                    foreach (Hediff hediff in pawn.health.hediffSet.hediffs)
                    {
                        if (hediff.def != null)
                        {
                            TechnoConfig hc = TechnoConfig.Get(hediff.def);
                            if (hc != null && hc.immuneToMindControl == true)
                                return false;
                        }
                    }
                }
            }
            return true;
        }
        internal static void TryControl(Thing controller, Thing victim, bool permanent = false)
        {
            if (CanBeControlled(controller, victim))
            {
                Faction fac = controller.Faction ?? null;
                if (victim is Pawn pawn)
                {
                    MindControl hediff = (MindControl)HediffMaker.MakeHediff(WNAMainDefOf.WNA_MindControlEffect, pawn);
                    hediff.yrFac = fac;
                    hediff.permanent = permanent;
                    if (fac != null)
                        pawn.Faction?.TryAffectGoodwillWith(fac, -100, reason: WNAMainDefOf.WNA_HE_MemberControlled);
                    pawn.health.AddHediff(hediff);
                    pawn.jobs?.StopAll();
                    pawn.pather?.StopDead();
                    pawn.mindState?.Reset(true, true);
                }
                else if (permanent)
                    victim.SetFaction(fac);
            }
            if (victim is Pawn p && p.RaceProps.intelligence != Intelligence.Humanlike)
            {
                BodyPartRecord brain = p.health.hediffSet.GetBrain();
                if (brain != null)
                {
                    int hp = brain.def.hitPoints;
                    float fact = UnityEngine.Random.Range(0.6f, 0.9f);
                    p.TakeDamage(new DamageInfo(DamageDefOf.Flame, fact * hp, float.MaxValue, -1, controller, brain));
                }
            }
        }
    }
}
