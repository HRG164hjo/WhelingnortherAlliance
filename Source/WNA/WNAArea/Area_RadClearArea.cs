using UnityEngine;
using Verse;

namespace WNA.WNAArea
{
    internal class Area_RadClearArea : Area
    {
        public override string Label => "WNA_RadClearArea".Translate();

        public override Color Color => new Color(0.2f, 0.8f, 0.8f);

        public override int ListPriority => 5000;

        public Area_RadClearArea()
        {
        }

        public Area_RadClearArea(AreaManager areaManager) : base(areaManager)
        {
        }

        public override string GetUniqueLoadID()
        {
            return "Area_" + ID + "_RadClearArea";
        }
    }
}
