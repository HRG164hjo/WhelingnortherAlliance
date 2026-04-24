using Verse;
using WNA.WNADefOf;
using WNA.WNAUtility;
using UnityEngine;

namespace WNA.WNADamageWorker
{
    public class Chrono : DamageWorker_AddGlobal
    {
        public override DamageResult Apply(DamageInfo dinfo, Thing thing)
        {
            if (!ChronoUtility.ChronoImmune(thing))
            {
                if (thing is Pawn pawn)
                {
                    var hediff = pawn.health.hediffSet.GetFirstHediffOfDef(WNAMainDefOf.WNA_ChronoCounter);
                    if (hediff == null)
                    {
                        hediff = HediffMaker.MakeHediff(WNAMainDefOf.WNA_ChronoCounter, pawn);
                        pawn.health.AddHediff(hediff);
                    }
                    hediff.Severity += dinfo.Amount;
                    int core = pawn.RaceProps.body.corePart != null ? pawn.RaceProps.body.corePart.def.hitPoints : 1;
                    float scale = core * pawn.BodySize * pawn.HealthScale;
                    pawn.health.AddHediff(hediff, null, dinfo);
                    if (hediff.Severity >= scale)
                        General.TotalRemoving(pawn, false);
                }
                else if (!thing.DestroyedOrNull())
                {
                    if (thing.def.Size != null)
                    {
                        float sizex = Mathf.Max(1, thing.def.Size.x);
                        float sizez = Mathf.Max(1, thing.def.Size.z);
                        float crit = 1 / Mathf.Sqrt(sizex * sizez);
                        float random = Rand.Range(0f, 0.9f);
                        if (random < crit)
                            General.DebuglikeDestroy(thing);
                    }
                    else
                        General.DebuglikeDestroy(thing);
                }
            }
            return new DamageResult();
        }
    }
}
