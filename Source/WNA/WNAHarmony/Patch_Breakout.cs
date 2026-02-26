using HarmonyLib;
using RimWorld;
using System.Collections.Generic;
using System.Linq;
using Verse;
using WNA.WNAUtility;

namespace WNA.WNAHarmony
{
    public class Patch_Breakout
    {
        private static bool HasProselytePrecept()
        {
            var ideo = Faction.OfPlayer.ideos?.PrimaryIdeo;
            return ideo?.HasPrecept(DefDatabase<PreceptDef>.GetNamedSilentFail("WNA_P_Proselyte")) == true;
        }
        private static void ApplyCatatonicBreakdown(Pawn pawn)
        {
            var hediff = HediffMaker.MakeHediff(HediffDefOf.CatatonicBreakdown, pawn);
            pawn.health.AddHediff(hediff);
            var comp = hediff.TryGetComp<HediffComp_Disappears>();
            if (comp != null)
                comp.ticksToDisappear = 900000;
            pawn.stances.CancelBusyStanceSoft();
            pawn.health.forceDowned = true;
        }
        [HarmonyPatch(typeof(PrisonBreakUtility),
            nameof(PrisonBreakUtility.CanParticipateInPrisonBreak))]
        public static class Patch_PrisonBreakUtility_CanParticipate
        {
            public static void Postfix(Pawn pawn, ref bool __result)
            {
                if (!__result || pawn?.Map == null) return;
                if (pawn.Map.Biome?.inVacuum == true)
                {
                    __result = false;
                    return;
                }
                Room room = pawn.GetRoom();
                if (room == null) return;
                var doors = RoomDoorUtility.GetAllDoors(room);
                if (doors.Count > 0 && doors.All(d => d?.def?.thingClass?.Name == "BarrierDoor"))
                    __result = false;
            }
        }
        [HarmonyPatch(typeof(ContainmentUtility),
            nameof(ContainmentUtility.CanParticipateInEscape))]
        public static class Patch_ContainmentUtility_CanParticipate
        {
            public static void Postfix(Pawn pawn, ref bool __result)
            {
                if (!__result || pawn?.Map == null) return;
                if (pawn.Map.Biome?.inVacuum == true)
                {
                    __result = false;
                    return;
                }
                Room room = pawn.GetRoom();
                if (room == null) return;
                var doors = RoomDoorUtility.GetAllDoors(room);
                if (doors.Count > 0 && doors.All(d => d?.def?.thingClass?.Name == "BarrierDoor"))
                    __result = false;
            }
        }
        [HarmonyPatch(typeof(PrisonBreakUtility), "StartPrisonBreakIn")]
        public static class Patch_PrisonBreakUtility_MakeDown
        {
            public static void Postfix(List<Pawn> outAllEscapingPrisoners)
            {
                if (outAllEscapingPrisoners.NullOrEmpty()) return;
                if (!HasProselytePrecept()) return;

                foreach (var pawn in outAllEscapingPrisoners)
                    ApplyCatatonicBreakdown(pawn);
            }
        }
    }
}
