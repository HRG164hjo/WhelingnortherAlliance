using UnityEngine;
using Verse;
using WNA.WNADefOf;
using WNA.WNAUtility;

namespace WNA.WNADamageWorker
{
    internal class DmgWorker_Temporal : DamageWorker_AddGlobal
    {
        public override DamageResult Apply(DamageInfo dinfo, Thing thing)
        {
            if (!WNAChronoUtility.ChronoImmune(thing))
            {
                if (thing is Pawn pawn)
                {
                    var hediff = pawn.health.hediffSet.GetFirstHediffOfDef(WNAMainDefOf.WNA_ChronoCounter);
                    if (hediff == null)
                    {
                        hediff = HediffMaker.MakeHediff(WNAMainDefOf.WNA_ChronoCounter, pawn);
                        pawn.health.AddHediff(hediff, null, dinfo);
                    }
                    hediff.Severity += dinfo.Amount;
                    int core = pawn.RaceProps.body.corePart != null ? pawn.RaceProps.body.corePart.def.hitPoints : 1;
                    float scale = core * pawn.BodySize * pawn.HealthScale;
                    if (hediff.Severity >= scale)
                        General.TotalRemoving(pawn, false);
                }
                else if (!thing.DestroyedOrNull())
                {
                    if (thing.def.Size.x * thing.def.Size.z >= 0)
                    {
                        float sizex = Mathf.Max(1, thing.def.Size.x);
                        float sizez = Mathf.Max(1, thing.def.Size.z);
                        float crit = 1 / Mathf.Sqrt(sizex * sizez);
                        float random = Rand.Range(0f, 0.9f);
                        if (random < crit)
                            General.DevDestroy(thing);
                    }
                    else
                        General.DevDestroy(thing);
                }
            }
            return new DamageResult();
        }
    }
}
