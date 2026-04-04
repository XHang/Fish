using StardewModdingAPI;
using StardewModdingAPI.Utilities;

namespace SimpleFishingMod
{
    public sealed class ModConfig
    {
        public KeybindList MoveLeft { get; set; } = new(new Keybind(SButton.A), new Keybind(SButton.Left));
        public KeybindList MoveRight { get; set; } = new(new Keybind(SButton.D), new Keybind(SButton.Right));
        public KeybindList MoveUp { get; set; } = new(new Keybind(SButton.W), new Keybind(SButton.Up));
        public KeybindList MoveDown { get; set; } = new(new Keybind(SButton.S), new Keybind(SButton.Down));
    }
}
