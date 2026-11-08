using UnityEngine;
using Verse;

namespace WNA.WNAUtility.Renderer
{
    internal sealed class NukeEffect
    {
        private static readonly Color FlashWhite = new Color(1f, 0.99f, 0.95f);

        private static readonly Color HotCore = new Color(1f, 0.92f, 0.72f);

        private static readonly Color FireYellow = new Color(1f, 0.68f, 0.26f);

        private static readonly Color FireOrange = new Color(0.95f, 0.38f, 0.1f);

        private static readonly Color FireDeep = new Color(0.56f, 0.14f, 0.05f);

        private static readonly Color SmokeBrown = new Color(0.4f, 0.34f, 0.29f);

        private static readonly Color SmokeDark = new Color(0.22f, 0.19f, 0.17f);

        private static readonly Color DustTan = new Color(0.46f, 0.4f, 0.33f);

        private const float GroundY = 0.06f;

        private Map map;

        private Vector3 center;

        private int startTick;

        private int durationTicks;

        private int seed;

        public bool Expired => map == null || AgeTicks >= durationTicks;

        public int AgeTicks
        {
            get
            {
                int now = CurrentTick;
                if (now == int.MaxValue || startTick == int.MaxValue)
                    return 0;
                return Mathf.Max(0, now - startTick);
            }
        }

        private static int CurrentTick
        {
            get
            {
                TickManager tickManager = Find.TickManager;
                return tickManager != null ? tickManager.TicksGame : int.MaxValue;
            }
        }

        public void Initialize(Map map, Vector3 center, int durationTicks)
        {
            this.map = map;
            this.center = center;
            this.durationTicks = Mathf.Max(1, durationTicks);
            this.startTick = CurrentTick;
            unchecked
            {
                seed = startTick * 6151 + Mathf.RoundToInt(center.x) * 7919 + Mathf.RoundToInt(center.z) * 6271;
            }
        }

        public void Release()
        {
            map = null;
        }

        public void Draw()
        {
            int age = AgeTicks;
            if (age >= durationTicks)
                return;
            DrawGroundGlow(age);
            DrawShockwave(age);
            DrawGroundDust(age);
            DrawStem(age);
            DrawCap(age);
            DrawFireball(age);
        }

        private void DrawGroundGlow(int age)
        {
            float t = Phase(age, 0, 140);
            if (t >= 1f)
                return;
            float radius = Mathf.Lerp(26f, 62f, EaseOut(t));
            float alpha = 0.9f * (1f - t) * (1f - t) * Mathf.Clamp01(age / 4f);
            Material material = NukeFxMaterials.Glow(Mul(FireOrange, alpha));
            DrawGround(radius * 2.2f, radius * 2.2f, material);
        }

        private void DrawShockwave(int age)
        {
            DrawRing(age, 0, 58, 10f, 86f, 0.95f, FlashWhite);
            DrawRing(age, 6, 80, 8f, 96f, 0.45f, FireYellow);
        }

        private void DrawRing(int age, int begin, int end, float startRadius, float endRadius, float peakAlpha, Color color)
        {
            float t = Phase(age, begin, end);
            if (t <= 0f || t >= 1f)
                return;
            float radius = Mathf.Lerp(startRadius, endRadius, EaseOut(t));
            float alpha = peakAlpha * (1f - t) * Mathf.Clamp01((age - begin) / 6f);
            float size = radius * 2f / NukeFxMaterials.RingBandRadius;
            DrawGround(size, size, NukeFxMaterials.Ring(Mul(color, alpha)));
        }

        private void DrawGroundDust(int age)
        {
            float t = Phase(age, 10, 180);
            if (t >= 1f)
                return;
            float radius = Mathf.Lerp(16f, 88f, EaseOut(t));
            float size = Mathf.Lerp(22f, 46f, t);
            float alpha = 0.34f * (1f - t) * Mathf.Clamp01((age - 10) / 8f);
            Material material = NukeFxMaterials.Cloud(Mul(DustTan, alpha));
            for (int i = 0; i < 4; i++)
            {
                float angle = seed % 360 + i * 90f + Hash01(seed, i, 3) * 30f;
                Vector3 dir = Quaternion.Euler(0f, angle, 0f) * Vector3.forward;
                float dist = radius * Mathf.Lerp(0.8f, 1.05f, Hash01(seed, i, 11));
                Vector3 pos = GroundPos + dir * dist + Vector3.up * (size * 0.25f);
                DrawBillboard(pos, size, size * 0.8f, material);
            }
        }

        private void DrawStem(int age)
        {
            if (age < 6)
                return;
            float rise = Phase(age, 6, 64);
            float height = Mathf.Lerp(6f, 46f, EaseOut(rise));
            float width = Mathf.Lerp(11f, 22f, rise);
            float fade = 1f - Phase(age, 130, 180);
            if (fade <= 0f)
                return;
            Material smoke = NukeFxMaterials.Cloud(Mul(SmokeBrown, 0.3f * fade));
            for (int i = 0; i < 3; i++)
            {
                float offsetX = (i - 1) * width * 0.3f;
                float offsetZ = (Hash01(seed, i, 5) - 0.5f) * width * 0.5f;
                float columnHeight = height * Mathf.Lerp(0.85f, 1f, Hash01(seed, i, 9));
                float columnWidth = width * Mathf.Lerp(0.75f, 1f, Hash01(seed, i, 13));
                Vector3 pos = GroundPos + new Vector3(offsetX, columnHeight * 0.5f, offsetZ);
                DrawBillboard(pos, columnWidth, columnHeight, smoke);
            }
            // The column is lit from below while the fireball is still burning.
            float hot = 1f - Phase(age, 40, 120);
            if (hot > 0f)
            {
                Vector3 pos = GroundPos + Vector3.up * (height * 0.35f);
                DrawBillboard(pos, width * 0.55f, height * 0.7f, NukeFxMaterials.Glow(Mul(FireOrange, 0.55f * hot)));
            }
        }

