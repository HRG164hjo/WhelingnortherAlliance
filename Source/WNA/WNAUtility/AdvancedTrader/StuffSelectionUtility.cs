using RimWorld;
using System;
using System.Collections.Generic;
using Verse;
using WNA.WNAMiscs.AdvancedTrader;

namespace WNA.WNAUtility.AdvancedTrader
{
    internal class StuffSelectionUtility
    {
        private static readonly List<ThingDef> EmptyOptions = new List<ThingDef>();
        private static readonly Dictionary<ThingDef, List<ThingDef>> optionsCache = new Dictionary<ThingDef, List<ThingDef>>();
        private static readonly Dictionary<ThingDef, ThingDef> defaultCache = new Dictionary<ThingDef, ThingDef>();
        public static void BeginSession()
        {
            optionsCache.Clear();
            defaultCache.Clear();
        }
        public static void EndSession()
        {
            optionsCache.Clear();
            defaultCache.Clear();
        }
        public static List<ThingDef> OptionsFor(ThingDef def, AdvancedTradeContext context)
        {
            if (def == null || !def.MadeFromStuff)
                return EmptyOptions;
            if (optionsCache.TryGetValue(def, out List<ThingDef> cached))
                return cached;
            List<ThingDef> list = new List<ThingDef>();
            TechLevel maxTech = context?.MaxTechLevel ?? TechLevel.Undefined;
            foreach (ThingDef stuff in GenStuff.AllowedStuffsFor(def, maxTech))
            {
                if (stuff != null && !list.Contains(stuff))
                    list.Add(stuff);
            }
            list.Sort((a, b) => string.Compare(a.LabelCap.RawText, b.LabelCap.RawText, StringComparison.Ordinal));
            optionsCache[def] = list;
            return list;
        }
        public static bool HasStuffChoice(ThingDef def, AdvancedTradeContext context)
        {
            if (def == null || !def.MadeFromStuff)
                return false;
            if (context != null && !context.AllowStuffChoice)
                return false;
            return OptionsFor(def, context).Count > 1;
        }
        public static ThingDef DefaultStuff(ThingDef def, AdvancedTradeContext context)
        {
            if (def == null || !def.MadeFromStuff)
                return null;
            if (defaultCache.TryGetValue(def, out ThingDef cached))
                return cached;
            ThingDef result = GenStuff.DefaultStuffFor(def);
            List<ThingDef> options = OptionsFor(def, context);
            if (result == null || !options.Contains(result))
                result = (options.Count > 0) ? options[0] : null;
            defaultCache[def] = result;
            return result;
        }
        public static ThingDef Normalize(ThingDef def, ThingDef stuff, AdvancedTradeContext context)
        {
            if (def == null || !def.MadeFromStuff)
                return null;
            if (stuff != null)
            {
                List<ThingDef> options = OptionsFor(def, context);
                for (int i = 0; i < options.Count; i++)
                {
                    if (options[i] == stuff)
                        return stuff;
                }
            }
            return DefaultStuff(def, context);
        }
        public static void OpenStuffMenu(ThingDef def, ThingDef current, AdvancedTradeContext context, Action<ThingDef> onPicked)
        {
            List<ThingDef> options = OptionsFor(def, context);
            if (options.Count == 0 || onPicked == null)
                return;
            List<FloatMenuOption> menuOptions = new List<FloatMenuOption>(options.Count);
            for (int i = 0; i < options.Count; i++)
            {
                ThingDef stuff = options[i];
                string label = stuff.LabelCap.RawText;
                if (stuff == current)
                    label += "  " + "ATUI_StuffCurrent".Translate();
                float price = AdvancedTradeUtility.UnitBuyPrice(def, stuff, context);
                if (price > 0f)
                    label += "   " + price.ToStringMoney();
                menuOptions.Add(new FloatMenuOption(label, delegate{onPicked(stuff);}, stuff));
            }
            Find.WindowStack.Add(new FloatMenu(menuOptions));
        }
    }
}
