using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Verse;

namespace WNA.WNAUtility.Renderer
{
    internal abstract class BeamEffect
    {
        protected Map map;
        protected Vector3 start;
        protected Vector3 end;
        protected int startTick;
        protected int durationTicks;
        public float LifePercent
        {
            get
            {
                if (durationTicks <= 0)
                    return 1f;
                return Mathf.Clamp01((CurrentTick - startTick) / (float)durationTicks);
            }
        }
        public bool Expired =>
            map == null || CurrentTick - startTick >= durationTicks;
        public int AgeTicks
        {
            get
            {
                int now = CurrentTick;
                if (now == int.MaxValue)
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
        public void Initialize(Map map, Vector3 start, Vector3 end, int durationTicks)
        {
            this.map = map;
            this.start = start;
            this.end = end;
            this.durationTicks = Mathf.Max(1, durationTicks);
            this.startTick = CurrentTick;
            OnInitialized();
        }
        protected virtual void OnInitialized()
        {
        }
        public abstract void Draw();
        public virtual void Release()
        {
            map = null;
        }
        protected static float Brightness(float lifePercent, float holdFraction)
        {
            float fadeIn = Mathf.Clamp01(lifePercent / 0.06f);
            if (lifePercent <= holdFraction)
                return fadeIn;
            float fadeOut = 1f - (lifePercent - holdFraction) / (1f - holdFraction);
            return fadeIn * Mathf.Clamp01(fadeOut);
        }
        protected static Color NormalizeColor(Color c)
        {
            return new Color(
                c.r > 1f ? c.r / 255f : c.r,
                c.g > 1f ? c.g / 255f : c.g,
                c.b > 1f ? c.b / 255f : c.b,
                c.a > 1f ? c.a / 255f : c.a);
        }
        protected static float Hash01(int seed, int a, int b)
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
        protected void DrawEndFlares(Color color, float width, float length, float brightness)
        {
            Vector3 dir = (end - start).Yto0();
            float total = dir.magnitude;
            if (total < 0.01f || width <= 0f || length <= 0f)
                return;
            dir /= total;
            Material material = BeamMaterials.Glow(color * brightness);
            if (material == null)
                return;
            DrawFlatQuad(start - dir * (length * 0.5f), start + dir * (length * 0.5f), width, material);
            DrawFlatQuad(end - dir * (length * 0.5f), end + dir * (length * 0.5f), width, material);
        }
        protected static void DrawFlatQuad(Vector3 a, Vector3 b, float width, Material material)
        {
            if (material == null || width <= 0f)
                return;
            Vector3 dir = b - a;
            dir.y = 0f;
            float length = dir.magnitude;
            if (length < 0.01f)
                return;
            Matrix4x4 matrix = default(Matrix4x4);
            matrix.SetTRS((a + b) * 0.5f, Quaternion.LookRotation(dir), new Vector3(width, 1f, length));
            Graphics.DrawMesh(MeshPool.plane10, matrix, material, 0);
        }
    }
}
