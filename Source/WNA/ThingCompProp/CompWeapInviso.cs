using RimWorld;
using System;
using System.Collections.Generic;
using Verse;

namespace WNA.ThingCompProp
{
    public class CompWeapInviso : CompProperties
    {
        public int stagger = 60;
        public FleckDef anim = null;
        public float cellSpread = 0f;
        public int burst = 1;

        public bool isDamager = true;
        public DamageDef damageDef = null;
        public float power = 0f;
        public float ap = 0f;

        /*
        public bool isHediffer = false; // 是否向目标添加Hediff
        public HediffDef hediffDef = null; // 添加的HediffDef
        public float hediff_sev = 0f; // 若Hediff有severity，则每次攻击为该hediff增加此数量的severity

        public bool isScatter = false; // 散射效果
        public float scat_range = 0f // 最大散布范围
        public int scat_count = 0f // 散布范围内随机进行的额外攻击的数量

        public bool isLaser = false; // 绘制光棱炮效果
        public Color laser_color = Color.white; // 光束颜色
        public float laser_duration = 0.5f; // 光束持续时间
        public float laser_width = 1f; // 光束宽度
        
        public bool isTesla = false; // 绘制磁暴线圈效果
        public Color bolt_color = Color.blue; // 电弧颜色
        public Color bolt2_color = Color.blue; // 电弧2的颜色
        public Color bolt3_color = Color.blue; // 电弧3的颜色
        public bool EMEffect = false; // 为true时攻击将瘫痪目标，使目标不能移动或进行行动
        public bool 

        public bool isRadBeam = false; // 绘制辐射工兵效果
        public Color rad_color = Color.green // 辐射束的颜色
        public float rad_width = 1f; // 辐射束（正弦波）的振幅
        public float rad_tense = 1f; // 辐射束（正弦波）的频率

        public bool isMagBeam = false; // 绘制磁能坦克效果
        public Color mag_color = Color.magenta // 磁力波的颜色
        public bool mag_revBeam = false; // 磁力波的传播方向，true表明磁力波从攻击者向目标延伸
        public bool maghold = false; // 磁力波将目标固定在原地不能移动
        public bool magmove = false; // 磁力波迫使目标向攻击者移动，不能与maghold同时为true
        public bool magmove_rev = false; // 磁力波迫使目标远离攻击者，仅当magmove=true时被考虑
        public float magmov_v = 1f; // 磁力波迫使目标移动的速度为1格/秒
        public float magAmbient = 0f; // 对磁力波穿过的目标，每帧造成这么多伤害，为0时关闭此逻辑

        public bool isRailgun = false; // 轨道炮效果
        public FleckDef railPart; // 绘制轨道炮效果的粒子效果，可以没有
        public float railp_dist; // 粒子之间的间距
        public float railAmbient = -1f; // 轨道炮穿透伤害效果，0表示关闭穿透伤害，-1表示伤害值与power相同
        */
        public CompWeapInviso()
        {
            compClass = typeof(WeapInviso);
        }
    }
    public class WeapInviso : CompTargetEffect
    {
        public CompWeapInviso Props => (CompWeapInviso)props;

        public override void DoEffectOn(Pawn user, Thing target)
        {
            if (target == null || target.Destroyed)
            {
                Log.Warning($"[{GetType().Name}] Tried to apply effect to a null or destroyed target ({target?.LabelCap ?? "null"}). Skipping.");
                return;
            }
            Map map = target.Map;
            if (map == null)
            {
                Log.Warning($"[{GetType().Name}] Target {target.LabelCap} has no map, cannot apply effect. Skipping.");
                return;
            }
            if (target is Pawn pawn && !pawn.Dead) pawn.stances.stagger.StaggerFor(Props.stagger, 0.1f);
            if (Props.anim != null) ApplyAnim(target);
            if (Props.isDamager)
            {
                IsDamagerEffect(user, target);
            }
            /*if (Props.isHediffer)
            {
                IsHedifferEffect(user, target);
            }
            if (Props.isScatter)
            {
                IsScatterEffect(user, target);
            }
            if (Props.isLaser)
            {
                IsLaserEffect(user, target);
            }
            if (Props.isTesla)
            {
                IsTeslaEffect(user, target);
            }
            if (Props.isRadBeam)
            {
                IsRadbeamEffect(user, target);
            }
            if (Props.isMagBeam)
            {
                IsMagbeamEffect(user, target);
            }
            if (Props.isRailgun)
            {
                IsRailgunEffect(user, target);
            }*/
        }

