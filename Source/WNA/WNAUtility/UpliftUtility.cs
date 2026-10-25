using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI;
using Verse.AI.Group;
using WNA.WNADefOf;
using WNA.WNARitual;
using WNA.WNAWorldComp;

namespace WNA.WNAUtility
{
    public enum UpliftSubjectKind
    {
        Invalid,
        Animal,
        Plant,
        Entity,
        Mech,
        Human,
        Humanoid
    }

    public static class UpliftUtility
    {
        public static bool ResearchCompleted =>
            WNAMainDefOf.WNA_AuthorizedUplifting != null && WNAMainDefOf.WNA_AuthorizedUplifting.IsFinished;

        public static bool RitualAvailable(out string reason)
        {
            reason = null;
            if (!ModsConfig.IdeologyActive)
            {
                reason = "WNA_UpliftUnavailableIdeology".Translate().Resolve();
                return false;
            }
            if (!ResearchCompleted)
            {
                reason = "WNA_UpliftUnavailableResearch".Translate().Resolve();
                return false;
            }
            Faction wna = General.WNAFaction;
            if (wna == null)
            {
                reason = "WNA_UpliftUnavailableFaction".Translate().Resolve();
                return false;
            }
            if (wna.HostileTo(Faction.OfPlayer))
            {
                reason = "WNA_UpliftUnavailableHostile".Translate().Resolve();
                return false;
            }
            return true;
        }

        public static Pawn GetSubjectPawn(Thing thing)
        {
            if (thing is Pawn pawn)
                return pawn;
            if (thing is Corpse corpse)
                return corpse.InnerPawn;
            return null;
        }

        public static Thing GetSpawnedParentOrSelf(Thing thing)
        {
            if (thing == null)
                return null;
            if (thing.Spawned)
                return thing;
            IThingHolder holder = thing.ParentHolder;
            while (holder != null)
            {
                if (holder is Thing parentThing && parentThing.Spawned)
                    return parentThing;
                if (holder is ThingComp parentComp && parentComp.parent.Spawned)
                    return parentComp.parent;
                holder = holder.ParentHolder;
            }
            return null;
        }

        public static UpliftSubjectKind Classify(Thing thing)
        {
            if (thing == null || thing.Destroyed)
                return UpliftSubjectKind.Invalid;
            if (thing is Plant)
                return UpliftSubjectKind.Plant;
            Pawn pawn = GetSubjectPawn(thing);
            if (pawn == null)
                return UpliftSubjectKind.Invalid;
            if (pawn.def == WNAMainDefOf.WNA_Whelingnorthan)
                return UpliftSubjectKind.Invalid;
            if (pawn.def == WNAMainDefOf.WNA_Humanoid)
                return UpliftSubjectKind.Humanoid;
            if (pawn.RaceProps.IsAnomalyEntity)
                return UpliftSubjectKind.Entity;
            if (pawn.RaceProps.IsMechanoid)
                return UpliftSubjectKind.Mech;
            if (pawn.RaceProps.Animal)
                return UpliftSubjectKind.Animal;
            if (pawn.RaceProps.Humanlike)
                return UpliftSubjectKind.Human;
            return UpliftSubjectKind.Invalid;
        }

        public static bool TryResolveSubject(Thing thing, out Thing subjectThing, out Pawn subjectPawn)
        {
            subjectThing = null;
            subjectPawn = null;
            if (thing == null || thing.Destroyed)
                return false;
            if (Classify(thing) != UpliftSubjectKind.Invalid)
            {
                subjectThing = thing;
                subjectPawn = GetSubjectPawn(thing);
                return true;
            }
            if (thing is Pawn || thing is Corpse)
                return false;
            if (thing is IThingHolder holder)
            {
                foreach (Thing contained in holder.GetDirectlyHeldThings())
                {
                    if (contained == null || contained.Destroyed)
                        continue;
                    if (Classify(contained) == UpliftSubjectKind.Invalid)
                        continue;
                    subjectThing = contained;
                    subjectPawn = GetSubjectPawn(contained);
                    return true;
                }
            }
            return false;
        }

