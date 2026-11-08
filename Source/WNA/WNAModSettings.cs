using HarmonyLib;
using System.Reflection;
using UnityEngine;
using Verse;

namespace WNA
{
    public class WNAMod : Mod
    {
        internal static WNAModSettings Settings;
        private const float ScrollbarWidth = 20f;
        private const float IndentWidth = 12f;
        private string returnerLimitBuf;
        private string returnerRecencyBuf;
        private Vector2 scrollPosition;
        private float contentHeight;
        public WNAMod(ModContentPack content) : base(content)
        {
            Settings = GetSettings<WNAModSettings>();
            ApplyHarmonyPatches();
        }
        private static void ApplyHarmonyPatches()
        {
            var harmony = new Harmony("hrg164hjo.whelingnorther.alliance");
            harmony.PatchAll(Assembly.GetExecutingAssembly());
            Log.Message("[WNA] Harmony patch success.");
        }
        public override string SettingsCategory() => "WNAMod".Translate();
        public override void DoSettingsWindowContents(Rect inRect)
        {
            float maxScroll = Mathf.Max(0f, contentHeight - inRect.height);
            if (scrollPosition.y > maxScroll)
            {
                scrollPosition.y = maxScroll;
            }
            Rect viewRect = new Rect(0f, 0f, inRect.width - ScrollbarWidth, Mathf.Max(contentHeight, inRect.height));
            Widgets.BeginScrollView(inRect, ref scrollPosition, viewRect);
            var listing = new Listing_Standard();
            listing.Begin(viewRect);
            listing.maxOneColumn = true;
            DoSettingsContents(listing);
            contentHeight = listing.CurHeight;
            listing.End();
            Widgets.EndScrollView();
        }
        private void DoSettingsContents(Listing_Standard listing)
        {
            CheckboxSetting(listing, "WNAMod_enablePawnSpreadRadWhenKilled", ref Settings.enablePawnSpreadRadWhenKilled);
            listing.GapLine();
            CheckboxSetting(listing, "WNAMod_enableIdeoConflictHostility", ref Settings.enableIdeoConflictHostility);
            listing.GapLine();
            CheckboxSetting(listing, "WNAMod_enablePermaconstDeathAid", ref Settings.enablePermaconstDeathAid);
            if (Settings.enablePermaconstDeathAid)
            {
                PercentSetting(listing, "WNAMod_permaconstDeathAidChance", ref Settings.permaconstDeathAidChance);
            }
            listing.GapLine();
            CheckboxSetting(listing, "WNAMod_reviveDestroyedSettlements", ref Settings.reviveDestroyedSettlements);
            listing.Label("WNAMod_reviveDestroyedSettlementsDesc".Translate());
            if (Settings.reviveDestroyedSettlements)
            {
                PercentSetting(listing, "WNAMod_capturedSettlementWNAChance", ref Settings.capturedSettlementWNAChance);
            }
            listing.GapLine();
            listing.Label("WNAMod_pawnReturnerMaxLimit".Translate());
            Rect limitRect = listing.GetRect(Text.LineHeight);
            Widgets.TextFieldNumeric(
                limitRect.LeftPart(0.4f),
                ref Settings.pawnReturnerMaxLimit,
                ref returnerLimitBuf,
                20f,
                2000f);
            listing.Gap(listing.verticalSpacing);
            listing.Label("WNAMod_pawnReturnerMaxLimitDesc".Translate());
            listing.GapLine();
            listing.Label("WNAMod_pawnReturnerRecencyDays".Translate());
            Rect recencyRect = listing.GetRect(Text.LineHeight);
            Widgets.TextFieldNumeric(
                recencyRect.LeftPart(0.4f),
                ref Settings.pawnReturnerRecencyDays,
                ref returnerRecencyBuf,
                0.001f,
                365f);
            listing.Gap(listing.verticalSpacing);
            listing.Label("WNAMod_pawnReturnerRecencyDaysDesc".Translate());
            listing.GapLine();
            CheckboxSetting(listing, "WNAMod_upliftSpawnAtRitualTarget", ref Settings.upliftSpawnAtRitualTarget);
            listing.Label("WNAMod_upliftSpawnAtRitualTargetDesc".Translate());
            listing.GapLine();
            CheckboxSetting(listing, "WNAMod_autoDefenseMaster", ref Settings.settlementAutoDefense);
            listing.Label("WNAMod_autoDefenseMasterDesc".Translate());
            if (Settings.settlementAutoDefense)
            {
                float fullWidth = listing.ColumnWidth;
                listing.Indent(IndentWidth);
                listing.ColumnWidth = fullWidth - IndentWidth;
                CheckboxSetting(listing, "WNAMod_autoDefenseCrossLayer", ref Settings.enableCrossLayerDefense);
                listing.Label("WNAMod_autoDefenseCrossLayerDesc".Translate());
                CheckboxSetting(listing, "WNAMod_autoDefenseSitesGravships", ref Settings.attackSitesAndGravships);
                listing.Label("WNAMod_autoDefenseSitesGravshipsDesc".Translate());
                CheckboxSetting(listing, "WNAMod_autoDefenseGeneratedMaps", ref Settings.attackGeneratedMaps);
                listing.Label("WNAMod_autoDefenseGeneratedMapsDesc".Translate());
                listing.ColumnWidth = fullWidth;
                listing.Outdent(IndentWidth);
            }
            listing.Gap(8f);
        }
        private static void CheckboxSetting(Listing_Standard listing, string labelKey, ref bool value)
        {
            string label = labelKey.Translate();
            float labelWidth = Mathf.Max(1f, listing.ColumnWidth - 24f);
            float height = Mathf.Max(Text.LineHeight, Text.CalcHeight(label, labelWidth));
            Widgets.CheckboxLabeled(listing.GetRect(height), label, ref value);
            listing.Gap(listing.verticalSpacing);
        }
        private static void PercentSetting(Listing_Standard listing, string labelKey, ref float value)
        {
            float fullWidth = listing.ColumnWidth;
            listing.Indent(IndentWidth);
            listing.ColumnWidth = fullWidth - IndentWidth;
            string label = labelKey.Translate() + ": " + Mathf.RoundToInt(value * 100f) + "%";
            float height = Mathf.Max(Text.LineHeight, Text.CalcHeight(label, listing.ColumnWidth));
            Widgets.Label(listing.GetRect(height), label);
            Rect sliderRect = listing.GetRect(Text.LineHeight).LeftPart(0.6f);
            value = Widgets.HorizontalSlider(sliderRect, value, 0f, 1f, roundTo: 0.01f);
            listing.Gap(listing.verticalSpacing);
            listing.ColumnWidth = fullWidth;
            listing.Outdent(IndentWidth);
        }
    }
    internal class WNAModSettings : ModSettings
    {
        internal bool enableIdeoConflictHostility = false;
        internal bool enablePermaconstDeathAid = false;
        internal bool enablePawnSpreadRadWhenKilled = false;
        internal int pawnReturnerMaxLimit = 200;
        internal float pawnReturnerRecencyDays = 2.357f;
        internal bool upliftSpawnAtRitualTarget = false;
        internal bool settlementAutoDefense = true;
        internal bool enableCrossLayerDefense = false;
        internal bool attackSitesAndGravships = false;
        internal bool attackGeneratedMaps = false;
        internal bool reviveDestroyedSettlements = false;
        internal float permaconstDeathAidChance = 0.07f;
        internal float capturedSettlementWNAChance = 0.07f;
        public override void ExposeData()
        {
            Scribe_Values.Look(ref enableIdeoConflictHostility, "enableIdeoConflictHostility", false);
            Scribe_Values.Look(ref enablePermaconstDeathAid, "enablePermaconstDeathAid", false);
            Scribe_Values.Look(ref enablePawnSpreadRadWhenKilled, "enablePawnSpreadRadWhenKilled", false);
            Scribe_Values.Look(ref pawnReturnerMaxLimit, "pawnReturnerMaxLimit", 200);
            Scribe_Values.Look(ref pawnReturnerRecencyDays, "pawnReturnerRecencyDays", 2.357f);
            Scribe_Values.Look(ref upliftSpawnAtRitualTarget, "upliftSpawnAtRitualTarget", false);
            Scribe_Values.Look(ref settlementAutoDefense, "settlementAutoDefense", true);
            Scribe_Values.Look(ref enableCrossLayerDefense, "enableCrossLayerDefense", false);
            Scribe_Values.Look(ref attackSitesAndGravships, "attackSitesAndGravships", false);
            Scribe_Values.Look(ref attackGeneratedMaps, "attackGeneratedMaps", false);
            Scribe_Values.Look(ref reviveDestroyedSettlements, "reviveDestroyedSettlements", false);
            Scribe_Values.Look(ref permaconstDeathAidChance, "permaconstDeathAidChance", 0.07f);
            Scribe_Values.Look(ref capturedSettlementWNAChance, "capturedSettlementWNAChance", 0.07f);
        }
    }
}
