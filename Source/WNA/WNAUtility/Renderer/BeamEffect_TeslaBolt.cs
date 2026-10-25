using UnityEngine;
using Verse;
using WNA.WNAModExtension;

namespace WNA.WNAUtility.Renderer
{
    internal class BeamEffect_TeslaBolt : BeamEffect
    {
        private const float HoldFraction = 0.35f;
        private const float SegmentLength = 0.28f;
        private const int MinSegments = 10;
        private const float WalkDamping = 0.62f;
        private const float WalkLimit = 1.5f;
        private const float CoreWidthScale = 0.45f;
        private const float FlareWidthScale = 3.2f;
        private const float FlareLengthScale = 2f;
        private readonly StripMesh[] strips = new StripMesh[WeaponTeslaBoltConfig.BoltCount];
        private readonly StripMesh[] cores = new StripMesh[WeaponTeslaBoltConfig.BoltCount];
        private readonly Color[] colors = new Color[WeaponTeslaBoltConfig.BoltCount];
        private readonly bool[] enabled = new bool[WeaponTeslaBoltConfig.BoltCount];
        private float amplitude = 0.35f;
        private float thickness = 0.15f;
        private int jitterInterval = 3;
        private int seed;
        public void Configure(WeaponTeslaBoltConfig config)
        {
            if (config == null)
                return;
            amplitude = Mathf.Max(0f, config.BoltAmplitude);
            thickness = Mathf.Max(0f, config.BoltThickness);
            jitterInterval = Mathf.Max(1, config.BoltJitterInterval);
            for (int b = 0; b < strips.Length; b++)
            {
                enabled[b] = config.IsBoltEnabled(b);
                colors[b] = NormalizeColor(config.GetBoltColor(b));
            }
        }
        protected override void OnInitialized()
        {
            unchecked
            {
                seed = startTick * 7919 + Mathf.RoundToInt(start.x * 13.7f) * 104729 + Mathf.RoundToInt(start.z * 29.3f);
            }
        }
        public override void Draw()
        {
            float brightness = Brightness(LifePercent, HoldFraction);
            if (brightness <= 0f)
                return;
            float length = (end - start).Yto0().magnitude;
            if (length < 0.05f || thickness <= 0f)
                return;
            Vector3 side = WNABeamLineUtility.HorizontalNormal(start, end);
            float halfWidth = thickness * 0.5f;
            int segments = Mathf.Clamp(Mathf.CeilToInt(length / SegmentLength), MinSegments, StripMesh.MaxPoints - 1);
            // RA2 tesla bolts re-strike constantly, so the arc shape rolls while it lasts.
            int rollIndex = AgeTicks / jitterInterval;

            for (int b = 0; b < strips.Length; b++)
            {
                if (!enabled[b])
                    continue;
                float boltBrightness = brightness * Mathf.Lerp(0.72f, 1f, Hash01(seed + b * 6151, AgeTicks, 0));
                Material material = BeamMaterials.Glow(colors[b] * boltBrightness);
                if (material == null)
                    continue;
                StripMesh strip = GetStrip(b);
                StripMesh core = GetCore(b);
                if (strip == null || core == null)
                    continue;
                BuildBolt(strip, core, b, segments, side, halfWidth, rollIndex);
                if (strip.HasGeometry)
                    Graphics.DrawMesh(strip.Mesh, Matrix4x4.identity, material, 0);
                Color coreColor = Color.Lerp(colors[b], Color.white, 0.65f);
                Material coreMaterial = BeamMaterials.Glow(coreColor * boltBrightness);
                if (coreMaterial != null && core.HasGeometry)
                    Graphics.DrawMesh(core.Mesh, Matrix4x4.identity, coreMaterial, 0);
                DrawEndFlares(coreColor, thickness * FlareWidthScale, thickness * FlareLengthScale, boltBrightness);
            }
        }
        private void BuildBolt(StripMesh strip, StripMesh core, int b, int segments, Vector3 side, float halfWidth, int rollIndex)
        {
            int boltSeed = seed + b * 7919;
            float offset = 0f;
            strip.Begin();
            core.Begin();
            for (int i = 0; i <= segments; i++)
            {
                float t = (float)i / (float)segments;
                float envelope = Mathf.Sin(t * Mathf.PI);
                if (i > 0 && i < segments)
                {
                    // A damped random walk with occasional kinks reads as a jagged lightning arc,
                    // unlike the smooth sine the previous version drew.
                    float step = Hash01(boltSeed, i, rollIndex) * 2f - 1f;
                    step *= 1f + Hash01(boltSeed + 104729, i, rollIndex) * 2.5f;
                    offset = Mathf.Clamp((offset + step) * WalkDamping, -WalkLimit, WalkLimit);
                }
                Vector3 center = Vector3.Lerp(start, end, t) + side * (offset * envelope * amplitude);
                strip.AddPoint(center, side * halfWidth, t);
                core.AddPoint(center, side * (halfWidth * CoreWidthScale), t);
            }
            strip.Finish();
            core.Finish();
        }

        private StripMesh GetStrip(int b)
        {
            if (strips[b] == null)
                strips[b] = StripMeshPool.Rent();
            return strips[b];
        }

        private StripMesh GetCore(int b)
        {
            if (cores[b] == null)
                cores[b] = StripMeshPool.Rent();
            return cores[b];
        }
        public override void Release()
        {
            for (int b = 0; b < strips.Length; b++)
            {
                StripMeshPool.Return(strips[b]);
                strips[b] = null;
                StripMeshPool.Return(cores[b]);
                cores[b] = null;
                enabled[b] = false;
            }
            base.Release();
        }
    }
}
