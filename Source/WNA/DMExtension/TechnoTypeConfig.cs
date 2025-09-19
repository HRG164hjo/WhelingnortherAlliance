using Verse;

namespace WNA.DMExtension
{
    public class TechnoTypeConfig : DefModExtension
    {
        public bool canChronoMove = false; // 可以进行超时空移动
        public bool chronoTrigger = true; // 两次超时空移动间的冷却时间
        public bool chronoTriggerFreeze = true; // 冷却时使传送者无法进行任何动作
        public int chronoDelayMin = 60; // 最低冷却时间，默认60tick，如果有多个可用值则总是用最小值
        public int chronoDelayDistance = -1;// -1意为不启用超时空传送冷却随距离增加的机制，如果有多个可用值则总是用最小值
        public int chronoDelayFactor = 10; // 传送距离在chronoDelayDistance的基础上，每增加1则冷却时间增加这个数。比如chronoDelayDistance=2，传送了9格距离，则超时空冷却时间+70.传送距离的计算向下取整，使用网格距离。如果有多个可用值则总是用最小值
        public FleckDef chronoIn = DefDatabase<FleckDef>.GetNamed("PsycastPsychicEffect", true);
        public FleckDef chronoOut = DefDatabase<FleckDef>.GetNamed("PsycastPsychicEffect", true);
        public bool immuneToChronoWarp = false; // 免疫超时空抹除，canChronoMove=true则强制为true
        /* 为后续追加的内容做准备
         * 设定通过是否有心灵控制技能决定一个pawn是否是心控单位
         * public bool immuneToMindControl = false; 后续计划的免疫心灵控制
         * public bool infiniteMindControl = false; 为true时无视mindControlSlot，实现（或模拟）几乎无限容量的心灵控制
         * public int mindControlSlot = 1;
         * 
         * public bool immuneToRadiation = false; 后续计划的免疫辐射场
         * 
         * public bool isJumpJet = false; 后续计划的飞行单位
         * 
         * public bool ironKill = true;
         */
    }
}
