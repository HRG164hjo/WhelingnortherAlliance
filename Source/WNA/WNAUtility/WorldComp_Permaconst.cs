using RimWorld;
using RimWorld.Planet;
using System;
using System.Collections.Generic;
using Verse;
using WNA.WNADefOf;
using WNA.WNAModExtension;

namespace WNA.WNAUtility
{
    public class WorldComp_Permaconst : WorldComponent
    {
        public WorldComp_Permaconst(World world) : base(world) { }
        private Faction wna => Find.FactionManager.FirstFactionOfDef(WNAMainDefOf.WNA_FactionWNA);
        private Faction pcc => Find.FactionManager.FirstFactionOfDef(WNAMainDefOf.WNA_FactionPCC);
        private int eventTick = 1414200;
        private int duration = -1;
        private bool unusualCondition => Faction.OfPlayer.HostileTo(wna);
        internal bool EventActive
        {
            get
            {
                if (duration >= 0)
                    return true;
                return false;
            }
        }
        public override void WorldComponentTick()
        {
            base.WorldComponentTick();
            if (Find.TickManager.TicksGame % 20 == 0 && duration >= 0)
                duration -= 20;
            if (Find.TickManager.TicksGame == 23
                || Find.TickManager.TicksGame == 235)
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
                eventTick -= 200 * (1 + Convert.ToInt32(unusualCondition));
                if(eventTick <= 0)
                {
                    TriggerEvent();
                    duration = 141420 * (1 + Convert.ToInt32(unusualCondition));
                    eventTick = 1414200 / (1 + Convert.ToInt32(unusualCondition));
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
            if (!ModsConfig.IdeologyActive)
                return;
            if (Find.IdeoManager == null || Find.IdeoManager.classicMode)
                return;
            if (wna == null || pcc == null)
                return;
            Ideo wnaIdeo = wna.ideos?.PrimaryIdeo;
            if (wnaIdeo == null)
                return;
            wnaIdeo.name = "WNA_IdeoName".Translate();
            var icon = WNAMainDefOf.WNA_IdeoIcon;
            if (icon != null)
                wnaIdeo.SetIcon(icon, WNAMainDefOf.WNA_PureBlack);
            pcc.ideos?.SetPrimary(wnaIdeo);
            var list = wnaIdeo.PreceptsListForReading;
            for (int i = list.Count - 1; i >= 0; i--)
            {
                Precept p = list[i];
                if (p.def == PreceptDefOf.IdeoBuilding)
                    wnaIdeo.RemovePrecept(p, replacing: true);
            }
            if (WNAMod.Settings.enableIdeoConflictHostility)
            {
                var pl = Faction.OfPlayerSilentFail;
                if (pl == null) return;
                Ideo plIdeo = pl.ideos?.PrimaryIdeo;
                var ext = wna.def.GetModExtension<IncompatibleIdeos>();
                bool conflict = HasIdeoConflict(plIdeo, ext);
                ApplyConflictToWna(pl, conflict);
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
        private bool HasIdeoConflict(Ideo playerIdeo, IncompatibleIdeos ext)
        {
            if (playerIdeo == null || ext == null) return false;
            if (ext.incompMemes != null)
            {
                for (int i = 0; i < ext.incompMemes.Count; i++)
                {
                    var m = ext.incompMemes[i];
                    if (m != null && playerIdeo.HasMeme(m)) return true;
                }
            }
            if (ext.incompPrecepts != null)
            {
                for (int i = 0; i < ext.incompPrecepts.Count; i++)
                {
                    var p = ext.incompPrecepts[i];
                    if (p != null && playerIdeo.HasPrecept(p)) return true;
                }
            }
            return false;
        }

        private void ApplyConflictToWna(Faction playerFaction, bool conflict)
        {
            if (wna.HasGoodwill && playerFaction.HasGoodwill)
            {
                if (conflict)
                {
                    int baseGw = wna.BaseGoodwillWith(playerFaction);
                    if (baseGw > -100)
                    {
                        wna.TryAffectGoodwillWith(
                            playerFaction,
                            -100 - baseGw,
                            canSendMessage: true,
                            canSendHostilityLetter: true,
                            reason: WNAMainDefOf.WNA_HE_ConflictIdeo
                        );
                    }
                }
                return;
            }
            if (conflict)
            {
                if (!wna.HostileTo(playerFaction))
                    wna.SetRelationDirect(playerFaction, FactionRelationKind.Hostile, canSendHostilityLetter: true);
            }
            else
            {
                if (wna.HostileTo(playerFaction))
                    wna.SetRelationDirect(playerFaction, FactionRelationKind.Neutral, canSendHostilityLetter: true);
            }
        }
        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref eventTick, "eventTick", -1);
        }
    }
}
