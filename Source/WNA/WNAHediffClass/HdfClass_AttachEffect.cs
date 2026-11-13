using RimWorld;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using Verse;
using Verse.Sound;
using WNA.WNADefOf;
using WNA.WNAMiscs;
using WNA.WNAModExtension;
using WNA.WNAUtility;

namespace WNA.WNAHediffClass
{
    internal class HdfClass_AttachEffect : HediffWithComps
    {
        public int stacks;
        public int ticksRemaining = -1;
        public List<AttachEffectShieldLayer> layers = new List<AttachEffectShieldLayer>();
        public int damageTicker;

        private Mote bubbleMote;
        private AttachEffectConfig config;

        private static readonly AttachEffectConfig Fallback = new AttachEffectConfig();

        internal AttachEffectConfig Config => config ?? (config = AttachEffectConfig.Get(def) ?? Fallback);

        internal int MaxStacks => Config.EffectiveMaxStacks;

        internal bool IsPermanent => Config.duration < 0;

        public override void PostMake()
        {
            base.PostMake();
            stacks = 1;
            layers.Clear();
            if (Config.HasShield)
                layers.Add(new AttachEffectShieldLayer(Config.shieldStrength));
            ticksRemaining = Config.duration;
            damageTicker = 0;
            SyncSeverity();
        }

        public override void PostAdd(DamageInfo? dinfo)
        {
            base.PostAdd(dinfo);
            SyncSeverity();
        }

        public override bool TryMergeWith(Hediff other)
        {
            if (other == null || other.def != def || other.Part != Part)
                return false;
            AddStack();
            return true;
        }

        internal void AddStack()
        {
            if (stacks < MaxStacks)
            {
                stacks++;
                if (Config.HasShield)
                    layers.Add(new AttachEffectShieldLayer(Config.shieldStrength));
                SyncSeverity();
            }
            if (Config.refreshDurationOnReapply && Config.duration >= 0)
                ticksRemaining = Config.duration;
        }

        private void SyncSeverity()
        {
            if (Severity != stacks)
                Severity = stacks;
        }

        public override bool ShouldRemove => stacks <= 0 || base.ShouldRemove;

        public override void TickInterval(int delta)
        {
            base.TickInterval(delta);
            if (pawn == null || pawn.Dead || pawn.Destroyed)
            {
                CleanupBubble();
                return;
            }
            TickShieldRespawn(delta);
            if (TickDuration(delta))
                return;
            TickDamageOverTime(delta);
            MaintainBubble();
        }

        private void TickShieldRespawn(int delta)
        {
            if (!Config.HasShield)
                return;
            for (int i = 0; i < layers.Count; i++)
            {
                AttachEffectShieldLayer layer = layers[i];
                if (layer.respawnTicks <= 0)
                    continue;
                layer.respawnTicks -= delta;
                if (layer.respawnTicks > 0)
                    continue;
                layer.respawnTicks = 0;
                layer.energy = Config.shieldStrength;
                OnShieldReset();
            }
        }

        private bool TickDuration(int delta)
        {
            if (ticksRemaining < 0)
                return false;
            ticksRemaining -= delta;
            if (ticksRemaining > 0)
                return false;
            RemoveOutermostLayer();
            if (stacks <= 0)
            {
                pawn.health.RemoveHediff(this);
                return true;
            }
            ticksRemaining = Config.duration;
            return false;
        }

        private void RemoveOutermostLayer()
        {
            if (layers.Count > 0)
                layers.RemoveAt(layers.Count - 1);
            stacks = Mathf.Max(0, stacks - 1);
            SyncSeverity();
        }

        internal bool TryGetOutermostIntactLayer(out AttachEffectShieldLayer layer)
        {
            for (int i = layers.Count - 1; i >= 0; i--)
            {
                if (layers[i].energy > 0f)
                {
                    layer = layers[i];
                    return true;
                }
            }
            layer = null;
            return false;
        }

        internal void BreakLayer(AttachEffectShieldLayer layer)
        {
            layer.energy = 0f;
            OnShieldBreak();
            if (Config.breakMode == AttachEffectShieldBreakMode.ConsumeStack)
            {
                layers.Remove(layer);
                stacks = Mathf.Max(0, stacks - 1);
                SyncSeverity();
                if (stacks <= 0)
                    pawn.health.RemoveHediff(this);
                return;
            }
            layer.respawnTicks = Config.respawnDelay;
        }

