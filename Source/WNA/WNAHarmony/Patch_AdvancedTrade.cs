using HarmonyLib;
using RimWorld;
using Verse;
using WNA.WNAMiscs.AdvancedTrader;
using WNA.WNAUtility.AdvancedTrader;

namespace WNA.WNAHarmony
{
    internal class Patch_AdvancedTrade
    {
        [HarmonyPatch(typeof(Dialog_Trade), nameof(Dialog_Trade.PostOpen))]
        public static class Patch_Dialog_Trade_PostOpen
        {
            public static bool Prefix(Dialog_Trade __instance)
            {
                if (__instance is Dialog_AdvancedTrade)
                {
                    return true;
                }
                if (TradeSession.giftMode)
                {
                    return true;
                }
                ITrader trader = TradeSession.trader;
                if (!AdvancedTradeUtility.IsAdvancedTrader(trader))
                {
                    return true;
                }
                Pawn negotiator = TradeSession.playerNegotiator;
                if (negotiator == null)
                {
                    return true;
                }
                if (!Find.WindowStack.TryRemove(__instance, doCloseSound: false))
                {
                    return true;
                }
                Find.WindowStack.Add(new Dialog_AdvancedTrade(negotiator, trader));
                return false;
            }
        }
        [HarmonyPatch(typeof(TradeDeal), nameof(TradeDeal.Reset))]
        public static class Patch_TradeDeal_Reset
        {
            public static void Postfix()
            {
                (TradeSession.trader as TraderProxy_Advanced)?.NotifyDealReset();
            }
        }
    }
}