        public static bool CanBeSubject(TargetInfo target, Map map, out string reason)
        {
            reason = null;
            if (!target.HasThing || target.Thing == null || target.Thing.Destroyed)
                return false;
            if (!TryResolveSubject(target.Thing, out Thing subjectThing, out Pawn pawn))
                return false;
            Thing spawnedParent = GetSpawnedParentOrSelf(subjectThing);
            if (spawnedParent == null)
            {
                reason = "WNA_UpliftSubjectUnavailable".Translate().Resolve();
                return false;
            }
            if (map != null && spawnedParent.Map != map)
                return false;
            WorldComp_UpliftProtection protection = WorldComp_UpliftProtection.Instance;
            if (protection != null && protection.IsProtected(subjectThing))
                return false;
            if (pawn != null)
            {
                if (General.IsInternalFaction(pawn.Faction) || (pawn.Faction == null && General.IsWNARace(pawn)))
                {
                    reason = "WNA_UpliftSubjectForbiddenFaction".Translate().Resolve();
                    return false;
                }
                Lord lord = pawn.GetLord();
                if (lord != null && lord.LordJob is LordJob_Ritual)
                {
                    reason = "WNA_UpliftSubjectBusy".Translate().Resolve();
                    return false;
                }
                if (!pawn.Spawned && !IsInContainer(pawn) && pawn.Corpse == null)
                {
                    reason = "WNA_UpliftSubjectUnavailable".Translate().Resolve();
                    return false;
                }
            }
            else if (!subjectThing.Spawned && !IsInContainer(subjectThing))
            {
                reason = "WNA_UpliftSubjectUnavailable".Translate().Resolve();
                return false;
            }
            return true;
        }

        private static bool IsInContainer(Thing thing)
        {
            if (thing.ParentHolder == null)
                return false;
            if (thing.ParentHolder is Pawn_CarryTracker)
                return false;
            return thing.ParentHolder is Building_HoldingPlatform
                || thing.ParentHolder is CompEntityHolder
                || thing.ParentHolder.GetDirectlyHeldThings() != null;
        }

        public static void SpawnSubjectMarker(Thing subjectThing, Pawn subjectPawn, Map map, IntVec3 fallbackCell)
        {
            if (map == null || WNAMainDefOf.WNA_Mote_UpliftMarker == null)
                return;
            Thing anchor = subjectThing ?? subjectPawn;
            if (anchor == null)
                return;
            Thing spawnedParent = GetSpawnedParentOrSelf(anchor);
            Vector3 loc;
            if (spawnedParent != null)
            {
                loc = spawnedParent.DrawPos;
            }
            else
            {
                if (!fallbackCell.IsValid)
                    return;
                loc = fallbackCell.ToVector3Shifted();
            }
            if (!loc.InBounds(map))
                return;
            float scale = 1f;
            if (subjectPawn != null && subjectPawn.BodySize > 0f)
                scale = Mathf.Clamp(subjectPawn.BodySize * 0.5f, 0.9f, 2f);
            MoteMaker.MakeStaticMote(loc, map, WNAMainDefOf.WNA_Mote_UpliftMarker, scale);
        }

        public static TargetingParameters SubjectTargetingParameters(Map map)
        {
            return new TargetingParameters
            {
                canTargetLocations = false,
                canTargetPawns = true,
                canTargetAnimals = true,
                canTargetHumans = true,
                canTargetMechs = true,
                canTargetEntities = true,
                canTargetSubhumans = true,
                canTargetCorpses = true,
                canTargetPlants = true,
                canTargetBuildings = true,
                canTargetItems = false,
                canTargetFires = false,
                mapObjectTargetsMustBeAutoAttackable = false,
                validator = t => CanBeSubject(t, map, out _)
            };
        }

