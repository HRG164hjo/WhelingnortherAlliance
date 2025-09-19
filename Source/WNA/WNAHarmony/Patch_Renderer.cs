using HarmonyLib;
using UnityEngine;
using Verse;
using WNA.WNADefOf;

namespace WNA.WNAHarmony
{
    public class Patch_Renderer
    {
        /*
        [HarmonyPatch(typeof(PawnRenderer), "RenderPawnInternal")]
        public static class PawnRenderer_RenderPawnInternal_Patch
        {
            public static void Postfix(PawnRenderer __instance, Pawn ___pawn, Vector3 rootLoc)
            {
                if (___pawn == null || ___pawn.Dead) return;
                var hediff = ___pawn.health.hediffSet.GetFirstHediffOfDef(WNAMainDefOf.WNA_ChronoFreeze);
                if (hediff != null)
                {
                    Color color = new Color(0.5f, 0.8f, 1.0f, 0.4f);
                    DrawAlphaOverlay(rootLoc, ___pawn, color);
                }
                var hediffMalicious = ___pawn.health.hediffSet.GetFirstHediffOfDef(WNAMainDefOf.WNA_ChronoFreezeMalicious);
                if (hediffMalicious != null)
                {
                    Color color = new Color(1.0f, 1.0f, 1.0f, 0.4f);
                    DrawAlphaOverlay(rootLoc, ___pawn, color);
                }
            }
            private static void DrawAlphaOverlay(Vector3 drawLoc, Pawn pawn, Color color)
            {
                // 你需要创建一个半透明的材质
                // 这是一个简单的例子，你可能需要一个更复杂的 Shader
                Material material = new Material(Shader.Find("Hidden/RimWorld/PawnTransparentShader"));
                material.color = color;
                // 获取身体网格
                Mesh mesh = MeshPool.humanlikeBodySet.MeshAt(pawn.Rotation);
                // 绘制一个覆盖层
                Graphics.DrawMesh(mesh, drawLoc, Quaternion.AngleAxis(0f, Vector3.up), material, 0);
            }
        }
        */
    }
}
