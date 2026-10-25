using UnityEngine;
using Verse;

namespace WNA.WNAUtility.Renderer
{
    [StaticConstructorOnStartup]
    internal static class BeamMaterials
    {
        private const int AlphaSteps = 20;

        private const int TextureWidth = 64;

        private const int TextureHeight = 4;

        private static Texture2D softGradient;
        public static Texture2D SoftGradient
        {
            get
            {
                if (softGradient == null)
                    softGradient = CreateSoftGradient();
                return softGradient;
            }
        }
        public static Material Glow(Color color)
        {
            float alpha = Mathf.Clamp01(color.a);
            int step = Mathf.RoundToInt(alpha * AlphaSteps);
            if (step <= 0)
                return null;
            Color quantized = new Color(color.r, color.g, color.b, step / (float)AlphaSteps);
            Texture2D tex = SoftGradient;
            if (tex == null)
                return null;
            return MaterialPool.MatFrom(tex, ShaderDatabase.MoteGlow, quantized);
        }

        private static Texture2D CreateSoftGradient()
        {
            Texture2D tex = new Texture2D(TextureWidth, TextureHeight, TextureFormat.RGBA32, false)
            {
                name = "TFBeamSoftGradient",
                wrapMode = TextureWrapMode.Clamp,
                filterMode = FilterMode.Bilinear,
                anisoLevel = 0
            };
            Color32[] pixels = new Color32[TextureWidth * TextureHeight];
            for (int x = 0; x < TextureWidth; x++)
            {
                float u = (x + 0.5f) / TextureWidth;
                float edge = Mathf.Abs(u * 2f - 1f);
                byte value = (byte)Mathf.RoundToInt(Mathf.Pow(1f - edge, 2.2f) * 255f);
                for (int y = 0; y < TextureHeight; y++)
                    pixels[y * TextureWidth + x] = new Color32(255, 255, 255, value);
            }
            tex.SetPixels32(pixels);
            tex.Apply(false, true);
            return tex;
        }
    }
}
