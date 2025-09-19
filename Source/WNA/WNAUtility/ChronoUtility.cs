using RimWorld;
using System;
using Verse;
using WNA.DMExtension;
using WNA.HediffClass;
using WNA.WNADefOf;

namespace WNA.WNAUtility
{
    public static class ChronoUtility
    {
        public enum ChronoFreezeSource
        {
            Teleport,
            Attack
        }
        private static int ManhaDist(IntVec3 a, IntVec3 b)
        {
            return Math.Abs(a.x - b.x) + Math.Abs(a.z - b.z);
        }
        private static ChronoGameComponent Comp => Current.Game.GetComponent<ChronoGameComponent>();
        public static bool CanChronoMove(Pawn pawn)
        {
            if (pawn.apparel != null)
            {
                foreach (var item in pawn.apparel.WornApparel)
                {
                    var ext = item.def.GetModExtension<TechnoTypeConfig>();
                    if (ext != null && ext.canChronoMove)
                        return true;
                }
            }

            var pawnExt = pawn.def.GetModExtension<TechnoTypeConfig>();
            return pawnExt != null && pawnExt.canChronoMove;
        }
        public static void SetCooldown(Pawn pawn, int ticks)
        {
            Comp.SetCooldown(pawn, ticks);
        }
        public static bool IsOnCooldown(Pawn pawn)
        {
            return Comp.IsOnCooldown(pawn);
        }
        public static int GetRemainingCooldown(Pawn pawn)
        {
            return Comp.GetRemainingCooldown(pawn);
        }
        /// 执行超时空移动，返回是否成功
        public static bool TryChronoMove(Pawn pawn, IntVec3 targetPos, Map map)
        {
            if (!CanChronoMove(pawn)) return false;

            var ext = GetBestChronoExtension(pawn);
            if (ext == null) return false;

            // 传送前粒子
            FleckMaker.Static(pawn.Position, map, ext.chronoOut, 1.0f);

            // 传送逻辑
            IntVec3 origin = pawn.Position;
            pawn.Position = targetPos;
            pawn.Notify_Teleported(true, false);

            // 传送后粒子
            FleckMaker.Static(targetPos, map, ext.chronoIn, 1.0f);

            // 冷却 & 冻结
            ApplyChronoCooldown(pawn, origin, targetPos, ext);

            return true;
        }

        /// 根据装备和pawn本体综合获取冷却参数（取最优）
        public static TechnoTypeConfig GetBestChronoExtension(Pawn pawn)
        {
            TechnoTypeConfig best = null;
            if (pawn.apparel != null)
            {
                foreach (var item in pawn.apparel.WornApparel)
                {
                    var ext = item.def.GetModExtension<TechnoTypeConfig>();
                    if (ext != null && ext.canChronoMove)
                    {
                        if (best == null || IsBetter(ext, best))
                            best = ext;
                    }
                }
            }
            var pawnExt = pawn.def.GetModExtension<TechnoTypeConfig>();
            if (pawnExt != null && pawnExt.canChronoMove)
            {
                if (best == null || IsBetter(pawnExt, best))
                    best = pawnExt;
            }
            return best;
        }
        private static bool IsBetter(TechnoTypeConfig a, TechnoTypeConfig b)
        {
            int aMin = a.chronoDelayMin;
            int bMin = b.chronoDelayMin;
            if (aMin != bMin)
                return aMin < bMin;
            return a.chronoDelayFactor < b.chronoDelayFactor;
        }

        /// 应用传送后的冷却时间（根据距离）
        public static void ApplyChronoCooldown(Pawn pawn, IntVec3 origin, IntVec3 targetPos, TechnoTypeConfig ext)
        {
            int dist = ManhaDist(origin, targetPos);
            int cooldown = ext.chronoDelayMin;
            if (ext.chronoDelayDistance >= 0)
            {
                int extraDist = Math.Max(0, dist - ext.chronoDelayDistance);
                cooldown += extraDist * ext.chronoDelayFactor;
            }
            SetCooldown(pawn, cooldown);
            if (ext.chronoTriggerFreeze)
            {
                if (pawn.health.hediffSet.HasHediff(WNAMainDefOf.WNA_ChronoFreeze)) return;
                var hediff = HediffMaker.MakeHediff(WNAMainDefOf.WNA_ChronoFreeze, pawn) as ChronoFreeze;
                hediff.source = ChronoFreezeSource.Teleport;
                hediff.ticksLeft = cooldown;
                pawn.health.AddHediff(hediff);
            }
        }
        public static void ApplyChronoWarp(Pawn victim, float damage, Pawn attacker)
        {
            var hediffSet = victim.health.hediffSet;
            if (!(hediffSet.GetFirstHediffOfDef(WNAMainDefOf.WNA_ChronoFreezeMalicious) is ChronoFreezeMalicious maliciousHediff))
            {
                maliciousHediff = HediffMaker.MakeHediff(WNAMainDefOf.WNA_ChronoFreezeMalicious, victim) as ChronoFreezeMalicious;
                maliciousHediff.warpDamage = damage;
                maliciousHediff.attackers.Add(attacker); ; // 赋值攻击者
                victim.health.AddHediff(maliciousHediff);
            }
            else
            {
                maliciousHediff.warpDamage += damage;
                if (!maliciousHediff.attackers.Contains(attacker)) maliciousHediff.attackers.Add(attacker);
            }
        }
    }
}