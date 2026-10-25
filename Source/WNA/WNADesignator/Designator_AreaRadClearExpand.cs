using RimWorld;
using UnityEngine;
using Verse;

namespace WNA.WNADesignator
{
    internal class Designator_AreaRadClearExpand : Designator_AreaRadClear
    {
        public Designator_AreaRadClearExpand() : base(DesignateMode.Add)
        {
            defaultLabel = "WNA_RadClearAreaExpand".Translate();
            defaultDesc = "WNA_RadClearAreaExpandDesc".Translate();
            icon = ContentFinder<Texture2D>.Get("UI/Designators/SnowClearAreaOn");
            defaultIconColor = new Color(0.784314f, 0.392157f, 0f);
            soundDragSustain = SoundDefOf.Designate_DragAreaAdd;
            soundDragChanged = SoundDefOf.Designate_DragZone_Changed;
            soundSucceeded = SoundDefOf.Designate_ZoneAdd_RemoveSnow;
            tutorTag = "AreaRadClearExpand";
        }
    }
}
