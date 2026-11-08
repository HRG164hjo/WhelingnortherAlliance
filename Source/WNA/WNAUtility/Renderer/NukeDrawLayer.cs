using System.Collections.Generic;
using UnityEngine;
using Verse;

namespace WNA.WNAUtility.Renderer
{
    internal sealed class NukeDrawLayer : MapDrawLayer
    {
        private readonly List<NukeEffect> effects = new List<NukeEffect>(2);
        public override bool Visible => effects.Count > 0;

        public NukeDrawLayer(Map map)
            : base(map)
        {
        }
        public override void Regenerate()
        {
        }

        public override void DrawLayer()
        {
            for (int i = effects.Count - 1; i >= 0; i--)
            {
                if (effects[i].Expired)
                {
                    effects[i].Release();
                    effects.RemoveAt(i);
                }
            }
            for (int i = 0; i < effects.Count; i++)
                effects[i].Draw();
        }
        public static void Spawn(Map map, Vector3 center, int durationTicks)
        {
            if (map == null || map.mapDrawer == null || map.mapDrawer.GlobalLayers == null)
                return;
            NukeDrawLayer layer = map.mapDrawer.GetGlobalLayer<NukeDrawLayer>();
            if (layer == null)
                return;
            NukeEffect effect = new NukeEffect();
            effect.Initialize(map, center, durationTicks);
            layer.effects.Add(effect);
        }
    }
}
