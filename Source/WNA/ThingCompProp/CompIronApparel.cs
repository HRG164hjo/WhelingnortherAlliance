using RimWorld;
using Verse;
using WNA.WNAUtility;

namespace WNA.ThingCompProp
{
    public class CompIronApparel : CompProperties
    {
        public int interval = 2000;
        public int duration = 2000;
        public CompIronApparel() => this.compClass = typeof(IronApparel);
    }
    public class IronApparel : ThingComp
    {
        public CompIronApparel Props => (CompIronApparel)props;
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
