using Verse;

namespace WNA.DMExtension
{
    public class AttachEffectConfig : DefModExtension
    {
        public string aeName;//如果thing被挂载了多个ae，用来对不同ae进行区分
        public bool? accumulate;//控制ae是否可以叠层，如果为true，那么任何ae源可以为该ae叠加1层ae
        public int? durationOverride;//控制每层ae持续的时间，如果有aeName相同但durationOverride不同的ae源向同一个thing挂载ae，则大值覆盖小值；每经过这么长时间ae层数-1，ae层数归零时ae消失
        public bool? ironCurtain;//控制是否启用铁幕效果，铁幕效果持续期间thing不能被伤害，不能被kill或destroy，对于pawn，铁幕期间也不能获得hediff
        public float? damageMultiplier;//thing（pawn或炮塔）造成的任何伤害乘以这个值的层数次幂
        public float? armorMultiplier;//thing（pawn或炮塔）受到任何伤害乘以这个值的层数次幂
        public float? speedMultiplier;//thing（主要是pawn）经过原版计算系统计算完的移动速度乘以这个值的层数次幂
        public bool? canSpread;//thing被摧毁时是否尝试扩散自己拥有的ae
        public float? spreadRadius;//如果canSpread=true，会尝试为多大范围内的目标挂载ae
        public bool? spreadAlly;//如果为false，不会尝试为范围内的友军thing挂载ae
        public bool? spreadEnemy;//如果为false，不会尝试为范围内的敌方thing挂载ae
        public float? spreadFactor;//扩散出的ae的层数为扩散源持有的ae层数乘以此值并向上取整
        public int? forceKillLevel;//如果一个thing拥有的ae层数超过这一值，则每250tick尝试强行kill或destroy该thing一次
        public static AttachEffectConfig Get(Def def)
        {
            return def?.GetModExtension<AttachEffectConfig>();
        }
    }
}
