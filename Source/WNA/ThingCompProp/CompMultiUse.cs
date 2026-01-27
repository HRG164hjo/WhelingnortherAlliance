using Verse;

namespace WNA.ThingCompProp
{
    public class PropMultiUse : CompProperties
    {
        public int uses = 2;
        public PropMultiUse()
        {
            compClass = typeof(CompMultiUse);
        }
    }
    public class CompMultiUse : ThingComp
    {
        public PropMultiUse Props => (PropMultiUse)props;
        private int count = -1;
        public int Count
        {
            get
            {
                if (count < 0)
                    count = Props.uses;
                return count;
            }
            set
            {
                count = value;
            }
        }
        public override string CompInspectStringExtra()
        {
            return base.CompInspectStringExtra() + $"{Count} / {Props.uses}";
        }
        public override bool AllowStackWith(Thing other)
        {
            if (!base.AllowStackWith(other))
                return false;
            CompMultiUse otherComp = other.TryGetComp<CompMultiUse>();
            if (otherComp == null)
                return false;
            return otherComp.Count == Count;
        }
        public override void PostExposeData()
        {
            base.PostExposeData();
            Scribe_Values.Look(ref count, "count", -1);
        }
        public override void PostPostMake()
        {
            base.PostPostMake();
            if (count < 0)
                count = Props.uses;
        }
    }
}
