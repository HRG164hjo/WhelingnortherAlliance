using HarmonyLib;
using RimWorld;
using Verse;

namespace WNA.WNAHarmony
{
    public class Patch_CompOverseerSubject
    {
        private static readonly HediffDef Hediff_InMechanoid = HediffDef.Named("WNA_InMechanoid");
        private static readonly PreceptDef Precept_Proselyte = DefDatabase<PreceptDef>.GetNamedSilentFail("WNA_P_Proselyte");

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
                    TryRemoveHediff(mech, Hediff_InMechanoid);
                    return;
                }
                if (overseer.Ideo?.HasPrecept(Precept_Proselyte) == true)
                    TryAddHediff(mech, Hediff_InMechanoid);
                else
                    TryRemoveHediff(mech, Hediff_InMechanoid);
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
                Hediff existing = mech.health.hediffSet.GetFirstHediffOfDef(Hediff_InMechanoid);
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
                Hediff existing = mech.health.hediffSet.GetFirstHediffOfDef(Hediff_InMechanoid);
                if (existing != null)
                    mech.health.RemoveHediff(existing);
            }
        }
    }
}
