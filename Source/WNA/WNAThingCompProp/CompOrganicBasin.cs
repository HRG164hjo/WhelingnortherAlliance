using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using Verse;
using Verse.AI;
using WNA.WNADefOf;

namespace WNA.WNAThingCompProp
{
    public class PropOrganicBasin : CompProperties
    {
        public float speedUpFactor = 1f;
        public float yieldFactor = 1f;
        public bool canUseCorpse = true;
        public bool canUseMechanoids = false;
        public bool canUseEntities = false;
        public bool canUseHumanoid = false;
        public PropOrganicBasin()
        {
            compClass = typeof(CompOrganicBasin);
        }
    }
    [StaticConstructorOnStartup]
    public class CompOrganicBasin : ThingComp
    {
        private bool hasTemplate;
        private ThingDef templateRaceDef;
        private int templateGrowthTicks;
        private int progressTicks;
        private float bodysize;
        private Dictionary<ThingDef, int> products = new Dictionary<ThingDef, int>();
        private CompPowerTrader powerComp;
        private CompRefuelable refuelableComp;
        private static readonly Texture2D SelectTex = ContentFinder<Texture2D>.Get("UI/Designators/ForbidOff");
        private static readonly Texture2D CancelTex = ContentFinder<Texture2D>.Get("UI/Designators/Cancel");
        public PropOrganicBasin Props => (PropOrganicBasin)props;
        public override void PostSpawnSetup(bool respawningAfterLoad)
        {
            base.PostSpawnSetup(respawningAfterLoad);
            CacheComps();
            if (!respawningAfterLoad && hasTemplate == false)
                ResetState();
        }
        public override void CompTickRare()
        {
            base.CompTickRare();
            if (!hasTemplate)
                return;
            if (HasPower())
                progressTicks += (int)(250 * Props.speedUpFactor * bodysize);
            if (progressTicks >= templateGrowthTicks && templateGrowthTicks > 0)
            {
                progressTicks = 0;
                ProduceItems();
            }
        }
        public override IEnumerable<Gizmo> CompGetGizmosExtra()
        {
            foreach (var g in base.CompGetGizmosExtra()) yield return g;
            if (parent.Faction != Faction.OfPlayer) yield break;
            yield return new Command_Target
            {
                icon = SelectTex,
                defaultLabel = "WNA_CompOrganicBasin_GetPawn".Translate(),
                targetingParams = GetTargetingParams(),
                action = target =>
                {
                    Thing t = target.Thing;
                    if (t != null) TryStartJob(t);
                }
            };
            if (hasTemplate == true)
            {
                yield return new Command_Action
                {
                    icon = CancelTex,
                    defaultLabel = "WNA_CompOrganicBasin_Clear".Translate(),
                    action = ResetState
                };
            }
        }
        private void ResetState()
        {
            hasTemplate = false;
            templateRaceDef = null;
            templateGrowthTicks = -1;
            bodysize = 0;
            progressTicks = 0;
            products.Clear();
        }
        private TargetingParameters GetTargetingParams()
        {
            return new TargetingParameters
            {
                canTargetPawns = true,
                canTargetCorpses = true,
                canTargetItems = true,
                validator = t =>
                {
                    if (t.Thing is Corpse corpse)
                        return Props.canUseCorpse;
                    if (t.Thing != null && t.Thing is Pawn p)
                    {
                        if (p.Dead)
                            return Props.canUseCorpse;
                        if (p.RaceProps.Animal
                            || p.RaceProps.Insect
                            || p.RaceProps.Dryad)
                            return true;
                        if (Props.canUseHumanoid
                            && p.RaceProps.Humanlike)
                            return true;
                        if (Props.canUseMechanoids
                            && (p.RaceProps.IsMechanoid
                                || p.RaceProps.IsDrone))
                            return true;
                        if (Props.canUseEntities
                            && p.RaceProps.IsAnomalyEntity
                            && ModsConfig.AnomalyActive)
                            return true;
                    }
                    return false;
                }
            };
        }
        private void CacheComps()
        {
            powerComp = parent.GetComp<CompPowerTrader>();
            refuelableComp = parent.GetComp<CompRefuelable>();
        }
        private bool HasPower()
        {
            if (powerComp != null && !powerComp.PowerOn)
                return false;
            if (refuelableComp != null && !refuelableComp.HasFuel)
                return false;
            return true;
        }
        private void TryStartJob(Thing targetThing)
        {
            var map = parent.Map;
            if (map == null || targetThing == null) return;
            Pawn worker = map.mapPawns.FreeColonistsSpawned
                .Where(p =>
                    p.Spawned &&
                    !p.Downed &&
                    p.Awake() &&
                    p.mindState?.mentalStateHandler?.InMentalState != true &&
                    p.CanReserveAndReach(targetThing, PathEndMode.Touch, Danger.Some) &&
                    p.CanReserveAndReach(parent, PathEndMode.InteractionCell, Danger.Some))
                .OrderBy(p => p.Position.DistanceToSquared(parent.Position))
                .FirstOrDefault();
            if (worker == null)
            {
                Messages.Message("WNA_CompOrganicBasin_NoWorker".Translate(), MessageTypeDefOf.NeutralEvent);
                return;
            }
            Job job = JobMaker.MakeJob(WNAMainDefOf.WNA_Job_SacrificePawn, targetThing, parent);
            job.playerForced = true;
            worker.jobs.TryTakeOrderedJob(job, JobTag.Misc);
        }
        internal void SetTemplateFromThing(Thing victim)
        {
            if (victim == null)
                return;
            if (victim is Pawn pawn)
            {
                ResetState();
                CacheTemplateFromPawn(pawn);
                if (!pawn.Dead)
                {
                    pawn.Kill(null);
                    pawn.Corpse.Destroy();
                }
            }
            if (victim is Corpse corpse)
            {
                ResetState();
                CacheTemplateFromPawn(corpse.InnerPawn);
                if (!corpse.Destroyed)
                    corpse.Destroy();
            }
        }
        private void CacheTemplateFromPawn(Pawn pawn)
        {
            hasTemplate = true;
            templateRaceDef = pawn.def;
            bodysize = Math.Max(pawn.BodySize, 1f);
            templateGrowthTicks = (int)(GetGrowthTicks(pawn) / 7.937005f);
            float interval = (int)GetGrowthTicks(pawn) * bodysize / 30000f;
            if (pawn.RaceProps?.meatDef != null)
            {
                var def = pawn.RaceProps.meatDef;
                var amount = Mathf.CeilToInt(bodysize
                * pawn.GetStatValue(StatDefOf.MeatAmount));
                products.AddDistinct(def, amount);
            }
            if (pawn.RaceProps?.leatherDef != null)
            {
                var def = pawn.RaceProps.leatherDef;
                var amount = Mathf.CeilToInt(bodysize
                * pawn.GetStatValue(StatDefOf.LeatherAmount));
                products.AddDistinct(def, amount);
            }
            if (pawn.def.HasComp<CompMilkable>())
            {
                var m = pawn.GetComp<CompMilkable>();
                var def = m.Props.milkDef;
                var amount = Mathf.CeilToInt(Mathf.Max(1,
                    (interval * m.Props.milkAmount / m.Props.milkIntervalDays)));
                products.AddDistinct(def, amount);
            }
            if (pawn.def.HasComp<CompEggLayer>())
            {
                var e = pawn.GetComp<CompEggLayer>();
                var def = e.Props.eggUnfertilizedDef;
                var amount = Mathf.CeilToInt(Mathf.Max(1,
                    (interval * e.Props.eggCountRange.max / e.Props.eggLayIntervalDays)));
                products.AddDistinct(def, amount);
            }
            if (pawn.def.HasComp<CompShearable>())
            {
                var sh = pawn.GetComp<CompShearable>();
                var def = sh.Props.woolDef;
                var amount = Mathf.CeilToInt(Mathf.Max(1,
                    (interval * sh.Props.woolAmount / sh.Props.shearIntervalDays)));
                products.AddDistinct(def, Mathf.CeilToInt(amount));
            }
            if (pawn.def.HasComp<CompSpawner>())
            {
                var sp = pawn.GetComp<CompSpawner>();
                var def = sp.PropsSpawner.thingToSpawn;
                var amount = Mathf.CeilToInt(Mathf.Max(1,
                    (interval * sp.PropsSpawner.spawnCount / sp.PropsSpawner.spawnIntervalRange.min)));
                products.AddDistinct(def, Mathf.CeilToInt(amount));
            }
            if (!pawn.def.butcherProducts.NullOrEmpty())
            {
                foreach (ThingDefCountClass tdcc in pawn.def.butcherProducts)
                    products.AddDistinct(tdcc.thingDef, tdcc.count);
            }
            if (!pawn.def.smeltProducts.NullOrEmpty())
            {
                foreach (ThingDefCountClass tdcc in pawn.def.smeltProducts)
                    products.AddDistinct(tdcc.thingDef, tdcc.count);
            }
            if (!pawn.def.killedLeavings.NullOrEmpty())
            {
                foreach (ThingDefCountClass tdcc in pawn.def.killedLeavings)
                    products.AddDistinct(tdcc.thingDef, tdcc.count);
            }
            if (!pawn.def.killedLeavingsPlayerHostile.NullOrEmpty())
            {
                foreach (ThingDefCountClass tdcc in pawn.def.killedLeavingsPlayerHostile)
                    products.AddDistinct(tdcc.thingDef, tdcc.count);
            }
            if (!pawn.def.killedLeavingsRanges.NullOrEmpty())
            {
                foreach (ThingDefCountRangeClass tdrc in pawn.def.killedLeavingsRanges)
                    products.AddDistinct(tdrc.thingDef, tdrc.countRange.max);
            }
        }
        private float GetGrowthTicks(Pawn pawn)
        {
            var stages = pawn?.RaceProps?.lifeStageAges;
            if (stages == null || stages.Count == 0)
                return 300000f;
            float adult = stages[stages.Count - 1].minAge;
            adult = Mathf.Clamp(adult, 0.166667f, 1f);
            return (int)(adult * GenDate.TicksPerYear);
        }
        private void TrySpawnByAmount(ThingDef def, float amt)
        {
            if (def == null || amt <= 0f)
                return;
            int a = Mathf.CeilToInt(amt) * (int)Find.Storyteller.difficulty.butcherYieldFactor;
            if (a <= 0)
                return;
            int stackLimit = Math.Max(1, def.stackLimit);
            IntVec3 pos = parent.InteractionCell;
            if (!pos.IsValid) pos = parent.Position;
            while (a > 0)
            {
                int take = Math.Min(a, stackLimit);
                Thing t = ThingMaker.MakeThing(def);
                t.stackCount = take;
                GenPlace.TryPlaceThing(t, pos, parent.Map, ThingPlaceMode.Near);
                a -= take;
            }
        }
        private void ProduceItems()
        {
            if (!hasTemplate || parent.Map == null) return;
            foreach (var kvp in products)
                TrySpawnByAmount(kvp.Key, kvp.Value);
        }
        public override string CompInspectStringExtra()
        {
            var sb = new StringBuilder();
            if (templateRaceDef == null)
            {
                sb.Append("WNA_CompOrganicBasin_Empty".Translate());
                return sb.ToString();
            }
            if (powerComp != null && !powerComp.PowerOn)
                sb.AppendLine("WNA_PropHydroponics_LowPower".Translate().ToString());
            if (refuelableComp != null && !refuelableComp.HasFuel)
                sb.AppendLine("WNA_PropHydroponics_NoFuel".Translate().ToString());
            sb.AppendLine("WNA_CompOrganicBasin_Contains".Translate(templateRaceDef.label).ToString());
            sb.AppendLine("WNA_CompOrganicBasin_HarvestIn".Translate(templateGrowthTicks - progressTicks).ToString());
            sb.Append("WNA_CompOrganicBasin_CurrentGrowth".Translate((Props.speedUpFactor * bodysize).ToString("F2")));
            return sb.ToString();
        }
        public override void PostExposeData()
        {
            base.PostExposeData();
            Scribe_Values.Look(ref hasTemplate, "hasTemplate", false);
            Scribe_Defs.Look(ref templateRaceDef, "templateRaceDef");
            Scribe_Values.Look(ref templateGrowthTicks, "templateGrowthTicks", 0);
            Scribe_Values.Look(ref progressTicks, "progressTicks", 0);
            Scribe_Collections.Look(ref products, "products", LookMode.Def, LookMode.Value);
            if (Scribe.mode == LoadSaveMode.LoadingVars && products == null)
                products = new Dictionary<ThingDef, int>();
        }
    }
}
