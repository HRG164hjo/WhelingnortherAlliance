using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;
using WNA.WNADefOf;
using WNA.WNAUtility;

namespace WNA.WNADesignator
{
    public class Designator_ConsensusMotion : Designator
    {
        public Designator_ConsensusMotion()
        {
            defaultLabel = "WNA_ConsensusMotion_GizmoLabel".Translate();
            defaultDesc = "WNA_ConsensusMotion_GizmoDesc".Translate();
            icon = ContentFinder<Texture2D>.Get("factions/wna_alliance");
        }

        public override bool Visible =>
            WNAMainDefOf.WNA_ConsensusMotion?.IsFinished == true
            && ModsConfig.IdeologyActive
            && Find.IdeoManager != null
            && !Find.IdeoManager.classicMode
            && Faction.OfPlayer != null;

        public override AcceptanceReport CanDesignateCell(IntVec3 loc)
        {
            return AcceptanceReport.WasRejected;
        }

        public override void ProcessInput(Event ev)
        {
            if (!CheckCanInteract())
                return;
            OpenIdeoMenu();
        }

        private static void OpenIdeoMenu()
        {
            var options = new List<FloatMenuOption>();
            var seen = new HashSet<Ideo>();
            AddOption(options, seen, General.WNAIdeo);
            List<Pawn> colonists = PawnsFinder.AllMapsCaravansAndTravellingTransporters_Alive_FreeColonists;
            for (int i = 0; i < colonists.Count; i++)
                AddOption(options, seen, colonists[i].Ideo);
            if (options.Count == 0)
            {
                Messages.Message("WNA_ConsensusMotion_NoIdeo".Translate(), MessageTypeDefOf.RejectInput, historical: false);
                return;
            }
            Find.WindowStack.Add(new FloatMenu(options));
        }

        private static void AddOption(List<FloatMenuOption> options, HashSet<Ideo> seen, Ideo ideo)
        {
            if (ideo == null || !seen.Add(ideo))
                return;
            options.Add(new FloatMenuOption(
                ideo.name,
                delegate { ConfirmPromote(ideo); },
                ideo.Icon,
                Color.white));
        }

        private static void ConfirmPromote(Ideo ideo)
        {
            Find.WindowStack.Add(Dialog_MessageBox.CreateConfirmation(
                "WNA_ConsensusMotion_Confirm".Translate(ideo.name),
                delegate { PromoteToAllColonists(ideo); },
                destructive: true,
                title: "WNA_ConsensusMotion_ConfirmTitle".Translate()));
        }

        private static void PromoteToAllColonists(Ideo ideo)
        {
            if (ideo == null)
                return;
            List<Pawn> colonists = PawnsFinder.AllMapsCaravansAndTravellingTransporters_Alive_FreeColonists;
            for (int i = 0; i < colonists.Count; i++)
            {
                Pawn pawn = colonists[i];
                if (pawn.ideo != null && pawn.Ideo != ideo)
                    pawn.ideo.SetIdeo(ideo);
            }
            Messages.Message("WNA_ConsensusMotion_Done".Translate(ideo.name), MessageTypeDefOf.PositiveEvent, historical: false);
        }
    }
}
