using UnityEngine;
using Verse;

namespace WNA
{
    internal class WNAMod : Mod
    {
        public static WNAModSettings Settings;
        public WNAMod(ModContentPack content) : base(content)
        {
            Settings = GetSettings<WNAModSettings>();
        }
        public override string SettingsCategory() => "WNAMod".Translate();
        public override void DoSettingsWindowContents(Rect inRect)
        {
            var listing = new Listing_Standard();
            listing.Begin(inRect);
            listing.CheckboxLabeled(
                "WNAMod_enableIdeoConflictHostility".Translate(),
                ref Settings.enableIdeoConflictHostility);
            listing.GapLine();
            listing.CheckboxLabeled(
                "WNAMod_enablePermaconstDeathAid".Translate(),
                ref Settings.enablePermaconstDeathAid);
            listing.End();
        }
    }
}
