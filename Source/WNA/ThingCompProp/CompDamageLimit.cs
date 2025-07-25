using Verse;

namespace WNA.ThingCompProp
{
    public class CompDamageLimit : CompProperties
    {
        public int damageLimit = -1;

        public CompDamageLimit()
        {
            compClass = typeof(DamageLimit);
        }
    }
    public class DamageLimit : ThingComp
    {
        public CompDamageLimit Props => (CompDamageLimit)props;

        public override void PostPreApplyDamage(ref DamageInfo dinfo, out bool absorbed)
        {
            base.PostPreApplyDamage(ref dinfo, out absorbed);
            if (absorbed || Props.damageLimit == -1) return;
            if (dinfo.Amount > Props.damageLimit)
            {
                dinfo.SetAmount(Props.damageLimit);
            }
        }
    }
}
