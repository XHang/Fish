using StardewModdingAPI;
using StardewValley;
using StardewValley.Menus;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI.Utilities;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using SimpleFishingMod.Integrations;

namespace SimpleFishingMod
{
    public class ModEntry : Mod
    {
        private sealed class LegacyModConfig
        {
            public List<SButton>? MoveLeft { get; set; }
            public List<SButton>? MoveRight { get; set; }
            public List<SButton>? MoveUp { get; set; }
            public List<SButton>? MoveDown { get; set; }
        }

        private static ModEntry? Instance;
        private bool waitingForMiniGame = false; // 是否正在运行自制的钓鱼小游戏

        private Texture2D? waterTexture = null;
        private Texture2D? treasureTexture = null;

        private bool? pendingBobberBarResult = null; // 自制小游戏结束后要强制设置的 BobberBar 结果（成功/失败），在 UpdateTicked 中检测到 BobberBar 恢复时应用
        private bool resolvingBobberBar = false; //游戏是否正在结算钓鱼结果

        internal ModConfig Config { get; private set; } = new();

        internal static bool HideVanillaBobberBar => Instance != null && (Instance.waitingForMiniGame || Instance.resolvingBobberBar || Instance.pendingBobberBarResult.HasValue);

        public override void Entry(IModHelper helper)
        {
            Instance = this;
            Config = LoadConfig(helper);
            NormalizeConfig();

            new Harmony(this.ModManifest.UniqueID).PatchAll();

            DifficultyTemplateRegistry.Register(_ => new Easy());
            DifficultyTemplateRegistry.Register(_ => new Medium());
            DifficultyTemplateRegistry.Register(_ => new Hard());

            helper.Events.GameLoop.GameLaunched += OnGameLaunched;

            // 直接加载 water.png
            try
            {
                waterTexture = helper.ModContent.Load<Texture2D>("assets/water.png");
                this.Monitor.Log($"✓ Loaded water.png: {waterTexture.Width}x{waterTexture.Height}", LogLevel.Info);
            }
            catch (Exception ex)
            {
                this.Monitor.Log($"Error loading water.png: {ex.Message}", LogLevel.Error);
            }

            try
            {
                treasureTexture = helper.ModContent.Load<Texture2D>("assets/treasure.png");
                this.Monitor.Log($"✓ Loaded treasure.png: {treasureTexture.Width}x{treasureTexture.Height}", LogLevel.Info);
            }
            catch (Exception ex)
            {
                this.Monitor.Log($"Error loading treasure.png: {ex.Message}", LogLevel.Error);
            }

            helper.Events.GameLoop.UpdateTicked += OnUpdateTicked;
        }

        private ModConfig LoadConfig(IModHelper helper)
        {
            try
            {
                return helper.ReadConfig<ModConfig>();
            }
            catch (Exception ex)
            {
                Monitor.Log($"Config format fallback: {ex.Message}", LogLevel.Warn);

                LegacyModConfig? legacy = helper.Data.ReadJsonFile<LegacyModConfig>("config.json");
                if (legacy == null)
                    return new ModConfig();

                return new ModConfig
                {
                    MoveLeft = ToKeybindList(legacy.MoveLeft, SButton.A, SButton.Left),
                    MoveRight = ToKeybindList(legacy.MoveRight, SButton.D, SButton.Right),
                    MoveUp = ToKeybindList(legacy.MoveUp, SButton.W, SButton.Up),
                    MoveDown = ToKeybindList(legacy.MoveDown, SButton.S, SButton.Down)
                };
            }
        }

        private static KeybindList ToKeybindList(List<SButton>? buttons, params SButton[] fallback)
        {
            IEnumerable<SButton> source = buttons is { Count: > 0 } ? buttons : fallback;
            return new KeybindList(source.Select(p => new Keybind(p)).ToArray());
        }

        private void OnGameLaunched(object? sender, StardewModdingAPI.Events.GameLaunchedEventArgs e)
        {
            RegisterGenericModConfigMenu();
        }

        internal bool IsBindingDown(KeybindList keybinds)
        {
            return keybinds.IsDown();
        }

        public Texture2D? GetWaterTexture()
        {
            return waterTexture;
        }

        public Texture2D? GetTreasureTexture()
        {
            return treasureTexture;
        }

        // 强制设置  结果
        private void ApplyFishingResult(BobberBar bar, bool success)
        {
            bar.distanceFromCatching = success ? 1.05f : -0.05f;
            bar.update(Game1.currentGameTime);
        }

