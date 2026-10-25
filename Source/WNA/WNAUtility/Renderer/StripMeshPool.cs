using System.Collections.Generic;

namespace WNA.WNAUtility.Renderer
{
    internal static class StripMeshPool
    {
        private static readonly Stack<StripMesh> free = new Stack<StripMesh>(8);
        public static StripMesh Rent()
        {
            if (free.Count > 0)
                return free.Pop();
            return new StripMesh();
        }
        public static void Return(StripMesh strip)
        {
            if (strip == null)
                return;
            strip.Begin();
            strip.Finish();
            free.Push(strip);
        }
    }
}
