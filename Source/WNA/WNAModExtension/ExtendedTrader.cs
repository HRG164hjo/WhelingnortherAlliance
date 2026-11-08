using RimWorld;
using System.Collections.Generic;
using Verse;

namespace WNA.WNAModExtension
{
    internal class ExtendedTrader : DefModExtension
    {
        public bool allowMinifiedBuildings = true;
        public bool allowStuffChoice = true;
        public TechLevel maxTechLevel = TechLevel.Archotech;
        public List<ThingCategoryDef> allowedCategories = null;
        public List<ThingDef> forceExcludedDefs = null;
        public int maxStackablePerLine = 1000000;
        public int maxInstancePerLine = 100;
        public int defaultStackableCount = 75;
        public int defaultMinifiedCount = 1;
        public bool useFixedQuality = false;
        public QualityCategory fixedQuality = QualityCategory.Normal;
        public bool canBuyUntradeable = false;
        public bool clearSoldGoodsOnClose = true;
}
}
