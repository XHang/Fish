using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewValley;
using StardewValley.Menus;
using Microsoft.Xna.Framework.Input;
using System;
using System.Collections;
using System.Collections.Generic;

namespace SimpleFishingMod
{
    public class FishFightMenu : IClickableMenu
    {
        private Texture2D fishTexture;
        private Texture2D? waterTileTexture;  // 从 beach tileset 提取的水面贴图
        private Texture2D bobberTexture;
        private Texture2D rippleTexture;
        private Texture2D treasureTexture;
        private Rectangle box;
        private Vector2 bobberPos;

        private enum Phase { Struggle, Pull }
        private Phase currentPhase = Phase.Struggle;

        private Vector2 fishVelocity;
        private double phaseStartTime;

        private Random rand = new Random();
        
        private ModEntry? modEntry;
        private float wobbleTime;
        private float struggleSoundCooldown;
        private float pullSoundCooldown;
        private List<RippleParticle> ripples = new();
        private readonly bool treasureAvailable;
        private bool treasureSpawned;
        private bool treasureCollected;
        private Vector2 treasurePos;
        private float treasureHoldTime;
        private const float TreasureHoldSeconds = 3f;

        public bool Finished = false;
        public bool Success = false;
        public bool TreasureCollected => treasureCollected;

        private class RippleParticle
        {
            public Vector2 Position;
            public float Life;
            public float MaxLife;
            public float StartScale;
            public float EndScale;
            public float Rotation;
        }

        public FishFightMenu(Texture2D fishTex, Texture2D backgroundTex, ModEntry? modEntry = null, bool treasureAvailable = false)
        {
            fishTexture = fishTex;
            this.modEntry = modEntry;
            this.treasureAvailable = treasureAvailable;
            bobberTexture = CreateBobberTexture();
            rippleTexture = CreateRippleTexture();
            treasureTexture = modEntry?.GetTreasureTexture() ?? throw new InvalidOperationException("treasure.png was not loaded.");

            box = new Rectangle(400, 200, 300, 300);
            bobberPos = new Vector2(box.Center.X, box.Center.Y);

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

        private void DrawProgressBar(SpriteBatch b, Vector2 center, float progress)
        {
            progress = MathHelper.Clamp(progress, 0f, 1f);
            int width = 64;
            int height = 8;
            int x = (int)center.X - width / 2;
            int y = (int)center.Y - 32;

            b.Draw(Game1.staminaRect, new Rectangle(x - 2, y - 2, width + 4, height + 4), Color.Black * 0.65f);
            b.Draw(Game1.staminaRect, new Rectangle(x, y, width, height), Color.DarkSlateGray * 0.85f);
            b.Draw(Game1.staminaRect, new Rectangle(x, y, (int)(width * progress), height), Color.Gold);
        }

        private Texture2D CreateRippleTexture()
        {
            const int size = 32;
            Texture2D texture = new Texture2D(Game1.graphics.GraphicsDevice, size, size);
            Color[] data = new Color[size * size];
            Vector2 center = new Vector2((size - 1) / 2f, (size - 1) / 2f);
            float radius = 11f;
            float thickness = 1.5f;

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float distance = Vector2.Distance(new Vector2(x, y), center);
                    float delta = Math.Abs(distance - radius);
                    if (delta <= thickness)
                    {
                        float alpha = 1f - (delta / thickness);
                        data[y * size + x] = new Color(220, 245, 255) * alpha;
                    }
                    else
                    {
                        data[y * size + x] = Color.Transparent;
                    }
                }
            }

            texture.SetData(data);
            return texture;
        }

