using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewValley;
using StardewValley.Menus;
using Microsoft.Xna.Framework.Input;
using System;

namespace SimpleFishingMod
{
    public class FishFightMenu : IClickableMenu
    {
        private Texture2D fishTexture;
        private Rectangle box;
        private Vector2 fishPos;

        private enum Phase { Struggle, Pull }
        private Phase currentPhase = Phase.Struggle;

        private Vector2 fishVelocity;
        private double phaseStartTime;

        private Random rand = new Random();

        public bool Finished = false;
        public bool Success = false;

        public FishFightMenu(Texture2D fishTex)
        {
            fishTexture = fishTex;

            box = new Rectangle(400, 200, 300, 300);
            fishPos = new Vector2(box.Center.X, box.Center.Y);

            StartStrugglePhase();
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
            b.Draw(Game1.staminaRect, box, Color.DarkBlue * 0.4f);

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

            b.Draw(
                fishTexture,
                fishPos,
                null,
                Color.White,
                0f,
                new Vector2(fishTexture.Width / 2, fishTexture.Height / 2),
                2f,
                SpriteEffects.None,
                1f
            );

            base.draw(b);
        }
    }
}