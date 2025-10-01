using RimWorld;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using Verse;

namespace WNA.WNAMiscs
{
    public class VerbType_Burst : Verb_LaunchProjectile
    {
        protected override int ShotsPerBurst => base.BurstShotCount;
        #region Reflection
        private static readonly FieldInfo forceMissCacheField =
            typeof(Verb_LaunchProjectile).GetField("forcedMissTargetEvenDispersalCache", BindingFlags.Instance | BindingFlags.NonPublic);
        protected List<IntVec3> ForceMissList
        {
            get => (List<IntVec3>)forceMissCacheField.GetValue(this);
        }
        private static readonly MethodInfo generateTargetsMethod =
            typeof(Verb_LaunchProjectile).GetMethod("GenerateEvenDispersalForcedMissTargets", BindingFlags.Static | BindingFlags.NonPublic);
        protected IEnumerable<IntVec3> ForcedMissReflect(IntVec3 root, float radius, int count)
        {
            if (generateTargetsMethod == null)
            {
                Log.ErrorOnce("Could not find GenerateEvenDispersalForcedMissTargets via reflection.", 123456);
                return new List<IntVec3>();
            }
            object[] parameters = new object[] { root, radius, count };
            return (IEnumerable<IntVec3>)generateTargetsMethod.Invoke(null, parameters);
        }
        #endregion
        public override void WarmupComplete()
        {
            base.WarmupComplete();
            burstShotsLeft = ShotsPerBurst;
            state = VerbState.Bursting;
            if (verbProps.ForcedMissRadius > 0.5f && !verbProps.forcedMissEvenDispersal)
            {
                float baseMissRadius = verbProps.ForcedMissRadius;
                if (CasterPawn != null) baseMissRadius *= verbProps.GetForceMissFactorFor(EquipmentSource, CasterPawn);
                float adjustedMissRadius = VerbUtility.CalculateAdjustedForcedMiss(baseMissRadius, currentTarget.Cell - Caster.Position);
                List<IntVec3> cache = ForceMissList;
                cache.Clear();
                cache.AddRange(ForcedMissReflect(currentTarget.Cell, adjustedMissRadius, burstShotsLeft));
                cache.SortByDescending(p => p.DistanceToSquared(Caster.Position));
            }
            while (burstShotsLeft > 0)
            {
                if (TryCastShot()) burstShotsLeft--;
                else
                {
                    burstShotsLeft = 0;
                    break;
                }
            }
            state = VerbState.Idle;
            if (CasterIsPawn && !NonInterruptingSelfCast)
                CasterPawn.stances.SetStance(new Stance_Cooldown(verbProps.AdjustedCooldownTicks(this, CasterPawn), currentTarget, this));
            castCompleteCallback?.Invoke();
            if (verbProps.consumeFuelPerBurst > 0f)
                caster.TryGetComp<CompRefuelable>()?.ConsumeFuel(verbProps.consumeFuelPerBurst);
        }
    }
}
