using UnityEngine;
using Verse;

namespace WNA.DMExtension
{
    public class TechnoConfig : DefModExtension
    {
        public Vector4? laserInnerColor;
        public Vector4? laserOuterColor;
        public float? laserOuterSpread;
        public float? laserThickness;
        public int? laserDuration;
        public bool? ignoreRoof;
        public bool? ironKill;
        public static TechnoConfig Get(Def def)
        {
            return def?.GetModExtension<TechnoConfig>();
        }
        /* public int mindControlCapacity = 1;
         * public bool infiniteMindControl = false;
         * public bool immuneToMindControl = false;
         * 
         * public bool canChronoMove = false; 可以进行超时空移动
         * public bool chronoTrigger = true; 连续超时空移动间需要冷却
         * public bool chronoTriggerFreeze = true; 冷却时使传送者无法进行任何动作
         * public int chronoDelayMin = 60; 最低冷却时间，默认60tick，如果有多个可用值则总是用最小值
         * public int chronoDelayFactor = 10; 传送冷却时间随传送距离而增加的帧数
         * public FleckDef chronoIn = DefDatabase<FleckDef>.GetNamed("PsycastPsychicEffect", true);
         * public FleckDef chronoOut = DefDatabase<FleckDef>.GetNamed("PsycastPsychicEffect", true);
         * public bool immuneToChronoWarp = false; 免疫超时空抹除，canChronoMove=true则强制为true
         *
         */
    }
}
