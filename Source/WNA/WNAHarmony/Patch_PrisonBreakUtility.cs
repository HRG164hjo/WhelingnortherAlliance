using HarmonyLib;
using RimWorld;
using System.Collections.Generic;
using System.Linq;
using Verse;
using WNA.WNAUtility;

namespace WNA.WNAHarmony
{
    internal class Patch_PrisonBreakUtility
    {
        [HarmonyPatch(typeof(PrisonBreakUtility),
            nameof(PrisonBreakUtility.CanParticipateInPrisonBreak))]
        internal static class Patch_PrisonBreakUtility_CanParticipate
        {
            internal static void Postfix(Pawn pawn, ref bool __result)
            {
                if (!__result || pawn == null) return;
                if (WNAPsychicDawnUtility.Active && pawn.IsPrisonerOfColony)
                {
                    __result = false;
                    return;
                }
                if (pawn.Map == null) return;
                if (pawn.Map.Biome?.inVacuum == true)
                {
                    __result = false;
                    return;
                }
                Room room = pawn.GetRoom();
                if (room == null) return;
                var doors = WNARoomDoorUtility.GetAllDoors(room);
                if (doors.Count > 0 && doors.All(d => d?.def?.thingClass?.Name == "BarrierDoor"))
                    __result = false;
            }
        }
        [HarmonyPatch(typeof(PrisonBreakUtility), "StartPrisonBreakIn")]
        internal static class Patch_PrisonBreakUtility_MakeDown
        {
            internal static void Postfix(List<Pawn> outAllEscapingPrisoners)
            {
                if (outAllEscapingPrisoners.NullOrEmpty())
                    return;
                if (!WNAPsychicDawnUtility.Active && !General.FactionHasIdeo(Faction.OfPlayer))
                    return;
                foreach (var pawn in outAllEscapingPrisoners)
                    WNAPsychicDawnUtility.ApplyCatatonicBreakdown(pawn);
            }
        }
    }
}
