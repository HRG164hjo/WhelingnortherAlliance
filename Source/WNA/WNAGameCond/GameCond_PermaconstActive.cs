using RimWorld;
using System.Collections.Generic;
using Verse;
using WNA.WNADefOf;

namespace WNA.WNAGameCond
{
    public class GameCond_PermaconstActive : GameCondition
    {
        private int interval = 236;
        private Faction pcc => Find.FactionManager.FirstFactionOfDef(WNAMainDefOf.WNA_FactionPCC);
        private Faction wna => Find.FactionManager.FirstFactionOfDef(WNAMainDefOf.WNA_FactionWNA);
        private Faction player => Faction.OfPlayer;
        public override void GameConditionTick()
        {
            base.GameConditionTick();
            if (Find.TickManager.TicksGame % interval == 0)
            {
                foreach (Map map in AffectedMaps)
                {
                    PermaconstBroadcast(map);
                }
            }
        }
        private void PermaconstBroadcast(Map map)
        {
            if (pcc == null) return;
            List<Pawn> allPawns = (List<Pawn>)map.mapPawns.AllPawnsSpawned;
            for (int i = allPawns.Count - 1; i >= 0; i--)
            {
                Pawn p = allPawns[i];
                if (p.Dead || p.health == null) continue;
                Hediff hidden = p.health.hediffSet.GetFirstHediffOfDef(WNAMainDefOf.WNA_PermaconstHidden);
                Hediff active = p.health.hediffSet.GetFirstHediffOfDef(WNAMainDefOf.WNA_PermaconstActive);
                if (hidden != null)
                {
                    BroadcastAlive(p, pcc);
                    p.health.RemoveHediff(hidden);
                    p.health.AddHediff(WNAMainDefOf.WNA_PermaconstActive);
                }
                if (active != null)
                    BroadcastAlive(p, pcc);
                if (p?.Faction?.def == WNAMainDefOf.WNA_FactionPCC ||
                    p?.Faction?.def == WNAMainDefOf.WNA_FactionWNA)
                    RemoveTargetHediffs(p);
            }
            List<Thing> corpses = map.listerThings.ThingsInGroup(ThingRequestGroup.Corpse);
            for (int j = corpses.Count - 1; j >= 0; j--)
            {
                if (!(corpses[j] is Corpse corpse) || corpse.InnerPawn == null || corpse.InnerPawn.health == null) continue;
                bool hasHidden = corpse.InnerPawn.health.hediffSet.HasHediff(WNAMainDefOf.WNA_PermaconstHidden);
                bool hasActive = corpse.InnerPawn.health.hediffSet.HasHediff(WNAMainDefOf.WNA_PermaconstActive);
                if (hasHidden || hasActive)
                    BroadcastDead(corpse, pcc);
            }
        }
        private void BroadcastAlive(Pawn p, Faction pcc)
        {
            if (IsLegalWNAPawn(p)) return;
            if (p.Faction != pcc && p.Faction != wna)
            {
                bool hiss = p.Faction == Faction.OfPlayer;
                p.SetFaction(pcc);
                p?.ideo?.SetIdeo(pcc.ideos.PrimaryIdeo);
                if (hiss && pcc.RelationWith(player).kind != FactionRelationKind.Hostile)
                {
                    string str = "WNA_Hiss".Translate();
                    pcc.SetRelationDirect(player, FactionRelationKind.Hostile, true, str);
                    Messages.Message("WNA_Message_ColonistTurned".Translate(p.LabelShort), p, MessageTypeDefOf.NegativeEvent);
                }
            }
        }
        private void BroadcastDead(Corpse corpse, Faction pcc)
        {
            Pawn pawn = corpse.InnerPawn;
            ResurrectionUtility.TryResurrect(pawn);
            if (!pawn.health.hediffSet.HasHediff(WNAMainDefOf.WNA_PermaconstHidden))
            {
                Hediff hidden = pawn.health.hediffSet.GetFirstHediffOfDef(WNAMainDefOf.WNA_PermaconstHidden);
                if (hidden != null)
                    pawn.health.RemoveHediff(hidden);
                pawn.health.AddHediff(WNAMainDefOf.WNA_PermaconstActive);
            }
            if (!pawn.health.hediffSet.HasHediff(WNAMainDefOf.WNA_PermaconstActive))
                pawn.health.AddHediff(WNAMainDefOf.WNA_PermaconstActive);
            if (pawn.Faction != pcc && pawn.Faction != wna)
                pawn.SetFaction(pcc);
        }
        private void RemoveTargetHediffs(Pawn pawn)
        {
            HediffSet hediffSet = pawn.health.hediffSet;
            List<Hediff> hediffsToRemove = new List<Hediff>();
            foreach (Hediff hediff in hediffSet.hediffs)
            {
                if (hediff is Hediff_Injury ||
                    hediff is Hediff_MissingPart ||
                    (hediff.Severity > 0 &&
                    hediff.def.isBad))
                    hediffsToRemove.Add(hediff);
            }
            foreach (Hediff hediff in hediffsToRemove)
                pawn.health.RemoveHediff(hediff);
        }
        private bool IsLegalWNAPawn(Pawn p)
        {
            if (p.def == WNAMainDefOf.WNA_WNThan || p.def == WNAMainDefOf.WNA_Human)
                return true;
            return false;
        }
    }
}
