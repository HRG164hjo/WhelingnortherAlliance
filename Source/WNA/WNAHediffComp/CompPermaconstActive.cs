using RimWorld;
using UnityEngine;
using Verse;
using WNA.WNAUtility;

namespace WNA.WNAHediffComp
{
    internal class PropPermaconstActive : HediffCompProperties
    {
        public PropPermaconstActive()
        {
            compClass = typeof(CompPermaconstActive);
        }
    }
    internal class CompPermaconstActive : HediffComp
    {
        public PropPermaconstActive Props => (PropPermaconstActive)props;
        public override void CompPostPostAdd(DamageInfo? dinfo)
        {
            base.CompPostPostAdd(dinfo);
            Pawn p = parent.pawn;
            if (p == null || WNAPermaconstUtility.Immunity(p)) return;
            if (General.PCCFaction != null
                && p.Faction != General.PCCFaction)
            {
                p.SetFaction(General.PCCFaction);
                p.stances?.stunner?.StunFor(57, null, false, false, true);
            }
        }
        public override void Notify_PawnPostApplyDamage(DamageInfo dinfo, float totalDamageDealt)
        {
            base.Notify_PawnPostApplyDamage(dinfo, totalDamageDealt);
            if (dinfo.Instigator != null && dinfo.Instigator is Pawn attacker)
                WNAPermaconstUtility.TryMarkHidden(attacker, parent.pawn);
        }
        public override void Notify_PawnDied(DamageInfo? dinfo, Hediff culprit = null)
        {
            base.Notify_PawnDied(dinfo, culprit);
            float rand = Random.value;
            if (rand <= 0.07f && WNAMod.Settings.enablePermaconstDeathAid)
            {
                Map map = parent.pawn.Map ?? parent.pawn.MapHeld;
                if (map == null)
                    return;
                Faction pcc = General.PCCFaction ?? General.WNAFaction;
                Difficulty diff = Find.Storyteller?.difficulty;
                if (diff == null)
                    return;
                float scale = Mathf.Max(1f, diff.threatScale);
                float rand2 = Mathf.Clamp(Random.value, 0.4f, 0.9f) * 4f;
                if (pcc != null)
                {
                    IncidentParms raidParms = StorytellerUtility
                        .DefaultParmsNow(IncidentCategoryDefOf.ThreatBig, map);
                    raidParms.customLetterLabel = "WNA_PermaconstRaidSmall".Translate();
                    raidParms.customLetterText = "WNA_PermaconstRaidSmall_Desc".Translate();
                    raidParms.faction = pcc;
                    raidParms.raidStrategy = RaidStrategyDefOf.ImmediateAttack;
                    raidParms.points = 2357 * rand2 * scale;
                    IncidentDefOf.RaidEnemy.Worker.TryExecute(raidParms);
                }
            }
        }
    }
}
