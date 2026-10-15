using System;

namespace Tylerland.Core
{
    /// <summary>
    /// Game Flow states according to the developer systems roadmap.
    /// Boot > Menu > NightIntro > NightActive > NightEnd > Salary > Choice > (Shop | Minigame) > NightIntro
    /// Dead > RunReset > Menu
    /// </summary>
    public enum GameState
    {
        Boot,
        Menu,
        NightIntro,
        NightActive,
        NightEnd,
        Salary,
        Choice,
        Shop,
        Minigame,
        Dead,
        RunReset,
        Victory
    }

    /// <summary>
    /// Helper rules for game states.
    /// </summary>
    public static class GameStateRules
    {
        /// <summary>
        /// Only in NightActive can the player pause the game.
        /// Pausing freezes clock, AI tick, and audio timers.
        /// </summary>
        public static bool CanPause(GameState state)
        {
            return state == GameState.NightActive;
        }

        /// <summary>
        /// Indicates if gameplay (office, camera, animatronics) is actively ticking.
        /// </summary>
        public static bool IsGameplayActive(GameState state)
        {
            return state == GameState.NightActive;
        }

        /// <summary>
        /// Indicates if the state represents an interactive UI menu screen.
        /// </summary>
        public static bool IsMenuScreen(GameState state)
        {
            return state == GameState.Menu ||
                   state == GameState.Salary ||
                   state == GameState.Choice ||
                   state == GameState.Shop;
        }
    }
}
