using HarmonyLib;
using RimWorld;
using System;
using UnityEngine;
using Verse;
using Verse.AI;

namespace WNA.WNAHarmony
{
    public class Patch_Job_Mining
    {
        [HarmonyPatch(typeof(JobDriver_Mine), "DoDamage")]
        public static class Patch_MiningDamage
        {
            static bool Prefix(JobDriver_Mine __instance, Thing target, Toil mine, Pawn actor, IntVec3 mineablePos)
            {
                int baseDamage = (target.def.building.isNaturalRock ? 80 : 40);
                float level = (actor?.skills?.GetSkill(SkillDefOf.Melee)?.Level ?? 0f) + (actor?.skills?.GetSkill(SkillDefOf.Mining)?.Level ?? 0f);
                float factor = Mathf.Sqrt(level + 0.01f);
                int num = (int)Math.Round(baseDamage * factor);
                if (num < 1) num = 1;
                if (!(target is Mineable mineable) || target.HitPoints > num)
                {
                    DamageInfo dinfo = new DamageInfo(DamageDefOf.Mining, num, 0f, -1f, actor);
                    target.TakeDamage(dinfo);
                    return false;
                }
                bool hasVeinDesignation = actor.Map.designationManager.DesignationAt(mineable.Position, DesignationDefOf.MineVein) != null;
                mineable.Notify_TookMiningDamage(target.HitPoints, actor);
                mineable.HitPoints = 0;
                mineable.DestroyMined(actor);
                if (hasVeinDesignation)
                {
                    IntVec3[] adjacentCells = GenAdj.AdjacentCells;
                    foreach (IntVec3 intVec in adjacentCells)
                    {
                        Designator_MineVein.FloodFillDesignations(mineablePos + intVec, actor.Map, mineable.def);
                    }
                }
                return false;
            }
        }
    }
}
