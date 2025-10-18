using Verse;

namespace WNA.ThingClass
{
    public class Whelingnorthan : Pawn
    {
        public override void PreApplyDamage(ref DamageInfo dinfo, out bool absorbed)
        {
            dinfo.SetAmount(0);
            dinfo.SetHitPart(null);
            absorbed = true;
        }
        public override void Kill(DamageInfo? dinfo, Hediff exactCulprit = null)
        {
            health.isBeingKilled = false;
        }
        public override void PostMake()
        {
            base.PostMake();
            if (this.mindState != null)
            {
                this.mindState = null;
                this.mindState.mentalBreaker = null;
                this.mindState.mentalStateHandler = null;
            }
        }
    }
}
