using RimWorld;

namespace WNA.WNAMiscs
{
    public class WarpOn : ActiveTransporter
    {
        protected override void Tick()
        {
            if (Contents == null) return;
            Contents.openDelay = 0;
            age = 1;
            base.Tick();
        }
    }
}
