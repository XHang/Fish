using StardewModdingAPI;
using StardewValley;
using StardewValley.Menus;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.IO;

namespace SimpleFishingMod
{
    public class ModEntry : Mod
    {
        private bool waitingForMiniGame = false;

        private Texture2D? waterTexture = null;

        private string? pendingFishId = null;
        private int pendingFishQuality = 0;
        
        private IModHelper? helper = null;

        public override void Entry(IModHelper helper)
        {
            this.helper = helper;
            
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

        private void OnUpdateTicked(object? sender, StardewModdingAPI.Events.UpdateTickedEventArgs e)
        {
            //
            // ① 检测原版钓鱼小游戏 BobberBar
            //
            if (Game1.activeClickableMenu is BobberBar bar && !waitingForMiniGame)
            {
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

                // 关闭原版小游戏
                Game1.exitActiveMenu();

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

                    Game1.exitActiveMenu();
                    waitingForMiniGame = false;

                 
                  

                    if (success)
                    {
                        EndFishing();
                        Game1.showGlobalMessage("You caught the fish!");
                      

                        if (pendingFishId != null)
                        {
                            Game1.player.addItemToInventory(
                                new StardewValley.Object(pendingFishId, 1, quality: pendingFishQuality)
                            );
                        }
                    }
                    else
                    {
                        EndFishing();
                        Game1.showRedMessage("The fish escaped!");
                        
                    }

                    pendingFishId = null;
                }
            }
        }
    }
}