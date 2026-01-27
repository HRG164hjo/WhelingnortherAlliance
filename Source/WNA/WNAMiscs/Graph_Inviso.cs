using UnityEngine;
using Verse;
using WNA.ThingCompProp;

namespace WNA.WNAMiscs
{
    public class Graph_Inviso : Graphic_Single
    {
        public override void DrawWorker(Vector3 loc, Rot4 rot, ThingDef thingDef, Thing thing, float extraRotation)
        {
            var comp = thing.TryGetComp<CompCamoPillbox>();
            float alpha = comp?.GetAlpha() ?? 1f;
            Color oldColor = color;
            color.a *= alpha;
            base.DrawWorker(loc, rot, thingDef, thing, extraRotation);
            color = oldColor;
        }
    }
}
