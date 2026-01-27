using RimWorld;
using System.Collections.Generic;
using UnityEngine;
using Verse;
using Verse.Sound;

namespace WNA.ThingCompProp
{
    public class PropSwtichMode : CompProperties
    {
        public string modeIIdefname;
        public PropSwtichMode()
        {
            compClass = typeof(CompSwtichMode);
        }
        public override IEnumerable<string> ConfigErrors(ThingDef parentDef)
        {
            foreach (string error in base.ConfigErrors(parentDef))
            {
                yield return error;
            }
            if (modeIIdefname.NullOrEmpty())
                yield return $"[WNA.SwitchMode] **modeIIdefname** UNDEFINED!!!";
        }
    }
    public class CompSwtichMode : ThingComp
    {
        public PropSwtichMode Props => (PropSwtichMode)props;
        private ThingDef targetWeaponDef;
        public override void PostPostMake()
        {
            base.PostPostMake();
            targetWeaponDef = DefDatabase<ThingDef>.GetNamed(Props.modeIIdefname, false);
            if (targetWeaponDef == null)
            {
                Log.Error($"[WNA Mod] CompSwtichMode: 武器 '{parent.def.defName}' 找不到目标 DefName '{Props.modeIIdefname}'。请检查 XML 配置。");
            }
        }
        public override IEnumerable<Gizmo> CompGetGizmosExtra()
        {
            foreach (Gizmo gizmo in base.CompGetGizmosExtra())
            {
                yield return gizmo;
            }
            if (this.parent.ParentHolder is Pawn_EquipmentTracker tracker)
            {
                if (tracker.pawn.equipment.Primary == this.parent && tracker.pawn.Drafted)
                {
                    if (targetWeaponDef == null) yield break;
                    yield return new Command_Action
                    {
                        defaultLabel = "WNA.SwitchMode.Label".Translate(),
                        defaultDesc = "WNA.SwitchMode.Desc".Translate(),
                        icon = ContentFinder<Texture2D>.Get("UI/Misc/BadTexture"),
                        action = TryTransformWeapon
                    };
                }
            }
        }
        private void TryTransformWeapon()
        {
            ThingWithComps currentWeapon = this.parent;
            if (targetWeaponDef == null)
            {
                Log.ErrorOnce($"[WNA Mod] 武器 {currentWeapon.def.defName} 无法变形，目标 Def 丢失。", currentWeapon.thingIDNumber ^ 0x616);
                return;
            }
            if (currentWeapon.ParentHolder is Pawn_EquipmentTracker equipmentTracker && equipmentTracker.pawn.equipment.Primary == currentWeapon)
            {
                Pawn pawn = equipmentTracker.pawn;
                if (!pawn.equipment.TryDropEquipment(currentWeapon, out ThingWithComps droppedWeapon, pawn.Position, true))
                {
                    Log.Error($"[WNA Mod] 无法从 Pawn {pawn.LabelShort} 卸下武器 {currentWeapon.LabelShort}。");
                    return;
                }
                currentWeapon.TryGetQuality(out QualityCategory quality);
                droppedWeapon.Destroy();
                if (!(ThingMaker.MakeThing(targetWeaponDef, currentWeapon.Stuff)
                    is ThingWithComps newWeapon)) return;
                newWeapon.compQuality?.SetQuality(QualityCategory.Normal, null);
                pawn.equipment.MakeRoomFor(newWeapon);
                pawn.equipment.AddEquipment(newWeapon);
                newWeapon.def.soundInteract?.PlayOneShot(new TargetInfo(pawn.Position, pawn.Map, false));
            }
        }
    }
}
