using System;
using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;
using WNA.WNADefOf;

namespace WNA.WNAUtility
{
    internal static class WNAPsychicDawnUtility
    {
        private const int CatatonicTicks = 900000;

        internal static bool Active => WNAMainDefOf.WNA_PsychicDawn?.IsFinished == true;

        internal static bool IsProtected(Thing thing)
        {
            if (thing == null || !Active)
                return false;
            return IsProtectedGear(thing) || IsProtectedBuilding(thing) || IsProtectedPlant(thing)
                || IsProtectedStoredThing(thing);
        }

        internal static bool IsProtectedBuilding(Thing thing)
        {
            if (!(thing is Building building) || !building.Spawned)
                return false;
            if (building.Faction != Faction.OfPlayer || !building.def.useHitPoints)
                return false;
            return InHomeArea(building);
        }

        internal static bool IsProtectedPlant(Thing thing)
        {
            if (!(thing is Plant plant) || !plant.Spawned || !plant.def.useHitPoints)
                return false;
            return IsInGrowingZone(plant);
        }

        internal static bool IsProtectedGear(Thing thing)
        {
            if (thing is Pawn || !thing.def.useHitPoints)
                return false;
            Pawn owner = GearOwner(thing);
            return owner != null && owner.Faction == Faction.OfPlayer;
        }

        internal static bool IsProtectedStoredThing(Thing thing)
        {
            if (thing == null || !thing.Spawned || !thing.def.useHitPoints)
                return false;
            if (thing is Pawn || thing is Building || thing is Plant || thing is Corpse)
                return false;
            Map map = thing.Map;
            if (map == null)
                return false;
            IntVec3 cell = thing.Position;
            if (!cell.InBounds(map))
                return false;
            SlotGroup group = map.haulDestinationManager.SlotGroupAt(cell);
            if (group == null)
                return false;
            return !(group.parent is Thing parentThing) || parentThing.Faction == Faction.OfPlayer;
        }

        private static Pawn GearOwner(Thing thing)
        {
            switch (thing.ParentHolder)
            {
                case Pawn_EquipmentTracker equipment:
                    return equipment.pawn;
                case Pawn_ApparelTracker apparel:
                    return apparel.pawn;
                case Pawn_InventoryTracker inventory:
                    return inventory.pawn;
                default:
                    return null;
            }
        }

        private static bool InHomeArea(Thing thing)
        {
            Map map = thing.Map;
            if (map == null)
                return false;
            IntVec3 cell = thing.Position;
            return cell.InBounds(map) && map.areaManager.Home[cell];
        }

        internal static bool IsInGrowingZone(Plant plant)
        {
            Map map = plant.Map;
            if (map == null)
                return false;
            IntVec3 cell = plant.Position;
            return cell.InBounds(map) && map.zoneManager.ZoneAt(cell) is IPlantToGrowSettable;
        }

        internal static float GrowthMultiplierFor(Plant plant)
        {
            TerrainDef terrain = plant.Position.GetTerrain(plant.Map);
            Building building = plant.Position.GetEdifice(plant.Map);
            float fertility = terrain?.fertility ?? 0f;
            if (building != null)
                fertility += building.def?.fertility ?? 0f;
            return Mathf.Max(1f, fertility * 2f);
        }

        internal static void HealToFull(Thing thing)
        {
            if (thing == null || !thing.def.useHitPoints)
                return;
            int maxHitPoints = thing.MaxHitPoints;
            if (maxHitPoints > 0 && thing.HitPoints < maxHitPoints)
                thing.HitPoints = maxHitPoints;
        }

        internal static void HealPawnGear(Pawn pawn)
        {
            if (pawn == null)
                return;
            if (pawn.equipment != null)
            {
                List<ThingWithComps> equipment = pawn.equipment.AllEquipmentListForReading;
                for (int i = 0; i < equipment.Count; i++)
                    HealToFull(equipment[i]);
            }
            if (pawn.apparel != null)
            {
                List<Apparel> apparel = pawn.apparel.WornApparel;
                for (int i = 0; i < apparel.Count; i++)
                    HealToFull(apparel[i]);
            }
            if (pawn.inventory != null)
            {
                foreach (Thing item in pawn.inventory.innerContainer)
                    HealToFull(item);
            }
        }

        internal static void ApplyMapEffects(Map map)
        {
            if (map == null)
                return;
            ClearPollutionArea(map);
            HealHomeAreaBuildings(map);
            HealGrowingZonePlants(map);
            HealStoredThings(map);
            HandleEscapers(map);
        }

