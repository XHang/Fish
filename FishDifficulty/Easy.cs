using StardewValley.Menus;

namespace SimpleFishingMod
{
    public sealed class Easy : diffidcultTemplate
    {
        public bool CanBeChonse(BobberBar bar)
        {
            return bar.difficulty <= 40;
        }

        public float StruggleFishSpeed => 0.6f;
        public float StrugglePhaseDurationSeconds => 5f;
        public float PullPhaseDurationSeconds => 5f;
    }
}
