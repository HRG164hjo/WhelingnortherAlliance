using RimWorld;
using System.Collections.Generic;
using Verse;

namespace WNA.ThingCompProp
{
    public class PropAttachHediff : CompProperties
    {
        public bool affectsOwner = false;
        public bool affectsNeutral = false;
        public bool affectsEnemy = false;
        public bool affectHumanlike = false;
        public bool affectOrganic = false;
        public bool affectMechanic = false;
        public float radius = 5f;
        public int interval = 250;
        public HediffDef hediffDef = null;
        public float severity = 1f;
        public List<GeneDef> immunity_Genes = new List<GeneDef>();
        public List<HediffDef> immunity_Hediffs = new List<HediffDef>();
        public List<PreceptDef> immunity_Precepts = new List<PreceptDef>();
        public List<ThingDef> immunity_Races = new List<ThingDef>();
        public PropAttachHediff()
        {
            compClass = typeof(CompAttachHediff);
        }
    }
    public class CompAttachHediff : ThingComp
    {
        public PropAttachHediff Props => (PropAttachHediff)props;
        public override void CompTick()
        {
            base.CompTick();
            if (!parent.IsHashIntervalTick(Props.interval) ||
                !parent.Spawned ||
                Props.hediffDef == null)
            {
                return;
            }
            foreach (Pawn pawn in GetAffectedPawnsInRadius())
            {
                TryAttachPawn(pawn);
            }
        }
        private IEnumerable<Pawn> GetAffectedPawnsInRadius()
        {
            float radiusSquared = Props.radius * Props.radius;
            Faction buildingFaction = parent.Faction;
            foreach (Pawn pawn in parent.Map.mapPawns.AllPawnsSpawned)
            {
                if (pawn.Position.DistanceToSquared(parent.Position) > radiusSquared)
                    continue;
                if (pawn.Dead)
                    continue;
                if (!CheckFactionRelation(pawn, buildingFaction))
                    continue;
                if (!CheckPawnKind(pawn))
                    continue;
                yield return pawn;
            }
        }
        private bool CheckFactionRelation(Pawn pawn, Faction buildingFaction)
        {
            if (buildingFaction == null || pawn.Faction == null)
                return Props.affectsNeutral;
            bool isSameFaction = pawn.Faction == buildingFaction;
            bool isEnemy = buildingFaction.HostileTo(pawn.Faction);
            bool isNeutral = !isSameFaction && !isEnemy;
            return (isSameFaction && Props.affectsOwner) ||
                   (isEnemy && Props.affectsEnemy) ||
                   (isNeutral && Props.affectsNeutral);
        }
        private bool CheckPawnKind(Pawn pawn)
        {
            if (Props.affectHumanlike && pawn.RaceProps.intelligence == Intelligence.Humanlike)
                return true;
            if (Props.affectMechanic && pawn.RaceProps.IsMechanoid)
                return true;
            if (Props.affectOrganic && !(pawn.RaceProps.IsMechanoid || pawn.RaceProps.intelligence == Intelligence.Humanlike))
                return true;
            return !Props.affectHumanlike &&
                   !Props.affectOrganic &&
                   !Props.affectMechanic;
        }
        private bool IsPawnImmune(Pawn pawn)
        {
            bool Ig = Props.immunity_Genes?.Count > 0 &&
                     pawn.genes != null &&
                     Props.immunity_Genes.Any(gene => pawn.genes.HasActiveGene(gene));

            bool Ih = Props.immunity_Hediffs?.Count > 0 &&
                               Props.immunity_Hediffs.Any(hediff => pawn.health.hediffSet.HasHediff(hediff));

            bool Ip = Props.immunity_Precepts?.Count > 0 &&
                                 pawn.Ideo != null &&
                                 Props.immunity_Precepts.Any(precept => pawn.Ideo.HasPrecept(precept));

            bool Ir = Props.immunity_Races?.Count > 0 &&
                             Props.immunity_Races.Contains(pawn.def);
            return Ig || Ih || Ip || Ir;
        }
        private void TryAttachPawn(Pawn pawn)
        {
            try
            {
                if (IsPawnImmune(pawn)) return;
                Hediff hediff = pawn.health.hediffSet.GetFirstHediffOfDef(Props.hediffDef);
                if (hediff != null) hediff.Severity += Props.severity;
                else
                {
                    hediff = HediffMaker.MakeHediff(Props.hediffDef, pawn);
                    pawn.health.AddHediff(hediff);
                    hediff.Severity = Props.severity;
                }
            }
            catch (System.Exception ex)
            {
                Log.Error($"Error applying hediff {Props.hediffDef?.defName} to pawn {pawn.Label}: {ex.Message}");
                Log.Error($"Stack trace: {ex.StackTrace}");
                return;
            }
        }
    }
}
