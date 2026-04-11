So many people are complaining about the fishing mini-game that they created a mod to skip it.
I am wondering why nobody wants to create a mod to replace the mini-game with an interesting one.

So here it is, an interesting one.
Please note 
1.  This is an early version, so it's buggy
2. I am not a professional game developer, so the mini-game is pretty basic, especially in terms of visual effects.

# How does it work?
Well, you can just fish like normal, but when a fish bites, a rectangle will appear where the buoy will display in the centre and
The fish under the buoy.
And the fish will start to struggle, it will choose a random direction, and go for it, and you have to curb it by pressing the opposite direction key multiple times.
The struggle will last for 5 seconds, and after that, the fish will be tired.
You have to pull the fish downwards by pressing the down key multiple times.
This process, which I call pull, will last for 3 seconds
The fish will be caught if it is forced to the bottom line of the rectangle during the process.
But if you didn't make it, the fish will start to struggle again until you catch it or it escapes.
That's it, pretty simple, right?

# Known Bugs
1. The original buoy will return to stagnate, like the fish didn't take the bait, when the fish do.

# Feature 
1. A treasure will be placed in the rectangle randomly.  
If you make the fish reach the location, you can get it after catching the fish.  
2. The Difficult is vary depending on the difficulty level of the fish

# TODO
- Keep the direct key pressed to curb or pull the fish, instead of pressing it multiple times.
- Add a progress bar on the top of the buoy indicating how fragile the fishing line is, and the line will break if the bar is full, then the fish will escape.
  - The bar only fills when you curb the fish when it's struggling after a specified time, which depends on the difficulty level of the fish
  - The increased speed of the bar depends on the level of the fishing rod(The level of the fishing rod also needs to be divided into multiple files, just like the difficulty of fish)
- The return values of StruggleFishSpeed, StrugglePhaseDurationSeconds, and PullPhaseDurationSeconds are randomly generated based on the difficulty level of the fish, instead of being fixed.
- If you catch a fish within a specified time determined by the difficulty of the fish, it is considered a Perfect Fishing, just like the original mini-game. Please use the in-game code to do so instead of writing on your own.
- Apply the Tackle effect to the mini-game. Below are their effects:(all the Tackle-related code must be divided into multiple files, just like the difficulty of the fish)
  - Trap Bobber: decrease the speed of the fish's struggle by 20% 
  - Cork Bobber: add a 50% chance to stop the fish's struggle for 3 second
  - Barbed Hook: add a 50% chance to make the fish pick downward direction when it starts to struggle