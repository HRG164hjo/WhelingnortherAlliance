using RimWorld;
using System.Collections.Generic;
using Verse;
using WNA.WNADefOf;
using WNA.WNAUtility;

namespace WNA.WNADamageWorker
{
    public class Culling : DamageWorker
    {
        public override DamageResult Apply(DamageInfo dinfo, Thing thing)
        {
            DamageInfo dinew = new DamageInfo(
                WNAMainDefOf.WNA_CastMelee,
                float.MaxValue,
                float.MaxValue,
                dinfo.Angle,
                dinfo.Instigator,
                dinfo.HitPart,
                dinfo.Weapon,
                dinfo.Category,
                dinfo.IntendedTarget,
                false,
                false,
                dinfo.WeaponQuality,
                dinfo.CheckForJobOverride,
                dinfo.PreventCascade);
            if (thing is Pawn pawn)
            {
                if (pawn.def == WNAMainDefOf.WNA_WNThan)
                    return new DamageResult();
                else
                {
                    RemoveNonBadHediffs(pawn);
                    BodyPartRecord core = pawn.RaceProps.body.corePart;
                    Hediff destroyed = HediffMaker.MakeHediff(HediffDefOf.MissingBodyPart, pawn, core);
                    destroyed.Severity = float.MaxValue;
                    pawn.health.AddHediff(destroyed, core);
                }
            }
            else if (!thing.DestroyedOrNull())
                General.DebuglikeDestroy(thing, DestroyMode.KillFinalize);
            return thing.DestroyedOrNull() ? new DamageResult() : base.Apply(dinew, thing);
        }
        private void RemoveNonBadHediffs(Pawn pawn)
        {
            List<Hediff> hediffsToRemove = new List<Hediff>();
            foreach (Hediff hediff in pawn.health.hediffSet.hediffs)
            {
                if (!hediff.def.isBad && hediff.def != WNAMainDefOf.WNA_Inhuman)
                    hediffsToRemove.Add(hediff);
            }
            for (int i = hediffsToRemove.Count - 1; i >= 0; i--) pawn.health.RemoveHediff(hediffsToRemove[i]);
        }
    }
}
