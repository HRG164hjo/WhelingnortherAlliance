using System.Collections.Generic;
using RimWorld;
using RimWorld.Planet;
using Verse;
using WNA.WNADefOf;
using WNA.WNAWorldComp;

namespace WNA.WNAUtility
{
    public static class WNAFactionDefeatUtility
    {
        public static bool IsApplicable(Faction faction)
        {
            if (faction == null || faction.IsPlayer || faction.Hidden || faction.temporary)
                return false;
            FactionDef def = faction.def;
            return def != WNAMainDefOf.WNA_FactionWNA && def != WNAMainDefOf.WNA_FactionPCC;
        }

        public static bool IsLockedDefeated(Faction faction)
        {
            return faction != null && faction.defeated && IsApplicable(faction);
        }

        public static void TryMarkDefeated(Faction faction, PlanetTile atTile)
        {
            if (!IsApplicable(faction) || faction.defeated)
                return;
            if (HasAnySettlement(faction))
                return;
            faction.defeated = true;
            HandleDefeat(faction, atTile, announce: true);
        }

        public static void HandleDefeat(Faction faction, PlanetTile atTile, bool announce)
        {
            if (Find.QuestManager.IsReservedByAnyQuest(faction))
                return;
            WorldComp_FactionDefeat comp = Find.World?.GetComponent<WorldComp_FactionDefeat>();
            if (comp != null && !comp.TryClaim(faction))
                return;
            ClearRuinOwnership(faction);
            ForceNeutralRelations(faction);
            if (announce)
                SendDefeatLetter(faction, atTile);
        }

        private static bool HasAnySettlement(Faction faction)
        {
            List<Settlement> settlements = Find.WorldObjects.Settlements;
            for (int i = 0; i < settlements.Count; i++)
            {
                if (settlements[i] != null && settlements[i].Faction == faction)
                    return true;
            }
            return false;
        }

        private static void ClearRuinOwnership(Faction faction)
        {
            // Destroy() and SetFaction mutate the cache, so walk a snapshot.
            List<DestroyedSettlement> ruins = new List<DestroyedSettlement>(Find.WorldObjects.DestroyedSettlements);
            for (int i = 0; i < ruins.Count; i++)
            {
                DestroyedSettlement ruin = ruins[i];
                if (ruin == null || ruin.Destroyed || ruin.Faction != faction)
                    continue;
                ruin.SetFaction(null);
            }
        }

        private static void ForceNeutralRelations(Faction faction)
        {
            List<Faction> others = Find.FactionManager.AllFactionsListForReading;
            for (int i = 0; i < others.Count; i++)
            {
                Faction other = others[i];
                if (other == null || other == faction)
                    continue;
                SetNeutral(faction, other);
            }
            Find.GoodwillSituationManager.RecalculateAll(canSendHostilityChangedLetter: false);
        }

        private static void SetNeutral(Faction a, Faction b)
        {
            if (!a.HasGoodwill || !b.HasGoodwill)
            {
                a.SetRelationDirect(b, FactionRelationKind.Neutral, canSendHostilityLetter: false);
                return;
            }
            FactionRelation forward = a.RelationWith(b, allowNull: true);
            FactionRelation backward = b.RelationWith(a, allowNull: true);
            if (forward == null && backward == null)
            {
                a.TryMakeInitialRelationsWith(b);
                forward = a.RelationWith(b, allowNull: true);
                backward = b.RelationWith(a, allowNull: true);
            }
            if (forward != null)
            {
                forward.kind = FactionRelationKind.Neutral;
                forward.baseGoodwill = 0;
            }
            if (backward != null)
            {
                backward.kind = FactionRelationKind.Neutral;
                backward.baseGoodwill = 0;
            }
        }

        private static void SendDefeatLetter(Faction faction, PlanetTile atTile)
        {
            TaggedString text = "WNA_FactionDefeated_Text".Translate(faction.NameColored);
            if (atTile.Valid)
                Find.LetterStack.ReceiveLetter("WNA_FactionDefeated_Label".Translate(), text, LetterDefOf.PositiveEvent, new LookTargets(atTile), faction);
            else
                Find.LetterStack.ReceiveLetter("WNA_FactionDefeated_Label".Translate(), text, LetterDefOf.PositiveEvent, null, faction);
        }
    }
}