        #region Generic Effect
        private void ApplyAnim(Thing target)
        {
            if (target == null || target.Destroyed || target.Map == null)
            {
                Log.Warning($"[{GetType().Name}] Attempted to apply animation to an invalid target ({target?.LabelCap ?? "null"}). Skipping.");
                return;
            }
            FleckMaker.Static(target.TrueCenter(), target.Map, Props.anim, 1f);
        }
        private IEnumerable<Thing> GetThingsInCellSpread(Thing originTarget, Pawn user, float radius)
        {
            if (originTarget == null || originTarget.Destroyed || originTarget.Map == null || radius <= 0f)
            {
                yield break;
            }
            Map map = originTarget.Map;
            IEnumerable<IntVec3> areaCells = GenRadial.RadialCellsAround(originTarget.Position, radius, true);
            foreach (IntVec3 cell in areaCells)
            {
                if (!cell.InBounds(map)) continue;
                List<Thing> thingsInCell = cell.GetThingList(map);
                if (thingsInCell.Any())
                {
                    foreach (Thing thing in thingsInCell)
                    {
                        if (thing != originTarget && thing != user)
                        {
                            yield return thing;
                        }
                    }
                }
            }
        }
        private void ApplyBurstEffect(Action<Thing, Pawn> effectAction, Thing target, Pawn user)
        {
            if (effectAction == null)
            {
                Log.ErrorOnce($"[{GetType().Name}] ApplyBurstEffect called with a null effectAction. This should not happen.", GetHashCode() + 2);
                return;
            }

            for (int i = 0; i < Props.burst; i++)
            {
                effectAction(target, user);
            }
        }

        #endregion
        
        #region Damager Effect
        private void IsDamagerEffect(Pawn user, Thing target)
        {
            if (Props.damageDef == null)
            {
                Log.ErrorOnce($"[{GetType().Name}] damageDef is not set for damage effect. This weapon will not deal damage. Check your XML Defs.", GetHashCode() + 1);
                return;
            }
            if (Props.cellSpread > 0f)
            {
                IEnumerable<Thing> affectedThings = GetThingsInCellSpread(target, user, Props.cellSpread);
                foreach (Thing thing in affectedThings)
                {
                    ApplyBurstEffect(ApplyDamage, thing, user);
                }
            }
            ApplyBurstEffect(ApplyDamage, target, user);
        }
        private void ApplyDamage(Thing target, Pawn user)
        {
            if (target == null || target.Destroyed || user == null)
            {
                Log.Warning($"[{GetType().Name}] Attempted to apply damage to an invalid target ({target?.LabelCap ?? "null"}) or with a null user. Skipping damage application.");
                return;
            }

            DamageInfo damageInfo = new DamageInfo(
                Props.damageDef,
                Props.power,
                armorPenetration: Props.ap,
                instigator: user
            );
            target.TakeDamage(damageInfo);
        }
        #endregion
        
        #region Hediffer Effect
        //,
        #endregion
        
        #region Scatter Effect
        //,
        #endregion
        
        #region Laser Effect
        //,
        #endregion
        
        #region Tesla Effect
        //,
        #endregion
        
        #region Radbeam Effect
        //,
        #endregion
        
        #region MagBeam Effect
        //,
        #endregion
        
        #region Railgun Effect
        //,
        #endregion
    }
}