        internal static void ClearPollutionArea(Map map)
        {
            if (!Active || !ModsConfig.BiotechActive || map.pollutionGrid == null)
                return;
            Area_PollutionClear area = map.areaManager.PollutionClear;
            if (area == null)
                return;
            foreach (IntVec3 cell in area.ActiveCells)
                map.pollutionGrid.SetPolluted(cell, false, silent: true);
        }

        internal static bool IsInPollutionClearArea(Map map, IntVec3 cell)
        {
            if (!Active || !ModsConfig.BiotechActive || map == null || !cell.InBounds(map))
                return false;
            Area_PollutionClear area = map.areaManager.PollutionClear;
            return area != null && area[cell];
        }

        internal static void ClearPollutionAt(Map map, IntVec3 cell)
        {
            if (!Active || !ModsConfig.BiotechActive || map == null || map.pollutionGrid == null || !cell.InBounds(map))
                return;
            map.pollutionGrid.SetPolluted(cell, false, silent: true);
        }

        private static void HealHomeAreaBuildings(Map map)
        {
            List<Building> buildings = map.listerBuildings.allBuildingsColonist;
            for (int i = 0; i < buildings.Count; i++)
            {
                Building building = buildings[i];
                if (building.Spawned && InHomeArea(building))
                    HealToFull(building);
            }
        }

        private static void HealGrowingZonePlants(Map map)
        {
            List<Zone> zones = map.zoneManager.AllZones;
            for (int i = 0; i < zones.Count; i++)
            {
                if (!(zones[i] is IPlantToGrowSettable zone))
                    continue;
                foreach (IntVec3 cell in zone.Cells)
                {
                    Plant plant = cell.GetPlant(map);
                    if (plant != null)
                        HealToFull(plant);
                }
            }
        }

        private static void HealStoredThings(Map map)
        {
            List<SlotGroup> groups = map.haulDestinationManager.AllGroupsListForReading;
            for (int i = 0; i < groups.Count; i++)
            {
                SlotGroup group = groups[i];
                if (group.parent is Thing parentThing && parentThing.Faction != Faction.OfPlayer)
                    continue;
                List<IntVec3> cells = group.CellsList;
                for (int j = 0; j < cells.Count; j++)
                {
                    List<Thing> things = cells[j].GetThingList(map);
                    for (int k = 0; k < things.Count; k++)
                    {
                        Thing thing = things[k];
                        if (IsProtectedStoredThing(thing))
                            HealToFull(thing);
                    }
                }
            }
        }

        private static void HandleEscapers(Map map)
        {
            IReadOnlyList<Pawn> pawns = map.mapPawns.AllPawnsSpawned;
            for (int i = 0; i < pawns.Count; i++)
            {
                Pawn pawn = pawns[i];
                if (pawn.IsPrisonerOfColony && PrisonBreakUtility.IsPrisonBreaking(pawn))
                {
                    ApplyCatatonicBreakdown(pawn);
                    continue;
                }
                if (pawn.IsEntity && pawn.TryGetComp<CompHoldingPlatformTarget>()?.isEscaping == true)
                    ApplyCatatonicBreakdown(pawn);
            }
        }

        internal static void ApplyCatatonicBreakdown(Pawn pawn)
        {
            if (pawn?.health == null)
                return;
            Hediff hediff = pawn.health.hediffSet.GetFirstHediffOfDef(HediffDefOf.CatatonicBreakdown);
            if (hediff == null)
            {
                hediff = HediffMaker.MakeHediff(HediffDefOf.CatatonicBreakdown, pawn);
                pawn.health.AddHediff(hediff);
            }
            if (hediff.TryGetComp<HediffComp_Disappears>() is HediffComp_Disappears disappears)
                disappears.ticksToDisappear = Mathf.Max(disappears.ticksToDisappear, CatatonicTicks);
            pawn.stances?.CancelBusyStanceSoft();
            pawn.health.forceDowned = true;
            CompHoldingPlatformTarget platformTarget = pawn.TryGetComp<CompHoldingPlatformTarget>();
            if (platformTarget != null)
                platformTarget.isEscaping = false;
        }

        internal static void OnResearchCompleted()
        {
            for (int i = 0; i < Find.Maps.Count; i++)
                ApplyMapEffects(Find.Maps[i]);
            foreach (Pawn pawn in PawnsFinder.AllMapsCaravansAndTravellingTransporters_Alive_OfPlayerFaction)
                HealPawnGear(pawn);
        }

