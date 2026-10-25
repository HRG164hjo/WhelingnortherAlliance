using RimWorld;
using Verse;
using WNA.WNAArea;
using WNA.WNAUtility;

namespace WNA.WNADesignator
{
    internal abstract class Designator_AreaRadClear : Designator_Cells
    {
        private readonly DesignateMode mode;

        public override bool DragDrawMeasurements => true;

        public override DrawStyleCategoryDef DrawStyleCategory => DrawStyleCategoryDefOf.Areas;

        protected Designator_AreaRadClear(DesignateMode mode)
        {
            this.mode = mode;
            soundDragSustain = SoundDefOf.Designate_DragStandard;
            soundDragChanged = SoundDefOf.Designate_DragStandard_Changed;
            useMouseIcon = true;
        }

        public override AcceptanceReport CanDesignateCell(IntVec3 c)
        {
            if (!c.InBounds(Map))
                return false;
            Area_RadClearArea area = WNARadFieldUtility.GetRadClearArea(Map);
            bool flag = area != null && area[c];
            return mode == DesignateMode.Add ? !flag : flag;
        }

        public override void DesignateSingleCell(IntVec3 c)
        {
            Area_RadClearArea area = WNARadFieldUtility.GetOrCreateRadClearArea(Map);
            if (area == null)
                return;
            area[c] = mode == DesignateMode.Add;
        }

        public override void SelectedUpdate()
        {
            GenUI.RenderMouseoverBracket();
            WNARadFieldUtility.GetRadClearArea(Map)?.MarkForDraw();
        }
    }
}
