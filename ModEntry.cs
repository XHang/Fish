using StardewModdingAPI;
using StardewValley;
using StardewValley.Menus;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.IO;
using System.Reflection;
using HarmonyLib;

namespace SimpleFishingMod
{
    public class ModEntry : Mod
    {
        private static ModEntry? Instance;
        private bool waitingForMiniGame = false;

        private Texture2D? waterTexture = null;
        private BobberBar? suspendedBobberBar = null;
        private bool? pendingBobberBarResult = null;
        private bool resolvingBobberBar = false;

        private string? pendingFishId = null;
        private int pendingFishQuality = 0;
        
        private IModHelper? helper = null;

        internal static bool HideVanillaBobberBar => Instance != null && (Instance.waitingForMiniGame || Instance.resolvingBobberBar || Instance.pendingBobberBarResult.HasValue);

        public override void Entry(IModHelper helper)
        {
            Instance = this;
            this.helper = helper;
            new Harmony(this.ModManifest.UniqueID).PatchAll();
            
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
            
            helper.Events.GameLoop.UpdateTicked += OnUpdateTicked;
        }

        public Texture2D? GetWaterTexture()
        {
            return waterTexture;
        }

        private void EndFishing()
        {
            Game1.player.completelyStopAnimatingOrDoingAction();
            Game1.player.UsingTool = false;
            Game1.player.CanMove = true;
            Game1.player.Halt();
        }

        private void ForceBobberBarResult(BobberBar bar, bool success)
        {
            try
            {
                FieldInfo? distanceField = typeof(BobberBar).GetField("distanceFromCatching", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
                if (distanceField?.FieldType == typeof(float))
                {
                    distanceField.SetValue(bar, success ? 1.05f : -0.05f);
                }

                bar.update(Game1.currentGameTime);
            }
            catch (Exception ex)
            {
                this.Monitor.Log($"Failed to force BobberBar result: {ex.Message}", LogLevel.Warn);
            }
        }

        private void OnUpdateTicked(object? sender, StardewModdingAPI.Events.UpdateTickedEventArgs e)
        {
            if (resolvingBobberBar)
            {
                if (pendingBobberBarResult.HasValue && Game1.activeClickableMenu is BobberBar resolvingBar)
                {
                    ForceBobberBarResult(resolvingBar, pendingBobberBarResult.Value);
                    return;
                }

                resolvingBobberBar = false;
                pendingBobberBarResult = null;
            }

            if (pendingBobberBarResult.HasValue && Game1.activeClickableMenu is BobberBar restoredBar)
            {
                ForceBobberBarResult(restoredBar, pendingBobberBarResult.Value);
                resolvingBobberBar = true;
                return;
            }

            //
            // ① 检测原版钓鱼小游戏 BobberBar
            //
            if (Game1.activeClickableMenu is BobberBar bar && !waitingForMiniGame)
            {
                if (resolvingBobberBar)
                    return;

                if (waterTexture == null)
                {
                    this.Monitor.Log("waterTexture is null!", LogLevel.Error);
                    return;
                }

                this.Monitor.Log($"BobberBar detected. Water: {waterTexture.Width}x{waterTexture.Height}", LogLevel.Info);

                // 读取鱼的信息
                pendingFishId = bar.whichFish;
                pendingFishQuality = bar.fishQuality;

                // 裁剪鱼 sprite
                StardewValley.Object fishObj = new StardewValley.Object(pendingFishId, 1);
                Rectangle src = Game1.getSourceRectForStandardTileSheet(
                    Game1.objectSpriteSheet,
                    fishObj.ParentSheetIndex,
                    16, 16
                );

                Texture2D fishTex = new Texture2D(Game1.graphics.GraphicsDevice, 16, 16);
                Color[] data = new Color[16 * 16];
                Game1.objectSpriteSheet.GetData(0, src, data, 0, data.Length);
                fishTex.SetData(data);

                suspendedBobberBar = bar;

                // 打开你的自定义小游戏，传入 water 背景纹理和 ModEntry 引用
                this.Monitor.Log("Creating FishFightMenu...", LogLevel.Info);
                Game1.activeClickableMenu = new FishFightMenu(fishTex, waterTexture!, this);

                waitingForMiniGame = true;
                return;
            }

            //
            // ② 检测 FishFightMenu 是否结束
            //
            if (waitingForMiniGame && Game1.activeClickableMenu is FishFightMenu fight)
            {
                if (fight.Finished)
                {
                    bool success = fight.Success;

                    waitingForMiniGame = false;

                    if (suspendedBobberBar != null)
                    {
                        Game1.activeClickableMenu = suspendedBobberBar;
                        pendingBobberBarResult = success;
                        suspendedBobberBar = null;
                    }
                    else
                    {
                        Game1.exitActiveMenu();
                        if (success)
                            Game1.showGlobalMessage("You caught the fish!");
                        else
                            Game1.showRedMessage("The fish escaped!");
                    }

                    pendingFishId = null;
                }
            }
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
