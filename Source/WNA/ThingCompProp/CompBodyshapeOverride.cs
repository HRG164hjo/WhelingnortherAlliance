using RimWorld;
using System.Collections.Generic;
using Verse;

namespace WNA.ThingCompProp
{
    public class PropBodyshapeOverride : CompProperties
    {
        public List<BodyTypeDef> bodyTypes;
        public BodyTypeDef maletype;
        public BodyTypeDef femaletype;
        public BodyTypeDef neutype;
        public PropBodyshapeOverride()
        {
            compClass = typeof(CompBodyshapeOverride);
        }
        public override void ResolveReferences(ThingDef parentDef)
        {
            base.ResolveReferences(parentDef);
            if (maletype == null)
                maletype = BodyTypeDefOf.Male;
            if (femaletype == null)
                femaletype = BodyTypeDefOf.Female;
            if (neutype == null)
                neutype = BodyTypeDefOf.Female;
            if (bodyTypes == null)
            {
                bodyTypes = new List<BodyTypeDef>
                {
                    BodyTypeDefOf.Thin,
                    BodyTypeDefOf.Fat,
                    BodyTypeDefOf.Hulk
                };
            }
        }
    }
    public class CompBodyshapeOverride : ThingComp
    {
        public PropBodyshapeOverride Props => (PropBodyshapeOverride)props;
        private BodyTypeDef initial;
        private bool changed = false;
        public override void Notify_Equipped(Pawn pawn)
        {
            base.Notify_Equipped(pawn);
            if (Props.bodyTypes != null && Props.bodyTypes.Contains(pawn.story.bodyType))
            {
                initial = pawn.story.bodyType;
                if (pawn.gender == Gender.Male)
                    pawn.story.bodyType = Props.maletype;
                else if (pawn.gender == Gender.Female)
                    pawn.story.bodyType = Props.femaletype;
                else pawn.story.bodyType = Props.neutype;
                changed = true;
            }
        }
        public override void Notify_Unequipped(Pawn pawn)
        {
            base.Notify_Unequipped(pawn);
            if (changed)
            {
                pawn.story.bodyType = initial;
                changed = false;
            }
        }
        public override void PostExposeData()
        {
            base.PostExposeData();
            Scribe_Defs.Look(ref initial, "initial");
            Scribe_Values.Look(ref changed, "changed", defaultValue: false);
        }
    }
}
