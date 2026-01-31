using RimWorld;
using System;
using UnityEngine;
using Verse;
using WNA.WNADefOf;
using WNA.WNAUtility;
using static Verse.GenExplosion;

namespace WNA.HediffCompProp
{
    public class PropRadFieldSpreader : HediffCompProperties
    {
        public RadSpreadConfig config = new RadSpreadConfig();
        public PropRadFieldSpreader() => compClass = typeof(CompRadFieldSpreader);
    }
    public class CompRadFieldSpreader : HediffComp
    {
        private PropRadFieldSpreader Props => (PropRadFieldSpreader)props;
        public override void CompPostTick(ref float severityAdjustment)
        {
            base.CompPostTick(ref severityAdjustment);
            Pawn pawn = base.Pawn;
            if (pawn.IsHashIntervalTick(Props.config.radLevelDelay) && pawn.Map != null)
            {
                float s = parent.Severity;
                if (s >= Props.config.threshold)
                {
                    float finalRadLevel = Props.config.radLevel;
                    float finalRadius = Props.config.radius;
                    if(Props.config.isSeverityRadLevel)
                        finalRadLevel *= s * Props.config.radLevelFactor;
                    if (Props.config.isSeverityRadRadius)
                        finalRadius *= (float)Math.Sqrt(s) * Props.config.radRadiusFactor;
                    RadFieldUtility.RadSpread(
                        pawn.Position,
                        pawn.Map,
                        Props.config,
                        finalRadius,
                        (int)finalRadLevel
                    );
                }
            }
        }
        public override void Notify_PawnKilled()
        {
            base.Notify_PawnKilled();
            Pawn pawn = Pawn;
            IntVec3 pos = pawn.Position;
            Map map = pawn.MapHeld;
            float radius = 9.9f * Mathf.Max(1f, Mathf.Sqrt(pawn.BodySize * pawn.HealthScale));
            float amount = Props.config.radLevel * 0.1f;
            SoundDef sound = DefDatabase<SoundDef>.GetNamed("Explosion_EMP");
            RadFieldUtility.RadSpread(
                pawn.Position,
                pawn.Map,
                Props.config,
                radius,
                5000 );
            DoExplosion(pos,
                map,
                radius,
                WNAMainDefOf.WNA_RadBurn,
                pawn,
                (int)amount,
                0f,
                sound );
        }
    }
}
