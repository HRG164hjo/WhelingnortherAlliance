using HarmonyLib;
using RimWorld;
using RimWorld.Planet;
using Verse;
using WNA.WNAUtility;
using WNA.WNAWorldComp;

namespace WNA.WNAHarmony
{
    // While the player's ideoligion conflicts with WNA's, WNA is to behave like a
    // permanent enemy: no gifting offered and no goodwill gain of any kind.
    internal class WNA_IdeoConflictHostility
    {
        private static bool IsBlockedPair(Faction a, Faction b)
        {
            if (!WorldComp_WorldSync.IdeoConflictActive)
                return false;
            Faction wna = General.WNAFaction;
            if (wna == null)
                return false;
            return (a == wna && b != null && b.IsPlayer)
                || (b == wna && a != null && a.IsPlayer);
        }
        [HarmonyPatch(typeof(CaravanArrivalAction_OfferGifts), nameof(CaravanArrivalAction_OfferGifts.CanOfferGiftsTo))]
        internal static class Patch_CanOfferGiftsTo
        {
            private static void Postfix(Settlement settlement, ref FloatMenuAcceptanceReport __result)
            {
                if (!__result)
                    return;
                if (IsBlockedPair(settlement?.Faction, Faction.OfPlayerSilentFail))
                    __result = FloatMenuAcceptanceReport.WithFailReason("WNA_GiftBlockedIdeoConflict".Translate());
            }
        }
        [HarmonyPatch(typeof(Faction), nameof(Faction.CanChangeGoodwillFor))]
        internal static class Patch_CanChangeGoodwillFor
        {
            private static void Postfix(Faction __instance, Faction other, int goodwillChange, ref bool __result)
            {
                if (!__result || goodwillChange <= 0)
                    return;
                if (IsBlockedPair(__instance, other))
                    __result = false;
            }
        }
    }
}
