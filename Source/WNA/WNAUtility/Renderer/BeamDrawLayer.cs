using System.Collections.Generic;
using Verse;

namespace WNA.WNAUtility.Renderer
{
    internal sealed class BeamDrawLayer : MapDrawLayer
    {
        private readonly List<BeamEffect> effects = new List<BeamEffect>(8);
        public override bool Visible => effects.Count > 0;

        public BeamDrawLayer(Map map)
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
                BeamEffect effect = effects[i];
                if (effect.Expired)
                {
                    effects.RemoveAt(i);
                    effect.Release();
                }
            }
            for (int i = 0; i < effects.Count; i++)
                effects[i].Draw();
        }
        public void Add(BeamEffect effect)
        {
            if (effect != null)
                effects.Add(effect);
        }
        public static BeamDrawLayer For(Map map)
        {
            if (map == null)
                return null;
            MapDrawer drawer = map.mapDrawer;
            if (drawer == null || drawer.GlobalLayers == null)
                return null;
            return drawer.GetGlobalLayer<BeamDrawLayer>();
        }
        public static void Spawn(BeamEffect effect, Map map)
        {
            if (effect == null)
                return;
            BeamDrawLayer layer = For(map);
            if (layer == null)
            {
                effect.Release();
                return;
            }
            layer.Add(effect);
        }
    }
}
