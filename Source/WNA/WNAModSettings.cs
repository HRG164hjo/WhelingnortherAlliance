using Verse;

namespace WNA
{
    internal class WNAModSettings : ModSettings
    {
        public bool enableIdeoConflictHostility = false;
        public bool enablePermaconstDeathAid = false;
        public override void ExposeData()
        {
            Scribe_Values.Look(ref enableIdeoConflictHostility, "enableIdeoConflictHostility", false);
            Scribe_Values.Look(ref enablePermaconstDeathAid, "enablePermaconstDeathAid", false);
        }
    }
}
