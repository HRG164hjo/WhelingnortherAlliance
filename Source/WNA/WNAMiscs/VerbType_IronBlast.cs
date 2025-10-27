using RimWorld;
using System;
using Verse;
using WNA.WNAUtility;

namespace WNA.WNAMiscs
{
    public class VerbType_IronBlast : VerbType_Laser
    {
        public override bool TryStartCastOn(
            LocalTargetInfo castTarg,
            LocalTargetInfo destTarg,
            bool surpriseAttack = false,
            bool canHitNonTargetPawns = true,
            bool preventFriendlyFire = true,
            bool nonInterruptingSelfCast = true)
        {
            if (CasterPawn != null && CasterPawn.Spawned)
                IronCurtainUtility.IronGive(CasterPawn, 900);
            return base.TryStartCastOn(castTarg, destTarg, surpriseAttack, canHitNonTargetPawns, preventFriendlyFire, nonInterruptingSelfCast);
        }
        protected override bool TryCastShot()
        {
            Pawn caster = CasterPawn;
            Thing thing = currentTarget.Thing;
            if (caster != null && caster.Spawned)
                IronCurtainUtility.IronGive(caster, 900);
            foreach (CompTargetEffect comp in base.EquipmentSource.GetComps<CompTargetEffect>())
            {
                try
                {
                    if (currentTarget.Thing != null)
                        comp.DoEffectOn(caster, currentTarget.Thing);
                    else if (currentTarget.Cell.IsValid)
                        comp.DoEffectOn(caster, currentTarget.Cell.GetFirstBuilding(EquipmentSource.Map));
                }
                catch (Exception e)
                {
                    Log.Warning($"[IronBlast] CompTargetEffect failed: {e}");
                }
            }
            return base.TryCastShot();
        }
    }
}
