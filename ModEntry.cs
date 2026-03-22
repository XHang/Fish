using StardewModdingAPI;
using StardewValley;
using StardewValley.Menus;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;

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
            helper.Events.GameLoop.UpdateTicked += OnUpdateTicked;
        }

        public Texture2D GenerateWaterTexture(int width, int height, float timeOffset = 0f)
        {
            return CreateWaterTexture(width, height, timeOffset);
        }

        public Color[] GenerateWaterColorData(int width, int height, float timeOffset = 0f)
        {
            Color[] colorData = new Color[width * height];

            // 创建精细的动态水纹理，带有多层波浪叠加
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    // 基础蓝色（从上到下的深浅变化）
                    float yGradient = y / (float)height;
                    int baseR = (int)(15 + yGradient * 60);
                    int baseG = (int)(70 + yGradient * 80);
                    int baseB = (int)(140 + yGradient * 100);

                    // 多层细小波浪
                    // 第一层：粗波浪
                    float wave1 = (float)Math.Sin((x * 0.02f + timeOffset * 0.3f)) * 8;
                    
                    // 第二层：中波浪
                    float wave2 = (float)Math.Sin((x * 0.04f + timeOffset * 0.5f) + (y * 0.01f)) * 6;
                    
                    // 第三层：细波浪
                    float wave3 = (float)Math.Sin((x * 0.08f - timeOffset * 0.7f)) * 4;
                    
                    // 第四层：微波纹
                    float wave4 = (float)Math.Sin((x * 0.15f + timeOffset) + (y * 0.02f)) * 3;

                    // 组合正弦噪声（更平滑）
                    float noise = (float)Math.Sin(x * 0.03f + y * 0.02f + timeOffset * 0.2f) * 0.3f +
                                  (float)Math.Cos(x * 0.05f - y * 0.01f - timeOffset * 0.3f) * 0.3f;
                    noise = (noise + 0.6f) * 0.2f; // 归一化

                    // 深度变化：越深越暗
                    float depthFactor = 1f - (y / (float)height) * 0.15f;

                    // 高光闪烁（更微妙）
                    float highlight = (float)Math.Sin(x * 0.06f + y * 0.02f + timeOffset * 1.5f) * 0.15f;

                    // 组合所有效果
                    int r = (int)Math.Max(0, Math.Min(255, baseR * depthFactor + (wave1 + wave2) * 0.3f + highlight * 20));
                    int g = (int)Math.Max(0, Math.Min(255, baseG * depthFactor + (wave2 + wave3) * 0.3f + highlight * 25 + noise * 15));
                    int b = (int)Math.Max(0, Math.Min(255, baseB * depthFactor + (wave3 + wave4) * 0.3f + highlight * 30 + noise * 20));

                    colorData[y * width + x] = new Color((byte)r, (byte)g, (byte)b, (byte)255);
                }
            }

            return colorData;
        }

        private Texture2D CreateWaterTexture(int width, int height, float timeOffset = 0f)
        {
            Texture2D texture = new Texture2D(Game1.graphics.GraphicsDevice, width, height);
            Color[] colorData = GenerateWaterColorData(width, height, timeOffset);
            texture.SetData(colorData);
            return texture;
        }

        private Texture2D CreateFallbackWaterTexture(int width, int height)
        {
            Texture2D texture = new Texture2D(Game1.graphics.GraphicsDevice, width, height);
            Color[] colorData = new Color[width * height];

            // 创建蓝色渐变效果作为备选
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    float gradient = y / (float)height;
                    int r = (int)(30 + gradient * 70);
                    int g = (int)(100 + gradient * 50);
                    int b = (int)(180 + gradient * 40);
                    colorData[y * width + x] = new Color((byte)r, (byte)g, (byte)b, (byte)255);
                }
            }

            texture.SetData(colorData);
            return texture;
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
                // 如果还没创建水纹理，先创建它
                if (waterTexture == null)
                {
                    waterTexture = CreateWaterTexture(300, 300);
                }

                this.Monitor.Log($"BobberBar detected. Using game water texture", LogLevel.Info);

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