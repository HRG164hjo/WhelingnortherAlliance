using UnityEngine;
using Verse;

namespace WNA.WNAMiscs
{
    public class Area_RadClearArea : Area
    {
        public override string Label => "WNA_Area_RadClear".Translate();
        public override Color Color => new Color(0.2f, 0.9f, 0.2f);
        public override int ListPriority => 5000;

        public Area_RadClearArea() { }
        public Area_RadClearArea(AreaManager areaManager) : base(areaManager) { }

        public override string GetUniqueLoadID() => "Area_" + ID + "_RadiationClear";
    }
}