        public static void CompleteUplift(LordJob_Ritual_Uplifting job)
        {
            if (job == null)
                return;
            Map map = job.Map;
            if (map == null)
                return;
            Thing subjectThing = job.subjectThing;
            Pawn sourcePawn = job.subjectPawn;
            if (sourcePawn != null && sourcePawn.Destroyed)
                sourcePawn = null;
            if ((subjectThing == null || subjectThing.Destroyed) && sourcePawn != null && sourcePawn.Corpse != null)
                subjectThing = sourcePawn.Corpse;
            if (subjectThing == null || subjectThing.Destroyed)
            {
                Messages.Message("WNA_UpliftSubjectGone".Translate(), MessageTypeDefOf.NegativeEvent, false);
                return;
            }
            if (sourcePawn == null)
                sourcePawn = GetSubjectPawn(subjectThing);
            UpliftSubjectKind kind = Classify(subjectThing);
            if (kind == UpliftSubjectKind.Invalid)
            {
                Messages.Message("WNA_UpliftSubjectGone".Translate(), MessageTypeDefOf.NegativeEvent, false);
                return;
            }

            IntVec3 origin = ResolveSubjectCell(subjectThing, job.subjectCell, map);
            if (WNAMod.Settings?.upliftSpawnAtRitualTarget == true && job.selectedTarget.IsValid)
            {
                IntVec3 ritualCell = job.selectedTarget.Cell;
                if (ritualCell.IsValid && ritualCell.InBounds(map))
                    origin = ritualCell;
            }
            IntVec3 spawnCell = CellFinder.StandableCellNear(origin, map, 8f);
            if (!spawnCell.IsValid)
            {
                CellFinder.TryFindRandomCellNear(origin, map, 12, c => c.Standable(map) && !c.Fogged(map), out spawnCell);
            }
            if (!spawnCell.IsValid)
                spawnCell = CellFinderLoose.RandomCellWith(c => c.Standable(map) && !c.Fogged(map), map);
            if (!spawnCell.IsValid)
                spawnCell = origin;

            Pawn result = GenerateResultPawn(job, kind, sourcePawn, subjectThing, map);
            if (result == null)
            {
                Messages.Message("WNA_UpliftFailed".Translate(), MessageTypeDefOf.NegativeEvent, false);
                return;
            }

            GenSpawn.Spawn(result, spawnCell, map, Rot4.Random);
            bool named = CopyIdentity(job, kind, sourcePawn, result);
            RemoveSubject(subjectThing, sourcePawn);
            if (!named)
            {
                Find.WindowStack.Add(new Dialog_NamePawn(
                    result,
                    NameFilter.First | NameFilter.Nick | NameFilter.Last,
                    NameFilter.First | NameFilter.Nick | NameFilter.Last,
                    null));
            }
            Messages.Message("WNA_UpliftCompleted".Translate(result.LabelShort.Named("PAWN")), result, MessageTypeDefOf.PositiveEvent, false);
        }

