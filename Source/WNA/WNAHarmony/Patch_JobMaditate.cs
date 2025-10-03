using HarmonyLib;
using RimWorld;
using System.Collections.Generic;
using UnityEngine;
using Verse;
using Verse.AI;
using WNA.WNADefOf;

namespace WNA.WNAHarmony
{
    public class Patch_JobMaditate
    {
        public static Dictionary<Pawn, int> MeditateStartTicks = new Dictionary<Pawn, int>();
        [HarmonyPatch(typeof(JobDriver_Meditate), "Notify_Starting", MethodType.Normal)]
        public static class JobDriver_Meditate_Starting
        {
            public static void Prefix(JobDriver_Meditate __instance)
            {
                Pawn pawn = __instance.pawn;
                if (pawn == null) return;
                if (Patch_JobMaditate.MeditateStartTicks.ContainsKey(pawn))
                    Patch_JobMaditate.MeditateStartTicks[pawn] = GenTicks.TicksGame;
                else
                    Patch_JobMaditate.MeditateStartTicks.Add(pawn, GenTicks.TicksGame);
            }
        }
        //[HarmonyPatch(typeof(RimWorld.JobDriver_Meditate), "EndJobWith", MethodType.Normal)]
        public static class JobDriver_Meditate_EndJobWith
        {
            private static readonly ThingDef rawres = WNAMainDefOf.WNA_Focus;
            private static readonly ThingDef wnthan = WNAMainDefOf.WNA_WNThan;
            private static readonly ThingDef whuman = WNAMainDefOf.WNA_Human;
            private static readonly PreceptDef wnpros = WNAMainDefOf.WNA_P_Proselyte;
            private static readonly TraitDef traitwna = WNAMainDefOf.WNA_Trait_Unshakable;
            private static readonly TraitDef traitvanilla = TraitDef.Named("Nerves");
            public static void Postfix(JobDriver __instance, JobCondition condition)
            {
                if (!(__instance is RimWorld.JobDriver_Meditate meditateDriver)) return;
                Pawn pawn = meditateDriver.pawn;
                if (pawn == null) return;
                if (condition != JobCondition.Succeeded &&
                    condition != JobCondition.InterruptForced &&
                    condition != JobCondition.InterruptOptional)
                {
                    Patch_JobMaditate.MeditateStartTicks.Remove(pawn);
                    return;
                }
                if (!Patch_JobMaditate.MeditateStartTicks.TryGetValue(pawn, out int startTick))
                {
                    return;
                }
                Patch_JobMaditate.MeditateStartTicks.Remove(pawn);
                int meditationDurationTicks = GenTicks.TicksGame - startTick;
                if (meditationDurationTicks < 250)
                {
                    return;
                }
                if (!IsValidPawn(pawn)) return;
                float consc = pawn.health.capacities.GetLevel(PawnCapacityDefOf.Consciousness);
                float skill = pawn.skills.GetSkill(SkillDefOf.Intellectual).Level;
                float stacknumFloat = 0.1f * skill * consc * ((float)meditationDurationTicks / 2000f);
                int stacknum = Mathf.CeilToInt(stacknumFloat);
                if (stacknum > 0)
                {
                    Thing thing = ThingMaker.MakeThing(rawres);
                    thing.stackCount = stacknum;
                    GenPlace.TryPlaceThing(thing, pawn.Position, pawn.Map, ThingPlaceMode.Near);
                }
            }
            private static bool IsValidPawn(Pawn pawn)
            {
                Trait validTrait = pawn.story.traits.GetTrait(traitvanilla);
                if (pawn.def == wnthan || pawn.def == whuman)
                    return true;
                if (pawn.Ideo == null || !pawn.Ideo.HasPrecept(wnpros))
                    return false;
                if ((pawn.story.traits.HasTrait(traitwna)) || (validTrait != null && validTrait.Degree == 2))
                    return true;
                return false;
            }
        }
    }
}
