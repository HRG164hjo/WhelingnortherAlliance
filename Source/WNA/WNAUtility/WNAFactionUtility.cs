using RimWorld;
using RimWorld.Planet;
using System.Linq;
using Verse;

namespace WNA.WNAUtility
{
    /*public class WNAFactionUtility
    {
        public static void TryReviveFaction()
        {
            var existing = Find.FactionManager.AllFactions.FirstOrDefault(f => f.def.defName == "WNA_FactionWNA");
            if (existing != null)
            {
                Log.Message("[WNA] Faction already exists, skipping revive.");
                return;
            }

            var factionDef = DefDatabase<FactionDef>.GetNamed("WNA_FactionWNA");
            var newFaction = FactionGenerator.NewGeneratedFaction(factionDef);
            Find.FactionManager.Add(newFaction);

            GenerateNewBases(newFaction);
        }
        private static void GenerateNewBases(Faction faction)
        {
            int numBases = Rand.RangeInclusive(2, 4);
            for (int i = 0; i < numBases; i++)
            {
                if (TileFinder.TryFindNewSiteTile(out int tile))
                {
                    Settlement settlement = (Settlement)WorldObjectMaker.MakeWorldObject(WorldObjectDefOf.Settlement);
                    settlement.SetFaction(faction);
                    settlement.Tile = tile;
                    Find.WorldObjects.Add(settlement);
                }
            }
            Log.Message($"[WNA] Recreated {numBases} bases for {faction.Name}.");
        }
        public static void TryOffensiveEvent(Faction wna)
        {
            var enemyFactions = Find.FactionManager.AllFactions
                .Where(f => !f.IsPlayer && f.HostileTo(wna) && f.def.humanlikeFaction)
                .ToList();
            if (enemyFactions.NullOrEmpty())
            {
                Log.Message("[WNA] No valid enemy faction found for offensive event.");
                return;
            }
            var target = enemyFactions.RandomElement();
            var targetSettlement = Find.WorldObjects.Settlements
                .FirstOrDefault(s => s.Faction == target);
            if (targetSettlement != null)
            {
                targetSettlement.SetFaction(wna);
                Log.Message($"[WNA] {wna.Name} has conquered {targetSettlement.Name} from {target.Name}!");
            }
            else
            {
                Find.FactionManager.Remove(target);
                Log.Message($"[WNA] {wna.Name} has annihilated {target.Name}!");
            }
        }
    }*/
}
