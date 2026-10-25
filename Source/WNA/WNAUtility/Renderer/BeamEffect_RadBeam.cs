using UnityEngine;
using Verse;
using WNA.WNAModExtension;

namespace WNA.WNAUtility.Renderer
{
    internal class BeamEffect_RadBeam : BeamEffect
    {
        private const float HoldFraction = 0.15f;
        private const float SegmentLength = 0.2f;

        private const int MinSegments = 8;
        private const float TravelCycles = 1f;
        private const float CoreWidthScale = 0.42f;

        private StripMesh strip;

        private StripMesh core;

        private Color color = new Color(0.45f, 1f, 0.55f);

        private float amplitude = 0.5f;

        private float thickness = 0.16f;

        private int seed;
        public void Configure(WeaponRadBeamConfig config)
        {
            if (config == null)
                return;
            color = NormalizeColor(config.RadBeamColor);
            amplitude = Mathf.Max(0f, config.RadBeamWidth);
            thickness = Mathf.Max(0f, config.RadBeamThickness);
        }

        protected override void OnInitialized()
        {
            strip = StripMeshPool.Rent();
            core = StripMeshPool.Rent();
            unchecked
            {
                seed = startTick * 3571 + Mathf.RoundToInt(start.x * 23.9f) * 15485863 + Mathf.RoundToInt(start.z * 31.1f) * 32452843;
            }
        }

        public override void Draw()
        {
            if (strip == null || core == null)
                return;
            float brightness = Brightness(LifePercent, HoldFraction);
            if (brightness <= 0f)
                return;
            // Radiation streams billow and throb rather than glowing steadily.
            brightness *= 0.78f + 0.22f * Mathf.Sin((AgeTicks + (seed & 63)) * 0.18f);
            float length = (end - start).Yto0().magnitude;
            if (length < 0.05f || thickness <= 0f)
                return;
            Vector3 side = WNABeamLineUtility.HorizontalNormal(start, end);
            float halfWidth = thickness * 0.5f;
            int segments = Mathf.Clamp(Mathf.CeilToInt(length / SegmentLength), MinSegments, StripMesh.MaxPoints - 1);
            float wavelength = Mathf.Clamp(amplitude * 2f, 0.6f, 8f);
            float cycleCount = Mathf.Max(1f, length / wavelength);
            float phase = -LifePercent * cycleCount * (Mathf.PI * 2f) * TravelCycles;
            float drift = (seed % 1000) * 0.017f;
            strip.Begin();
            core.Begin();
            for (int i = 0; i <= segments; i++)
            {
                float t = (float)i / (float)segments;
                float envelope = Mathf.Sin(t * Mathf.PI);
                // Layered harmonics give the turbulent, drifting edge of a radiation stream.
                float wave = Mathf.Sin(t * cycleCount * (Mathf.PI * 2f) + phase) * 0.55f
                    + Mathf.Sin(t * cycleCount * (Mathf.PI * 2f) * 2.37f - phase * 1.6f + 1.3f) * 0.3f
                    + Mathf.Sin(t * cycleCount * (Mathf.PI * 2f) * 0.61f + phase * 0.5f + drift) * 0.4f;
                Vector3 center = Vector3.Lerp(start, end, t) + side * (wave * amplitude * envelope);
                float breath = (1f + 0.35f * Mathf.Sin(t * cycleCount * (Mathf.PI * 2f) * 1.5f + phase + drift)) * (0.65f + 0.35f * envelope);
                strip.AddPoint(center, side * (halfWidth * breath), t);
                core.AddPoint(center, side * (halfWidth * CoreWidthScale * breath), t);
            }
            strip.Finish();
            core.Finish();
            Material glowMaterial = BeamMaterials.Glow(color * brightness);
            if (glowMaterial != null && strip.HasGeometry)
                Graphics.DrawMesh(strip.Mesh, Matrix4x4.identity, glowMaterial, 0);
            Color coreColor = Color.Lerp(color, Color.white, 0.55f);
            Material coreMaterial = BeamMaterials.Glow(coreColor * brightness);
            if (coreMaterial != null && core.HasGeometry)
                Graphics.DrawMesh(core.Mesh, Matrix4x4.identity, coreMaterial, 0);
        }

        public override void Release()
        {
            StripMeshPool.Return(strip);
            strip = null;
            StripMeshPool.Return(core);
            core = null;
            base.Release();
        }
    }
}
