using Verse;
using WNA.WNAUtility;

namespace WNA.ThingCompProp
{
    public class CompIronGuardian : CompProperties
    {
        public int interval = 250;
        public float radius = 10f;
        public int duration = 2400;
        public CompIronGuardian()
        {
            compClass = typeof(IronGuardian);
        }
    }
    public class IronGuardian : ThingComp
    {
        public CompIronGuardian Props => (CompIronGuardian)props;
        public override void CompTick()
        {
            base.CompTick();
            var map = parent.Map;
            if (!parent.IsHashIntervalTick(Props.interval))
            {
                if (map == null) return;
                var center = parent.Position;
                foreach (var cell in GenRadial.RadialCellsAround(center, Props.radius, true))
                {
                    if (!cell.InBounds(map)) continue;
                    foreach (var t in cell.GetThingList(map))
                    {
                        if (t == null || t == parent) continue;
                        if (t.Faction != parent.Faction) continue;
                        IronCurtainUtility.IronGive(t, Props.duration);
                    }
                }
            }
        }
    }
}