        #region EducationMotion

        private const float XpPerSkill = 86400f;

        internal static bool EducationMotionActive => WNAMainDefOf.WNA_EducationMotion?.IsFinished == true;

        internal static void RunEducationMotion()
        {
            List<Pawn> colonists = PawnsFinder.AllMapsCaravansAndTravellingTransporters_Alive_FreeColonists;
            List<SkillDef> skills = DefDatabase<SkillDef>.AllDefsListForReading;
            for (int i = 0; i < colonists.Count; i++)
            {
                Pawn pawn = colonists[i];
                if (pawn?.skills == null)
                    continue;
                for (int j = 0; j < skills.Count; j++)
                    pawn.skills.Learn(skills[j], XpPerSkill, direct: true, ignoreLearnRate: true);
            }

            List<Pawn> pawns = PawnsFinder.AllMapsCaravansAndTravellingTransporters_Alive;
            List<TrainableDef> trainables = TrainableUtility.TrainableDefsInListOrder;
            for (int i = 0; i < pawns.Count; i++)
            {
                Pawn animal = pawns[i];
                if (animal == null || !animal.IsColonyAnimal || animal.training == null)
                    continue;
                for (int j = 0; j < trainables.Count; j++)
                {
                    TrainableDef trainable = trainables[j];
                    if (animal.training.HasLearned(trainable))
                        continue;
                    if (!animal.training.CanAssignToTrain(trainable).Accepted)
                        continue;
                    animal.training.Train(trainable, null);
                }
            }
        }

        #endregion

        #region ConversionMotion

        internal static bool ConversionMotionActive => WNAMainDefOf.WNA_ConversionMotion?.IsFinished == true;

        internal static bool TryRemoveNegativeTrait(Pawn pawn)
        {
            TraitSet traits = pawn?.story?.traits;
            if (traits == null)
                return false;

            Trait negative = FindNegativeTrait(traits);
            if (negative == null)
                return false;

            traits.RemoveTrait(negative, unsuppressConflicts: true);
            return true;
        }

        internal static void ApplyReeducationAftermath(Pawn prisoner, Pawn warden)
        {
            if (prisoner?.guest == null)
                return;

            prisoner.guest.resistance = 0f;
            prisoner.guest.will = 0f;
            prisoner.guest.Recruitable = true;

            if (ModsConfig.IdeologyActive
                && prisoner.ideo != null
                && Find.IdeoManager != null
                && !Find.IdeoManager.classicMode)
            {
                prisoner.ideo.OffsetCertainty(-100f);
            }

            if (WNAMainDefOf.WNA_ThoughtFear != null
                && prisoner.needs?.mood?.thoughts?.memories != null)
            {
                Thought_Memory fear = (Thought_Memory)ThoughtMaker.MakeThought(WNAMainDefOf.WNA_ThoughtFear);
                fear.permanent = true;
                prisoner.needs.mood.thoughts.memories.TryGainMemory(fear, warden);
            }

            Messages.Message("WNA_ConversionMotion_Done".Translate(prisoner.LabelShort),
                prisoner, MessageTypeDefOf.NeutralEvent, historical: false);
        }

        #endregion

        #region ConfessionMotion

        private const int MaxConfessionColonists = 8;

        internal static bool ConfessionMotionActive => WNAMainDefOf.WNA_ConfessionMotion?.IsFinished == true;

        internal static void RunConfessionMotion()
        {
            List<Pawn> colonists = PawnsFinder.AllMapsCaravansAndTravellingTransporters_Alive_FreeColonists;
            if (colonists.NullOrEmpty())
                return;

            List<Pawn> candidates = new List<Pawn>(colonists);
            candidates.Shuffle();
            int count = Math.Min(MaxConfessionColonists, candidates.Count);
            List<string> names = new List<string>();
            for (int i = 0; i < count; i++)
            {
                Pawn pawn = candidates[i];
                if (pawn?.story?.traits == null)
                    continue;
                ResolveConfession(pawn);
                names.Add(pawn.LabelShort);
            }

            if (names.Count > 0)
            {
                Messages.Message(
                    "WNA_ConfessionMotion_Event".Translate(string.Join(", ", names)),
                    MessageTypeDefOf.NeutralEvent,
                    historical: false);
            }
        }

