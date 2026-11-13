using RimWorld;
using RimWorld.Planet;
using System;
using System.Collections.Generic;
using Verse;
using Waylith.Math;
using WNA.WNAModExtension;
using WNA.WNAUtility.AdvancedTrader;

namespace WNA.WNAMiscs.AdvancedTrader
{
    internal class AdvancedTradeCart
    {
        private readonly List<BuyLine> lines = new List<BuyLine>();
        private readonly AdvancedTradeContext context;
        public AdvancedTradeCart(AdvancedTradeContext context)
        {
            this.context = context;
        }
        public AdvancedTradeContext Context => context;
        public List<BuyLine> Lines => lines;
        public bool Empty => lines.Count == 0;
        public BuyLine Find(ThingDef def, ThingDef stuff)
        {
            for (int i = 0; i < lines.Count; i++)
            {
                if (lines[i].def == def && lines[i].stuff == stuff)
                    return lines[i];
            }
            return null;
        }
        public bool HasLineFor(ThingDef def, ThingDef stuff)
        {
            return Find(def, stuff) != null;
        }
        public BuyLine Add(ThingDef def, int count = -1)
        {
            ThingDef stuff = StuffSelectionUtility.DefaultStuff(def, context);
            return Add(def, stuff, count);
        }
        public BuyLine Add(ThingDef def, ThingDef stuff, int count = -1)
        {
            stuff = StuffSelectionUtility.Normalize(def, stuff, context);
            if (count < 0)
                count = DefaultCountFor(def);
            BuyLine line = Find(def, stuff);
            if (line != null)
            {
                line.SetCount(line.Count + count);
                return line;
            }
            line = new BuyLine(def, stuff, count, context);
            if (line.IsEmpty)
                return null;
            lines.Add(line);
            return line;
        }
        public void Remove(BuyLine line)
        {
            if (line == null || !lines.Remove(line))
                return;
            line.ClearThings();
        }
        public BuyLine ChangeStuff(BuyLine line, ThingDef newStuff)
        {
            if (line == null)
                return null;
            newStuff = StuffSelectionUtility.Normalize(line.def, newStuff, context);
            if (line.stuff == newStuff)
                return line;
            BuyLine target = Find(line.def, newStuff);
            if (target != null)
            {
                int count = line.Count;
                Remove(line);
                target.SetCount(target.Count + count);
                return target;
            }
            line.ChangeStuff(newStuff);
            return line;
        }
        public void Clear()
        {
            for (int i = 0; i < lines.Count; i++)
                lines[i].ClearThings();
            lines.Clear();
        }
        public IEnumerable<Thing> AllThings()
        {
            for (int i = 0; i < lines.Count; i++)
            {
                List<Thing> ts = lines[i].things;
                for (int j = 0; j < ts.Count; j++)
                    yield return ts[j];
            }
        }
        public BuyLine LineOf(Thing thing)
        {
            for (int i = 0; i < lines.Count; i++)
            {
                List<Thing> ts = lines[i].things;
                for (int j = 0; j < ts.Count; j++)
                {
                    if (ReferenceEquals(ts[j], thing))
                    {
                        return lines[i];
                    }
                }
            }
            return null;
        }
        public bool Owns(Thing thing)
        {
            return LineOf(thing) != null;
        }
        private int DefaultCountFor(ThingDef def)
        {
            if (def.Minifiable)
                return config_defaultMinifiedCount;
            if (def.stackLimit > 1)
                return Mathf.Clamp(config_defaultStackableCount, 1, def.stackLimit);
            return 1;
        }
        private int config_defaultMinifiedCount => context.config?.defaultMinifiedCount ?? 1;
        private int config_defaultStackableCount => context.config?.defaultStackableCount ?? 25;
    }
    internal class BuyLine
    {
        public readonly ThingDef def;
        public ThingDef stuff;
        public readonly List<Thing> things = new List<Thing>();
        private readonly AdvancedTradeContext context;
        private int countInt;
        public BuyLine(ThingDef def, ThingDef stuff, int count, AdvancedTradeContext context)
        {
            this.def = def;
            this.stuff = stuff;
            this.context = context;
            SetCount(count);
        }
        public int Count => countInt;
        public bool IsEmpty => countInt <= 0 || things.Count == 0;
        public bool IsStackable => def.stackLimit > 1 && !def.Minifiable;
        public int Cap => context.MaxStackable;
        public Thing Representative => (things.Count > 0) ? things[0] : null;
        public string Label
        {
            get
            {
                Thing rep = Representative;
                if (rep != null)
                {
                    return rep.LabelNoCount;
                }
                return (stuff != null) ? def.LabelCap + " (" + stuff.LabelCap + ")" : def.LabelCap;
            }
        }
        public void SetCount(int value)
        {
            countInt = Mathf.Clamp(value, 0, Cap);
            EnsureCapacity(countInt);
        }
        public void SyncCountFrom(int value)
        {
            countInt = Mathf.Clamp(value, 0, Cap);
        }

        public void Materialize(int count)
        {
            EnsureCapacity(Mathf.Clamp(count, 0, Cap));
        }

        private void EnsureCapacity(int value)
        {
            if (value == 0)
            {
                things.Clear();
                return;
            }
            if (IsStackable)
            {
                if (things.Count == 0)
                    things.Add(CreateInstance(Cap));
                else
                    things[0].stackCount = Cap;
                return;
            }
            while (things.Count < value && things.Count < Cap)
                things.Add(CreateInstance(1));
        }
        public void ChangeStuff(ThingDef newStuff)
        {
            if (stuff == newStuff)
                return;
            stuff = newStuff;
            things.Clear();
            EnsureCapacity(countInt);
        }
        public void ClearThings()
        {
            things.Clear();
            countInt = 0;
        }
        private Thing CreateInstance(int stackCount)
        {
            Thing thing = ThingMaker.MakeThing(def, stuff);
            thing.stackCount = stackCount;
            thing.PostGeneratedForTrader(context.traderKind, context.Tile, context.faction);
            if (context.UseFixedQuality)
            {
                CompQuality quality = thing.TryGetComp<CompQuality>();
                if (quality != null)
                    quality.SetQuality(context.FixedQuality, ArtGenerationContext.Outsider);
            }
            return thing.TryMakeMinified();
        }
    }
    internal class AdvancedTradeContext
    {
        private readonly ITrader trader;

        public readonly TraderKindDef traderKind;

        public readonly Faction faction;

        public readonly ExtendedTrader config;

        private PlanetTile? tileCache;

        public AdvancedTradeContext(ITrader trader)
        {
            this.trader = trader;
            traderKind = trader?.TraderKind;
            faction = trader?.Faction;
            config = traderKind?.GetModExtension<ExtendedTrader>();
        }
        public PlanetTile Tile
        {
            get
            {
                if (!tileCache.HasValue)
                {
                    tileCache = AdvancedTradeUtility.ResolveTraderTile(trader);
                }
                return tileCache.Value;
            }
        }

        public int MaxStackable => config?.maxStackablePerLine ?? 1000;

        public bool AllowStuffChoice => config == null || config.allowStuffChoice;

        public bool ClearSoldGoodsOnClose => config != null && config.clearSoldGoodsOnClose;

        public bool UseFixedQuality => config != null && config.useFixedQuality;

        public QualityCategory FixedQuality => (config != null) ? config.fixedQuality : QualityCategory.Normal;

        public TechLevel MaxTechLevel => config?.maxTechLevel ?? TechLevel.Undefined;
    }
}