        private void StartStrugglePhase()
        {
            currentPhase = Phase.Struggle;
            phaseStartTime = Game1.currentGameTime.TotalGameTime.TotalSeconds;
            struggleSoundCooldown = 0f;

            float leftDist = bobberPos.X - box.Left;
            float rightDist = box.Right - bobberPos.X;
            float upDist = bobberPos.Y - box.Top;

            float minDist = Math.Min(leftDist, Math.Min(rightDist, upDist));
            int dir;

            if (minDist <= 120f)
            {
                List<int> options = new();
                if (leftDist == minDist) options.Add(0);
                if (upDist == minDist) options.Add(1);
                if (rightDist == minDist) options.Add(2);

                dir = options[rand.Next(options.Count)];
            }
            else
            {
                dir = rand.Next(3);
            }

            if (dir == 0) fishVelocity = new Vector2(-0.6f, 0);
            if (dir == 1) fishVelocity = new Vector2(0, -0.6f);
            if (dir == 2) fishVelocity = new Vector2(0.6f, 0);

            // ? 不再重置鱼位置（保持上一轮的位置）
            // fishPos = new Vector2(box.Center.X, box.Center.Y);  <-- 删除
        }

        private void StartPullPhase()
        {
            currentPhase = Phase.Pull;
            phaseStartTime = Game1.currentGameTime.TotalGameTime.TotalSeconds;
            pullSoundCooldown = 0f;

            if (treasureAvailable && !treasureSpawned && !treasureCollected)
            {
                treasureSpawned = true;
                treasureHoldTime = 0f;

                float margin = 40f;
                treasurePos = new Vector2(
                    rand.Next((int)box.Left + (int)margin, (int)box.Right - (int)margin),
                    rand.Next((int)box.Top + (int)margin, (int)box.Bottom - (int)margin)
                );
            }
        }

        private bool IsConfiguredMoveKey(string bindingName, Keys key)
        {
            object? value = Game1.options.GetType().GetField(bindingName)?.GetValue(Game1.options)
                ?? Game1.options.GetType().GetProperty(bindingName)?.GetValue(Game1.options);

            if (value is not IEnumerable entries)
                return false;

            foreach (object? entry in entries)
            {
                if (entry == null)
                    continue;

                if (EntryMatchesKey(entry, key))
                    return true;
            }

            return false;
        }

