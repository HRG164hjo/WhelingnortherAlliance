using RimWorld;
using System.Collections.Generic;
using UnityEngine;
using Verse;

namespace WNA.WNAUtility
{
    internal static class WNABeamLineUtility
    {
        private static readonly List<IntVec3> wallCells = new List<IntVec3>(64);
        public static void CellsAlongSegment(Vector3 from, Vector3 to, List<IntVec3> result)
        {
            result.Clear();
            Vector3 a = from.Yto0();
            Vector3 delta = to.Yto0() - a;
            float length = delta.magnitude;
            int steps = Mathf.Clamp(Mathf.CeilToInt(length * 4f), 1, 1024);
            IntVec3 last = IntVec3.Invalid;
            for (int i = 0; i <= steps; i++)
            {
                IntVec3 c = (a + delta * ((float)i / (float)steps)).ToIntVec3();
                if (c != last)
                {
                    result.Add(c);
                    last = c;
                }
            }
        }
        public static float DistanceToSegmentHorizontal(Vector3 p, Vector3 a, Vector3 b)
        {
            Vector3 ap = p.Yto0() - a.Yto0();
            Vector3 ab = b.Yto0() - a.Yto0();
            float sqrLen = ab.sqrMagnitude;
            if (sqrLen < 1E-06f)
                return ap.magnitude;
            float t = Mathf.Clamp01(Vector3.Dot(ap, ab) / sqrLen);
            return (ap - ab * t).magnitude;
        }
        public static Vector3 HorizontalNormal(Vector3 a, Vector3 b)
        {
            Vector3 dir = b.Yto0() - a.Yto0();
            float len = dir.magnitude;
            if (len < 1E-05f)
                return Vector3.right;
            dir /= len;
            return new Vector3(-dir.z, 0f, dir.x);
        }
        public static Vector3 HorizontalForward(Vector3 a, Vector3 b)
        {
            Vector3 dir = b.Yto0() - a.Yto0();
            float len = dir.magnitude;
            if (len < 1E-05f)
                return Vector3.forward;
            return dir / len;
        }
        public static bool TryGetBlockingWall(Map map, Vector3 from, Vector3 to, IntVec3 ignoreCell, out Thing wall)
        {
            wall = null;
            if (map == null)
                return false;
            CellsAlongSegment(from, to, wallCells);
            for (int i = 0; i < wallCells.Count; i++)
            {
                IntVec3 c = wallCells[i];
                if (c == ignoreCell || !c.InBounds(map))
                    continue;
                List<Thing> things = c.GetThingList(map);
                for (int j = 0; j < things.Count; j++)
                {
                    Thing thing = things[j];
                    if (thing.def.Fillage != FillCategory.Full)
                        continue;
                    if (thing is Building_Door door && door.Open)
                        continue;
                    wall = thing;
                    wallCells.Clear();
                    return true;
                }
            }
            wallCells.Clear();
            return false;
        }
    }
}
