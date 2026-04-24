using HarmonyLib;
using RimWorld;
using System.Collections.Generic;
using System.Linq;
using Verse;
using WNA.WNADefOf;
using WNA.WNAModExtension;

namespace WNA.WNAHarmony
{
    public class Patch_TradeShip
    {
        [HarmonyPatch(typeof(TradeShip))]
        [HarmonyPatch(MethodType.Constructor)]
        [HarmonyPatch(new System.Type[] { typeof(TraderKindDef), typeof(Faction) })]
        public static class Patch_TradeShip_Namer
        {
            [HarmonyPostfix]
            public static void Postfix(TradeShip __instance, TraderKindDef def, Faction faction)
            {
                if (def == null) return;

                var ext = def.GetModExtension<PassingShipNamer>();
                if (ext == null) return;

                RulePackDef namer = ext.namer ?? WNAMainDefOf.WNA_NamerPassingShip;
                if (namer == null) return;

                List<string> tmpExtantNames = new List<string>();

                foreach (var map in Find.Maps)
                    tmpExtantNames.AddRange(map.passingShipManager.passingShips.Select(s => s.name));

                string generated = NameGenerator.GenerateName(namer, tmpExtantNames);

                if (faction != null)
                    __instance.name = "GuildTradeShipName".Translate(generated, faction.Name);
                else
                    __instance.name = generated;
            }
        }
    }
}
