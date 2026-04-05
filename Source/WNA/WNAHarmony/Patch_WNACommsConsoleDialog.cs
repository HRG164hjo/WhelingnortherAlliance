using HarmonyLib;
using RimWorld;
using System.Linq;
using Verse;
using WNA.WNADefOf;

namespace WNA.WNAHarmony
{
    public class Patch_WNACommsConsoleDialog
    {
        [HarmonyPatch(typeof(FactionDialogMaker), nameof(FactionDialogMaker.FactionDialogFor))]
        public static class Patch_FactionDialogFor
        {
            static void Postfix(ref DiaNode __result, Pawn negotiator, Faction faction)
            {
                var resA = ResearchProjectDef.Named("WNA_TheVoid");
                var resB = ResearchProjectDef.Named("WNA_WhelingnortherApocalypse");
                Faction wna = Find.FactionManager.FirstFactionOfDef(WNAMainDefOf.WNA_FactionWNA);
                if (faction.def != WNAMainDefOf.WNA_FactionWNA) return;
                if (faction.PlayerRelationKind != FactionRelationKind.Ally) return;
                if (!resA.IsFinished || resB.IsFinished) return;
                DiaOption rootOption = new DiaOption("WNA_Research_Dialog".Translate())
                {
                    resolveTree = false
                };
                DiaNode descNode = new DiaNode("WNA_OptionDesc_Dialog".Translate());
                DiaOption confirmStep1 = new DiaOption("WNA_Option_Dialog".Translate()) { resolveTree = false };
                DiaNode warningNode = new DiaNode("WNA_FinalWarning_Dialog".Translate());
                DiaOption finalConfirm = new DiaOption("WNA_OptionWellKnown_Dialog".Translate());
                int silverCount = negotiator.Map.resourceCounter.Silver;
                if (silverCount < 2357)
                    finalConfirm.Disable("WNA_NoSilver_Dialog".Translate());
                else
                {
                    finalConfirm.action = () =>
                    {
                        TradeUtility.LaunchSilver(negotiator.Map, 2357);
                        Find.ResearchManager.FinishProject(resB);
                        ApplyCultureChange(wna.ideos.PrimaryIdeo);
                        Find.LetterStack.ReceiveLetter("WNA_OptionDone_Dialog".Translate(),
                            "WNA_OptionDoneDesc_Dialog".Translate(),
                            LetterDefOf.NeutralEvent);
                    };
                    finalConfirm.resolveTree = true;
                }
                DiaOption alternative = new DiaOption("WNA_AltOption_Dialog".Translate())
                {
                    action = () =>
                    {
                        //ClearAllStorage(negotiator.Map);
                        if (silverCount >= 2357)
                            TradeUtility.LaunchSilver(negotiator.Map, 2357);
                        faction.TryAffectGoodwillWith(Faction.OfPlayer, -faction.PlayerGoodwill, false);
                        Find.ResearchManager.FinishProject(resB);
                        Find.LetterStack.ReceiveLetter("WNA_OptionDone_Dialog".Translate(),
                            "WNA_OptionDoneDesc_Dialog".Translate(),
                            LetterDefOf.NeutralEvent);
                    },
                    resolveTree = true,
                };
                DiaOption goBack = new DiaOption("WNA_GoBack_Dialog".Translate()) { resolveTree = true };
                warningNode.options.Add(finalConfirm);
                warningNode.options.Add(alternative);
                warningNode.options.Add(goBack);
                confirmStep1.link = warningNode;
                descNode.options.Add(confirmStep1);
                descNode.options.Add(new DiaOption("WNA_GoBack_Dialog".Translate()) { resolveTree = true });
                rootOption.link = descNode;
                bool exists = __result.options.Any(opt =>
                    opt.link != null &&
                    opt.link.text == "WNA_OptionDesc_Dialog".Translate()
                );
                if (!exists)
                    __result.options.Insert(0, rootOption);
            }
        }
        private static void ApplyCultureChange(Ideo newIdeo)
        {
            if (newIdeo == null) return;
            var allPawns = PawnsFinder.AllMapsCaravansAndTravellingTransporters_Alive_Colonists
                           .Concat(Find.WorldPawns.AllPawnsAliveOrDead.Where(p => p.IsColonist));
            foreach (var pawn in allPawns)
                pawn.ideo?.SetIdeo(newIdeo);
        }
        private static void ClearAllStorage(Map map)
        {
            var zones = map.zoneManager.AllZones.OfType<Zone_Stockpile>().ToList();
            foreach (var zone in zones)
            {
                foreach (var item in zone.AllContainedThings.ToList())
                    if(!(item is Pawn pawn) || item.def != ThingDefOf.Silver)
                        WNAUtility.General.DebugalDestroy(item);
            }
        }
    }
}
