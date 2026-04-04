using StardewValley.Menus;

namespace SimpleFishingMod
{
    public sealed class Medium : diffidcultTemplate
    {
        public bool CanBeChonse(BobberBar bar)
        {
            return bar.difficulty > 40 && bar.difficulty <= 80;
        }

        public float StruggleFishSpeed => 0.8f;
        public float StrugglePhaseDurationSeconds => 4f;
        public float PullPhaseDurationSeconds => 4f;
    }
}