        // Refilling inside BreakLayer would let the cascade re-absorb with the same layer forever.
        internal void ResolveInstantRespawn()
        {
            if (Config.breakMode != AttachEffectShieldBreakMode.Regenerate)
                return;
            for (int i = 0; i < layers.Count; i++)
            {
                AttachEffectShieldLayer layer = layers[i];
                if (layer.energy > 0f || layer.respawnTicks != 0)
                    continue;
                layer.energy = Config.shieldStrength;
                OnShieldReset();
            }
        }

        internal void NotifyAbsorbed(DamageInfo dinfo)
        {
            if (!pawn.Spawned || pawn.Map == null)
                return;
            SoundDefOf.EnergyShield_AbsorbDamage.PlayOneShot(new TargetInfo(pawn.Position, pawn.Map));
            Vector3 impact = Vector3Utility.HorizontalVectorFromAngle(dinfo.Angle);
            Vector3 loc = pawn.TrueCenter() + impact.RotatedBy(180f) * 0.5f;
            FleckMaker.Static(loc, pawn.Map, FleckDefOf.ExplosionFlash, Mathf.Min(10f, 2f + dinfo.Amount / 10f));
        }

        private void OnShieldBreak()
        {
            if (!pawn.Spawned || pawn.Map == null)
                return;
            EffecterDefOf.Shield_Break.SpawnAttached(pawn, pawn.Map, Config.maxDrawSize);
            FleckMaker.Static(pawn.TrueCenter(), pawn.Map, FleckDefOf.ExplosionFlash, 12f);
            CleanupBubble();
        }

        private void OnShieldReset()
        {
            if (!pawn.Spawned || pawn.Map == null)
                return;
            SoundDefOf.EnergyShield_Reset.PlayOneShot(new TargetInfo(pawn.Position, pawn.Map));
            FleckMaker.ThrowLightningGlow(pawn.TrueCenter(), pawn.Map, 3f);
        }

        private void TickDamageOverTime(int delta)
        {
            if (!Config.HasDamageOverTime || !pawn.Spawned || pawn.Map == null)
                return;
            damageTicker += delta;
            if (damageTicker < Config.damageDelay)
                return;
            damageTicker = 0;
            float amount = Config.damageAmount * (Config.damageScaleWithStacks ? stacks : 1);
            if (amount > 0f)
                ApplyPeriodicDamage(amount);
            else if (amount < 0f)
                ApplyPeriodicHeal(-amount);
        }

        private void ApplyPeriodicDamage(float amount)
        {
            BodyPartRecord part = pawn.health.hediffSet.GetNotMissingParts().RandomElementWithFallback();
            if (part == null)
                return;
            DamageDef damageDef = Config.damageDef ?? DamageDefOf.Blunt;
            try
            {
                AttachEffectUtility.applyingSelfDoT = true;
                pawn.TakeDamage(new DamageInfo(damageDef, amount, 0f, -1f, pawn, part));
            }
            finally
            {
                AttachEffectUtility.applyingSelfDoT = false;
            }
        }

        private void ApplyPeriodicHeal(float amount)
        {
            List<BodyPartRecord> injured = new List<BodyPartRecord>(pawn.health.hediffSet.GetInjuredParts());
            if (injured.Count == 0)
                return;
            BodyPartRecord part = injured[Rand.Range(0, injured.Count)];
            List<Hediff> snapshot = new List<Hediff>(pawn.health.hediffSet.hediffs);
            float remaining = amount;
            for (int i = 0; i < snapshot.Count && remaining > 0f; i++)
            {
                if (!(snapshot[i] is Hediff_Injury injury) || injury.Part != part)
                    continue;
                float heal = Mathf.Min(remaining, injury.Severity);
                injury.Severity -= heal;
                remaining -= heal;
            }
        }

        private void MaintainBubble()
        {
            if (!Config.HasShield || !pawn.Spawned || pawn.Map == null || !TryGetOutermostIntactLayer(out AttachEffectShieldLayer layer))
            {
                CleanupBubble();
                return;
            }
            if (bubbleMote == null || bubbleMote.Destroyed)
            {
                ThingDef moteDef = Config.shieldMoteDef ?? WNAMainDefOf.WNA_Mote_AttachEffectShield;
                bubbleMote = MoteMaker.MakeAttachedOverlay(pawn, moteDef, Vector3.zero);
                if (bubbleMote != null)
                    bubbleMote.instanceColor = Config.shieldColor;
            }
            if (bubbleMote == null)
                return;
            bubbleMote.Maintain();
            bubbleMote.Scale = Mathf.Lerp(Config.minDrawSize, Config.maxDrawSize, Mathf.Clamp01(layer.energy / Config.shieldStrength));
        }

