using Verse;

namespace WNA.WNAModExtension
{
    internal class TechnoConfig : DefModExtension
    {
        // Skyfallers skip roof impact; turrets and weapons may fire while roofed.
        public bool? ignoreRoof = null;
        public bool? immuneToRadiation = null;
        public bool? immuneToWarp = null;
        public bool? immuneToMindControl = null;
        public bool? canFreePass = null;
        public static TechnoConfig Get(Def def)
        {
            return def?.GetModExtension<TechnoConfig>();
        }
    }
}