        private void OnUpdateTicked(object? sender, StardewModdingAPI.Events.UpdateTickedEventArgs e)
        {
            if (TryResolveForcedBobberBar())
                return;

            if (TryStartCustomMiniGame())
                return;

            TryFinishCustomMiniGame();
        }

        private bool TryResolveForcedBobberBar()
        {
            if (!resolvingBobberBar)
                return false;

            if (pendingBobberBarResult.HasValue && Game1.activeClickableMenu is BobberBar resolvingBar)
            {
                ApplyFishingResult(resolvingBar, pendingBobberBarResult.Value);
                return true;
            }

            resolvingBobberBar = false;
            pendingBobberBarResult = null;
            return false;
        }

        private bool TryStartCustomMiniGame()
        {
            if (waitingForMiniGame)
                return false;

            if (Game1.activeClickableMenu is not BobberBar bar)
                return false;

            if (waterTexture == null)
            {
                Monitor.Log("waterTexture is null!", LogLevel.Error);
                return false;
            }

            var treasure = bar.treasure;

            Game1.activeClickableMenu = new FishFightMenu(bar, this, treasure);
            waitingForMiniGame = true;

            return true;
        }

        private void TryFinishCustomMiniGame()
        {
            if (!waitingForMiniGame)
                return;

            if (Game1.activeClickableMenu is not FishFightMenu fight)
                return;

            if (!fight.Finished)
                return;

            FishFightMenu m = (FishFightMenu)Game1.activeClickableMenu;

            if (m.bar != null)
            {
                if (fight.Success)
                    m.bar.treasureCaught = fight.TreasureCollected;
                waitingForMiniGame = false;
                Game1.activeClickableMenu = m.bar;
                pendingBobberBarResult = fight.Success;
                resolvingBobberBar = true;
                return;
            }

            Game1.exitActiveMenu();
            if (fight.Success)
                Game1.showGlobalMessage("You caught the fish!");
            else
                Game1.showRedMessage("The fish escaped!");
        }

        private void RegisterGenericModConfigMenu()
        {
            IGenericModConfigMenuApi? configMenu = this.Helper.ModRegistry.GetApi<IGenericModConfigMenuApi>("spacechase0.GenericModConfigMenu");
            if (configMenu is null)
                return;

            configMenu.Register(
                mod: this.ModManifest,
                reset: () =>
                {
                    this.Config = new ModConfig();
                    NormalizeConfig();
                },
                save: () =>
                {
                    NormalizeConfig();
                    this.Helper.WriteConfig(this.Config);
                }
            );

            configMenu.AddKeybindList(
                mod: this.ModManifest,
                getValue: () => Config.MoveLeft,
                setValue: value => Config.MoveLeft = value,
                name: () => "Move Left",
                tooltip: () => "Press keys/buttons directly to set left movement."
            );

            configMenu.AddKeybindList(
                mod: this.ModManifest,
                getValue: () => Config.MoveRight,
                setValue: value => Config.MoveRight = value,
                name: () => "Move Right",
                tooltip: () => "Press keys/buttons directly to set right movement."
            );

            configMenu.AddKeybindList(
                mod: this.ModManifest,
                getValue: () => Config.MoveUp,
                setValue: value => Config.MoveUp = value,
                name: () => "Move Up",
                tooltip: () => "Press keys/buttons directly to set up movement."
            );

            configMenu.AddKeybindList(
                mod: this.ModManifest,
                getValue: () => Config.MoveDown,
                setValue: value => Config.MoveDown = value,
                name: () => "Move Down",
                tooltip: () => "Press keys/buttons directly to set down movement."
            );
        }

        private void NormalizeConfig()
        {
            Config.MoveLeft ??= new KeybindList(new Keybind(SButton.A), new Keybind(SButton.Left));
            Config.MoveRight ??= new KeybindList(new Keybind(SButton.D), new Keybind(SButton.Right));
            Config.MoveUp ??= new KeybindList(new Keybind(SButton.W), new Keybind(SButton.Up));
            Config.MoveDown ??= new KeybindList(new Keybind(SButton.S), new Keybind(SButton.Down));
        }
    }

    [HarmonyPatch(typeof(BobberBar), nameof(BobberBar.draw))]
    internal static class BobberBarDrawPatch
    {
        public static bool Prefix()
        {
            return !ModEntry.HideVanillaBobberBar;
        }
    }
}
