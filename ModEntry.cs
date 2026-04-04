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
        private bool waitingForMiniGame = false; // 是否正在运行自制的钓鱼小游戏

        private Texture2D? waterTexture = null;
        private Texture2D? treasureTexture = null;
        private BobberBar? suspendedBobberBar = null; // 暂存原版的钓鱼小游戏实例
        private bool? pendingBobberBarResult = null; // 自制小游戏结束后要强制设置的 BobberBar 结果（成功/失败），在 UpdateTicked 中检测到 BobberBar 恢复时应用
        private bool resolvingBobberBar = false; //游戏是否正在结算钓鱼结果

        
   

        internal static bool HideVanillaBobberBar => Instance != null && (Instance.waitingForMiniGame || Instance.resolvingBobberBar || Instance.pendingBobberBarResult.HasValue);

        public override void Entry(IModHelper helper)
        {
            Instance = this;
         
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
            var fishTex = CreateFishTexture(bar.whichFish);

            suspendedBobberBar = bar;
            Game1.activeClickableMenu = new FishFightMenu(fishTex, this, treasure);
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

            waitingForMiniGame = false;

            if (suspendedBobberBar != null)
            {
                if (fight.Success)
                    suspendedBobberBar.treasureCaught = fight.TreasureCollected;

                Game1.activeClickableMenu = suspendedBobberBar;
                pendingBobberBarResult = fight.Success;
                suspendedBobberBar = null;
                resolvingBobberBar = true;
                return;
            }

            Game1.exitActiveMenu();
            if (fight.Success)
                Game1.showGlobalMessage("You caught the fish!");
            else
                Game1.showRedMessage("The fish escaped!");
        }

        private Texture2D CreateFishTexture(string fishId)
        {
            StardewValley.Object fishObj = new StardewValley.Object(fishId, 1);
            Rectangle src = Game1.getSourceRectForStandardTileSheet(
                Game1.objectSpriteSheet,
                fishObj.ParentSheetIndex,
                16, 16);

            Texture2D fishTex = new Texture2D(Game1.graphics.GraphicsDevice, 16, 16);
            Color[] data = new Color[16 * 16];
            Game1.objectSpriteSheet.GetData(0, src, data, 0, data.Length);
            fishTex.SetData(data);

            return fishTex;
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
