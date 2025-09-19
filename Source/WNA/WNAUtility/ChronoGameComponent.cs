using System.Collections.Generic;
using System.Linq;
using Verse;

namespace WNA.WNAUtility
{
    public class ChronoGameComponent : GameComponent
    {
        private Dictionary<Pawn, int> pawnCooldownDict = new Dictionary<Pawn, int>();
        public ChronoGameComponent() : base() {}
        public override void GameComponentTick()
        {
            if (pawnCooldownDict.Count == 0) return;

            List<Pawn> pawnsToRemove = new List<Pawn>();
            var keys = pawnCooldownDict.Keys.ToList();

            foreach (var pawn in keys)
            {
                if (pawn == null || pawn.DestroyedOrNull())
                {
                    pawnsToRemove.Add(pawn);
                    continue;
                }

                int remaining = pawnCooldownDict[pawn] - 1;
                if (remaining <= 0) pawnsToRemove.Add(pawn);
                else pawnCooldownDict[pawn] = remaining;
            }
            foreach (var pawn in pawnsToRemove)
            {
                pawnCooldownDict.Remove(pawn);
            }
        }
        public void SetCooldown(Pawn pawn, int ticks)
        {
            if (pawn == null || pawn.DestroyedOrNull()) return;

            pawnCooldownDict[pawn] = ticks;
        }

        public bool IsOnCooldown(Pawn pawn)
        {
            return pawn != null && pawnCooldownDict.TryGetValue(pawn, out int ticks) && ticks > 0;
        }

        public int GetRemainingCooldown(Pawn pawn)
        {
            if (pawn != null && pawnCooldownDict.TryGetValue(pawn, out int ticks))
                return ticks;
            return 0;
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Collections.Look(ref pawnCooldownDict, "pawnCooldownDict", LookMode.Reference, LookMode.Value);
            if (Scribe.mode == LoadSaveMode.PostLoadInit)
            {
                var invalidKeys = pawnCooldownDict.Keys.Where(p => p == null || p.DestroyedOrNull()).ToList();
                foreach (var key in invalidKeys)
                {
                    pawnCooldownDict.Remove(key);
                }
            }
        }
    }
}