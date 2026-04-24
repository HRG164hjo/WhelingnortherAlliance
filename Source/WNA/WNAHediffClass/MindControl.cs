using RimWorld;
using System.Text;
using Verse;
using WNA.WNADefOf;

namespace WNA.WNAHediffClass
{
    public class MindControl : HediffWithComps
    {
        public Faction myFac = null;
        public Faction yrFac = null;
        public bool permanent = false;
        internal bool IsPermanent
        {
            get
            {
                if (permanent)
                    return true;
                if (yrFac == null)
                    return true;
                if (yrFac != null && myFac != null && yrFac == myFac)
                    return true;    
                return false;
            }
        }
        public override void PostAdd(DamageInfo? dinfo)
        {
            base.PostAdd(dinfo);
            if (IsPermanent)
                myFac = null;
            else
                myFac = pawn.Faction;
            if (yrFac != null)
                pawn.SetFaction(yrFac);
        }
        public override void PostRemoved()
        {
            base.PostRemoved();
            if (IsPermanent)
            {
                MindControl mc = (MindControl)HediffMaker.MakeHediff(WNAMainDefOf.WNA_MindControlEffect, pawn);
                mc.permanent = true;
                mc.myFac = null;
                mc.yrFac = pawn.Faction ?? null;
                pawn.health.AddHediff(mc);
            }
            else
            {
                if (myFac == null || myFac.defeated)
                    pawn.SetFaction(null);
                else
                    pawn.SetFaction(myFac);
            }
        }
        public override string TipStringExtra
        {
            get
            {
                StringBuilder sb = new StringBuilder();
                if (yrFac != null)
                    sb.AppendLine("WNA_MindControl_ControlledBy".Translate() + ": " + yrFac.Name);
                if (IsPermanent)
                    sb.AppendLine("WNA_MindControl_Permanent".Translate());
                else
                {
                    sb.AppendLine((myFac != null) ?
                        ("WNA_MindControl_WasFrom".Translate() + ": " + myFac.Name) :
                        ("WNA_MindControl_WasFrom".Translate() + ": " + "WNA_MindControl_IsWildman".Translate()));
                }
                return sb.ToString();
            }
        }
        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_References.Look(ref myFac, "myFac");
            Scribe_References.Look(ref yrFac, "yrFac");
            Scribe_Values.Look(ref permanent, "permanent", false);
        }
    }
}
