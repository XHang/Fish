using StardewValley.Menus;

namespace SimpleFishingMod
{
    public sealed class Hard : diffidcultTemplate
    {
        public bool CanBeChonse(BobberBar bar)
        {
            return bar.difficulty > 80;
        }

        public float StruggleFishSpeed => 1.0f;
        public float StrugglePhaseDurationSeconds => 3f;
        public float PullPhaseDurationSeconds => 3f;
    }
}
