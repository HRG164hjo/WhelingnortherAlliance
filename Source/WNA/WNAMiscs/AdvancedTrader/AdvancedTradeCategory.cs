using RimWorld;
using System;
using System.Collections.Generic;
using Verse;
using WNA.WNAModExtension;

namespace WNA.WNAMiscs.AdvancedTrader
{
    internal enum AdvancedTradeCategory
    {
        All,
        Items,
        Buildings,
        Weapons,
        Apparel,
        Resources,
        Food,
        Drugs
    }
    internal static class AdvancedTradeCatalog
    {
        private static List<ThingDef> baseIndex;
        public static readonly AdvancedTradeCategory[] AllCategories =
        {
            AdvancedTradeCategory.All,
            AdvancedTradeCategory.Items,
            AdvancedTradeCategory.Buildings,
            AdvancedTradeCategory.Weapons,
            AdvancedTradeCategory.Apparel,
            AdvancedTradeCategory.Resources,
            AdvancedTradeCategory.Food,
            AdvancedTradeCategory.Drugs
        };

        public static int BaseCount => baseIndex?.Count ?? 0;
        public static void EnsureBaseIndex()
        {
            if (baseIndex != null)
                return;
            baseIndex = new List<ThingDef>();
            foreach (ThingDef def in DefDatabase<ThingDef>.AllDefs)
            {
                if (IsCatalogCandidate(def))
                    baseIndex.Add(def);
            }
            baseIndex.Sort((a, b) => string.Compare(a.label, b.label, StringComparison.OrdinalIgnoreCase));
        }
        public static void InvalidateBaseIndex()
        {
            baseIndex = null;
        }
        public static int Search(QuickSearchFilter filter, AdvancedTradeCategory category,
            ExtendedTrader config, List<ThingDef> outResults)
        {
            outResults.Clear();
            EnsureBaseIndex();
            for (int i = 0; i < baseIndex.Count; i++)
            {
                ThingDef def = baseIndex[i];
                if (!AllowedByConfig(def, config))
                    continue;
                if (!MatchesCategory(def, category))
                    continue;
                if (!filter.Matches(def) && !MatchesDefName(filter, def))
                    continue;
                outResults.Add(def);
            }
            return outResults.Count;
        }
        private static bool IsCatalogCandidate(ThingDef def)
        {
            if (def == null || def.label.NullOrEmpty() || def.defName.NullOrEmpty())
                return false;
            if (def.IsBlueprint || def.IsFrame || def.IsCorpse)
                return false;
            if (def == ThingDefOf.Silver)
                return false;
            if (def.race != null || typeof(Pawn).IsAssignableFrom(def.thingClass))
                return false;
            if (def == ThingDefOf.MinifiedThing || def.defName.StartsWith("Minified"))
                return false;
            if (def.category == ThingCategory.Item)
                return true;
            if (def.category == ThingCategory.Building)
                return def.Minifiable;
            return false;
        }
        private static bool MatchesDefName(QuickSearchFilter filter, ThingDef def)
        {
            if (!filter.Active || Find.HiddenItemsManager.Hidden(def))
                return false;
            string search = filter.Text.Trim();
            return !search.NullOrEmpty()
                && def.defName.IndexOf(search, StringComparison.OrdinalIgnoreCase) >= 0;
        }
        private static bool AllowedByConfig(ThingDef def, ExtendedTrader config)
        {
            if (config == null)
                return def.tradeability.TraderCanSell();
            if (!config.canBuyUntradeable && !def.tradeability.TraderCanSell())
                return false;
            if (config.forceExcludedDefs != null && config.forceExcludedDefs.Contains(def))
                return false;
            if (def.category == ThingCategory.Building && !config.allowMinifiedBuildings)
                return false;
            if ((int)def.techLevel > (int)config.maxTechLevel)
                return false;
            if (config.allowedCategories != null && config.allowedCategories.Count > 0)
            {
                for (int i = 0; i < config.allowedCategories.Count; i++)
                {
                    if (def.IsWithinCategory(config.allowedCategories[i]))
                        return true;
                }
                return false;
            }
            return true;
        }
        public static bool MatchesCategory(ThingDef def, AdvancedTradeCategory category)
        {
            switch (category)
            {
                case AdvancedTradeCategory.All:
                    return true;
                case AdvancedTradeCategory.Buildings:
                    return def.category == ThingCategory.Building;
                case AdvancedTradeCategory.Weapons:
                    return def.IsWeapon;
                case AdvancedTradeCategory.Apparel:
                    return def.IsApparel;
                case AdvancedTradeCategory.Drugs:
                    return def.IsDrug;
                case AdvancedTradeCategory.Food:
                    return def.IsNutritionGivingIngestible;
                case AdvancedTradeCategory.Resources:
                    return def.IsStuff || def.CountAsResource;
                case AdvancedTradeCategory.Items:
                    return def.category == ThingCategory.Item && !def.IsWeapon && !def.IsApparel && !def.IsDrug;
                default:
                    return true;
            }
        }
    }
}
