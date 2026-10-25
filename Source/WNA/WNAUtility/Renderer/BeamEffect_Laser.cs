using UnityEngine;
using WNA.WNAModExtension;

namespace WNA.WNAUtility.Renderer
{
    internal class BeamEffect_Laser : BeamEffect
    {
        private const float HoldFraction = 0.3f;
        private const float CoreWidthScale = 0.34f;
        private const float FlareWidthScale = 2.6f;
        private const float FlareLengthScale = 1.8f;

        private Color innerColor = Color.white;

        private Color outerColor = Color.white;

        private float thickness = 0.2f;

        private float outerSpread = 3f;

        private int seed;
        public void Configure(WeaponLaserConfig config)
        {
            if (config == null)
                return;
            innerColor = NormalizeColor(config.LaserInnerColor);
            outerColor = NormalizeColor(config.LaserOuterColor);
            thickness = Mathf.Max(0f, config.LaserThickness);
            outerSpread = Mathf.Max(0f, config.LaserOuterSpread);
        }

        protected override void OnInitialized()
        {
            unchecked
            {
                seed = startTick * 6151 + Mathf.RoundToInt(start.x * 17.3f) * 1299709 + Mathf.RoundToInt(start.z * 41.7f) * 15485863;
            }
        }

        public override void Draw()
        {
            float brightness = Brightness(LifePercent, HoldFraction);
            if (brightness <= 0f || thickness <= 0f)
                return;
            // RA2 photon beams crackle instead of holding a steady intensity.
            float flicker = Mathf.Lerp(0.72f, 1f, Hash01(seed, AgeTicks, 0));
            brightness *= flicker;
            if (outerSpread > 0f && outerColor.a > 0f)
                DrawFlatQuad(start, end, thickness * outerSpread, BeamMaterials.Glow(outerColor * brightness));
            DrawFlatQuad(start, end, thickness, BeamMaterials.Glow(innerColor * brightness));
            // White-hot filament down the middle, as in the prism/photon beams.
            Color core = Color.Lerp(innerColor, Color.white, 0.75f);
            DrawFlatQuad(start, end, thickness * CoreWidthScale, BeamMaterials.Glow(core * brightness));
            DrawEndFlares(core, thickness * FlareWidthScale, thickness * FlareLengthScale, brightness);
        }
    }
}
