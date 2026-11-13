using UnityEngine;
using Verse;

namespace WNA.WNAHediffClass
{
    [StaticConstructorOnStartup]
    internal class Gizmo_AttachEffectShield : Gizmo
    {
        public HdfClass_AttachEffect hediff;

        private static readonly Texture2D FullShieldBarTex = SolidColorMaterials.NewSolidColorTexture(new Color(0.2f, 0.2f, 0.24f));
        private static readonly Texture2D EmptyShieldBarTex = SolidColorMaterials.NewSolidColorTexture(Color.clear);

        public Gizmo_AttachEffectShield()
        {
            Order = -100f;
        }

        public override float GetWidth(float maxWidth)
        {
            return 140f;
        }

        public override GizmoResult GizmoOnGUI(Vector2 topLeft, float maxWidth, GizmoRenderParms parms)
        {
            Rect rect = new Rect(topLeft.x, topLeft.y, GetWidth(maxWidth), 75f);
            Rect rect2 = rect.ContractedBy(6f);
            Widgets.DrawWindowBackground(rect);

            float max = hediff.Config.shieldStrength;
            hediff.TryGetOutermostIntactLayer(out AttachEffectShieldLayer layer);
            float energy = layer?.energy ?? 0f;

            Rect titleRect = rect2;
            titleRect.height = rect.height / 2f;
            Text.Font = GameFont.Tiny;
            string title = "WNA_AE_ShieldGizmo".Translate();
            if (hediff.Config.cumulative || hediff.stacks > 1)
                title += " x" + hediff.stacks;
            Widgets.Label(titleRect, title);

            Rect barRect = rect2;
            barRect.yMin = rect2.y + rect2.height / 2f;
            Widgets.FillableBar(barRect, max > 0f ? energy / max : 0f, FullShieldBarTex, EmptyShieldBarTex, doBorder: false);
            Text.Font = GameFont.Small;
            Text.Anchor = TextAnchor.MiddleCenter;
            Widgets.Label(barRect, energy.ToString("F0") + " / " + max.ToString("F0"));
            Text.Anchor = TextAnchor.UpperLeft;
            TooltipHandler.TipRegion(rect2, hediff.TipStringExtra);
            return new GizmoResult(GizmoState.Clear);
        }
    }
}