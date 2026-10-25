using RimWorld;
using Verse;

namespace WNA.WNAModExtension
{
    internal class MindControlConfig : DefModExtension
    {
        public bool permanent = false;
        public int maxTargets = -1;
        public bool releaseOnControllerDeath = true;
        public bool releaseOnControllerFactionChange = true;
        public bool releaseWhenOutOfRange = false;
        public float range = -1f;
        public int goodwillChange = -100;
        public HistoryEventDef goodwillReason = null;
        public bool brainBurn = true;
        public FloatRange brainBurnHpFactor = new FloatRange(0.6f, 0.9f);
        public bool sendMessage = true;

        internal static readonly MindControlConfig Default = new MindControlConfig();

        internal static MindControlConfig Get(Def def)
        {
            return def?.GetModExtension<MindControlConfig>();
        }
    }
}
