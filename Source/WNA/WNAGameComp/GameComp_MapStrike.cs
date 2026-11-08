using RimWorld;
using RimWorld.Planet;
using System.Collections.Generic;
using UnityEngine;
using Verse;
using WNA.WNADefOf;
using WNA.WNAUtility;
using WNA.WNAUtility.Renderer;

namespace WNA.WNAGameComp
{
    public class GameComp_MapStrike : GameComponent
    {
        private const int VisualDurationTicks = 180;
        private const int AfterglowDelayTicks = 45;
        private const int WarheadTimeoutTicks = 120;
        private const int FleckIntervalTicks = 4;
        private const float StrikeRadius = 79.8f;
        private static readonly Color DustColor = new Color(0.55f, 0.48f, 0.4f, 0.55f);

        private List<PendingStrike> pendingStrikes = new List<PendingStrike>();
        public GameComp_MapStrike(Game game)
        {
        }
        public static void ScheduleStrike(Map map, WorldObject source)
        {
            if (map == null || Current.Game == null)
                return;
            GameComp_MapStrike component = Current.Game.GetComponent<GameComp_MapStrike>();
            if (component == null)
            {
                Log.Error("[WNA] GameComponent_WNAMapStrike is missing; removing the struck map immediately.");
                WNASettlementDefenseUtility.RemoveStruckMap(map);
                return;
            }
            int now = Find.TickManager.TicksGame;
            PendingStrike strike = new PendingStrike
            {
                map = map,
                phase = 0,
                deadlineTick = now + WarheadTimeoutTicks
            };
            CameraJumper.TryJump(map.Center, map);
            strike.warhead = SpawnWarhead(map);
            component.pendingStrikes.Add(strike);
            WNASettlementDefenseUtility.NotifyStrikeIncoming(map, source);
        }
        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Collections.Look(ref pendingStrikes, "wnaPendingMapStrikes", LookMode.Deep);
            if (Scribe.mode == LoadSaveMode.PostLoadInit && pendingStrikes == null)
                pendingStrikes = new List<PendingStrike>();
        }
        public override void GameComponentTick()
        {
            if (pendingStrikes == null || pendingStrikes.Count == 0)
                return;
            int now = Find.TickManager.TicksGame;
            for (int i = pendingStrikes.Count - 1; i >= 0; i--)
            {
                PendingStrike strike = pendingStrikes[i];
                if (strike == null || strike.map == null || !Find.Maps.Contains(strike.map))
                {
                    pendingStrikes.RemoveAt(i);
                    continue;
                }
                if (strike.phase == 0)
                {
                    // Poll every tick: the warhead destroys itself on impact and that is the trigger.
                    if (strike.warhead == null || strike.warhead.Destroyed || !strike.warhead.Spawned || now >= strike.deadlineTick)
                        DoStrikeImpact(strike, now);
                    continue;
                }
                TickStrikeFlecks(strike, now);
                if (strike.flashFadeTick > 0 && now >= strike.flashFadeTick)
                {
                    strike.flashFadeTick = 0;
                    ScreenFader.StartFade(new Color(1f, 1f, 1f, 0f), 1.25f);
                }
                if (now < strike.nextTick)
                    continue;
                if (strike.phase == 1)
                {
                    strike.phase = 2;
                    strike.nextTick = strike.impactTick + VisualDurationTicks;
                    strike.afterglow = SpawnSkyThing(strike.map, WNAMainDefOf.WNA_NukeAfterglow, 20, 55, 105);
                }
                else
                {
                    Map map = strike.map;
                    pendingStrikes.RemoveAt(i);
                    QueueMapRemoval(map);
                }
            }
        }
        private static Thing SpawnWarhead(Map map)
        {
            if (WNAMainDefOf.WNA_NukePayload == null)
                return null;
            Thing warhead = ThingMaker.MakeThing(WNAMainDefOf.WNA_NukePayload);
            if (warhead == null)
                return null;
            GenSpawn.Spawn(warhead, map.Center, map, WipeMode.Vanish);
            return warhead;
        }
        private static Thing SpawnSkyThing(Map map, ThingDef def, int fadeIn, int hold, int fadeOut)
        {
            if (def == null)
                return null;
            Thing thing = ThingMaker.MakeThing(def);
            if (thing == null)
                return null;
            GenSpawn.Spawn(thing, map.Center, map, WipeMode.Vanish);
            thing.TryGetComp<CompAffectsSky>()?.StartFadeInHoldFadeOut(fadeIn, hold, fadeOut);
            return thing;
        }
        private static void DoStrikeImpact(PendingStrike strike, int now)
        {
            Map map = strike.map;
            Vector3 center = map.Center.ToVector3Shifted();
            strike.phase = 1;
            strike.impactTick = now;
            strike.nextTick = now + AfterglowDelayTicks;
            strike.nextFleckTick = now;
            strike.warhead = null;
            ScreenFader.StartFade(Color.white, 0.06f);
            strike.flashFadeTick = now + 14;
            NukeDrawLayer.Spawn(map, center, VisualDurationTicks);
            strike.flash = SpawnSkyThing(map, WNAMainDefOf.WNA_StrikeFlash, 2, 40, 45);
            DoStrikeExplosion(map, strike.flash);
        }
        private static void TickStrikeFlecks(PendingStrike strike, int now)
        {
            if (now < strike.nextFleckTick || now > strike.impactTick + VisualDurationTicks)
                return;
            strike.nextFleckTick = now + FleckIntervalTicks;
            Map map = strike.map;
            float t = Mathf.Clamp01((now - strike.impactTick) / (float)VisualDurationTicks);
            float radius = Mathf.Lerp(12f, StrikeRadius, 1f - (1f - t) * (1f - t));
            Vector3 center = map.Center.ToVector3Shifted();
            for (int i = 0; i < 2; i++)
            {
                float angle = Rand.Range(0f, Mathf.PI * 2f);
                Vector3 pos = center + new Vector3(Mathf.Cos(angle) * radius, Rand.Range(0.4f, 2.6f), Mathf.Sin(angle) * radius);
                if (!pos.ToIntVec3().InBounds(map))
                    continue;
                FleckMaker.ThrowSmoke(pos, map, Rand.Range(2.4f, 4.2f) * (1f - t * 0.5f));
                FleckMaker.ThrowDustPuffThick(pos + Vector3.up, map, Rand.Range(2f, 3.4f), DustColor);
            }
        }
        private static void DoStrikeExplosion(Map map, Thing flash)
        {
            List<Thing> ignoredThings = null;
            List<Thing> corpseThings = map.listerThings.ThingsInGroup(ThingRequestGroup.Corpse);
            for (int i = 0; i < corpseThings.Count; i++)
            {
                if (corpseThings[i] is Corpse corpse && corpse.InnerPawn != null && corpse.InnerPawn.Faction == Faction.OfPlayer)
                {
                    ignoredThings = ignoredThings ?? new List<Thing>();
                    ignoredThings.Add(corpse);
                }
            }
            if (flash != null && !flash.Destroyed && flash.Spawned)
            {
                ignoredThings = ignoredThings ?? new List<Thing>();
                ignoredThings.Add(flash);
            }
            SoundDef blastSound = DefDatabase<SoundDef>.GetNamedSilentFail("Explosion_GiantBomb");
            GenExplosion.DoExplosion(
                center: map.Center,
                map: map,
                radius: StrikeRadius,
                damType: WNAMainDefOf.WNA_ChimingBlaze,
                instigator: null,
                damAmount: 9999999,
                armorPenetration: 9999999f,
                explosionSound: blastSound,
                damageFalloff: false,
                ignoredThings: ignoredThings,
                doVisualEffects: true,
                doSoundEffects: blastSound != null,
                screenShakeFactor: 1.5f,
                postExplosionSpawnThingDef: WNAMainDefOf.WNA_Ether_Aeroblaze,
                postExplosionSpawnChance: 0.007f);
        }
        private static void QueueMapRemoval(Map map)
        {
            LongEventHandler.QueueLongEvent(delegate
            {
                WNASettlementDefenseUtility.RemoveStruckMap(map);
            }, "WNA_AutoDefense_MapRemoving", false, null);
        }
        public class PendingStrike : IExposable
        {
            public Map map;
            public int phase;
            public int nextTick;
            public int impactTick;
            public int deadlineTick;
            public int nextFleckTick;
            public int flashFadeTick;
            public Thing warhead;
            public Thing flash;
            public Thing afterglow;
            public void ExposeData()
            {
                Scribe_References.Look(ref map, "map");
                Scribe_Values.Look(ref phase, "phase", 0);
                Scribe_Values.Look(ref nextTick, "nextTick", 0);
                Scribe_Values.Look(ref impactTick, "impactTick", 0);
                Scribe_Values.Look(ref deadlineTick, "deadlineTick", 0);
                Scribe_Values.Look(ref nextFleckTick, "nextFleckTick", 0);
                Scribe_Values.Look(ref flashFadeTick, "flashFadeTick", 0);
                Scribe_References.Look(ref warhead, "warhead");
                Scribe_References.Look(ref flash, "flash");
                Scribe_References.Look(ref afterglow, "afterglow");
            }
        }
    }
}