        private static Pawn GenerateResultPawn(LordJob_Ritual_Uplifting job, UpliftSubjectKind kind, Pawn sourcePawn, Thing subjectThing, Map map)
        {
            PawnKindDef kindDef = ResultKindDef(kind);
            if (kindDef == null)
                return null;
            bool humanOrigin = kind == UpliftSubjectKind.Human || kind == UpliftSubjectKind.Humanoid;
            float? bioAge = null;
            float? chronoAge = null;
            if (humanOrigin)
            {
                if (sourcePawn != null)
                {
                    bioAge = sourcePawn.ageTracker.AgeBiologicalYearsFloat;
                    chronoAge = sourcePawn.ageTracker.AgeChronologicalYearsFloat;
                }
            }
            else
            {
                bioAge = 18f;
                if (sourcePawn != null)
                    chronoAge = sourcePawn.ageTracker.AgeChronologicalYearsFloat;
                else if (subjectThing is Plant plant)
                    chronoAge = plant.Age / 3600000f;
                else
                    chronoAge = 0f;
            }
            Gender? gender;
            if (sourcePawn != null && (sourcePawn.gender == Gender.Male || sourcePawn.gender == Gender.Female))
                gender = sourcePawn.gender;
            else
                gender = Rand.Bool ? Gender.Male : Gender.Female;
            XenotypeDef xenotype = kind == UpliftSubjectKind.Humanoid
                ? DefDatabase<XenotypeDef>.GetNamed("WNA_Mortaliner", false)
                : null;
            if (!ModsConfig.BiotechActive)
                xenotype = null;
            Ideo resultIdeo = ResolveResultIdeo(job);
            PawnGenerationRequest request = new PawnGenerationRequest(
                kindDef,
                Faction.OfPlayer,
                PawnGenerationContext.NonPlayer,
                map.Tile,
                forceGenerateNewPawn: true,
                canGeneratePawnRelations: false,
                fixedBiologicalAge: bioAge,
                fixedChronologicalAge: chronoAge,
                fixedGender: gender,
                fixedIdeo: resultIdeo,
                forceNoBackstory: true,
                forceNoGear: true,
                forcedXenotype: xenotype);
            Pawn result = PawnGenerator.GeneratePawn(request);
            if (result == null)
                return null;
            if (result.Faction != Faction.OfPlayer)
                result.SetFactionDirect(Faction.OfPlayer);
            if (ModsConfig.IdeologyActive && resultIdeo != null && result.ideo != null && result.Ideo != resultIdeo)
                result.ideo.SetIdeo(resultIdeo);
            if (result.story != null)
            {
                if (humanOrigin)
                {
                    if (sourcePawn?.story?.Childhood != null)
                        result.story.Childhood = sourcePawn.story.Childhood;
                    result.story.Adulthood = sourcePawn?.story?.Adulthood;
                }
                else
                {
                    result.story.Childhood = ResultBackstory(kind);
                    result.story.Adulthood = null;
                }
            }
            return result;
        }

        private static PawnKindDef ResultKindDef(UpliftSubjectKind kind)
        {
            switch (kind)
            {
                case UpliftSubjectKind.Animal:
                case UpliftSubjectKind.Plant:
                case UpliftSubjectKind.Entity:
                case UpliftSubjectKind.Mech:
                    return WNAMainDefOf.WNA_UpliftedHuman;
                case UpliftSubjectKind.Human:
                    return WNAMainDefOf.WNA_UpliftedHumanoid;
                case UpliftSubjectKind.Humanoid:
                    return WNAMainDefOf.WNA_UpliftedWhelingnorthan;
                default:
                    return null;
            }
        }

        private static BackstoryDef ResultBackstory(UpliftSubjectKind kind)
        {
            switch (kind)
            {
                case UpliftSubjectKind.Animal:
                    return WNAMainDefOf.WNA_BC_UpliftedAnimal;
                case UpliftSubjectKind.Plant:
                    return WNAMainDefOf.WNA_BC_UpliftedPlant;
                case UpliftSubjectKind.Entity:
                    return WNAMainDefOf.WNA_BC_UpliftedEntity;
                case UpliftSubjectKind.Mech:
                    return WNAMainDefOf.WNA_BC_UpliftedMech;
                default:
                    return null;
            }
        }

        private static Ideo ResolveResultIdeo(LordJob_Ritual_Uplifting job)
        {
            Ideo ritualIdeo = job.Ritual?.ideo;
            if (ritualIdeo != null && ritualIdeo.HasPrecept(WNAMainDefOf.WNA_Proselyte))
                return ritualIdeo;
            if (job.assignments != null)
            {
                foreach (Pawn participant in job.assignments.Participants)
                {
                    if (General.PawnHasIdeo(participant))
                        return participant.Ideo;
                }
            }
            return General.WNAIdeo ?? ritualIdeo;
        }

