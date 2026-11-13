using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;

namespace WNA.WNAThingComp
{
    internal class PropBattleUnitRepairable : CompProperties
    {
        public bool autoRepairByDefault = true;

        public PropBattleUnitRepairable()
        {
            compClass = typeof(CompBattleUnitRepairable);
        }
    }

    internal class CompBattleUnitRepairable : ThingComp
    {
        public bool autoRepair;
        public PropBattleUnitRepairable Props => (PropBattleUnitRepairable)props;

        public override void PostSpawnSetup(bool respawningAfterLoad)
        {
            base.PostSpawnSetup(respawningAfterLoad);
            if (!respawningAfterLoad)
                autoRepair = Props.autoRepairByDefault;
        }

        public override IEnumerable<Gizmo> CompGetGizmosExtra()
        {
            if (parent.Faction == Faction.OfPlayer && !parent.Destroyed)
            {
                Command_Toggle toggle = new Command_Toggle
                {
                    defaultLabel = "CommandAutoRepair".Translate(),
                    defaultDesc = "CommandAutoRepairDesc".Translate(),
                    icon = ContentFinder<Texture2D>.Get("UI/Gizmos/AutoRepair"),
                    isActive = () => autoRepair,
                    toggleAction = delegate
                    {
                        autoRepair = !autoRepair;
                    }
                };
                yield return toggle;
            }
        }

        public override void PostExposeData()
        {
            Scribe_Values.Look(ref autoRepair, "autoRepair", defaultValue: Props.autoRepairByDefault);
        }
    }
}
