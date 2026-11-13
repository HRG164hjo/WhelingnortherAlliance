using UnityEngine;
using Verse;

namespace WNA.WNADefOf
{
    public class WNA_RadFieldDef : Def
    {
        public int intensityMax = 500;
        public int decayPerInterval = 1;
        public int decayIntervalTicks = 250;
        public Color color = new Color(200f / 255f, 100f / 255f, 0f);
        public float overlayAlpha = 0.6f;
        public DamageDef damageDef;
        public float damagePerIntensity = 0.1f;
        public int damageIntervalTicks = 45;
        public bool clearable = true;
        public ThingDef clearThing;
        public bool showInReadout = true;

        public Color RenderColor
        {
            get
            {
                return new Color(
                    color.r > 1f ? color.r / 255f : color.r,
                    color.g > 1f ? color.g / 255f : color.g,
                    color.b > 1f ? color.b / 255f : color.b,
                    color.a > 1f ? color.a / 255f : color.a);
            }
        }
    }
}
