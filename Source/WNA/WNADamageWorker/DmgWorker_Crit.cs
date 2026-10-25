using UnityEngine;
using Verse;

namespace WNA.WNADamageWorker
{
    internal class DmgWorker_Crit : DamageWorker_AddInjury
    {
        [System.ThreadStatic]
        private static bool applyingCrit;
        public override DamageResult Apply(DamageInfo dinfo, Thing victim)
        {
            DamageResult result = base.Apply(dinfo, victim);
            if (!applyingCrit && victim is Pawn pawn && result.LastHitPart != null
                && (result.LastHitPart.IsCorePart || result.headshot))
            {
                float critAmount = result.LastHitPart.def.hitPoints
                    * Mathf.Max(1f, pawn.HealthScale, (1 / pawn.HealthScale))
                    * Mathf.Max(1f, pawn.BodySize, (1 / pawn.BodySize))
                    * Random.Range(1.414214f, 1.732051f);
                DamageInfo critInfo = dinfo;
                critInfo.SetAmount(critAmount);
                critInfo.SetHitPart(result.LastHitPart);
                applyingCrit = true;
                try
                {
                    pawn.TakeDamage(critInfo);
                }
                finally
                {
                    applyingCrit = false;
                }
            }
            // base.Apply already kills (and destroys) non-pawn victims at 0 HP; don't kill twice.
            if (victim is Thing thing && !thing.Destroyed && thing.HitPoints > 0
                && thing.def.useHitPoints
                && thing.HitPoints <= (int)(thing.MaxHitPoints / 3))
                thing.Kill(dinfo);
            return result;
        }
    }
}
