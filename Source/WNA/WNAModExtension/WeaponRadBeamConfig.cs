using UnityEngine;
using Verse;

namespace WNA.WNAModExtension
{
    internal class WeaponRadBeamConfig : DefModExtension
    {
        public float RadBeamWidth = 0.6f;
        public float RadBeamThickness = 0.16f;
        public Color RadBeamColor = new Color(0, 255, 0);
        public int RadBeamDuration = 26;
        public static WeaponRadBeamConfig Get(Def def)
        {
            return def?.GetModExtension<WeaponRadBeamConfig>();
        }
    }
}
