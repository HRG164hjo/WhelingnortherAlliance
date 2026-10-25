using RimWorld;
using Verse;

namespace WNA.WNAMiscs
{
    internal class MindControlOrigin : IExposable
    {
        public Faction faction;
        public Faction guestHost;
        public GuestStatus guestStatus = GuestStatus.Guest;
        public bool hadGuestStatus;

        internal static MindControlOrigin Capture(Pawn pawn)
        {
            MindControlOrigin origin = new MindControlOrigin();
            if (pawn == null)
                return origin;
            origin.faction = pawn.Faction;
            if (pawn.guest != null && pawn.HostFaction != null)
            {
                if (pawn.IsPrisoner)
                {
                    origin.hadGuestStatus = true;
                    origin.guestStatus = GuestStatus.Prisoner;
                    origin.guestHost = pawn.HostFaction;
                }
                else if (pawn.IsSlave)
                {
                    origin.hadGuestStatus = true;
                    origin.guestStatus = GuestStatus.Slave;
                    origin.guestHost = pawn.HostFaction;
                }
                else if (pawn.guest.GuestStatus == GuestStatus.Guest)
                {
                    origin.hadGuestStatus = true;
                    origin.guestStatus = GuestStatus.Guest;
                    origin.guestHost = pawn.HostFaction;
                }
            }
            return origin;
        }

        internal void Restore(Pawn pawn)
        {
            if (pawn == null || pawn.Dead || pawn.Destroyed)
                return;
            Faction target = faction;
            if (target != null && target.defeated)
                target = null;
            if (pawn.Faction != target)
                pawn.SetFaction(target);
            if (hadGuestStatus && guestHost != null && !guestHost.defeated)
                pawn.guest?.SetGuestStatus(guestHost, guestStatus);
        }

        public void ExposeData()
        {
            Scribe_References.Look(ref faction, "faction");
            Scribe_References.Look(ref guestHost, "guestHost");
            Scribe_Values.Look(ref guestStatus, "guestStatus", GuestStatus.Guest);
            Scribe_Values.Look(ref hadGuestStatus, "hadGuestStatus", false);
        }
    }
}
