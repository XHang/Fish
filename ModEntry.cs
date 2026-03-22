using StardewModdingAPI;
using StardewValley;
using StardewValley.Menus;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace SimpleFishingMod
{
    public class ModEntry : Mod
    {
        private bool waitingForMiniGame = false;

        private string? pendingFishId = null;
        private int pendingFishQuality = 0;

        public override void Entry(IModHelper helper)
        {
            helper.Events.GameLoop.UpdateTicked += OnUpdateTicked;
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

                // 打开你的自定义小游戏
                Game1.activeClickableMenu = new FishFightMenu(fishTex);

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