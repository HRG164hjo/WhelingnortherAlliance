using RimWorld;
using System.Collections.Generic;
using Verse;

namespace WNA.WNAThingCompProp
{
    public class PropSwtichMode : CompProperties
    {
        public ThingDef targetWeaponDef;
        public bool mustBeDrafted = false;
        public PropSwtichMode()
        {
            compClass = typeof(CompSwtichMode);
        }
        public override IEnumerable<string> ConfigErrors(ThingDef parentDef)
        {
            foreach (var err in base.ConfigErrors(parentDef))
                yield return err;
            if (targetWeaponDef == null)
                yield return $"{parentDef.defName}: targetWeaponDef is null.";
            if (targetWeaponDef != null && !targetWeaponDef.IsWeapon)
                yield return $"{parentDef.defName}: targetWeaponDef({targetWeaponDef.defName}) is not a weapon.";
        }
    }
    [StaticConstructorOnStartup]
    public class CompSwtichMode : CompEquippable
    {
        public PropSwtichMode Props => (PropSwtichMode)props;
        public override IEnumerable<Gizmo> CompGetEquippedGizmosExtra()
        {
            foreach (var g in base.CompGetEquippedGizmosExtra())
                yield return g;
            Pawn holder = Holder;
            if (holder == null || holder.Faction != Faction.OfPlayer)
                yield break;
            if (Props.targetWeaponDef == null || !Props.targetWeaponDef.IsWeapon)
                yield break;
            if (Props.mustBeDrafted && (holder.Drafted == false))
                yield break;
            var cmd = new Command_Action
            {
                defaultLabel = "切换模式",
                defaultDesc = $"切换为：{Props.targetWeaponDef.LabelCap}",
                icon = Props.targetWeaponDef.uiIcon,
                action = TrySwitch
            };
            if (Props.mustBeDrafted && !holder.Drafted)
                cmd.Disable("需要征召状态");
            yield return cmd;
        }
        private void TrySwitch()
        {
            Pawn holder = Holder;
            if (holder == null || holder.equipment == null)
                return;
            if (Props.mustBeDrafted && !holder.Drafted)
                return;
            ThingDef targetDef = Props.targetWeaponDef;
            if (targetDef == null || !targetDef.IsWeapon)
                return;
            if (!(parent is ThingWithComps oldWeapon))
                return;
            ThingDef stuff = null;
            if (targetDef.MadeFromStuff)
            {
                if (oldWeapon.Stuff != null && oldWeapon.Stuff.stuffProps.CanMake(targetDef))
                    stuff = oldWeapon.Stuff;
                else
                    stuff = GenStuff.DefaultStuffFor(targetDef);
            }
            ThingWithComps newWeapon = (ThingWithComps)ThingMaker.MakeThing(targetDef, stuff);
            CompQuality oldQ = oldWeapon.TryGetComp<CompQuality>();
            CompQuality newQ = newWeapon.TryGetComp<CompQuality>();
            if (newQ != null)
            {
                if (oldQ != null)
                    newQ.SetQuality(oldQ.Quality, ArtGenerationContext.Colony); // 有 -> 有：继承
                else
                    newQ.SetQuality(QualityCategory.Normal, ArtGenerationContext.Colony); // 无 -> 有：Normal
            }
            holder.equipment.Remove(oldWeapon);
            oldWeapon.Destroy(DestroyMode.Vanish);
            holder.equipment.AddEquipment(newWeapon);
        }
    }
}