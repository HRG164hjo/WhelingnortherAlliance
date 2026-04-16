using HarmonyLib;
using RimWorld;
using Verse;
using WNA.WNAUtility;

namespace WNA.WNAHarmony
{
    public class Patch_MechanitorUtility
    {
        [HarmonyPatch(typeof(MechanitorUtility), nameof(MechanitorUtility.IsColonyMechRequiringMechanitor))]
        public static class Patch_MechanitorUtility_IsColonyMechRequiringMechanitor
        {
            static void Postfix(Pawn mech, ref bool __result)
            {
                if (__result && MindControlUtility.MindControlled(mech))
                    __result = false;
            }
        }
        // CompOverseerSubject
        private static readonly HediffDef hediff = HediffDef.Named("WNA_InMechanoid");
        private static readonly PreceptDef p = DefDatabase<PreceptDef>.GetNamedSilentFail("WNA_P_Proselyte");
        [HarmonyPatch(typeof(CompOverseerSubject), nameof(CompOverseerSubject.CompTick))]
        public static class Patch_CompOverseerSubject_CompTick
        {
            public static void Postfix(CompOverseerSubject __instance)
            {
                Pawn mech = __instance.Parent;
                if (!mech.IsHashIntervalTick(250))
                    return;
                Pawn overseer = (Pawn)AccessTools.Property(typeof(CompOverseerSubject), "Overseer").GetValue(__instance);
                if (overseer == null || __instance.State != OverseerSubjectState.Overseen)
                {
                    TryRemoveHediff(mech, hediff);
                    return;
                }
                if (overseer.Ideo?.HasPrecept(p) == true)
                    TryAddHediff(mech, hediff);
                else
                    TryRemoveHediff(mech, hediff);
            }
            private static void TryAddHediff(Pawn pawn, HediffDef def)
            {
                if (pawn?.health?.hediffSet == null)
                    return;
                if (!pawn.health.hediffSet.HasHediff(def))
                    pawn.health.AddHediff(def);
            }
            private static void TryRemoveHediff(Pawn pawn, HediffDef def)
            {
                if (pawn?.health?.hediffSet == null)
                    return;
                Hediff existing = pawn.health.hediffSet.GetFirstHediffOfDef(def);
                if (existing != null)
                    pawn.health.RemoveHediff(existing);
            }
        }
        [HarmonyPatch(typeof(CompOverseerSubject), nameof(CompOverseerSubject.Notify_DisconnectedFromOverseer))]
        public static class Patch_CompOverseerSubject_Notify_Disconnected
        {
            public static void Postfix(CompOverseerSubject __instance)
            {
                Pawn mech = __instance.Parent;
                Hediff existing = mech.health.hediffSet.GetFirstHediffOfDef(hediff);
                if (existing != null)
                    mech.health.RemoveHediff(existing);
            }
        }
        [HarmonyPatch(typeof(CompOverseerSubject), "ForceFeral")]
        public static class Patch_CompOverseerSubject_ForceFeral
        {
            public static void Postfix(CompOverseerSubject __instance)
            {
                Pawn mech = __instance.Parent;
                Hediff existing = mech.health.hediffSet.GetFirstHediffOfDef(hediff);
                if (existing != null)
                    mech.health.RemoveHediff(existing);
            }
        }
    }
}
