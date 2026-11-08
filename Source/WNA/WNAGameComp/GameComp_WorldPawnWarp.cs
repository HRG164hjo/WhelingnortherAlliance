using System.Collections.Generic;
using RimWorld;
using Verse;
using WNA.WNAUtility;
using WNA.WNARitual;

namespace WNA.WNAGameComp
{
    internal class GameComp_WorldPawnWarp : GameComponent
    {
        public class PendingSummon : IExposable
        {
            public Pawn pawn;
            public Map map;
            public IntVec3 cell = IntVec3.Invalid;
            public int readyTick;
            public bool pinned;

            public void ExposeData()
            {
                Scribe_References.Look(ref pawn, "pawn");
                Scribe_References.Look(ref map, "map");
                Scribe_Values.Look(ref cell, "cell", IntVec3.Invalid);
                Scribe_Values.Look(ref readyTick, "readyTick", 0);
                Scribe_Values.Look(ref pinned, "pinned", defaultValue: false);
            }
        }

        private List<PendingSummon> pendingSummons = new List<PendingSummon>();
        private bool selectionPending;
        private Map selectionMap;
        private IntVec3 selectionCell = IntVec3.Invalid;

        public static GameComp_WorldPawnWarp Instance => Current.Game?.GetComponent<GameComp_WorldPawnWarp>();

        public GameComp_WorldPawnWarp(Game game)
        {
        }

        public bool IsPending(Pawn pawn)
        {
            if (pawn == null)
                return false;
            for (int i = 0; i < pendingSummons.Count; i++)
            {
                if (pendingSummons[i]?.pawn == pawn)
                    return true;
            }
            return false;
        }

        public void Schedule(List<Pawn> pawns, Map map, IntVec3 cell, int delayTicks)
        {
            if (pawns.NullOrEmpty())
                return;
            int readyTick = Find.TickManager.TicksGame + delayTicks;
            for (int i = 0; i < pawns.Count; i++)
            {
                Pawn pawn = pawns[i];
                if (pawn == null || pawn.Destroyed || pawn.Discarded || IsPending(pawn))
                    continue;
                PendingSummon summon = new PendingSummon
                {
                    pawn = pawn,
                    map = map,
                    cell = cell,
                    readyTick = readyTick
                };
                summon.pinned = Find.WorldPawns.ForcefullyKeptPawns.Add(pawn);
                pendingSummons.Add(summon);
            }
        }

        public void SetSelectionPending(Map map, IntVec3 cell)
        {
            selectionPending = true;
            selectionMap = map;
            selectionCell = cell;
        }

        public void ClearSelection()
        {
            selectionPending = false;
            selectionMap = null;
            selectionCell = IntVec3.Invalid;
        }

        public Map SelectionMap => selectionMap;

        public IntVec3 SelectionCell => selectionCell;

        public override void GameComponentTick()
        {
            if (pendingSummons.Count == 0)
                return;
            int now = Find.TickManager.TicksGame;
            for (int i = pendingSummons.Count - 1; i >= 0; i--)
            {
                PendingSummon summon = pendingSummons[i];
                Pawn pawn = summon?.pawn;
                if (pawn == null || pawn.Destroyed || pawn.Discarded)
                {
                    Unpin(summon);
                    pendingSummons.RemoveAt(i);
                    continue;
                }
                if (now < summon.readyTick)
                    continue;
                Map map = summon.map != null && Find.Maps.Contains(summon.map) ? summon.map : Find.AnyPlayerHomeMap;
                if (map == null)
                    continue;
                if (WorldPawnWarpUtility.TrySummon(pawn, map, summon.cell))
                {
                    Unpin(summon);
                    pendingSummons.RemoveAt(i);
                }
            }
        }

        public override void GameComponentUpdate()
        {
            if (!selectionPending || Scribe.mode != LoadSaveMode.Inactive)
                return;
            if (Current.ProgramState != ProgramState.Playing || Find.WindowStack == null)
                return;
            if (Find.WindowStack.IsOpen<Dialog_WorldPawnWarpTargets>())
                return;
            WorldPawnWarpUtility.BeginSelection(selectionMap, selectionCell);
        }

        private static void Unpin(PendingSummon summon)
        {
            if (summon == null || !summon.pinned || summon.pawn == null)
                return;
            Find.WorldPawns?.ForcefullyKeptPawns.Remove(summon.pawn);
            summon.pinned = false;
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Collections.Look(ref pendingSummons, "pendingSummons", LookMode.Deep);
            Scribe_Values.Look(ref selectionPending, "selectionPending", defaultValue: false);
            Scribe_References.Look(ref selectionMap, "selectionMap");
            Scribe_Values.Look(ref selectionCell, "selectionCell", IntVec3.Invalid);
            if (Scribe.mode != LoadSaveMode.PostLoadInit)
                return;
            if (pendingSummons == null)
                pendingSummons = new List<PendingSummon>();
            pendingSummons.RemoveAll(s => s == null || s.pawn == null);
            if (!selectionPending)
            {
                selectionMap = null;
                selectionCell = IntVec3.Invalid;
            }
        }
    }
}
