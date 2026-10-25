using Verse;

namespace WNA.WNAThingClass
{
    internal class ThingClass_Humanoid : Pawn
    {
        public override void PreApplyDamage(ref DamageInfo dinfo, out bool absorbed)
        {
            if (!dinfo.Def.ExternalViolenceFor(this)
                && dinfo.Def.defName != "WNA_Dmg_Culling")
                absorbed = true;
            if (dinfo.Def.isExplosive || dinfo.Def.isRanged)
                absorbed = true;
            if (dinfo.Instigator == null
                || dinfo.Instigator == this)
                absorbed = true;
            if (dinfo.Amount >= BodySize * HealthScale * 57
                || dinfo.Amount <= BodySize * HealthScale * 23)
                absorbed = true;
            if (dinfo.IgnoreArmor == true)
                absorbed = true;
            base.PreApplyDamage(ref dinfo, out absorbed);
        }
        public override void PostMake()
        {
            base.PostMake();
            if (mindState != null)
            {
                mindState = null;
                mindState.mentalBreaker = null;
                mindState.mentalStateHandler = null;
                mindState.inspirationHandler = null;
            }
        }
    }
}
