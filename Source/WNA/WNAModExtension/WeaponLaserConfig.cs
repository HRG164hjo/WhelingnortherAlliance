using UnityEngine;
using Verse;

namespace WNA.WNAModExtension
{
    internal class WeaponLaserConfig : DefModExtension
    {
        public Color LaserInnerColor = new Color(255, 0, 0);
        public Color LaserOuterColor = new Color(255, 255, 255);
        public float LaserThickness = 0.2f;
        public float LaserOuterSpread = 1.414f;
        public int LaserDuration = 18;
        public static WeaponLaserConfig Get(Def def)
        {
            return def?.GetModExtension<WeaponLaserConfig>();
        }
    }
}
