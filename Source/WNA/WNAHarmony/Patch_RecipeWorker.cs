using HarmonyLib;
using Verse;
using WNA.ThingCompProp;

namespace WNA.WNAHarmony
{
    public class Patch_RecipeWorker
    {
        [HarmonyPatch(typeof(RecipeWorker), "ConsumeIngredient")]
        public class Patch_ConsumeIngredient
        {
            [HarmonyPrefix]
            public static bool Prefix(Thing ingredient, RecipeDef recipe, Map map)
            {
                if (ingredient != null)
                {
                    CompMultiUse compUse = ingredient.TryGetComp<CompMultiUse>();
                    if (compUse != null)
                    {
                        if (compUse.Count > 1)
                        {
                            compUse.Count--;
                            return false;
                        }
                        compUse.Count = compUse.Props.uses;
                    }
                }
                return true;
            }
        }
    }
}
