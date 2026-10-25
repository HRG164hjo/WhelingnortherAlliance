using RimWorld;
using System.Collections.Generic;
using Verse;

namespace WNA.WNAModExtension
{
    internal class IncompatibleIdeos : DefModExtension
    {
        public List<MemeDef> incompMemes = new List<MemeDef>();
        public List<PreceptDef> incompPrecepts = new List<PreceptDef>();
        public static IncompatibleIdeos Get(Def def)
        {
            return def?.GetModExtension<IncompatibleIdeos>();
        }
    }
}
