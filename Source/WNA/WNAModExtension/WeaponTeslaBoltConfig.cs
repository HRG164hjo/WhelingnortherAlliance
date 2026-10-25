using UnityEngine;
using Verse;

namespace WNA.WNAModExtension
{
    internal class WeaponTeslaBoltConfig : DefModExtension
    {
        public const int BoltCount = 3;
        public int BoltDuration = 14;
        public bool Bolt1Enable = true;

        public bool Bolt2Enable = true;

        public bool Bolt3Enable = true;
        public Color Bolt1Color = new Color(166, 217, 255);

        public Color Bolt2Color = new Color(102, 179, 255);

        public Color Bolt3Color = new Color(255, 255, 255);
        // optional
        public float BoltAmplitude = 0.35f;
        public float BoltThickness = 0.15f;
        public int BoltJitterInterval = 3;
        public bool IsBoltEnabled(int i)
        {
            switch (i)
            {
                case 0:
                    return Bolt1Enable;
                case 1:
                    return Bolt2Enable;
                case 2:
                    return Bolt3Enable;
                default:
                    return false;
            }
        }
        public Color GetBoltColor(int i)
        {
            switch (i)
            {
                case 0:
                    return Bolt1Color;
                case 1:
                    return Bolt2Color;
                case 2:
                    return Bolt3Color;
                default:
                    return Color.white;
            }
        }
        public static WeaponTeslaBoltConfig Get(Def def)
        {
            return def?.GetModExtension<WeaponTeslaBoltConfig>();
        }

    }
}