        private void DrawCap(int age)
        {
            if (age < 32)
                return;
            float t = Phase(age, 32, 170);
            float y = Mathf.Lerp(34f, 62f, EaseOut(t));
            float radius = Mathf.Lerp(13f, 54f, EaseOut(t));
            float fade = 1f - Phase(age, 145, 180);
            if (fade <= 0f)
                return;
            float alpha = 0.36f * fade;
            Material top = NukeFxMaterials.Cloud(Mul(SmokeBrown, alpha));
            Material under = NukeFxMaterials.Cloud(Mul(SmokeDark, alpha));
            Vector3 capPos = GroundPos + Vector3.up * y;

            DrawBillboard(capPos, radius * 1.9f, radius * 1.15f, top);
            for (int i = 0; i < 6; i++)
            {
                float angle = i * 60f + Hash01(seed, i, 21) * 40f;
                Vector3 dir = Quaternion.Euler(0f, angle, 0f) * Vector3.forward;
                float dist = radius * Mathf.Lerp(0.62f, 0.82f, Hash01(seed, i, 27));
                float rise = (Hash01(seed, i, 31) - 0.55f) * radius * 0.5f;
                Vector3 pos = capPos + dir * dist + Vector3.up * rise;
                float size = radius * Mathf.Lerp(0.75f, 1.05f, Hash01(seed, i, 37));
                DrawBillboard(pos, size, size * 0.85f, i % 2 == 0 ? under : top);
            }
            float hot = 1f - Phase(age, 55, 125);
            if (hot > 0f)
            {
                Vector3 pos = capPos - Vector3.up * (radius * 0.2f);
                DrawBillboard(pos, radius * 1.1f, radius * 0.9f, NukeFxMaterials.Glow(Mul(FireOrange, 0.7f * hot)));
            }
        }

        private void DrawFireball(int age)
        {
            float t = Phase(age, 0, 88);
            if (t >= 1f)
                return;
            float radius = Mathf.Lerp(6f, 46f, EaseOut(t));
            float y = Mathf.Lerp(2.5f, 15f, EaseOut(t));
            float fade = 1f - Mathf.Clamp01((age - 58f) / 30f);
            float flicker = Mathf.Lerp(0.82f, 1f, Hash01(seed, age, 41));
            Vector3 pos = GroundPos + Vector3.up * y;

            DrawBillboard(pos, radius * 4.2f, radius * 4.2f, NukeFxMaterials.Glow(Mul(FireDeep, 0.35f * fade * flicker)));
            DrawBillboard(pos, radius * 2.6f, radius * 2.6f, NukeFxMaterials.Glow(Mul(FireOrange, 0.55f * fade * flicker)));
            DrawBillboard(pos, radius * 1.7f, radius * 1.7f, NukeFxMaterials.Glow(Mul(FireYellow, 0.7f * fade * flicker)));
            float flash = 1f - Phase(age, 0, 34);
            if (flash > 0f)
                DrawBillboard(pos, radius * 1.1f, radius * 1.1f, NukeFxMaterials.Glow(Mul(HotCore, 0.95f * flash)));
        }

        private Vector3 GroundPos => new Vector3(center.x, center.y, center.z);

        private void DrawGround(float width, float length, Material material)
        {
            if (material == null || width <= 0f || length <= 0f)
                return;
            Vector3 pos = new Vector3(center.x, center.y + GroundY, center.z);
            Matrix4x4 matrix = default(Matrix4x4);
            matrix.SetTRS(pos, Quaternion.identity, new Vector3(width, 1f, length));
            Graphics.DrawMesh(MeshPool.plane10, matrix, material, 0);
        }

        private static void DrawBillboard(Vector3 pos, float width, float height, Material material)
        {
            if (material == null || width <= 0f || height <= 0f)
                return;
            Camera camera = Find.Camera;
            Vector3 toCamera = camera != null ? camera.transform.position - pos : Vector3.forward;
            toCamera.y = 0f;
            if (toCamera.sqrMagnitude < 0.0001f)
                toCamera = Vector3.forward;
            Quaternion rotation = Quaternion.LookRotation(toCamera) * Quaternion.Euler(90f, 0f, 0f);
            Matrix4x4 matrix = default(Matrix4x4);
            matrix.SetTRS(pos, rotation, new Vector3(width, 1f, height));
            Graphics.DrawMesh(MeshPool.plane10, matrix, material, 0);
        }

        private static Color Mul(Color color, float alpha)
        {
            return new Color(color.r, color.g, color.b, Mathf.Clamp01(alpha));
        }

        private static float Phase(int age, int begin, int end)
        {
            if (end <= begin)
                return age >= end ? 1f : 0f;
            return Mathf.Clamp01((age - begin) / (float)(end - begin));
        }

        private static float EaseOut(float t)
        {
            float inv = 1f - t;
            return 1f - inv * inv;
        }

        private static float Hash01(int seed, int a, int b)
        {
            unchecked
            {
                uint h = (uint)(seed * 747796405 + a * 2891336453 + b * 1013904223);
                h = (h ^ (h >> 16)) * 2246822519u;
                h = (h ^ (h >> 13)) * 3266489917u;
                h ^= h >> 16;
                return (h & 0xFFFFFF) / 16777215f;
            }
        }
    }
}