        // Returns true when the result already carries a usable name.
        private static bool CopyIdentity(LordJob_Ritual_Uplifting job, UpliftSubjectKind kind, Pawn sourcePawn, Pawn result)
        {
            if (sourcePawn == null || result == null)
                return false;
            bool humanOrigin = kind == UpliftSubjectKind.Human || kind == UpliftSubjectKind.Humanoid;
            bool named = sourcePawn.Name != null;
            if (named)
                result.Name = sourcePawn.Name;
            if (result.story == null)
                return named;
            if (humanOrigin)
            {
                result.story.birthLastName = sourcePawn.story?.birthLastName;
                CopySkills(sourcePawn, result);
                CopyRelations(sourcePawn, result);
            }
            ApplyTraits(kind, sourcePawn, result);
            CopyAbilities(sourcePawn, result);
            if (kind == UpliftSubjectKind.Human && sourcePawn.story != null)
            {
                if (sourcePawn.story.headType != null)
                    result.story.headType = sourcePawn.story.headType;
                if (sourcePawn.story.bodyType != null)
                    result.story.bodyType = sourcePawn.story.bodyType;
                if (sourcePawn.story.hairDef != null)
                    result.story.hairDef = sourcePawn.story.hairDef;
                result.story.skinColorOverride = sourcePawn.story.skinColorOverride;
                result.story.favoriteColor = sourcePawn.story.favoriteColor;
                if (sourcePawn.Corpse == null)
                    result.story.HairColor = sourcePawn.story.HairColor;
                result.Drawer?.renderer?.SetAllGraphicsDirty();
            }
            result.Notify_DisabledWorkTypesChanged();
            return named;
        }

        private static void ApplyTraits(UpliftSubjectKind kind, Pawn sourcePawn, Pawn result)
        {
            TraitSet traits = result.story?.traits;
            if (traits == null)
                return;
            List<Trait> generated = new List<Trait>(traits.allTraits);
            foreach (Trait trait in generated)
            {
                if (trait == null || trait.sourceGene != null)
                    continue;
                traits.RemoveTrait(trait);
            }
            bool humanOrigin = kind == UpliftSubjectKind.Human || kind == UpliftSubjectKind.Humanoid;
            List<TraitDef> negativeTraits = WNAMainDefOf.WNA_NegativeTraitList?.traits;
            if (humanOrigin && sourcePawn?.story?.traits != null)
            {
                List<Trait> sourceTraits = new List<Trait>(sourcePawn.story.traits.allTraits);
                foreach (Trait trait in sourceTraits)
                {
                    if (trait?.def == null || trait.Suppressed)
                        continue;
                    if (negativeTraits != null && negativeTraits.Contains(trait.def))
                        continue;
                    TryAddTrait(traits, trait.def, trait.Degree, forced: false);
                }
            }
            if (result.kindDef?.forcedTraits != null)
            {
                foreach (TraitRequirement requirement in result.kindDef.forcedTraits)
                {
                    if (requirement?.def == null)
                        continue;
                    TryAddTrait(traits, requirement.def, requirement.degree ?? 0, forced: true);
                }
            }
        }

        private static void TryAddTrait(TraitSet traits, TraitDef def, int degree, bool forced)
        {
            if (def == null || traits.HasTrait(def))
                return;
            Trait candidate = new Trait(def, degree, forced);
            foreach (Trait existing in traits.allTraits)
            {
                if (existing?.def == null)
                    continue;
                if (def.ConflictsWith(existing) || existing.def.ConflictsWith(candidate))
                    return;
            }
            traits.GainTrait(candidate);
        }

        private static void CopyAbilities(Pawn source, Pawn result)
        {
            if (source?.abilities == null || result?.abilities == null)
                return;
            List<Ability> abilities = source.abilities.abilities;
            for (int i = 0; i < abilities.Count; i++)
            {
                AbilityDef def = abilities[i]?.def;
                if (def == null)
                    continue;
                if (result.abilities.GetAbility(def, includeTemporary: true) != null)
                    continue;
                result.abilities.GainAbility(def);
            }
        }

        private static void CopySkills(Pawn source, Pawn result)
        {
            if (source.skills == null || result.skills == null)
                return;
            foreach (SkillRecord src in source.skills.skills)
            {
                SkillRecord dst = result.skills.GetSkill(src.def);
                if (dst == null || dst.TotallyDisabled)
                    continue;
                dst.Level = src.Level;
                dst.passion = src.passion;
                dst.xpSinceLastLevel = src.xpSinceLastLevel;
                dst.xpSinceMidnight = src.xpSinceMidnight;
            }
        }

