using RimWorld;
using Verse;
using WNA.WNADefOf;
using WNA.WNAThingClass;
using WNA.WNAUtility;

namespace WNA.WNADamageWorker
{
    internal class DmgWorker_Culling : DamageWorker
    {
        public override DamageResult Apply(DamageInfo dinfo, Thing thing)
        {
            if (thing is Pawn pawn)
            {
                if (pawn is ThingClass_Whelingnorthan)
                    return new DamageResult();
                else
                {
                    General.HediffRemoveAll(pawn);
                    BodyPartRecord core = pawn.RaceProps.body.corePart;
                    Hediff destroyed = HediffMaker.MakeHediff(HediffDefOf.MissingBodyPart, pawn, core);
                    destroyed.Severity = float.MaxValue;
                    pawn.health.AddHediff(destroyed, core);
                }
            }
            else if (!thing.DestroyedOrNull())
            {
                if (thing.def.destroyable == false)
                {
                    if (DebugSettings.godMode)
                        General.DevDestroy(thing, DestroyMode.KillFinalize);
                    else
                        return new DamageResult();
                }
                General.DevDestroy(thing, DestroyMode.KillFinalize);
            }
            return thing.DestroyedOrNull() ? new DamageResult() : base.Apply(dinfo, thing);
        }
    }
}
