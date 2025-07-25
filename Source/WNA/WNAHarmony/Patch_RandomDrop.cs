using HarmonyLib;
using RimWorld;
using System;
using System.Collections.Generic;
using Verse;

namespace WNA.WNAHarmony
{
    [HarmonyPatch(typeof(PawnsArrivalModeWorker_RandomDrop), "Arrive")]
    public class Patch_RandomDrop
    {
        [HarmonyPrefix]
        public static bool Prefix(List<Pawn> pawns, IncidentParms parms)
        {
            try
            {
                if (pawns == null || parms?.target == null || !(parms.target is Map map))
                {
                    Log.Warning("[RandomDropFix] Invalid parameters, fallback to original logic.");
                    return true;
                }
                bool canRoofPunch = parms.faction?.HostileTo(Faction.OfPlayer) ?? false;
                for (int i = 0; i < pawns.Count; i++)
                {
                    DropPodUtility.DropThingsNear(
                        dropCenter: DropCellFinder.RandomDropSpot(map),
                        map: map,
                        things: Gen.YieldSingle((Thing)pawns[i]),
                        openDelay: parms.podOpenDelay,
                        canInstaDropDuringInit: false,
                        leaveSlag: true,
                        canRoofPunch: canRoofPunch,
                        forbid: true,
                        allowFogged: true,
                        faction: parms.faction
                    );
                }
                return false;
            }
            catch (Exception ex)
            {
                Log.Error($"[RandomDropFix] Error in patched Arrive method: {ex}");
                return true;
            }
        }
    }
}
