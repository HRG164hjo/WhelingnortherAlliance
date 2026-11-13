using RimWorld;
using UnityEngine;
using Verse;
using WNA.WNAMiscs;

namespace WNA.WNAModExtension
{
    internal class AttachEffectConfig : DefModExtension
    {
        public bool cumulative = false;
        public int maxStacks = 1;
        public int duration = -1;
        public bool refreshDurationOnReapply = true;

        public float shieldStrength = 0f;
        public AttachEffectShieldAbsorbMode shieldAbsorbMode = AttachEffectShieldAbsorbMode.ByDamage;
        public float shieldHitCost = 1f;
        public bool absorbOverDamage = true;
        public AttachEffectShieldBreakMode breakMode = AttachEffectShieldBreakMode.Regenerate;
        public int respawnDelay = 0;
        public bool absorbMelee = true;
        public ThingDef shieldMoteDef = null;
        public Color shieldColor = Color.white;
        public float minDrawSize = 1.4f;
        public float maxDrawSize = 1.6f;

        public bool disableAttack = false;
        public bool disableAbility = false;

        public float damageAmount = 0f;
        public int damageDelay = 60;
        public DamageDef damageDef = null;
        public bool damageScaleWithStacks = false;

        internal int EffectiveMaxStacks => cumulative ? (maxStacks < 0 ? int.MaxValue : Mathf.Max(1, maxStacks)) : 1;

        internal bool HasShield => shieldStrength > 0f;

        internal bool HasDamageOverTime => damageAmount != 0f;

        internal static AttachEffectConfig Get(Def def)
        {
            return def?.GetModExtension<AttachEffectConfig>();
        }
    }
}