using RimWorld;
using Verse;

namespace WNA.WNADefOf
{
    [DefOf]
    public static class WNARadMeshFlagDefOf
    {
        public static MapMeshFlagDef WNA_RadField;

        static WNARadMeshFlagDefOf()
        {
            DefOfHelper.EnsureInitializedInCtor(typeof(WNARadMeshFlagDefOf));
        }
    }
}
