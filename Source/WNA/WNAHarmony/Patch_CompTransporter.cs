using HarmonyLib;
using RimWorld;
using System.Collections.Generic;
using Verse;

namespace WNA.WNAHarmony
{
    public class Patch_CompTransporter
    {
        private static readonly HashSet<CompTransporter> noDropInstances = new HashSet<CompTransporter>();
        [HarmonyPatch(typeof(CompTransporter))]
        [HarmonyPatch("PostDeSpawn")]
        public static class CompTransporter_PostDeSpawn
        {
            public static bool Prefix(CompTransporter __instance, Map map, DestroyMode mode)
            {
                if (__instance.parent.BeingTransportedOnGravship || mode == DestroyMode.WillReplace || mode == DestroyMode.Vanish)
                {
                    noDropInstances.Add(__instance);
                    return false;
                }
                return true;
            }
        }
        [HarmonyPatch(typeof(CompTransporter), "CancelLoad", new System.Type[] { })]
        public static class CompTransporter_CancelLoadNoArg
        {
            public static bool Prefix(CompTransporter __instance)
            {
                if (noDropInstances.Contains(__instance))
                {
                    noDropInstances.Remove(__instance);
                    return false;
                }
                return true;
            }
        }
        [HarmonyPatch(typeof(CompTransporter), "CancelLoad", new System.Type[] { typeof(Map) })]
        public static class CompTransporter_CancelLoadMap
        {
            public static bool Prefix(CompTransporter __instance, Map map)
            {
                if (noDropInstances.Contains(__instance))
                {
                    noDropInstances.Remove(__instance);
                    return false;
                }
                return true;
            }
        }
        [HarmonyPatch(typeof(CompTransporter))]
        [HarmonyPatch("CleanUpLoadingVars")]
        public static class CompTransporter_CleanUpLoadingVars
        {
            public static bool Prefix(CompTransporter __instance, Map map)
            {
                if (noDropInstances.Contains(__instance)) return false;
                return true;
            }
            public static void Postfix(CompTransporter __instance, Map map)
            {
                if (noDropInstances.Contains(__instance))
                {
                    __instance.groupID = -1;
                    __instance.leftToLoad?.Clear();
                    __instance.Shuttle?.CleanUpLoadingVars();
                    var notifiedCantLoadMoreField = AccessTools.FieldRefAccess<CompTransporter, bool>("notifiedCantLoadMore");
                    var massUsageDirtyField = AccessTools.FieldRefAccess<CompTransporter, bool>("massUsageDirty");
                    notifiedCantLoadMoreField(__instance) = false;
                    massUsageDirtyField(__instance) = true;
                    noDropInstances.Remove(__instance);
                }
            }
        }
        [HarmonyPatch(typeof(CompTransporter))]
        [HarmonyPatch("PostExposeData")]
        public static class CompTransporter_PostExposeData_Fix
        {
            public static void Prefix(CompTransporter __instance)
            {
                if (Scribe.mode == LoadSaveMode.Saving)
                {
                    List<TransferableOneWay> leftToLoad = __instance.leftToLoad;
                    if (leftToLoad != null)
                    {
                        leftToLoad.RemoveAll((TransferableOneWay t) => t == null);
                        leftToLoad.RemoveAll((TransferableOneWay t) => !t.HasAnyThing);
                    }
                }
            }
        }
    }
}