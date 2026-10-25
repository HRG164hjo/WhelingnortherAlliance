using System.Collections.Generic;
using RimWorld;
using Verse;
using WNA.WNADefOf;

namespace WNA.WNAUtility
{
    public static class AbilityGrantUtility
    {
        public const string RecipientRoleId = "ability_recipient";

        public static bool RitualAvailable(out string reason)
        {
            reason = null;
            if (!ModsConfig.IdeologyActive)
            {
                reason = "WNA_AbilityRitual_RequiresIdeology".Translate().Resolve();
                return false;
            }
            ResearchProjectDef research = WNAMainDefOf.WNA_TranscendentalArts;
            if (research == null || !research.IsFinished)
            {
                string researchName = research != null ? research.LabelCap.Resolve() : "WNA_TranscendentalArts";
                reason = "WNA_AbilityRitual_RequiresResearch".Translate(researchName.Named("RESEARCH")).Resolve();
                return false;
            }
            return true;
        }

        public static List<AbilityDef> SelectableAbilities(Pawn pawn)
        {
            List<AbilityDef> result = new List<AbilityDef>();
            if (pawn == null || pawn.abilities == null)
                return result;
            HashSet<AbilityDef> known = new HashSet<AbilityDef>();
            List<Ability> current = pawn.abilities.AllAbilitiesForReading;
            for (int i = 0; i < current.Count; i++)
            {
                if (current[i]?.def != null)
                    known.Add(current[i].def);
            }
            int psylinkLevel = pawn.GetPsylinkLevel();
            foreach (AbilityDef def in DefDatabase<AbilityDef>.AllDefs)
            {
                if (def == null || def.verbProperties == null)
                    continue;
                if (known.Contains(def))
                    continue;
                if (def.IsPsycast && psylinkLevel < def.level)
                    continue;
                result.Add(def);
            }
            return result;
        }

        public static string SourceLabel(AbilityDef def)
        {
            ModContentPack pack = def?.modContentPack;
            if (pack == null)
                return "WNA_AbilityRitual_UnknownSource".Translate().Resolve();
            switch (pack.PackageId)
            {
                case "ludeon.rimworld":
                    return "WNA_AbilityRitual_SourceCore".Translate().Resolve();
                case "ludeon.rimworld.royalty":
                    return "WNA_AbilityRitual_SourceRoyalty".Translate().Resolve();
                case "ludeon.rimworld.ideology":
                    return "WNA_AbilityRitual_SourceIdeology".Translate().Resolve();
                case "ludeon.rimworld.biotech":
                    return "WNA_AbilityRitual_SourceBiotech".Translate().Resolve();
                case "ludeon.rimworld.anomaly":
                    return "WNA_AbilityRitual_SourceAnomaly".Translate().Resolve();
                case "ludeon.rimworld.odyssey":
                    return "WNA_AbilityRitual_SourceOdyssey".Translate().Resolve();
                case "hrg164hjo.whelingnorther.alliance":
                    return "WNA_AbilityRitual_SourceWNA".Translate().Resolve();
                default:
                    return pack.Name;
            }
        }

        public static int SourceOrder(string packageId)
        {
            switch (packageId)
            {
                case "ludeon.rimworld": return 0;
                case "ludeon.rimworld.royalty": return 1;
                case "ludeon.rimworld.ideology": return 2;
                case "ludeon.rimworld.biotech": return 3;
                case "ludeon.rimworld.anomaly": return 4;
                case "ludeon.rimworld.odyssey": return 5;
                case "hrg164hjo.whelingnorther.alliance": return 6;
                default: return 100;
            }
        }

        public static string CategoryLabel(AbilityDef def)
        {
            if (def?.category == null)
                return "WNA_AbilityRitual_Uncategorized".Translate().Resolve();
            return def.category.LabelCap.Resolve();
        }
    }
}
