using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewValley;
using StardewValley.Menus;
using Microsoft.Xna.Framework.Input;
using System;
using System.Collections.Generic;

namespace SimpleFishingMod
{
    public class FishFightMenu : IClickableMenu
    {
        private Texture2D fishTexture;
        private Texture2D? waterTileTexture;  // 从 beach tileset 提取的水面贴图
        private Texture2D bobberTexture;
        private Texture2D bubbleTexture;
        private Rectangle box;
        private Vector2 fishPos;

        private enum Phase { Struggle, Pull }
        private Phase currentPhase = Phase.Struggle;

        private Vector2 fishVelocity;
        private double phaseStartTime;

        private Random rand = new Random();
        
        private ModEntry? modEntry;
        private float wobbleTime;
        private List<BubbleParticle> bubbles = new();

        public bool Finished = false;
        public bool Success = false;

        private class BubbleParticle
        {
            public Vector2 Position;
            public Vector2 Velocity;
            public float Scale;
            public float Life;
            public float MaxLife;
        }

        public FishFightMenu(Texture2D fishTex, Texture2D backgroundTex, ModEntry? modEntry = null)
        {
            fishTexture = fishTex;
            this.modEntry = modEntry;
            bobberTexture = CreateBobberTexture();
            bubbleTexture = CreateBubbleTexture();

            box = new Rectangle(400, 200, 300, 300);
            fishPos = new Vector2(box.Center.X, box.Center.Y);

            // 加载已截取的水面图片
            if (modEntry != null)
            {
                try
                {
                    waterTileTexture = modEntry.GetWaterTexture();
                    System.Console.WriteLine($"Loaded water texture: {waterTileTexture?.Width}x{waterTileTexture?.Height}");
                }
                catch (Exception ex)
                {
                    System.Console.WriteLine($"Failed to load water texture: {ex.Message}");
                }
            }

            StartStrugglePhase();
        }

        private Texture2D CreateBobberTexture()
        {
            const int size = 20;
            Texture2D texture = new Texture2D(Game1.graphics.GraphicsDevice, size, size);
            Color[] data = new Color[size * size];
            Vector2 center = new Vector2((size - 1) / 2f, (size - 1) / 2f);
            float radius = 8.2f;

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float distance = Vector2.Distance(new Vector2(x, y), center);
                    Color color = Color.Transparent;

                    if (distance <= radius)
                    {
                        color = y < center.Y
                            ? new Color(245, 245, 245)
                            : new Color(215, 40, 40);

                        if (distance >= radius - 1f)
                            color = new Color(25, 25, 25);

                        if (Math.Abs(y - center.Y) <= 1f && distance <= radius - 1f)
                            color = new Color(25, 25, 25);

                        float buttonDistance = Vector2.Distance(new Vector2(x, y), center);
                        if (buttonDistance <= 3.2f)
                            color = new Color(35, 35, 35);
                        if (buttonDistance <= 2.2f)
                            color = new Color(235, 235, 235);
                        if (buttonDistance <= 1.1f)
                            color = Color.White;
                    }

                    data[y * size + x] = color;
                }
            }

