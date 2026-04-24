using RimWorld;
using UnityEngine;
using Verse;
using WNA.WNADefOf;

namespace WNA.WNAHediffCompProp
{
    public class PropPermaconstActive : HediffCompProperties
    {
        public PropPermaconstActive()
        {
            compClass = typeof(CompPermaconstActive);
        }
    }
    public class CompPermaconstActive : HediffComp
    {
        public PropPermaconstActive Props => (PropPermaconstActive)props;
        private bool PawnValid(Pawn pawn)
        {
            if (pawn == null) return false;
            if (pawn.Faction != null)
            {
                if (pawn.Faction.def == WNAMainDefOf.WNA_FactionWNA ||
                    pawn.Faction.def == WNAMainDefOf.WNA_FactionPCC)
                    return true;
                if (pawn.Faction == Faction.OfPlayer && pawn.Ideo != null)
                {
                    if (pawn.Ideo.HasPrecept(WNAMainDefOf.WNA_P_Proselyte))
                        return true;
                }
            }
            return false;
        }
        public override void CompPostPostAdd(DamageInfo? dinfo)
        {
            base.CompPostPostAdd(dinfo);
            Pawn p = parent.pawn;
            if (p == null || PawnValid(p)) return;
            Faction pcc = Find.FactionManager.FirstFactionOfDef(WNAMainDefOf.WNA_FactionPCC);
            if (pcc != null && p.Faction != pcc)
            {
                p.SetFaction(pcc);
                p.stances?.stunner?.StunFor(67, null, false, false, true);
            }
        }
        public override void Notify_PawnPostApplyDamage(DamageInfo dinfo, float totalDamageDealt)
        {
            base.Notify_PawnPostApplyDamage(dinfo, totalDamageDealt);
            if (dinfo.Instigator != null && dinfo.Instigator is Pawn attacker)
            {
                if (PawnValid(attacker))
                    return;
                if (attacker.health != null && attacker.RaceProps.IsFlesh)
                {
                    if (!attacker.health.hediffSet.HasHediff(WNAMainDefOf.WNA_PermaconstHidden))
                    {
                        attacker.health.AddHediff(WNAMainDefOf.WNA_PermaconstHidden);
                        attacker.stances?.stunner?.StunFor(23, parent.pawn, true, true, true);
                    }
                }
            }
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
                Faction pcc = Find.FactionManager.FirstFactionOfDef(WNAMainDefOf.WNA_FactionPCC)
                    ?? Find.FactionManager.FirstFactionOfDef(WNAMainDefOf.WNA_FactionWNA);
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
