using RimWorld;
using System;
using Verse;
using WNA.WNADefOf;

namespace WNA.WNAIncident
{
    public class PermaconstMaintenance : IncidentWorker
    {
        protected override bool CanFireNowSub(IncidentParms parms)
        {
            Map map = (Map)parms.target;
            return base.CanFireNowSub(parms) &&
                   !map.gameConditionManager.ConditionIsActive(WNAMainDefOf.WNA_GameCond_PermaconstActive);
        }
        protected override bool TryExecuteWorker(IncidentParms parms)
        {
            Map map = (Map)parms.target;
            Faction pcc = Find.FactionManager.FirstFactionOfDef(WNAMainDefOf.WNA_FactionPCC);
            int duration = 141420;
            GameCondition cond = GameConditionMaker.MakeCondition(WNAMainDefOf.WNA_GameCond_PermaconstActive, duration);
            map.gameConditionManager.RegisterCondition(cond);
            if (pcc != null && pcc.HostileTo(Faction.OfPlayer))
            {
                IncidentParms raidParms = StorytellerUtility.DefaultParmsNow(IncidentCategoryDefOf.ThreatBig, map);
                raidParms.faction = pcc;
                raidParms.raidStrategy = RaidStrategyDefOf.ImmediateAttack;
                IncidentDefOf.RaidEnemy.Worker.TryExecute(raidParms);
                raidParms.points = Math.Max(parms.points, 10000);
                IncidentDefOf.RaidEnemy.Worker.TryExecute(raidParms);
            }
            Find.LetterStack.ReceiveLetter(
                "WNA_Letter_Permaconst".Translate(),
                "WNA_Letter_Permaconst_Desc".Translate(),
                LetterDefOf.ThreatBig,
                new TargetInfo(map.Center, map)
            );
            return true;
        }
    }
}
