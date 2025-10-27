using HarmonyLib;
using Verse;
using static Verse.DamageWorker;

namespace WNA.WNAUtility
{
    public static class IronCurtainUtility
    {
        public static void IronGive(Thing t, int durationTicks)
        {
            Iron_GameComp.Instance?.IronGive(t, durationTicks);
        }
        public static void IronRemove(Thing t)
        {
            Iron_GameComp.Instance?.IronRemove(t);
        }
        public static bool IsIroned(Thing t)
        {
            return Iron_GameComp.Instance?.IsIroned(t) ?? false;
        }
        public static void IronKill(Map map, IntVec3 center, float radius)
        {
            Iron_GameComp.Instance?.IronKill(map, center, radius);
        }
    }
    [HarmonyPatch(typeof(Thing), "TakeDamage")]
    public static class Patch_Thing_DamageIron
    {
        static bool Prefix(ref DamageInfo dinfo, Thing __instance, ref DamageResult __result)
        {
            if (__instance == null) return true;
            if (IronCurtainUtility.IsIroned(__instance))
            {
                __result = new DamageResult();
                return false;
            }
            return true;
        }
    }
    [HarmonyPatch(typeof(Thing), "Destroy")]
    public static class Patch_Thing_DestroyIron
    {
        static bool Prefix(Thing __instance, DestroyMode mode)
        {
            if (__instance == null) return true;
            if (IronCurtainUtility.IsIroned(__instance))
            {
                if (mode == DestroyMode.WillReplace ||
                    mode == DestroyMode.QuestLogic) return true;
                return false;
            }
            return true;
        }
    }
    [HarmonyPatch(typeof(HediffSet), "AddDirect")]
    public static class Patch_HediffIron
    {
        static bool Prefix(HediffSet __instance, Hediff hediff)
        {
            var pawn = __instance.pawn;
            if (pawn != null && IronCurtainUtility.IsIroned(pawn))
                return false;
            return true;
        }
    }
}
