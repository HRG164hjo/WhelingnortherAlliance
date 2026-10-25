using HarmonyLib;
using System.Reflection;
using UnityEngine;
using Verse;

namespace WNA
{
    public class WNAMod : Mod
    {
        internal static WNAModSettings Settings;
        private string returnerLimitBuf;
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
            var listing = new Listing_Standard();
            listing.Begin(inRect);
            listing.CheckboxLabeled(
                "WNAMod_enablePawnSpreadRadWhenKilled".Translate(),
                ref Settings.enablePawnSpreadRadWhenKilled);
            listing.GapLine();
            listing.CheckboxLabeled(
                "WNAMod_enableIdeoConflictHostility".Translate(),
                ref Settings.enableIdeoConflictHostility);
            listing.GapLine();
            listing.CheckboxLabeled(
                "WNAMod_enablePermaconstDeathAid".Translate(),
                ref Settings.enablePermaconstDeathAid);
            listing.GapLine();
            listing.TextFieldNumericLabeled(
                "WNAMod_pawnReturnerMaxLimit".Translate(),
                ref Settings.pawnReturnerMaxLimit,
                ref returnerLimitBuf,
                20f,
                2000f);
            listing.Label("WNAMod_pawnReturnerMaxLimitDesc".Translate());
            listing.GapLine();
            listing.CheckboxLabeled(
                "WNAMod_upliftSpawnAtRitualTarget".Translate(),
                ref Settings.upliftSpawnAtRitualTarget);
            listing.Label("WNAMod_upliftSpawnAtRitualTargetDesc".Translate());
            listing.End();
        }
    }
    internal class WNAModSettings : ModSettings
    {
        internal bool enableIdeoConflictHostility = false;
        internal bool enablePermaconstDeathAid = false;
        internal bool enablePawnSpreadRadWhenKilled = false;
        internal int pawnReturnerMaxLimit = 200;
        internal bool upliftSpawnAtRitualTarget = false;
        public override void ExposeData()
        {
            Scribe_Values.Look(ref enableIdeoConflictHostility, "enableIdeoConflictHostility", false);
            Scribe_Values.Look(ref enablePermaconstDeathAid, "enablePermaconstDeathAid", false);
            Scribe_Values.Look(ref enablePawnSpreadRadWhenKilled, "enablePawnSpreadRadWhenKilled", false);
            Scribe_Values.Look(ref pawnReturnerMaxLimit, "pawnReturnerMaxLimit", 200);
            Scribe_Values.Look(ref upliftSpawnAtRitualTarget, "upliftSpawnAtRitualTarget", false);
        }
    }
}
