using RimWorld;
using Verse;

namespace WNA.WNARitual
{
    // CanEverSpectate() ignores ShouldInitAsSpectator(), so the recipient-only rule
    // needs this filter to block spectators in the begin-ritual dialog.
    public class RitualSpectatorFilter_RecipientOnly : RitualSpectatorFilter
    {
        public override bool Allowed(Pawn p)
        {
            return false;
        }
    }
}
