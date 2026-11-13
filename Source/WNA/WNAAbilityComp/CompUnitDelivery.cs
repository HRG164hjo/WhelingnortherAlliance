using System.Collections.Generic;
using RimWorld;
using RimWorld.Planet;
using UnityEngine;
using Verse;
using Verse.Sound;

namespace WNA.WNAAbilityComp
{
    internal class PawnDeliveryEntry
    {
        public PawnKindDef kind = null;
        public int count = 1;
    }

    internal class ThingDeliveryEntry
    {
        public ThingDef thing = null;
        public int count = 1;
    }

    internal class PropUnitDelivery : CompProperties_AbilityEffect
    {
        public List<PawnDeliveryEntry> pawns = null;
        public List<ThingDeliveryEntry> things = null;
        public float radius = 3.9f;
        public bool aroundCaster = true;
        public bool limitAliveCount = false;
        public int maxAlive = 6;
        public EffecterDef spawnEffecter = null;
        public SoundDef spawnSound = null;

        public PropUnitDelivery()
        {
            compClass = typeof(CompUnitDelivery);
        }
    }

    internal class CompUnitDelivery : CompAbilityEffect
    {
        private List<Pawn> deliveredPawns = new List<Pawn>();

        private new PropUnitDelivery Props => (PropUnitDelivery)props;

        public override bool CanCast
        {
            get
            {
                if (!Props.limitAliveCount)
                    return true;
                PruneDeliveredPawns();
                return deliveredPawns.Count < Props.maxAlive;
            }
        }

        public override bool CanApplyOn(LocalTargetInfo target, LocalTargetInfo dest)
        {
            Pawn caster = parent.pawn;
            if (caster == null || !caster.Spawned || caster.Map == null)
                return false;
            if (Props.limitAliveCount)
            {
                PruneDeliveredPawns();
                if (deliveredPawns.Count >= Props.maxAlive)
                    return false;
            }
            return true;
        }

        public override void Apply(LocalTargetInfo target, LocalTargetInfo dest)
        {
            base.Apply(target, dest);
            Pawn caster = parent.pawn;
            if (caster == null || !caster.Spawned || caster.Map == null)
                return;
            Map map = caster.Map;
            IntVec3 center = Props.aroundCaster || !target.IsValid ? caster.Position : target.Cell;
            PruneDeliveredPawns();

            int remaining = Props.limitAliveCount
                ? Mathf.Max(0, Props.maxAlive - deliveredPawns.Count)
                : int.MaxValue;

            if (remaining > 0 && !Props.pawns.NullOrEmpty())
            {
                foreach (PawnDeliveryEntry entry in Props.pawns)
                {
                    if (entry?.kind == null || entry.count <= 0)
                        continue;
                    int count = Mathf.Min(entry.count, remaining);
                    for (int i = 0; i < count; i++)
                    {
                        Pawn summoned = GenerateUnit(entry.kind, caster);
                        if (summoned == null)
                            continue;
                        if (!TryPlace(summoned, map, center))
                        {
                            Find.WorldPawns.PassToWorld(summoned, PawnDiscardDecideMode.Discard);
                            continue;
                        }
                        deliveredPawns.Add(summoned);
                        remaining--;
                    }
                    if (remaining <= 0)
                        break;
                }
            }

            if (!Props.things.NullOrEmpty())
            {
                foreach (ThingDeliveryEntry entry in Props.things)
                {
                    if (entry?.thing == null || entry.count <= 0)
                        continue;
                    for (int i = 0; i < entry.count; i++)
                    {
                        Thing thing = ThingMaker.MakeThing(entry.thing);
                        if (!TryPlace(thing, map, center))
                            thing.Destroy();
                    }
                }
            }

            if (Props.spawnEffecter != null)
            {
                Effecter effecter = Props.spawnEffecter.Spawn();
                effecter.Trigger(new TargetInfo(center, map), new TargetInfo(center, map));
                effecter.Cleanup();
            }
            if (Props.spawnSound != null)
                Props.spawnSound.PlayOneShot(new TargetInfo(center, map));
        }

        public override void CompTick()
        {
            if (Find.TickManager.TicksGame % 250 == 0)
                PruneDeliveredPawns();
        }

        public override void PostExposeData()
        {
            base.PostExposeData();
            Scribe_Collections.Look(ref deliveredPawns, "deliveredPawns", LookMode.Reference);
            if (Scribe.mode == LoadSaveMode.PostLoadInit)
                PruneDeliveredPawns();
        }

        private void PruneDeliveredPawns()
        {
            if (deliveredPawns == null)
            {
                deliveredPawns = new List<Pawn>();
                return;
            }
            deliveredPawns.RemoveAll(p => p == null || p.Dead || p.Destroyed);
        }

        private static Pawn GenerateUnit(PawnKindDef kind, Pawn caster)
        {
            return PawnGenerator.GeneratePawn(new PawnGenerationRequest(
                kind: kind,
                faction: caster.Faction,
                context: PawnGenerationContext.NonPlayer,
                tile: caster.Map.Tile,
                canGeneratePawnRelations: false,
                colonistRelationChanceFactor: 0f,
                allowFood: false,
                allowAddictions: false,
                forceNoIdeo: true,
                forceNoGear: true,
                fixedBiologicalAge: 0f,
                fixedChronologicalAge: 0f));
        }

        private bool TryPlace(Thing thing, Map map, IntVec3 center)
        {
            if (TryFindSpawnCell(map, center, out IntVec3 cell))
            {
                GenSpawn.Spawn(thing, cell, map, Rot4.Random);
                return true;
            }
            return GenPlace.TryPlaceThing(thing, center, map, ThingPlaceMode.Near);
        }

        private bool TryFindSpawnCell(Map map, IntVec3 center, out IntVec3 cell)
        {
            foreach (IntVec3 c in GenRadial.RadialCellsAround(center, Props.radius, true).InRandomOrder())
            {
                if (!c.InBounds(map) || c.Fogged(map))
                    continue;
                if (!c.Standable(map))
                    continue;
                if (c.GetFirstPawn(map) != null)
                    continue;
                if (c.GetEdifice(map) != null)
                    continue;
                cell = c;
                return true;
            }
            cell = IntVec3.Invalid;
            return false;
        }
    }
}
