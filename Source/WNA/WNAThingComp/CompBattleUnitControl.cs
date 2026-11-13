using RimWorld;
using Verse;
using WNA.WNAThingClass;

namespace WNA.WNAThingComp
{
    internal class PropBattleUnitControl : CompProperties
    {
        public PropBattleUnitControl()
        {
            compClass = typeof(CompBattleUnitControl);
        }
    }

    internal class CompBattleUnitControl : ThingComp
    {
        public override void PostSpawnSetup(bool respawningAfterLoad)
        {
            base.PostSpawnSetup(respawningAfterLoad);
            EnsurePlayerControl(parent as Pawn);
        }

        public static void EnsurePlayerControl(Pawn pawn)
        {
            if (!(pawn is ThingClass_BattleUnit) || pawn.Dead || !pawn.Spawned)
                return;
            if (pawn.Faction == null || !pawn.Faction.IsPlayer)
                return;
            if (pawn.playerSettings == null)
                pawn.playerSettings = new Pawn_PlayerSettings(pawn);
            if (pawn.drafter == null)
                pawn.drafter = new Pawn_DraftController(pawn);
        }
    }
}
