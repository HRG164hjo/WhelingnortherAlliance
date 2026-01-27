using RimWorld;
using UnityEngine;
using Verse;

namespace WNA.AbilityCompProp
{
    public class PropHediffDeprivation : CompProperties_AbilityEffect
    {
        public HediffDef hediffToRemove = null;
        public HediffDef hediffTrauma = null;
        public HediffDef hediffFallBack = null;
        public float sevTrauma = 1f;
        public float sevFallBack = 1f;
        public PropHediffDeprivation()
        {
            compClass = typeof(CompHediffDeprivation);
        }
    }
    public class CompHediffDeprivation : CompAbilityEffect
    {
        public new PropHediffDeprivation Props => (PropHediffDeprivation)props;
        public override void Apply(LocalTargetInfo target, LocalTargetInfo dest)
        {
            base.Apply(target, dest);
            Pawn pawn = target.Pawn;
            if (pawn == null || pawn.Dead) return;
            if (TryRemoveHediff(pawn)) ApplyTrauma(pawn);
            else ApplyFallback(pawn);
        }
        private bool TryRemoveHediff(Pawn pawn)
        {
            if (Props.hediffToRemove == null) return false;
            Hediff target = pawn.health.hediffSet.GetFirstHediffOfDef(Props.hediffToRemove);
            if (target == null) return false;
            pawn.health.RemoveHediff(target);
            return true;
        }
        private void ApplyTrauma(Pawn pawn)
        {
            if (Props.hediffTrauma == null) return;
            Hediff trauma = pawn.health.AddHediff(Props.hediffTrauma);
            if (trauma != null)
                trauma.Severity = Mathf.Clamp(
                    Props.sevTrauma, trauma.def.minSeverity, trauma.def.maxSeverity);
        }
        private void ApplyFallback(Pawn pawn)
        {
            if (Props.hediffFallBack == null) return;
            Hediff fallBack = pawn.health.AddHediff(Props.hediffFallBack);
            if (fallBack != null)
                fallBack.Severity = Mathf.Clamp(
                    Props.sevFallBack, fallBack.def.minSeverity, fallBack.def.maxSeverity);
        }
        public override bool Valid(LocalTargetInfo target, bool throwMessages = false)
        {
            if (target.Pawn != null && !target.Pawn.Dead)
                return true;
            return false;
        }
    }
}
