using RimWorld;
using RimWorld.Planet;
using System.Collections.Generic;
using Verse;
using WNA.WNADefOf;
using WNA.WNAModExtension;
using static WNA.WNAUtility.General;

namespace WNA.WNAWorldComp
{
    internal class WorldComp_WorldSync : WorldComponent
    {
        public WorldComp_WorldSync(World world) : base(world){}
        private int eventTick = 1414200;
        private int duration = -1;
        private bool syncPending;
        private bool ideoConflict;
        internal bool EventActive
        {
            get
            {
                if(duration >= 0)
                    return true;
                return false;
            }
        }
        internal static bool IdeoConflictActive
        {
            get
            {
                WorldComp_WorldSync comp = Find.World?.GetComponent<WorldComp_WorldSync>();
                return comp != null && comp.ideoConflict;
            }
        }
        public override void WorldComponentTick()
        {
            base.WorldComponentTick();
            if (syncPending)
            {
                syncPending = false;
                IdeoSync();
                RelationSync();
            }
            if (Find.TickManager.TicksGame == 23
                || Find.TickManager.TicksGame == 235)
            {
                IdeoSync();
                RelationSync();
            }
            if (Find.TickManager.TicksGame % 20 == 0 && duration >= 0)
                duration -= 20;
            if (Find.TickManager.TicksGame % 2357 == 0)
            {
                IdeoSync();
                RelationSync();
            }
            if (Find.TickManager.TicksGame % 43 == 0)
            {
                HediffSync();
                List<Pawn> pawns = PawnsFinder.AllMapsWorldAndTemporary_Alive;
                for (int i = 0; i < pawns.Count; i++)
                    ProcessWildWNAPawn(pawns[i]);
            }
            if (Find.TickManager.TicksGame % 200 == 0)
            {
                int playerHostile = (WNAFaction != null && Faction.OfPlayer.HostileTo(WNAFaction)) ? 1 : 0;
                eventTick -= 200 * (1 + playerHostile);
                if (eventTick <= 0)
                {
                    TriggerEvent();
                    duration = 141420 * (1 + playerHostile);
                    eventTick = 1414200 / (1 + playerHostile);
                }
            }
        }
        public override void FinalizeInit(bool fromLoad)
        {
            base.FinalizeInit(fromLoad);
            syncPending = true;
        }
        private static void ProcessWildWNAPawn(Pawn pawn)
        {
            if (WNAIdeo == null)
                return;
            if (IsWildWNAPawn(pawn))
            {
                pawn.ideo.SetIdeo(WNAIdeo);
                if (pawn.Faction != Faction.OfPlayer)
                    if (pawn.Faction != WNAFaction && pawn.Faction != PCCFaction)
                        pawn.SetFaction(WNAFaction);
                    else pawn.stances.stunner.StunFor(2357, null, false);
            }
        }
        private void IdeoSync()
        {
            if (!ModsConfig.IdeologyActive)
                return;
            if (Find.IdeoManager == null || Find.IdeoManager.classicMode)
                return;
            if (WNAFaction == null || PCCFaction == null || WNAIdeo == null)
                return;
            WNAIdeo.name = "WNA_IdeoName".Translate();
            var icon = WNAMainDefOf.WNA_IdeoIcon;
            ColorDef color = DefDatabase<ColorDef>.GetNamed("Black", false)
                ?? DefDatabase<ColorDef>.GetNamed("DarkBlack", false);
            if (icon != null)
                WNAIdeo.SetIcon(icon, color);
            var list = WNAIdeo.PreceptsListForReading;
            for (int i = list.Count - 1; i >= 0; i--)
            {
                Precept p = list[i];
                if (p.def == PreceptDefOf.IdeoBuilding)
                    WNAIdeo.RemovePrecept(p, replacing: true);
            }
            SyncPCCIdeo();
            SyncConflictState();
            if (!WNAIdeo.HasPrecept(WNAMainDefOf.WNA_Proselyte))
            {
                WNAIdeo.ClearPrecepts();
                WNAIdeo.AddPrecept(PreceptMaker.MakePrecept(WNAMainDefOf.WNA_Proselyte), init: true);
                foreach (var li in WNAMainDefOf.Structure_WNA_Ideo.requireOne)
                {
                    PreceptDef p = li[0];
                    WNAIdeo.AddPrecept(PreceptMaker.MakePrecept(p), init: true);
                }
            }
            if (!WNAIdeo.HasPrecept(PreceptDefOf.RoleChange))
            {
                WNAIdeo.AddPrecept(
                    PreceptMaker.MakePrecept(PreceptDefOf.RoleChange),
                    init: true,
                    fillWith: PreceptDefOf.RoleChange.ritualPatternBase);
            }
            EnsureUplifting(WNAIdeo);
            EnsureAbilityGrant(WNAIdeo);
            Faction playerFaction = Faction.OfPlayerSilentFail;
            if (playerFaction?.ideos != null)
            {
                foreach (Ideo playerIdeo in playerFaction.ideos.AllIdeos)
                {
                    EnsureUplifting(playerIdeo);
                    EnsureAbilityGrant(playerIdeo);
                }
            }
        }
        private static void EnsureUplifting(Ideo ideo)
        {
            if (ideo == null || WNAMainDefOf.WNA_Uplifting == null)
                return;
            if (ideo.HasPrecept(WNAMainDefOf.WNA_Uplifting))
                return;
            ideo.AddPrecept(
                PreceptMaker.MakePrecept(WNAMainDefOf.WNA_Uplifting),
                init: true,
                fillWith: WNAMainDefOf.WNA_Uplifting.ritualPatternBase);
        }
        private static void EnsureAbilityGrant(Ideo ideo)
        {
            if (ideo == null || WNAMainDefOf.WNA_AbilityGrant == null)
                return;
            if (ideo.HasPrecept(WNAMainDefOf.WNA_AbilityGrant))
                return;
            ideo.AddPrecept(
                PreceptMaker.MakePrecept(WNAMainDefOf.WNA_AbilityGrant),
                init: true,
                fillWith: WNAMainDefOf.WNA_AbilityGrant.ritualPatternBase);
        }
        private static void SyncPCCIdeo()
        {
            if (PCCFaction?.ideos == null || WNAIdeo == null)
                return;
            if (PCCFaction.ideos.PrimaryIdeo == WNAIdeo)
                return;
            PCCFaction.ideos.SetPrimary(WNAIdeo);
        }
        private void SyncConflictState()
        {
            Faction plfac = Faction.OfPlayerSilentFail;
            ideoConflict = WNAMod.Settings.enableIdeoConflictHostility
                && plfac != null
                && HasIdeoConflict(plfac.ideos?.PrimaryIdeo, IncompatibleIdeos.Get(WNAFaction.def));
            if (plfac != null)
                ApplyConflictToWNA(plfac, ideoConflict);
        }
        private static bool HasIdeoConflict(Ideo playerIdeo, IncompatibleIdeos ext)
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
        private static void ApplyConflictToWNA(Faction plfac, bool conflict)
        {
            if (WNAFaction.HasGoodwill && plfac.HasGoodwill)
            {
                if (conflict)
                {
                    int baseGw = WNAFaction.BaseGoodwillWith(plfac);
                    if (baseGw > -100)
                    {
                        WNAFaction.TryAffectGoodwillWith(
                            plfac,
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
                if (!WNAFaction.HostileTo(plfac))
                    WNAFaction.SetRelationDirect(plfac, FactionRelationKind.Hostile, canSendHostilityLetter: true);
            }
            else
            {
                if (WNAFaction.HostileTo(plfac))
                    WNAFaction.SetRelationDirect(plfac, FactionRelationKind.Neutral, canSendHostilityLetter: true);
            }
        }
        private static void RelationSync()
        {
            if (WNAFaction == null || PCCFaction == null)
                return;
            FactionRelationKind relationwna = WNAFaction.RelationWith(Faction.OfPlayer).kind;
            FactionRelation relationpcc = PCCFaction.RelationWith(Faction.OfPlayer);
            if (PCCFaction.HostileTo(WNAFaction))
                PCCFaction.SetRelationDirect(WNAFaction, FactionRelationKind.Ally, false);
            if (relationwna == FactionRelationKind.Hostile)
            {
                if (relationpcc.kind != FactionRelationKind.Hostile)
                {
                    string str = "WNA_LordHostility".Translate();
                    PCCFaction.SetRelationDirect(Faction.OfPlayer, FactionRelationKind.Hostile, true, str);
                }
            }
            else if (relationwna == FactionRelationKind.Ally)
            {
                if (relationpcc.kind != FactionRelationKind.Neutral)
                {
                    string str = "WNA_LordNeutralized".Translate();
                    PCCFaction.SetRelationDirect(Faction.OfPlayer, FactionRelationKind.Neutral, true, str);
                }
            }
        }
        private static void HediffSync()
        {
            if (PCCFaction == null)
                return;
            foreach (Map map in Find.Maps)
            {
                var pccPawns = map.mapPawns.SpawnedPawnsInFaction(PCCFaction);
                foreach (Pawn p in pccPawns)
                {
                    if (p.Dead || p.health == null)
                        continue;
                    if (!p.health.hediffSet.HasHediff(WNAMainDefOf.WNA_PermaconstActive))
                        p.health.AddHediff(WNAMainDefOf.WNA_PermaconstActive);
                }
            }
        }
        private static void TriggerEvent()
        {
            Map map = Find.AnyPlayerHomeMap;
            if (map == null)
                return;
            IncidentDef incident = WNAMainDefOf.WNA_Incident_Permaconst;
            IncidentParms parms = StorytellerUtility.DefaultParmsNow(incident.category, map);
            incident.Worker.TryExecute(parms);
        }
        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref eventTick, "eventTick", -1);
            Scribe_Values.Look(ref duration, "duration", -1);
            Scribe_Values.Look(ref ideoConflict, "ideoConflict", false);
        }
    }
}
