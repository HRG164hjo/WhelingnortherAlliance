using RimWorld;
using System.Text;
using Verse;
using WNA.WNADefOf;
using WNA.WNAGameComp;
using WNA.WNAMiscs;

namespace WNA.WNAHediffClass
{
    internal class HdfClass_MindControl : HediffWithComps
    {
        public Faction myFac = null;
        public Faction yrFac = null;
        public bool permanent = false;
        public Thing source = null;
        public Faction sourceFaction = null;
        public MindControlOrigin origin = null;
        public int appliedTick = 0;
        public float releaseRange = -1f;
        public bool releaseOnSourceLost = true;
        public bool releaseOnFactionChange = true;
        public bool requireSourcePower = false;

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
            if (origin == null)
                origin = MindControlOrigin.Capture(pawn);
            if (sourceFaction == null)
                sourceFaction = yrFac;
            if (source is Building building && building.GetComp<CompPowerTrader>() != null)
                requireSourcePower = true;
            if (IsPermanent)
                myFac = null;
            else
                myFac = origin.faction;
            if (yrFac != null && pawn.Faction != yrFac)
                pawn.SetFaction(yrFac);
        }

        public override void PostRemoved()
        {
            base.PostRemoved();
            Current.Game?.GetComponent<GameComp_MindControl>()?.Unregister(pawn);
            if (pawn == null)
                return;
            if (IsPermanent)
            {
                ReapplyPermanent();
                return;
            }
            bool playerAffiliated = origin != null
                ? (origin.faction == Faction.OfPlayer || origin.guestHost == Faction.OfPlayer)
                : myFac == Faction.OfPlayer;
            if (origin != null)
                origin.Restore(pawn);
            else if (myFac == null || myFac.defeated)
            {
                if (pawn.Faction != null)
                    pawn.SetFaction(null);
            }
            else if (pawn.Faction != myFac)
                pawn.SetFaction(myFac);
            if (playerAffiliated && pawn.Spawned && !pawn.Dead)
                Messages.Message("WNA_MindControl_Released_Message".Translate(pawn.LabelShort), pawn, MessageTypeDefOf.PositiveEvent, false);
        }

        private void ReapplyPermanent()
        {
            if (pawn == null || pawn.Dead || pawn.Destroyed)
                return;
            HdfClass_MindControl mc = (HdfClass_MindControl)HediffMaker.MakeHediff(WNAMainDefOf.WNA_MindControlEffect, pawn);
            mc.permanent = true;
            mc.myFac = null;
            mc.yrFac = yrFac ?? pawn.Faction;
            mc.source = source;
            mc.sourceFaction = sourceFaction;
            mc.origin = origin;
            mc.appliedTick = appliedTick;
            mc.releaseRange = -1f;
            mc.releaseOnSourceLost = false;
            mc.releaseOnFactionChange = false;
            mc.requireSourcePower = false;
            pawn.health.AddHediff(mc);
        }

        private string OriginLabel()
        {
            if (origin != null && origin.hadGuestStatus)
            {
                string key = origin.guestStatus == GuestStatus.Prisoner
                    ? "WNA_MindControl_WasPrisoner"
                    : origin.guestStatus == GuestStatus.Slave
                        ? "WNA_MindControl_WasSlave"
                        : "WNA_MindControl_WasGuest";
                string text = key.Translate();
                if (origin.guestHost != null)
                    text += " (" + origin.guestHost.Name + ")";
                return text;
            }
            if (origin != null && origin.faction != null)
                return origin.faction.Name;
            if (myFac != null)
                return myFac.Name;
            return "WNA_MindControl_IsWildman".Translate();
        }

        public override string TipStringExtra
        {
            get
            {
                StringBuilder sb = new StringBuilder();
                if (sourceFaction != null)
                    sb.AppendLine("WNA_MindControl_ControlledBy".Translate() + ": " + sourceFaction.Name);
                if (source != null)
                    sb.AppendLine("WNA_MindControl_Source".Translate() + ": " + source.LabelShort);
                if (IsPermanent)
                {
                    sb.AppendLine("WNA_MindControl_Permanent".Translate());
                    sb.AppendLine("WNA_MindControl_WasFrom".Translate() + ": " + OriginLabel());
                }
                else
                {
                    sb.AppendLine("WNA_MindControl_WasFrom".Translate() + ": " + OriginLabel());
                }
                return sb.ToString();
            }
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_References.Look(ref myFac, "myFac");
            Scribe_References.Look(ref yrFac, "yrFac");
            Scribe_References.Look(ref source, "source");
            Scribe_References.Look(ref sourceFaction, "sourceFaction");
            Scribe_Values.Look(ref permanent, "permanent", false);
            Scribe_Values.Look(ref appliedTick, "appliedTick", 0);
            Scribe_Values.Look(ref releaseRange, "releaseRange", -1f);
            Scribe_Values.Look(ref releaseOnSourceLost, "releaseOnSourceLost", true);
            Scribe_Values.Look(ref releaseOnFactionChange, "releaseOnFactionChange", true);
            Scribe_Values.Look(ref requireSourcePower, "requireSourcePower", false);
            Scribe_Deep.Look(ref origin, "origin");
        }
    }
}
