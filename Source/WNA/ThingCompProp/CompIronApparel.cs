using RimWorld;
using Verse;
using WNA.WNAUtility;

namespace WNA.ThingCompProp
{
    public class PropIronApparel : CompProperties
    {
        public int interval = 2000;
        public int duration = 2000;
        public PropIronApparel() => this.compClass = typeof(CompIronApparel);
    }
    public class CompIronApparel : ThingComp
    {
        public PropIronApparel Props => (PropIronApparel)props;
        public override void CompTick()
        {
            base.CompTick();
            Thing parentThing = parent;
            Pawn wearer = null;
            if (parent.IsHashIntervalTick(Props.interval))
            {
                if (parent is Apparel app) wearer = app.Wearer;
                if (wearer != null)
                    IronCurtainUtility.IronGive(wearer, Props.duration);
            }
        }
    }
}