            texture.SetData(data);
            return texture;
        }

        private Texture2D CreateBubbleTexture()
        {
            Texture2D texture = new Texture2D(Game1.graphics.GraphicsDevice, 8, 8);
            Color[] data = new Color[8 * 8];
            Vector2 center = new Vector2(3.5f, 3.5f);

            for (int y = 0; y < 8; y++)
            {
                for (int x = 0; x < 8; x++)
                {
                    float distance = Vector2.Distance(new Vector2(x, y), center);
                    data[y * 8 + x] = distance <= 3.2f
                        ? new Color(220, 245, 255)
                        : Color.Transparent;
                }
            }

            texture.SetData(data);
            return texture;
        }

        private void StartStrugglePhase()
        {
            currentPhase = Phase.Struggle;
            phaseStartTime = Game1.currentGameTime.TotalGameTime.TotalSeconds;

            // ⭐ 鱼速度调慢
            int dir = rand.Next(3);
            if (dir == 0) fishVelocity = new Vector2(-0.6f, 0);
            if (dir == 1) fishVelocity = new Vector2(0.6f, 0);
            if (dir == 2) fishVelocity = new Vector2(0, -0.6f);

            // ⭐ 不再重置鱼位置（保持上一轮的位置）
            // fishPos = new Vector2(box.Center.X, box.Center.Y);  <-- 删除
        }

        private void StartPullPhase()
        {
            currentPhase = Phase.Pull;
            phaseStartTime = Game1.currentGameTime.TotalGameTime.TotalSeconds;
        }

        public override void update(GameTime time)
        {
            base.update(time);

            if (Finished) return;

            wobbleTime += (float)time.ElapsedGameTime.TotalSeconds;

            if (rand.NextDouble() < 0.08)
            {
                bubbles.Add(new BubbleParticle
                {
                    Position = fishPos + new Vector2(rand.Next(-10, 11), rand.Next(-6, 7)),
                    Velocity = new Vector2((float)(rand.NextDouble() - 0.5) * 0.3f, -(0.6f + (float)rand.NextDouble() * 0.7f)),
                    Scale = 0.5f + (float)rand.NextDouble() * 0.6f,
                    Life = 0.8f + (float)rand.NextDouble() * 0.6f,
                    MaxLife = 0.8f + (float)rand.NextDouble() * 0.6f,
                });
            }

            for (int i = bubbles.Count - 1; i >= 0; i--)
            {
                BubbleParticle bubble = bubbles[i];
                bubble.Position += bubble.Velocity;
                bubble.Life -= (float)time.ElapsedGameTime.TotalSeconds;

                if (bubble.Life <= 0)
                    bubbles.RemoveAt(i);
                else
                    bubbles[i] = bubble;
            }

            double elapsed = time.TotalGameTime.TotalSeconds - phaseStartTime;

            if (currentPhase == Phase.Struggle)
            {
                fishPos += fishVelocity;

                // 鱼逃出框 → 失败
                if (!box.Contains(fishPos))
                {
                    Finished = true;
                    Success = false;
                    return;
                }

                // 5 秒挣扎结束 → 进入 Pull 阶段
                if (elapsed >= 5)
                {
                    StartPullPhase();
                }
            }
            else if (currentPhase == Phase.Pull)
            {
                // ⭐ 3 秒内没拉到底 → 回到 Struggle（循环）
                if (elapsed >= 3)
                {
                    StartStrugglePhase();
                    return;
                }

                // 拉到底 → 成功
                if (fishPos.Y >= box.Bottom - 20)
                {
                    Finished = true;
                    Success = true;
                    return;
                }
            }
        }

        public override void receiveKeyPress(Keys key)
        {
            if (Finished) return;

            if (currentPhase == Phase.Struggle)
            {
                // 鱼往左跑 → 按 Right 抵抗
                if (fishVelocity.X < 0 && key == Keys.Right)
                    fishPos.X += 4;

                // 鱼往右跑 → 按 Left 抵抗
                if (fishVelocity.X > 0 && key == Keys.Left)
                    fishPos.X -= 4;

                // 鱼往上跑 → 按 Down 抵抗
                if (fishVelocity.Y < 0 && key == Keys.Down)
                    fishPos.Y += 4;
            }
            else if (currentPhase == Phase.Pull)
            {
                // ⭐ Pull 阶段允许按任意方向键移动鱼
                if (key == Keys.Left) fishPos.X -= 4;
                if (key == Keys.Right) fishPos.X += 4;
                if (key == Keys.Up) fishPos.Y -= 4;
                if (key == Keys.Down) fishPos.Y += 4;
            }
        }

        public override void draw(SpriteBatch b)
        {
            // 绘制边框
            IClickableMenu.drawTextureBox(
                b,
                Game1.menuTexture,
                new Rectangle(0, 256, 60, 60),
                box.X - 16,
                box.Y - 16,
                box.Width + 32,
                box.Height + 32,
                Color.Green,
                1f
            );

            // 直接绘制水面纹理（300x300，无需拉伸或平铺）
            if (waterTileTexture != null)
            {
                b.Draw(
                    waterTileTexture,
                    box,
                    Color.White
                );
            }

            foreach (BubbleParticle bubble in bubbles)
            {
                float alpha = MathHelper.Clamp(bubble.Life / bubble.MaxLife, 0f, 1f);
                b.Draw(
                    bubbleTexture,
                    bubble.Position,
                    null,
                    Color.White * alpha,
                    0f,
                    new Vector2(4f, 4f),
                    bubble.Scale,
                    SpriteEffects.None,
                    1f
                );
            }

            Vector2 bobberOffset = new Vector2(
                (float)Math.Sin(wobbleTime * 10f) * 2f,
                (float)Math.Cos(wobbleTime * 14f) * 1.5f
            );

            // 绘制浮标
            b.Draw(
                bobberTexture,
                fishPos + bobberOffset,
                null,
                Color.White,
                (float)Math.Sin(wobbleTime * 8f) * 0.08f,
                new Vector2(bobberTexture.Width / 2f, bobberTexture.Height / 2f),
                2f,
                SpriteEffects.None,
                1f
            );

            base.draw(b);
        }
    }
}