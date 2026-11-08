using HarmonyLib;
using RimWorld.Planet;
using WNA.WNAUtility;

namespace WNA.WNAHarmony
{
    internal class Patch_CapturedSettlement
    {
        // The removal funnel every WorldObject.Destroy goes through, so third-party sweeps that delete ruins
        // directly - without MapParent.CheckRemoveMapNow - are captured too. Game teardown never calls Remove.
        [HarmonyPatch(typeof(WorldObjectsHolder), "Remove")]
        internal static class WorldObjectsHolder_Remove_Patch
        {
            private static void Postfix(WorldObject o)
            {
                WNACapturedSettlementUtility.Notify_WorldObjectRemoved(o);
            }
        }
    }
}
