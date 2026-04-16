using RimWorld;
using System.Collections.Generic;
using System.Linq;
using Verse;
using WNA.WNAHediffClass;

namespace WNA.WNARecipeWorker
{
    public class ControlRelease : Recipe_Surgery
    {
        private static HediffDef controller =>
            DefDatabase<HediffDef>.GetNamed("WNA_MindControlEffect");
        public override bool AvailableOnNow(Thing thing, BodyPartRecord part = null)
        {
            if (!base.AvailableOnNow(thing, part)) return false;
            if (!(thing is Pawn pawn)) return false;
            return GetReleasableMindControl(pawn) != null;
        }
        public override void ApplyOnPawn(Pawn pawn, BodyPartRecord part, Pawn billDoer, List<Thing> ingredients, Bill bill)
        {
            if (billDoer != null)
            {
                if (CheckSurgeryFail(billDoer, pawn, ingredients, part, bill))
                    return;
                TaleRecorder.RecordTale(TaleDefOf.DidSurgery, billDoer, pawn);
            }
            var mc = GetReleasableMindControl(pawn);
            if (mc == null) return;
            Faction myFac = mc.myFac;
            pawn.health.RemoveHediff(mc);
            if (Faction.OfPlayer != null &&
                myFac != null &&
                !myFac.defeated &&
                myFac != Faction.OfPlayer)
                Faction.OfPlayer.TryAffectGoodwillWith(myFac, Rand.RangeInclusive(60, 100));
        }
        private MindControl GetReleasableMindControl(Pawn pawn)
        {
            return pawn.health?.hediffSet?.hediffs?.OfType<MindControl>()
                .FirstOrDefault(h => h.def == controller && !h.IsPermanent);
        }
    }
}