        private bool EntryMatchesKey(object entry, Keys key)
        {
            string keyName = key.ToString();
            string text = entry.ToString() ?? string.Empty;

            if (text.Equals(keyName, StringComparison.OrdinalIgnoreCase)
                || text.EndsWith($".{keyName}", StringComparison.OrdinalIgnoreCase)
                || text.Contains($"{keyName}", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            foreach (string memberName in new[] { "Key", "Button", "button", "key" })
            {
                object? memberValue = entry.GetType().GetProperty(memberName)?.GetValue(entry)
                    ?? entry.GetType().GetField(memberName)?.GetValue(entry);

                if (memberValue != null && memberValue.ToString() == keyName)
                    return true;
            }

            return false;
        }

        private bool IsMoveLeftKey(Keys key) => IsConfiguredMoveKey("moveLeftButton", key);

        private bool IsMoveRightKey(Keys key) => IsConfiguredMoveKey("moveRightButton", key);

        private bool IsMoveUpKey(Keys key) => IsConfiguredMoveKey("moveUpButton", key);

        private bool IsMoveDownKey(Keys key) => IsConfiguredMoveKey("moveDownButton", key);

        private bool TryGetConfiguredKey(object entry, out Keys configuredKey)
        {
            foreach (string memberName in new[] { "Key", "Button", "button", "key" })
            {
                object? memberValue = entry.GetType().GetProperty(memberName)?.GetValue(entry)
                    ?? entry.GetType().GetField(memberName)?.GetValue(entry);

                if (memberValue != null && Enum.TryParse(memberValue.ToString(), true, out configuredKey))
                    return true;
            }

            string text = entry.ToString() ?? string.Empty;
            if (text.Contains('.'))
                text = text[(text.LastIndexOf('.') + 1)..];

            return Enum.TryParse(text, true, out configuredKey);
        }

        private bool IsConfiguredMoveKeyHeld(string bindingName)
        {
            object? value = Game1.options.GetType().GetField(bindingName)?.GetValue(Game1.options)
                ?? Game1.options.GetType().GetProperty(bindingName)?.GetValue(Game1.options);

            if (value is not IEnumerable entries)
                return false;

            KeyboardState keyboardState = Keyboard.GetState();
            foreach (object? entry in entries)
            {
                if (entry != null && TryGetConfiguredKey(entry, out Keys configuredKey) && keyboardState.IsKeyDown(configuredKey))
                    return true;
            }

            return false;
        }

        public override void update(GameTime time)
        {
            base.update(time);

            if (Finished) return;

            float elapsedSeconds = (float)time.ElapsedGameTime.TotalSeconds;
            if (struggleSoundCooldown > 0f)
                struggleSoundCooldown -= elapsedSeconds;
            if (pullSoundCooldown > 0f)
                pullSoundCooldown -= elapsedSeconds;

            wobbleTime += (float)time.ElapsedGameTime.TotalSeconds;

            float wobbleStrength = currentPhase == Phase.Struggle ? 1f : 0f;
            Vector2 bobberOffset = new Vector2(
                (float)Math.Sin(wobbleTime * 180f) * 2f * wobbleStrength + (float)Math.Cos(wobbleTime * 180f) * 2f * wobbleStrength,
                (float)Math.Cos(wobbleTime * 180f) * 2f * wobbleStrength + (float)Math.Sin(wobbleTime * 180f) * 2f * wobbleStrength
            );

            int spawnCount = currentPhase == Phase.Struggle ? rand.Next(1, 3) : rand.Next(0, 2);
            for (int s = 0; s < spawnCount; s++)
            {
                ripples.Add(new RippleParticle
                {
                    Position = bobberPos + bobberOffset + new Vector2((float)(rand.NextDouble() - 0.5) * 4f, (float)(rand.NextDouble() - 0.5) * 4f),
                    Life = 3.22f + (float)rand.NextDouble() * 0.16f,
                    MaxLife = 8.22f + (float)rand.NextDouble() * 0.16f,
                    StartScale = 0.15f + (float)rand.NextDouble() * 0.08f,
                    EndScale = 1.0f + (float)rand.NextDouble() * 0.45f,
                    Rotation = (float)(rand.NextDouble() * Math.PI * 2),
                });
            }

            for (int i = ripples.Count - 1; i >= 0; i--)
            {
                RippleParticle ripple = ripples[i];
                ripple.Life -= (float)time.ElapsedGameTime.TotalSeconds;

                if (ripple.Life <= 0)
                    ripples.RemoveAt(i);
                else
                    ripples[i] = ripple;
            }

            double elapsed = time.TotalGameTime.TotalSeconds - phaseStartTime;

            if (currentPhase == Phase.Struggle)
            {
                if (struggleSoundCooldown <= 0f)
                {
                    Game1.playSound("waterSlosh");
                    struggleSoundCooldown = 0.22f;
                }

                bobberPos += fishVelocity;

                // 鱼逃出框 → 失败
                if (!box.Contains(bobberPos))
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
                bool isPullingDown = IsConfiguredMoveKeyHeld("moveDownButton");

                if (isPullingDown && pullSoundCooldown <= 0f)
                {
                    Game1.playSound("fishingRodBend");
                    pullSoundCooldown = 0.08f;
                }

                if (treasureSpawned && !treasureCollected)
                {
                    Rectangle treasureRect = new Rectangle((int)treasurePos.X - 12, (int)treasurePos.Y - 12, 24, 24);
                    Rectangle bobberRect = new Rectangle((int)bobberPos.X - 12, (int)bobberPos.Y - 12, 24, 24);

                    if (treasureRect.Intersects(bobberRect))
                        treasureHoldTime += (float)time.ElapsedGameTime.TotalSeconds;
                    else
                        treasureHoldTime = 0f;

                    if (treasureHoldTime >= TreasureHoldSeconds)
                    {
                        treasureCollected = true;
                        Game1.playSound("newArtifact");
                    }
                }

                // ? 3 秒内没拉到底 → 回到 Struggle（循环）
                if (elapsed >= 3)
                {
                    StartStrugglePhase();
                    return;
                }

                // 拉到底 → 成功
                if (bobberPos.Y >= box.Bottom - 20)
                {
                    Finished = true;
                    Success = true;
                    return;
                }

            if (treasureSpawned && !treasureCollected)
            {
                // treasure disappears if not collected during this pull window
                if (currentPhase != Phase.Pull)
                    treasureSpawned = false;
            }
            }
        }

        public override void receiveKeyPress(Keys key)
        {
            if (Finished) return;

            if (currentPhase == Phase.Struggle)
            {
                if (IsMoveLeftKey(key)) bobberPos.X -= 4;
                if (IsMoveRightKey(key)) bobberPos.X += 4;
                if (IsMoveUpKey(key)) bobberPos.Y -= 4;
                if (IsMoveDownKey(key)) bobberPos.Y += 4;
            }
            else if (currentPhase == Phase.Pull)
            {
                if (IsMoveLeftKey(key)) bobberPos.X -= 4;
                if (IsMoveRightKey(key)) bobberPos.X += 4;
                if (IsMoveUpKey(key)) bobberPos.Y -= 4;
                if (IsMoveDownKey(key)) bobberPos.Y += 4;
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

            if (treasureSpawned && !treasureCollected)
            {
                Vector2 treasureDrawPos = new Vector2(treasurePos.X - treasureTexture.Width / 2f, treasurePos.Y - treasureTexture.Height / 2f);
                b.Draw(treasureTexture, treasureDrawPos, Color.White);
                DrawProgressBar(b, treasurePos, treasureHoldTime / TreasureHoldSeconds);
            }

            foreach (RippleParticle ripple in ripples)
            {
                float progress = 1f - MathHelper.Clamp(ripple.Life / ripple.MaxLife, 0f, 1f);
                float alpha = (1f - progress) * 0.85f;
                float scale = MathHelper.Lerp(ripple.StartScale, ripple.EndScale, progress);
                b.Draw(
                    rippleTexture,
                    ripple.Position,
                    null,
                    Color.White * alpha,
                    ripple.Rotation,
                    new Vector2(rippleTexture.Width / 2f, rippleTexture.Height / 2f),
                    scale,
                    SpriteEffects.None,
                    1f
                );
            }

            float wobbleStrength = currentPhase == Phase.Struggle ? 1f : 0f;
            Vector2 bobberOffset = new Vector2(
                (float)Math.Sin(wobbleTime * 180f) * 2f * wobbleStrength + (float)Math.Cos(wobbleTime * 180f) * 2f * wobbleStrength,
                (float)Math.Cos(wobbleTime * 180f) * 2f * wobbleStrength + (float)Math.Sin(wobbleTime * 180f) * 2f * wobbleStrength
            );

            Vector2 bobberDrawPos = bobberPos + bobberOffset;
            Vector2 fishDrawPos = bobberDrawPos + new Vector2(0f, 34f);

            // 绘制浮标
            b.Draw(
                bobberTexture,
                bobberDrawPos,
                null,
                Color.White,
                (float)Math.Sin(wobbleTime * 34f) * 0.08f * wobbleStrength,
                new Vector2(bobberTexture.Width / 2f, bobberTexture.Height / 2f),
                2f,
                SpriteEffects.None,
                1f
            );

            Rectangle fishBounds = new Rectangle(
                (int)(fishDrawPos.X - fishTexture.Width),
                (int)(fishDrawPos.Y - fishTexture.Height),
                fishTexture.Width * 2,
                fishTexture.Height * 2
            );
            Rectangle clippedFishBounds = Rectangle.Intersect(fishBounds, box);

            if (clippedFishBounds.Width > 0 && clippedFishBounds.Height > 0)
            {
                float scale = 2f;
                Vector2 fishTopLeft = new Vector2(fishDrawPos.X - fishTexture.Width, fishDrawPos.Y - fishTexture.Height);
                Rectangle source = new Rectangle(
                    (int)((clippedFishBounds.X - fishTopLeft.X) / scale),
                    (int)((clippedFishBounds.Y - fishTopLeft.Y) / scale),
                    Math.Min(fishTexture.Width, (int)Math.Ceiling(clippedFishBounds.Width / scale)),
                    Math.Min(fishTexture.Height, (int)Math.Ceiling(clippedFishBounds.Height / scale))
                );

                b.Draw(
                    fishTexture,
                    clippedFishBounds,
                    source,
                    Color.White,
                    0f,
                    Vector2.Zero,
                    SpriteEffects.None,
                    1f
                );
            }

            base.draw(b);
        }
    }
}