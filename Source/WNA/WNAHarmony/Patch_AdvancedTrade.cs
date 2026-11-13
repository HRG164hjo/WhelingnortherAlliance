using HarmonyLib;
using RimWorld;
using System.Collections.Generic;
using UnityEngine;
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
        [HarmonyPatch(typeof(Tradeable), nameof(Tradeable.CountHeldBy))]
        public static class Patch_Tradeable_CountHeldBy
        {
            public static void Postfix(Tradeable __instance, Transactor trans, ref int __result)
            {
                if (trans != Transactor.Trader)
                {
                    return;
                }
                BuyLine line = CartLineOf(__instance);
                if (line != null)
                {
                    __result = line.Cap;
                }
            }
        }
        [HarmonyPatch(typeof(Tradeable), nameof(Tradeable.ResolveTrade))]
        public static class Patch_Tradeable_ResolveTrade
        {
            public static void Prefix(Tradeable __instance)
            {
                if (__instance.ActionToDo != TradeAction.PlayerBuys)
                {
                    return;
                }
                BuyLine line = CartLineOf(__instance);
                if (line == null)
                {
                    return;
                }
                int count = __instance.CountToTransferToSource;
                if (count <= 0)
                {
                    return;
                }
                // Materialize only at execution so the quantity field can offer the full cap cheaply.
                line.Materialize(count);
                List<Thing> traderThings = __instance.thingsTrader;
                List<Thing> lineThings = line.things;
                for (int i = Mathf.Min(traderThings.Count, lineThings.Count); i < lineThings.Count; i++)
                {
                    traderThings.Add(lineThings[i]);
                }
            }
        }
        private static BuyLine CartLineOf(Tradeable tradeable)
        {
            if (!TradeSession.Active || !(TradeSession.trader is TraderProxy_Advanced proxy))
            {
                return null;
            }
            return proxy.cart.LineOf(tradeable.FirstThingTrader);
        }
    }
}
