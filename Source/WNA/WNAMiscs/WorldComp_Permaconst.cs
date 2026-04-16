using RimWorld;
using RimWorld.Planet;
using System.Collections.Generic;
using Verse;
using WNA.WNADefOf;

namespace WNA.WNAMiscs
{
    public class WorldComp_Permaconst : WorldComponent
    {
        public WorldComp_Permaconst(World world) : base(world) { }
        private Faction wna => Find.FactionManager.FirstFactionOfDef(WNAMainDefOf.WNA_FactionWNA);
        private Faction pcc => Find.FactionManager.FirstFactionOfDef(WNAMainDefOf.WNA_FactionPCC);
        private int eventTick = 1414200;
        public override void WorldComponentTick()
        {
            base.WorldComponentTick();
            if (Find.TickManager.TicksGame == 23)
            {
                IdeoCheck();
                RelationCheck();
            }
            if (Find.TickManager.TicksGame % 2357 == 0)
            {
                IdeoCheck();
                RelationCheck();
            }
            if (Find.TickManager.TicksGame % 43 == 0)
            {
                HediffCheck(); List<Pawn> pawns = PawnsFinder.AllMapsWorldAndTemporary_Alive;
                for (int i = 0; i < pawns.Count; i++)
                    ProcessPawn(pawns[i]);
            }
            if (Find.TickManager.TicksGame % 200 == 0)
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
            IdeoCheck();
            base.FinalizeInit(fromLoad);
        }
        private bool ShouldProcess(Pawn pawn)
        {
            return (pawn.def == WNAMainDefOf.WNA_WNThan || pawn.def == WNAMainDefOf.WNA_Human)
                && (pawn.Faction != wna && pawn.Faction != pcc);
        }
        private void ProcessPawn(Pawn pawn)
        {
            if (wna == null)
                return;
            Ideo ideo = wna.ideos.PrimaryIdeo;
            if (ideo == null)
                return;
            if (ShouldProcess(pawn))
            {
                pawn.ideo.SetIdeo(ideo);
                if (pawn.Faction != Faction.OfPlayer)
                    if (pawn.Faction != wna && pawn.Faction != pcc)
                        pawn.SetFaction(wna);
                    else pawn.stances.stunner.StunFor(2357, null, false);
            }
        }
        private void IdeoCheck()
        {
            if (!ModsConfig.IdeologyActive) return;
            if (wna != null && pcc != null)
            {
                Ideo ideo = wna.ideos.PrimaryIdeo;
                if (ideo == null) return;
                var icon = WNAMainDefOf.WNA_IdeoIcon;
                if (icon != null)
                    ideo.SetIcon(icon, WNAMainDefOf.WNA_PureBlack);
                pcc.ideos?.SetPrimary(ideo);
            }
        }
        private void HediffCheck()
        {
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
