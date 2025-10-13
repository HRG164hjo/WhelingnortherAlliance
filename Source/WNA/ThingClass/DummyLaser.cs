using UnityEngine;
using Verse;

namespace WNA.ThingClass
{
    public class DummyLaser : Thing
    {
        public Vector3 start;
        public Vector3 end;
        public Color cIn = Color.red;
        public Color cOut = Color.red;
        public float width = 0.2f;
        public float spr = 1.2f;
        public int dura = 30;
        public int dura2 = 36;
        private int age = 0;
        protected override void Tick()
        {
            base.Tick();
            age++;
            if (age > Mathf.Max(dura, dura2))
                Destroy();
        }
        protected override void DrawAt(Vector3 drawLoc, bool flip = false)
        {
            base.DrawAt(drawLoc, flip);
            Vector3 dir = end - start;
            float length = dir.MagnitudeHorizontal();
            if (length < 0.01f)
                return;
            Vector3 mid = (start + end) / 2f;
            Quaternion rot = Quaternion.LookRotation(dir.Yto0());
            float innerAlpha = 1f - (float)age / dura;
            Color inner = new Color(cIn.r, cIn.g, cIn.b, cIn.a * innerAlpha);
            Vector3 scale = new Vector3(width, 1f, length);
            Matrix4x4 matrix = default;
            matrix.SetTRS(mid + Vector3.up * 0.05f, rot, scale);
            Material matInner = SolidColorMaterials.SimpleSolidColorMaterial(inner, false);
            Graphics.DrawMesh(MeshPool.plane10, matrix, matInner, 0);
            float outerAlpha = 1f - (float)age / dura2;
            Color outer = new Color(cOut.r, cOut.g, cOut.b, cOut.a * outerAlpha);
            Vector3 scaleGlow = new Vector3(width * spr, 1f, length);
            Matrix4x4 matrixGlow = default;
            matrixGlow.SetTRS(mid + Vector3.up * 0.051f, rot, scaleGlow);
            Material matGlow = SolidColorMaterials.SimpleSolidColorMaterial(outer, false);
            Graphics.DrawMesh(MeshPool.plane10, matrixGlow, matGlow, 0);
        }
    }
}
