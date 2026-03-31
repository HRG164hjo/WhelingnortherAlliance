using RimWorld;
using RimWorld.Planet;
using Verse;
using WNA.WNADefOf;

namespace WNA.WNAMiscs
{
    public class WorldComp_Permaconst : WorldComponent
    {
        public WorldComp_Permaconst(World world) : base(world) { }
        private int eventTick = 1414200;
        public override void WorldComponentTick()
        {
            base.WorldComponentTick();
            int currentTick = Find.TickManager.TicksGame;
            if (currentTick == 23)
            {
                IdeoCheck();
                RelationCheck();
            }
            if (currentTick % 2357 == 0)
            {
                IdeoCheck();
                RelationCheck();
            }
            if (currentTick % 43 == 0)
                HediffCheck();
            if (currentTick % 200 == 0)
            {
                eventTick -= 200;
                if(eventTick <= 0)
                {
                    TriggerEvent();
                    eventTick = 1414200;
                }
            }
        }
        public override void FinalizeInit(bool fromLoad)
        {
            base.FinalizeInit(fromLoad);
            IdeoCheck();
        }
        private void IdeoCheck()
        {
            if (!ModsConfig.IdeologyActive) return;
            Faction wna = Find.FactionManager.FirstFactionOfDef(WNAMainDefOf.WNA_FactionWNA);
            Faction pcc = Find.FactionManager.FirstFactionOfDef(WNAMainDefOf.WNA_FactionPCC);
            if (wna != null && pcc != null)
            {
                Ideo wnaIdeo = wna.ideos.PrimaryIdeo;
                if (wnaIdeo == null) return;
                var icon = WNAMainDefOf.WNA_IdeoIcon;
                if (icon != null)
                    wnaIdeo.SetIcon(icon, WNAMainDefOf.WNA_PureBlack);
                pcc.ideos?.SetPrimary(wnaIdeo);
            }
        }
        private void HediffCheck()
        {
            Faction pcc = Find.FactionManager.FirstFactionOfDef(WNAMainDefOf.WNA_FactionPCC);
            if (pcc == null) return;
            foreach (Map map in Find.Maps)
            {
                var pccPawns = map.mapPawns.SpawnedPawnsInFaction(pcc);
                foreach (Pawn p in pccPawns)
                {
                    if (p.Dead || p.health == null) continue;
                    if (!p.health.hediffSet.HasHediff(WNAMainDefOf.WNA_PermaconstActive))
                        p.health.AddHediff(WNAMainDefOf.WNA_PermaconstActive);
                }
            }
        }
        private void RelationCheck()
        {
            Faction wna = Find.FactionManager.FirstFactionOfDef(WNAMainDefOf.WNA_FactionWNA);
            Faction pcc = Find.FactionManager.FirstFactionOfDef(WNAMainDefOf.WNA_FactionPCC);
            Faction player = Faction.OfPlayer;
            if (wna == null || pcc == null || player == null) return;
            FactionRelationKind relationwna = wna.RelationWith(player).kind;
            FactionRelation relationpcc = pcc.RelationWith(player);
            if (pcc.HostileTo(wna))
                pcc.SetRelationDirect(wna, FactionRelationKind.Ally, false);
            if (relationwna == FactionRelationKind.Hostile)
            {
                if (relationpcc.kind != FactionRelationKind.Hostile)
                {
                    string str = "WNA_LordHostility".Translate();
                    pcc.SetRelationDirect(player, FactionRelationKind.Hostile, true, str);
                }
            }
            else
            {
                if (relationpcc.kind != FactionRelationKind.Neutral)
                {
                    string str = "WNA_LordNeutralized".Translate();
                    pcc.SetRelationDirect(player, FactionRelationKind.Neutral, false, str);
                }
            }
        }
        private void TriggerEvent()
        {
            Map map = Find.AnyPlayerHomeMap;
            if (map == null) return;
            IncidentDef incident = WNAMainDefOf.WNA_Incident_Permaconst;
            IncidentParms parms = StorytellerUtility.DefaultParmsNow(incident.category, map);
            incident.Worker.TryExecute(parms);
        }
        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref eventTick, "eventTick", -1);
        }
    }
}