        private void CleanupBubble()
        {
            if (bubbleMote != null && !bubbleMote.Destroyed)
                bubbleMote.Destroy();
            bubbleMote = null;
        }

        public override void PostRemoved()
        {
            base.PostRemoved();
            CleanupBubble();
        }

        public override void Notify_PawnDied(DamageInfo? dinfo, Hediff culprit = null)
        {
            base.Notify_PawnDied(dinfo, culprit);
            CleanupBubble();
        }

        public override string LabelInBrackets
        {
            get
            {
                StringBuilder sb = new StringBuilder();
                string baseLabel = base.LabelInBrackets;
                if (!baseLabel.NullOrEmpty())
                    sb.Append(baseLabel);
                if (Config.cumulative || stacks > 1)
                {
                    if (sb.Length > 0)
                        sb.Append(", ");
                    sb.Append("x").Append(stacks);
                }
                if (ticksRemaining >= 0)
                {
                    if (sb.Length > 0)
                        sb.Append(", ");
                    sb.Append(ticksRemaining.ToStringTicksToPeriod(allowSeconds: true, shortForm: true));
                }
                return sb.ToString();
            }
        }

        public override string TipStringExtra
        {
            get
            {
                StringBuilder sb = new StringBuilder();
                string baseTip = base.TipStringExtra;
                if (!baseTip.NullOrEmpty())
                    sb.AppendLine(baseTip);
                string cap = MaxStacks == int.MaxValue ? "WNA_AE_Unlimited".Translate().Resolve() : MaxStacks.ToString();
                sb.AppendLine("WNA_AE_Stacks".Translate(stacks, cap));
                if (IsPermanent)
                    sb.AppendLine("WNA_AE_Permanent".Translate());
                else
                    sb.AppendLine("WNA_AE_Remaining".Translate(ticksRemaining.ToStringTicksToPeriod()));
                if (Config.HasShield)
                {
                    if (TryGetOutermostIntactLayer(out AttachEffectShieldLayer layer))
                        sb.AppendLine("WNA_AE_Shield".Translate(layer.energy.ToString("F0"), Config.shieldStrength.ToString("F0")));
                    else if (layers.Count > 0 && layers[layers.Count - 1].respawnTicks > 0)
                        sb.AppendLine("WNA_AE_ShieldRespawning".Translate(layers[layers.Count - 1].respawnTicks.ToStringTicksToPeriod()));
                    else
                        sb.AppendLine("WNA_AE_ShieldBroken".Translate());
                }
                if (Config.HasDamageOverTime)
                {
                    float amount = Config.damageAmount * (Config.damageScaleWithStacks ? stacks : 1);
                    string key = amount > 0f ? "WNA_AE_DoT" : "WNA_AE_DoTHeal";
                    sb.AppendLine(key.Translate(Mathf.Abs(amount).ToString("F1"), Config.damageDelay.ToStringTicksToPeriod()));
                }
                if (Config.disableAttack)
                    sb.AppendLine("WNA_AE_DisableAttack".Translate());
                if (Config.disableAbility)
                    sb.AppendLine("WNA_AE_DisableAbility".Translate());
                return sb.ToString();
            }
        }

        public override IEnumerable<Gizmo> GetGizmos()
        {
            foreach (Gizmo gizmo in base.GetGizmos())
                yield return gizmo;
            if (!Config.HasShield || pawn == null)
                yield break;
            if ((pawn.Faction == Faction.OfPlayer || pawn.RaceProps.IsMechanoid)
                && Find.Selector.SingleSelectedThing == pawn)
            {
                yield return new Gizmo_AttachEffectShield { hediff = this };
            }
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref stacks, "stacks", 1);
            Scribe_Values.Look(ref ticksRemaining, "ticksRemaining", -1);
            Scribe_Values.Look(ref damageTicker, "damageTicker", 0);
            Scribe_Collections.Look(ref layers, "layers", LookMode.Deep);
            if (Scribe.mode != LoadSaveMode.PostLoadInit)
                return;
            if (layers == null)
                layers = new List<AttachEffectShieldLayer>();
            stacks = Mathf.Clamp(stacks, 0, MaxStacks);
            if (!Config.HasShield)
                layers.Clear();
            else
            {
                while (layers.Count > stacks)
                    layers.RemoveAt(layers.Count - 1);
                while (layers.Count < stacks)
                    layers.Add(new AttachEffectShieldLayer(Config.shieldStrength));
            }
            SyncSeverity();
        }
    }
}