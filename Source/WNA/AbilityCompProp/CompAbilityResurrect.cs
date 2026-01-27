using RimWorld;
using UnityEngine;
using Verse;

namespace WNA.AbilityCompProp
{
    public class PropAbilityResurrect : CompProperties_AbilityEffect
    {
        public PropAbilityResurrect()
        {
            compClass = typeof(CompAbilityResurrect);
        }
    }
    public class CompAbilityResurrect : CompAbilityEffect
    {
        public new PropAbilityResurrect Props => (PropAbilityResurrect)props;

        public override void Apply(LocalTargetInfo target, LocalTargetInfo dest)
        {
            base.Apply(target, dest);
            Pawn innerPawn = ((Corpse)target.Thing).InnerPawn;
            if (ResurrectionUtility.TryResurrect(innerPawn))
            {
                Messages.Message("MessagePawnResurrected".Translate(innerPawn), innerPawn, MessageTypeDefOf.PositiveEvent);
                MoteMaker.MakeAttachedOverlay(innerPawn, ThingDefOf.Mote_ResurrectFlash, Vector3.zero);
            }
        }
    }
}