        private static void ResolveConfession(Pawn pawn)
        {
            TraitSet traits = pawn.story.traits;
            if (TryRemoveNegativeTrait(pawn))
                return;

            TraitDef unshakable = WNAMainDefOf.WNA_Trait_Unshakable;
            if (unshakable != null && !traits.HasTrait(unshakable))
            {
                traits.GainTrait(new Trait(unshakable));
                return;
            }

            InspirationHandler inspirationHandler = pawn.mindState?.inspirationHandler;
            if (inspirationHandler == null || inspirationHandler.Inspired)
                return;
            InspirationDef inspiration = inspirationHandler.GetRandomAvailableInspirationDef();
            if (inspiration != null)
            {
                inspirationHandler.TryStartInspiration(
                    inspiration,
                    "WNA_ConfessionMotion_InspirationReason".Translate());
            }
        }

        private static Trait FindNegativeTrait(TraitSet traits)
        {
            List<TraitDef> negativeTraits = WNAMainDefOf.WNA_NegativeTraitList?.traits;
            if (negativeTraits.NullOrEmpty())
                return null;

            List<Trait> candidates = new List<Trait>();
            List<Trait> allTraits = traits.allTraits;
            for (int i = 0; i < allTraits.Count; i++)
            {
                Trait trait = allTraits[i];
                if (trait == null || trait.Suppressed)
                    continue;
                if (negativeTraits.Contains(trait.def))
                    candidates.Add(trait);
            }
            return candidates.Count > 0 ? candidates.RandomElement() : null;
        }

        #endregion

        #region PreservationAdministration

        internal static bool PreservationAdministrationActive => WNAMainDefOf.WNA_PreservationAdministration?.IsFinished == true;

        internal static bool IsPlayerSlave(Pawn pawn)
        {
            return pawn != null && pawn.IsSlave && pawn.Faction == Faction.OfPlayer;
        }

        internal static bool IsPlayerMech(Pawn pawn)
        {
            return pawn != null
                && ModsConfig.BiotechActive
                && pawn.RaceProps.IsMechanoid
                && pawn.Faction == Faction.OfPlayer;
        }

        internal static void ApplyPreservation(Pawn pawn)
        {
            if (pawn == null)
                return;
            if (IsPlayerSlave(pawn))
            {
                if (pawn.needs != null && pawn.needs.TryGetNeed(out Need_Suppression suppression))
                    suppression.CurLevel = suppression.MaxLevel;
                if (pawn.guest != null)
                {
                    pawn.guest.resistance = 0f;
                    pawn.guest.will = 0f;
                }
            }
            if (IsPlayerMech(pawn))
            {
                if (pawn.needs?.energy != null)
                    pawn.needs.energy.CurLevel = pawn.needs.energy.MaxLevel;
                RemoveSelfShutdown(pawn);
            }
        }

        internal static void RemoveSelfShutdown(Pawn pawn)
        {
            if (pawn.health?.hediffSet == null)
                return;
            Hediff hediff = pawn.health.hediffSet.GetFirstHediffOfDef(HediffDefOf.SelfShutdown);
            if (hediff == null)
                return;
            pawn.health.RemoveHediff(hediff);
            pawn.jobs?.CheckForJobOverride();
        }

        #endregion

        #region MindReading

        internal static bool MindReadingActive => WNAMainDefOf.WNA_MindReading?.IsFinished == true;

        internal static bool IsMindReadingTarget(Pawn pawn)
        {
            return pawn != null
                && pawn.Faction != Faction.OfPlayer
                && pawn.HostFaction != Faction.OfPlayer;
        }

        #endregion

        #region MemoryRedaction

        internal static bool MemoryRedactionActive => WNAMainDefOf.WNA_MemoryRedaction?.IsFinished == true;

        internal static void RunMemoryRedaction()
        {
            List<Pawn> pawns = new List<Pawn>();
            foreach (Pawn pawn in PawnsFinder.AllMapsCaravansAndTravellingTransporters_Alive_OfPlayerFaction)
            {
                if (pawn != null && (pawn.IsColonist || pawn.IsSlaveOfColony))
                    pawns.Add(pawn);
            }

            for (int i = 0; i < pawns.Count; i++)
                RemoveNegativeThoughts(pawns[i]);

            for (int i = 0; i < pawns.Count; i++)
            {
                for (int j = i + 1; j < pawns.Count; j++)
                    RemoveNegativeRelationsBetween(pawns[i], pawns[j]);
            }
        }

