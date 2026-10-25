using RimWorld;
using Verse;
using WNA.WNAUtility;

namespace WNA.WNAThingClass
{
    internal class ThingClass_Whelingnorthan : Pawn
    {
        public override void PreApplyDamage(ref DamageInfo dinfo, out bool absorbed)
        {
            if (!General.ConsideredBetrayer(this))
            {
                dinfo.SetAmount(0);
                dinfo.SetHitPart(null);
                absorbed = true;
            }
            base.PreApplyDamage(ref dinfo, out absorbed);
        }
        public override void Kill(DamageInfo? dinfo, Hediff exactCulprit = null)
        {
            if (!General.ConsideredBetrayer(this))
                health.isBeingKilled = false;
            base.Kill(dinfo, exactCulprit);
        }
        public override void Destroy(DestroyMode mode = DestroyMode.Vanish)
        {
            if (!DebugSettings.godMode && !General.ConsideredBetrayer(this))
                return;
            base.Destroy(mode);
        }
        public override void Discard(bool silentlyRemoveReferences = false)
        {
            if (!DebugSettings.godMode && !General.ConsideredBetrayer(this))
                return;
            base.Discard(silentlyRemoveReferences);
        }
        public override void TickRare()
        {
            base.TickRare();
            if (!General.ConsideredBetrayer(this) || PawnUtility.IsFactionLeader(this))
                return;
            Map map = Map;
            IntVec3 strikeCell = Position;
            General.TotalRemoving(this);
            if (map != null)
                map.weatherManager.eventHandler.AddEvent(new WeatherEvent_LightningStrike(map, strikeCell));
        }
        public override void PostMake()
        {
            base.PostMake();
            if (mindState != null)
            {
                mindState = null;
                mindState.mentalBreaker = null;
                mindState.mentalStateHandler = null;
                mindState.inspirationHandler = null;
            }
            if (stances != null)
            {
                stances = null;
                stances.stagger = null;
                stances.stunner = null;
            }
            if (needs != null)
                needs = null;
        }
    }
}
