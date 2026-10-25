using RimWorld;
using RimWorld.Planet;
using System.Collections.Generic;
using Verse;
using WNA.WNADefOf;
using WNA.WNAThingClass;

namespace WNA.WNAUtility
{
    public static class General
    {
        internal static Faction WNAFaction => Find.FactionManager.FirstFactionOfDef(WNAMainDefOf.WNA_FactionWNA);
        internal static Faction PCCFaction => Find.FactionManager.FirstFactionOfDef(WNAMainDefOf.WNA_FactionPCC);
        internal static Ideo WNAIdeo => WNAFaction?.ideos?.PrimaryIdeo;
        internal static readonly HashSet<string> listedHediffs = new HashSet<string>()
        {
            // Anomaly
            "DarkPsychicShock",
            "CubeInterest",
            "CubeWithdrawal",
            "MetalhorrorImplant",
            // Main
            "WNA_ChronoCounter",
        };
        // Thoughts explicitly exempted from the apathy gene's mood nullification.
        internal static readonly HashSet<string> apathyMoodWhitelist = new HashSet<string>()
        {
            "WNA_ThoughtApathy",
        };
        internal static bool HasApathyGene(Pawn pawn)
        {
            return pawn?.genes != null && pawn.genes.HasActiveGene(WNAMainDefOf.WNA_Apathy);
        }
        internal static bool ApathyMoodNullified(Pawn pawn, ThoughtDef def)
        {
            return HasApathyGene(pawn) && !apathyMoodWhitelist.Contains(def?.defName);
        }
        internal static bool IsWNARace(Pawn pawn)
        {
            return pawn?.def == WNAMainDefOf.WNA_Whelingnorthan || pawn?.def == WNAMainDefOf.WNA_Humanoid || pawn is ThingClass_Whelingnorthan || pawn is ThingClass_Humanoid;
        }
        internal static bool IsWildWNAPawn(Pawn pawn)
        {
            return IsWNARace(pawn)
                && !(pawn.Faction == General.WNAFaction || pawn.Faction == General.PCCFaction);
        }
        internal static bool IsInternalFaction(Faction faction)
        {
            return faction != null
                && (faction.def == WNAMainDefOf.WNA_FactionWNA || faction.def == WNAMainDefOf.WNA_FactionPCC);
        }
        internal static bool PawnHasIdeo(Pawn pawn, bool strict = false)
        {
            return strict? (pawn?.Ideo == WNAIdeo) : (pawn?.Ideo?.HasPrecept(WNAMainDefOf.WNA_Proselyte) == true);
        }
        internal static bool FactionHasIdeo(Faction faction, bool strict = false)
        {
            return  strict? (faction.ideos?.PrimaryIdeo == WNAIdeo) : (faction.ideos?.PrimaryIdeo?.HasPrecept(WNAMainDefOf.WNA_Proselyte) == true);
        }
        internal static bool ConsideredBetrayer(Pawn pawn)
        {
            if (pawn == null || pawn.DestroyedOrNull())
                return false;
            if (!ModsConfig.IdeologyActive)
                return false;
            if (Find.IdeoManager == null || Find.IdeoManager.classicMode)
                return false;
            Ideo wnaIdeo = WNAIdeo;
            if (wnaIdeo == null)
                return false;
            if (pawn.Spawned || pawn.IsWorldPawn())
            {
                if (pawn.Faction != null)
                {
                    if (IsInternalFaction(pawn.Faction) && pawn.Ideo == wnaIdeo)
                        return false;
                    if (pawn.Faction == Faction.OfPlayer && pawn.Ideo?.HasPrecept(WNAMainDefOf.WNA_Proselyte) == true)
                        return false;
                    return true;
                }
                return true;
            }
            return true;
        }
        internal static void DevDestroy(Thing thing, DestroyMode mode = DestroyMode.Vanish)
        {
            Thing.allowDestroyNonDestroyable = true;
            try
            {
                thing.Destroy(mode);
            }
            finally
            {
                Thing.allowDestroyNonDestroyable = false;
            }
        }
        internal static void TotalRemoving(Pawn pawn, bool remains = false)
        {
            if (pawn == null || pawn.DestroyedOrNull())
                return;
            pawn.relations?.ClearAllRelations();
            pawn.ownership?.UnclaimAll();
            HediffRemoveAll(pawn, false);
            if (remains)
            {
                pawn.apparel?.DropAll(pawn.Position);
                pawn.inventory?.DropAllNearPawn(pawn.Position);
                pawn.equipment?.DropAllEquipment(pawn.Position);
            }
            else
            {
                pawn.apparel?.DestroyAll();
                pawn.inventory?.DestroyAll();
                pawn.equipment?.DestroyAllEquipment();
            }
            DevDestroy(pawn);
            if (pawn.IsWorldPawn())
                Find.WorldPawns.RemoveAndDiscardPawnViaGC(pawn);
        }
        internal static void HediffRemoveAll(Pawn pawn, bool removeBad = false)
        {
            if (pawn == null || pawn.DestroyedOrNull())
                return;
            var hediffs = pawn.health?.hediffSet?.hediffs;
            if (hediffs == null)
                return;
            var li = new List<Hediff>(hediffs);
            for (int i = 0; i < li.Count; i++)
            {
                Hediff hediff = li[i];
                if (hediff == null || !hediffs.Contains(hediff))
                    continue;
                if (removeBad)
                {
                    if (hediff.def.isBad
                        || hediff is Hediff_Injury
                        || hediff is Hediff_MissingPart
                        || listedHediffs.Contains(hediff.def.defName))
                        pawn.health.RemoveHediff(hediff);
                }
                else
                {
                    if (!hediff.def.isBad
                        && !(hediff is Hediff_Injury)
                        && !(hediff is Hediff_MissingPart)
                        && !listedHediffs.Contains(hediff.def.defName))
                        pawn.health.RemoveHediff(hediff);
                }
            }
        }
    }
}