        private static void RemoveNegativeThoughts(Pawn pawn)
        {
            MemoryThoughtHandler memories = pawn.needs?.mood?.thoughts?.memories;
            if (memories == null)
                return;

            List<Thought_Memory> list = memories.Memories;
            for (int i = list.Count - 1; i >= 0; i--)
            {
                Thought_Memory memory = list[i];
                if (memory.MoodOffset() < 0f)
                    memories.RemoveMemory(memory);
            }
        }

        private static void RemoveNegativeRelationsBetween(Pawn a, Pawn b)
        {
            RemoveNegativeDirectRelations(a, b);
            RemoveNegativeDirectRelations(b, a);
            RemoveNegativeSocialMemories(a, b);
            RemoveNegativeSocialMemories(b, a);
        }

        private static void RemoveNegativeDirectRelations(Pawn pawn, Pawn other)
        {
            Pawn_RelationsTracker relations = pawn.relations;
            if (relations == null)
                return;

            List<DirectPawnRelation> list = relations.DirectRelations;
            for (int i = list.Count - 1; i >= 0; i--)
            {
                DirectPawnRelation relation = list[i];
                if (relation.otherPawn == other && relation.def.opinionOffset < 0)
                    relations.RemoveDirectRelation(relation);
            }
        }

        private static void RemoveNegativeSocialMemories(Pawn pawn, Pawn other)
        {
            MemoryThoughtHandler memories = pawn.needs?.mood?.thoughts?.memories;
            if (memories == null)
                return;

            List<Thought_Memory> list = memories.Memories;
            for (int i = list.Count - 1; i >= 0; i--)
            {
                if (list[i] is Thought_MemorySocial social
                    && social.otherPawn == other
                    && social.OpinionOffset() < 0f)
                {
                    memories.RemoveMemory(social);
                }
            }
        }

        #endregion

        #region IdentityExclusion

        internal static bool IdentityExclusionActive => WNAMainDefOf.WNA_IdentityExclusion?.IsFinished == true;

        internal static bool IsIdentityExclusionTarget(Pawn pawn)
        {
            if (pawn == null)
                return false;
            if (!pawn.IsColonist && !pawn.IsSlaveOfColony && !pawn.IsPrisonerOfColony)
                return false;
            return !General.IsWNARace(pawn) || General.ConsideredBetrayer(pawn);
        }

        internal static void SentencePawn(Pawn pawn)
        {
            if (pawn?.guilt == null)
                return;

            pawn.guilt.Notify_Guilty();
            int durationTicks = pawn.guilt.TicksUntilInnocent;

            if (WNAMainDefOf.WNA_ThoughtCondemned == null || pawn.needs?.mood?.thoughts?.memories == null)
                return;

            Thought_Memory memory = (Thought_Memory)ThoughtMaker.MakeThought(WNAMainDefOf.WNA_ThoughtCondemned);
            if (durationTicks > 0)
                memory.durationTicksOverride = durationTicks;
            pawn.needs.mood.thoughts.memories.TryGainMemory(memory);
        }

        internal static void ExecutePawn(Pawn pawn)
        {
            if (pawn == null)
                return;

            Map map = pawn.MapHeld;
            IntVec3 cell = pawn.PositionHeld;

            try
            {
                pawn.Kill(null);
            }
            catch (Exception exception)
            {
                Log.Error($"WNA identity exclusion: error while killing {pawn.ToStringSafe()}.\n{exception}");
            }

            if (!pawn.Dead)
            {
                try
                {
                    pawn.Destroy(DestroyMode.Vanish);
                }
                catch (Exception exception)
                {
                    Log.Error($"WNA identity exclusion: error while destroying {pawn.ToStringSafe()}.\n{exception}");
                }
            }

            if (!pawn.Dead && !pawn.Destroyed)
            {
                Log.Error("WNA_IdentityExclusion_RemoveFailed".Translate(pawn.ToStringSafe()));
                return;
            }

            if (map != null && cell.IsValid && cell.InBounds(map))
                map.weatherManager.eventHandler.AddEvent(new WeatherEvent_LightningStrike(map, cell));

            foreach (Pawn other in PawnsFinder.AllMapsCaravansAndTravellingTransporters_Alive)
            {
                if (other == pawn || !other.Spawned)
                    continue;
                if (!other.IsColonist && !other.IsSlaveOfColony && !other.IsPrisonerOfColony)
                    continue;
                MoteMaker.MakeColonistActionOverlay(other, ThingDefOf.Mote_ColonistFleeing);
            }
        }

        #endregion
    }
}
