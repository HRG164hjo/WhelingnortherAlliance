using System.Collections.Generic;
using RimWorld;
using RimWorld.Planet;
using Verse;

namespace WNA.WNAThingClass
{
    internal class ThingClass_BattleUnit : Pawn
    {
        public override void PreApplyDamage(ref DamageInfo dinfo, out bool absorbed)
        {
            if (dinfo.Def == null || !dinfo.Def.ExternalViolenceFor(this))
            {
                absorbed = true;
                return;
            }
            base.PreApplyDamage(ref dinfo, out absorbed);
        }

        public override void Kill(DamageInfo? dinfo, Hediff exactCulprit = null)
        {
            base.Kill(dinfo, exactCulprit);
            if (!Destroyed || this.IsCaravanMember())
                return;
            WorldPawns worldPawns = Find.WorldPawns;
            if (worldPawns.Contains(this))
                worldPawns.RemoveAndDiscardPawnViaGC(this);
        }

        public override IEnumerable<Gizmo> GetGizmos()
        {
            foreach (Gizmo gizmo in base.GetGizmos())
                yield return gizmo;
            if (Spawned && Faction != null && Faction.IsPlayer && drafter != null && !drafter.ShowDraftGizmo)
                yield return DraftGizmo();
        }

        private Gizmo DraftGizmo()
        {
            Command_Toggle toggle = new Command_Toggle
            {
                hotKey = KeyBindingDefOf.Command_ColonistDraft,
                isActive = () => drafter.Drafted,
                toggleAction = delegate
                {
                    drafter.Drafted = !drafter.Drafted;
                },
                defaultDesc = "CommandToggleDraftDesc".Translate(),
                icon = TexCommand.Draft,
                turnOnSound = SoundDefOf.DraftOn,
                turnOffSound = SoundDefOf.DraftOff,
                groupKeyIgnoreContent = 81729172,
                defaultLabel = (drafter.Drafted ? "CommandUndraftLabel" : "CommandDraftLabel").Translate()
            };
            if (Downed)
                toggle.Disable("IsIncapped".Translate(LabelShort, this));
            return toggle;
        }
    }
}
