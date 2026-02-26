using RimWorld;
using System.Linq;
using Verse;
using WNA.WNADefOf;

namespace WNA.Incident
{
    public class DiscipleJoin : IncidentWorker
    {
        protected override bool CanFireNowSub(IncidentParms parms)
        {
            Faction faction = Find.FactionManager.FirstFactionOfDef(WNAMainDefOf.WNA_FactionWNA);
            if (Faction.OfPlayer.HostileTo(faction))
                return base.CanFireNowSub(parms) && CanSpawnJoiner((Map)parms.target);
            return false;
        }
        private Ideo GetIdeoShouldBe()
        {
            if (!ModsConfig.IdeologyActive) return Faction.OfPlayer.ideos.PrimaryIdeo;
            Faction wnaFaction = Find.FactionManager.AllFactions.FirstOrDefault(f => f.def == WNAMainDefOf.WNA_FactionWNA);
            return wnaFaction?.ideos.PrimaryIdeo ?? Faction.OfPlayer.ideos.PrimaryIdeo;
        }
        public virtual Pawn GeneratePawn()
        {
            Gender? fixedGender = null;
            if (def.pawnFixedGender != 0)
            {
                fixedGender = def.pawnFixedGender;
            }
            Ideo ideoshouldbe = GetIdeoShouldBe();

            return PawnGenerator.GeneratePawn(new PawnGenerationRequest(def.pawnKind, Faction.OfPlayer, PawnGenerationContext.NonPlayer, -1, forceGenerateNewPawn: true, allowDead: false, allowDowned: false, canGeneratePawnRelations: false, mustBeCapableOfViolence: true, colonistRelationChanceFactor: 0f, forceAddFreeWarmLayerIfNeeded: false, allowGay: false, allowPregnant: false, allowFood: true, allowAddictions: false, inhabitant: false, certainlyBeenInCryptosleep: false, forceRedressWorldPawnIfFormerColonist: false, worldPawnFactionDoesntMatter: false, fixedGender: fixedGender, fixedIdeo: ideoshouldbe));
        }
        public virtual bool CanSpawnJoiner(Map map)
        {
            return TryFindEntryCell(map, out IntVec3 cell);
        }
        protected override bool TryExecuteWorker(IncidentParms parms)
        {
            Map map = (Map)parms.target;
            if (!TryFindEntryCell(map, out IntVec3 cell))
                return false;
            Pawn pawn = GeneratePawn();
            GenSpawn.Spawn(pawn, cell, map);
            if (def.pawnHediff != null)
                pawn.health.AddHediff(def.pawnHediff);
            TaggedString text = def.letterText.Formatted(pawn.Named("PAWN"));
            if (def.pawnHediff != null)
                text = text.Formatted(NamedArgumentUtility.Named(def.pawnHediff, "HEDIFF"));
            text = text.AdjustedFor(pawn);
            TaggedString title = def.letterLabel.Formatted(pawn.Named("PAWN")).AdjustedFor(pawn);
            PawnRelationUtility.TryAppendRelationsWithColonistsInfo(ref text, ref title, pawn);
            SendStandardLetter(title, text, LetterDefOf.PositiveEvent, parms, pawn);
            return true;
        }
        private bool TryFindEntryCell(Map map, out IntVec3 cell)
        {
            return CellFinder.TryFindRandomEdgeCellWith((IntVec3 c) => map.reachability.CanReachColony(c) && !c.Fogged(map), map, CellFinder.EdgeRoadChance_Neutral, out cell);
        }
    }
}
