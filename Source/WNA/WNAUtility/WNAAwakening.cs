using System.Linq;
using Verse;

namespace WNA.WNAUtility
{
    /*public class WNAAwakening : GameComponent
    {
        private bool flagActivated = false;
        private int nextCheckTick = -1;
        private const int CheckInterval = 60000 * 36;
        public WNAAwakening(Game game) { }

        public override void GameComponentTick()
        {
            base.GameComponentTick();
            if (!flagActivated)
                return;
            if (Find.TickManager.TicksGame >= nextCheckTick)
            {
                nextCheckTick = Find.TickManager.TicksGame + CheckInterval;
                CheckFactionStatus();
            }
        }
        public void ActivateFlag()
        {
            flagActivated = true;
            nextCheckTick = Find.TickManager.TicksGame + CheckInterval;
            WNAFactionUtility.TryReviveFaction();
        }
        private void CheckFactionStatus()
        {
            var f = Find.FactionManager.AllFactions.FirstOrDefault(x => x.def.defName == "WNA_FactionWNA");
            if (f == null || f.defeated)
            {
                Log.Message("[WNA] Faction defeated again, reviving...");
                WNAFactionUtility.TryReviveFaction();
            }
            else
            {
                Log.Message("[WNA] Faction stable, launching offensive...");
                WNAFactionUtility.TryOffensiveEvent(f);
            }
        }
        public override void ExposeData()
        {
            Scribe_Values.Look(ref flagActivated, "flagActivated", false);
            Scribe_Values.Look(ref nextCheckTick, "nextCheckTick", -1);
        }
    }*/
}
