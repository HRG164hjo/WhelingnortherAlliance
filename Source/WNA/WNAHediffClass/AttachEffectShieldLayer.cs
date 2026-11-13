using Verse;

namespace WNA.WNAHediffClass
{
    internal class AttachEffectShieldLayer : IExposable
    {
        public float energy;
        public int respawnTicks;

        public AttachEffectShieldLayer()
        {
        }

        public AttachEffectShieldLayer(float energy)
        {
            this.energy = energy;
            respawnTicks = 0;
        }

        public void ExposeData()
        {
            Scribe_Values.Look(ref energy, "energy", 0f);
            Scribe_Values.Look(ref respawnTicks, "respawnTicks", 0);
        }
    }
}