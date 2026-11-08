using UnityEngine;
using Verse;

namespace WNA.WNAUtility.Renderer
{
    [StaticConstructorOnStartup]
    internal static class NukeFxMaterials
    {
        // Shockwave quads are sized so this fraction of their halfwidth lands on the ring band.
        internal const float RingBandRadius = 0.82f;

        private const int AlphaSteps = 24;

        private const int ColorSteps = 16;

        private const int RadialSize = 64;

        private const int CloudSize = 96;

        private const int RingSize = 128;

        private static Texture2D radialGlow;

        private static Texture2D cloudPuff;

        private static Texture2D ringBand;

        public static Material Glow(Color color)
        {
            return Cached(GetRadialGlow(), ShaderDatabase.MoteGlow, color);
        }

        public static Material Cloud(Color color)
        {
            return Cached(GetCloudPuff(), ShaderDatabase.Mote, color);
        }

        public static Material Ring(Color color)
        {
            return Cached(GetRingBand(), ShaderDatabase.MoteGlow, color);
        }

        private static Material Cached(Texture2D tex, Shader shader, Color color)
        {
            float alpha = Mathf.Clamp01(color.a);
            int alphaStep = Mathf.RoundToInt(alpha * AlphaSteps);
            if (alphaStep <= 0 || tex == null || shader == null)
                return null;
            Color quantized = new Color(
                Quantize(color.r),
                Quantize(color.g),
                Quantize(color.b),
                alphaStep / (float)AlphaSteps);
            return MaterialPool.MatFrom(tex, shader, quantized);
        }

        private static float Quantize(float value)
        {
            return Mathf.RoundToInt(Mathf.Clamp01(value) * ColorSteps) / (float)ColorSteps;
        }

        private static Texture2D GetRadialGlow()
        {
            if (radialGlow == null)
                radialGlow = CreateRadialGlow();
            return radialGlow;
        }

        private static Texture2D GetCloudPuff()
        {
            if (cloudPuff == null)
                cloudPuff = CreateCloudPuff();
            return cloudPuff;
        }

        private static Texture2D GetRingBand()
        {
            if (ringBand == null)
                ringBand = CreateRingBand();
            return ringBand;
        }

        private static Texture2D NewTexture(int size, string name)
        {
            return new Texture2D(size, size, TextureFormat.RGBA32, false)
            {
                name = name,
                wrapMode = TextureWrapMode.Clamp,
                filterMode = FilterMode.Bilinear,
                anisoLevel = 0
            };
        }

        private static Texture2D CreateRadialGlow()
        {
            Texture2D tex = NewTexture(RadialSize, "WNANukeRadialGlow");
            Color32[] pixels = new Color32[RadialSize * RadialSize];
            for (int y = 0; y < RadialSize; y++)
            {
                float v = (y + 0.5f) / RadialSize * 2f - 1f;
                for (int x = 0; x < RadialSize; x++)
                {
                    float u = (x + 0.5f) / RadialSize * 2f - 1f;
                    float r = Mathf.Sqrt(u * u + v * v);
                    float falloff = Mathf.Clamp01(1f - r);
                    byte value = (byte)Mathf.RoundToInt(Mathf.Pow(falloff, 1.7f) * 255f);
                    pixels[y * RadialSize + x] = new Color32(255, 255, 255, value);
                }
            }
            tex.SetPixels32(pixels);
            tex.Apply(false, true);
            return tex;
        }

        private static Texture2D CreateCloudPuff()
        {
            Texture2D tex = NewTexture(CloudSize, "WNANukeCloudPuff");
            Color32[] pixels = new Color32[CloudSize * CloudSize];
            for (int y = 0; y < CloudSize; y++)
            {
                float v = (y + 0.5f) / CloudSize;
                for (int x = 0; x < CloudSize; x++)
                {
                    float u = (x + 0.5f) / CloudSize;
                    float dx = u * 2f - 1f;
                    float dy = v * 2f - 1f;
                    float lump = Fractal(u * 3.4f, v * 3.4f);
                    // Break the circular silhouette with the same noise so puffs read as smoke, not discs.
                    float r = Mathf.Sqrt(dx * dx + dy * dy) * (1f + 0.42f * (lump - 0.5f));
                    float shape = Mathf.Clamp01((1.15f - r) / 0.6f);
                    float alpha = shape * shape * (3f - 2f * shape) * Mathf.Lerp(0.55f, 1f, lump);
                    pixels[y * CloudSize + x] = new Color32(255, 255, 255, (byte)Mathf.RoundToInt(Mathf.Clamp01(alpha) * 255f));
                }
            }
            tex.SetPixels32(pixels);
            tex.Apply(false, true);
            return tex;
        }

        private static Texture2D CreateRingBand()
        {
            Texture2D tex = NewTexture(RingSize, "WNANukeRingBand");
            Color32[] pixels = new Color32[RingSize * RingSize];
            for (int y = 0; y < RingSize; y++)
            {
                float v = (y + 0.5f) / RingSize * 2f - 1f;
                for (int x = 0; x < RingSize; x++)
                {
                    float u = (x + 0.5f) / RingSize * 2f - 1f;
                    float r = Mathf.Sqrt(u * u + v * v);
                    float d = (r - RingBandRadius) / 0.095f;
                    float band = Mathf.Exp(-d * d * 1.5f);
                    // Faint interior wash ties the front to the fireball instead of floating detached.
                    float wash = Mathf.Clamp01(1f - r / RingBandRadius) * 0.16f;
                    byte value = (byte)Mathf.RoundToInt(Mathf.Clamp01(band + wash) * 255f);
                    pixels[y * RingSize + x] = new Color32(255, 255, 255, value);
                }
            }
            tex.SetPixels32(pixels);
            tex.Apply(false, true);
            return tex;
        }

        private static float Fractal(float x, float y)
        {
            float sum = 0f;
            float norm = 0f;
            float amplitude = 0.55f;
            float frequency = 1f;
            for (int i = 0; i < 3; i++)
            {
                sum += ValueNoise(x * frequency, y * frequency) * amplitude;
                norm += amplitude;
                amplitude *= 0.5f;
                frequency *= 2.1f;
            }
            return norm > 0f ? sum / norm : 0.5f;
        }

        private static float ValueNoise(float x, float y)
        {
            int x0 = Mathf.FloorToInt(x);
            int y0 = Mathf.FloorToInt(y);
            float fx = x - x0;
            float fy = y - y0;
            float sx = fx * fx * (3f - 2f * fx);
            float sy = fy * fy * (3f - 2f * fy);
            float a = Hash(x0, y0);
            float b = Hash(x0 + 1, y0);
            float c = Hash(x0, y0 + 1);
            float d = Hash(x0 + 1, y0 + 1);
            return Mathf.Lerp(Mathf.Lerp(a, b, sx), Mathf.Lerp(c, d, sx), sy);
        }

        private static float Hash(int x, int y)
        {
            unchecked
            {
                uint h = (uint)(x * 374761393 + y * 668265263);
                h = (h ^ (h >> 13)) * 1274126177u;
                h ^= h >> 16;
                return (h & 0xFFFFFF) / 16777215f;
            }
        }
    }
}
