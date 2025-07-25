using RimWorld;
using UnityEngine;
using Verse;
using Verse.Sound;
using WNA.WNADefOf;

namespace WNA.ThingClass
{
    public class SpikeTrap : Building_Trap
    {
        private float Count => 5 + (5 * Mathf.FloorToInt(this.GetStatValue(StatDefOf.Mass)));

        public override void SpawnSetup(Map map, bool respawningAfterLoad)
        {
            base.SpawnSetup(map, respawningAfterLoad);
            if (!respawningAfterLoad)
            {
                SoundDefOf.TrapArm.PlayOneShot(new TargetInfo(base.Position, map));
            }
        }

        protected override void SpringSub(Pawn p)
        {
            if (base.Spawned)
            {
                SoundDefOf.TrapSpring.PlayOneShot(new TargetInfo(base.Position, base.Map));
            }
            if (p == null || p.Dead || !p.Spawned) return;
            float num = this.GetStatValue(StatDefOf.TrapMeleeDamage) * Count * 5f;
            float ap = this.GetStatValue(StatDefOf.Mass) * Count * 5f;
            for (int i = 0; i < Count; i++)
            {
                DamageInfo dinfo = new DamageInfo(WNAMainDefOf.WNA_DemoCut, num, ap, -1f, this);
                DamageWorker.DamageResult damageResult = p.TakeDamage(dinfo);
                if (i == 0)
                {
                    BattleLogEntry_DamageTaken battleLogEntry_DamageTaken = new BattleLogEntry_DamageTaken(p, RulePackDefOf.DamageEvent_TrapSpike);
                    Find.BattleLog.Add(battleLogEntry_DamageTaken);
                    damageResult.AssociateWithLog(battleLogEntry_DamageTaken);
                }
            }
        }
    }

}
