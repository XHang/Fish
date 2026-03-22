using StardewModdingAPI;
using StardewValley;
using StardewValley.Menus;
using Microsoft.Xna.Framework.Input;

namespace SimpleFishingMod
{
    public class ModEntry : Mod
    {
        private bool waitingForKey = false;
        private double startTime = 0;

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
            // Detect original fishing minigame
            if (Game1.activeClickableMenu is BobberBar bar && !waitingForKey)
            {
                // ✔ safest and most stable way to get the fish
                pendingFishId = bar.whichFish;
                pendingFishQuality = bar.fishQuality;

                Game1.exitActiveMenu();

                waitingForKey = true;
                startTime = Game1.currentGameTime.TotalGameTime.TotalSeconds;

                Game1.showGlobalMessage("Press 5 within 5 seconds to catch the fish!");
                return;
            }

            if (waitingForKey)
            {
                double elapsed = Game1.currentGameTime.TotalGameTime.TotalSeconds - startTime;

                if (elapsed > 5)
                {
                    waitingForKey = false;
                    EndFishing();
                    Game1.showRedMessage("The fish escaped!");
                    return;
                }

                var state = Keyboard.GetState();
                if (state.IsKeyDown(Keys.D5) || state.IsKeyDown(Keys.NumPad5))
                {
                    waitingForKey = false;
                    EndFishing();

                    Game1.showGlobalMessage("You caught the fish!");

                    if (pendingFishId != null)
                    {
                        Game1.player.addItemToInventory(
                            new StardewValley.Object(pendingFishId, 1, quality: pendingFishQuality)
                        );
                    }

                    pendingFishId = null;
                }
            }
        }
    }
}