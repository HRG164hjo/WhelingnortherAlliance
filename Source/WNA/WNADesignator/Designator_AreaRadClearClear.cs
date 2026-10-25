using RimWorld;
using UnityEngine;
using Verse;

namespace WNA.WNADesignator
{
    internal class Designator_AreaRadClearClear : Designator_AreaRadClear
    {
        public Designator_AreaRadClearClear() : base(DesignateMode.Remove)
        {
            defaultLabel = "WNA_RadClearAreaRemove".Translate();
            defaultDesc = "WNA_RadClearAreaRemoveDesc".Translate();
            icon = ContentFinder<Texture2D>.Get("UI/Designators/SnowClearAreaOff");
            defaultIconColor = new Color(0.784314f, 0.392157f, 0f);
            soundDragSustain = SoundDefOf.Designate_DragAreaDelete;
            soundDragChanged = null;
            soundSucceeded = SoundDefOf.Designate_ZoneDelete;
        }
    }
}