        private static void CopyRelations(Pawn source, Pawn result)
        {
            if (source.relations == null || result.relations == null)
                return;
            List<DirectPawnRelation> outgoing = new List<DirectPawnRelation>(source.relations.DirectRelations);
            foreach (DirectPawnRelation relation in outgoing)
            {
                if (relation?.def == null)
                    continue;
                Pawn other = relation.otherPawn;
                if (other == null || other == source || other == result || other.Destroyed || relation.def.implied)
                    continue;
                if (!result.relations.DirectRelationExists(relation.def, other))
                    result.relations.AddDirectRelation(relation.def, other);
            }
            foreach (Pawn other in PawnsFinder.AllMapsWorldAndTemporary_AliveOrDead)
            {
                if (other == null || other == source || other == result || other.Destroyed || other.relations == null)
                    continue;
                List<DirectPawnRelation> others = other.relations.DirectRelations;
                for (int i = others.Count - 1; i >= 0; i--)
                {
                    DirectPawnRelation relation = others[i];
                    if (relation?.def == null || relation.otherPawn != source || relation.def.reflexive || relation.def.implied)
                        continue;
                    if (!result.relations.DirectRelationExists(relation.def, other))
                        other.relations.AddDirectRelation(relation.def, result);
                }
            }
        }

        private static IntVec3 ResolveSubjectCell(Thing subjectThing, IntVec3 fallback, Map map)
        {
            IntVec3 cell = subjectThing.PositionHeld;
            if (!cell.IsValid || !cell.InBounds(map))
                cell = fallback;
            if (!cell.IsValid || !cell.InBounds(map))
                cell = map.Center;
            return cell;
        }

        private static void RemoveSubject(Thing subjectThing, Pawn sourcePawn)
        {
            if (subjectThing is Corpse corpse)
            {
                corpse.Destroy(DestroyMode.Vanish);
                if (sourcePawn != null && !sourcePawn.Destroyed && sourcePawn.Corpse == null)
                    Find.WorldPawns.RemoveAndDiscardPawnViaGC(sourcePawn);
                return;
            }
            if (subjectThing is Plant plant)
            {
                plant.Destroy(DestroyMode.Vanish);
                return;
            }
            Pawn pawn = sourcePawn ?? subjectThing as Pawn;
            if (pawn == null)
            {
                if (subjectThing != null && !subjectThing.Destroyed)
                    subjectThing.Destroy(DestroyMode.Vanish);
                return;
            }
            TryEject(pawn);
            DropGear(pawn);
            General.TotalRemoving(pawn);
        }

        private static void TryEject(Thing thing)
        {
            if (thing.Spawned || thing.ParentHolder == null)
                return;
            if (thing.ParentHolder is Pawn_CarryTracker)
                return;
            if (thing.ParentHolder is Building_HoldingPlatform platform)
            {
                platform.EjectContents();
                return;
            }
            if (thing.ParentHolder is CompEntityHolder holder)
            {
                holder.EjectContents();
                return;
            }
            ThingOwner owner = thing.ParentHolder.GetDirectlyHeldThings();
            Map map = GetSpawnedParentOrSelf(thing)?.Map;
            if (owner != null && map != null && owner.Contains(thing))
                owner.TryDrop(thing, thing.PositionHeld, map, ThingPlaceMode.Near, out _);
        }

        private static void DropGear(Pawn pawn)
        {
            if (pawn == null || pawn.Dead)
                return;
            IntVec3 pos = pawn.Spawned ? pawn.Position : pawn.PositionHeld;
            if (!pos.IsValid)
                return;
            pawn.apparel?.DropAll(pos);
            pawn.equipment?.DropAllEquipment(pos);
            pawn.inventory?.DropAllNearPawn(pos);
        }
    }
}
