using UnityEngine;

namespace Tylerland.Config
{
    /// <summary>
    /// Defines how an animatronic reacts when watched on the security tablet cameras.
    /// </summary>
    public enum CameraStallBehavior
    {
        None = 0,
        /// <summary>Stalls movement ticks while the player is actively viewing the animatronic's current camera room (like Freddy / Pirate Cove).</summary>
        StalledWhenWatchedOnCamera = 1,
        /// <summary>Stalls movement ticks whenever the security tablet is raised, regardless of channel.</summary>
        StalledWheneverMonitorOpen = 2,
        /// <summary>Manipulates camera static and spoofs feeds when moving (Snake).</summary>
        ManipulatesCameraFeeds = 3
    }

    /// <summary>
    /// Section 5.2: Cast Configuration ScriptableObject.
    /// Defines movement route, tick interval, attack window, counter-weapon, and 2D hand-drawn sprite slots
    /// for a single animatronic (Tyler, Shark, Eagle, Snake, or Boss).
    /// </summary>
    [CreateAssetMenu(fileName = "AnimatronicConfig", menuName = "Tylerland/Animatronic Config", order = 3)]
    public class AnimatronicConfig : ScriptableObject
    {
        [Header("Identity")]
        [Tooltip("Internal ID matching RunState ('tyler', 'shark', 'eagle', 'snake', 'boss')")]
        public string animatronicId = "tyler";

        [Tooltip("Display name shown on UI and logs")]
        public string displayName = "Tyler";

        [Tooltip("Theme color used for debug indicators and placeholder silhouettes")]
        public Color themeColor = new Color(0.85f, 0.25f, 0.25f, 1f);

        [Header("Target Door & Facility Route (Roadmap 5.1 & 5.2)")]
        [Tooltip("Target office door index: 0 = Left (Shark), 1 = Center (Tyler), 2 = Right (Eagle), 3 = Back (Snake)")]
        [Range(0, 3)]
        public int targetDoorIndex = 1;

        [Tooltip("Ordered list of Camera Room indices (0-9) this animatronic visits before reaching the doorway blindspot.\n" +
                 "0=CAM 1A Stage, 1=CAM 1B Dining, 2=CAM 2A West Hall, 3=CAM 2B West Corner,\n" +
                 "4=CAM 3 Pirate Cove, 5=CAM 4A East Hall, 6=CAM 4B East Corner,\n" +
                 "7=CAM 5 Backstage, 8=CAM 6 Kitchen, 9=CAM 7 Service Vent")]
        public int[] cameraRouteIndices = new int[] { 0, 1, 7, 1 };

        [Tooltip("Stage index the animatronic resets to after being blocked by a closed door (usually 1 so they don't reset all the way to stage 0)")]
        public int retreatStageIndex = 1;

        [Header("AI Movement & Attack Timings")]
        [Tooltip("Seconds between movement opportunity rolls (FNAF 1 style: ~3.0s to 5.0s)")]
        [Range(1.0f, 15.0f)]
        public float baseTickInterval = 4.8f;

        [Tooltip("Bonus AI levels added gradually from 12 AM to 6 AM via NightClock.HourBlendFactor")]
        [Range(0, 10)]
        public int lateNightAIRamp = 3;

        [Tooltip("Seconds the player has to close the door once this animatronic enters the doorway blindspot (AttackReady)")]
        [Range(1.0f, 10.0f)]
        public float attackWindowSeconds = 3.5f;

        [Tooltip("How this animatronic interacts with the security camera monitor")]
        public CameraStallBehavior cameraBehavior = CameraStallBehavior.StalledWhenWatchedOnCamera;

        [Header("Special Weapon Counter (Roadmap 5.5)")]
        [Tooltip("ID of the one-use weapon that counters and damages this animatronic ('Shears', 'Harpoon', 'Net', 'Prod')")]
        public string counterWeaponId = "Shears";

        [Header("2D Hand-Drawn Sprites (Artist Assets)")]
        [Tooltip("2D sprites for each stage along the camera route (optional; overrides room feed when present)")]
        public Sprite[] cameraStageSprites;

        [Tooltip("2D sprites when standing in the doorway blindspot, indexed by Damage Level (0=Intact, 1=Cracked, 2=Broken, 3=Critical)")]
        public Sprite[] doorwaySpritesByDamage = new Sprite[4];

        [Tooltip("Full-screen 2D hand-drawn jumpscare animation frames (or single jumpscare image)")]
        public Sprite[] jumpscareFrames;

        [Header("Audio Cues")]
        public AudioClip footstepSound;
        public AudioClip blindspotWarningSound;
        public AudioClip doorBlockedBangSound;
        public AudioClip jumpscareScreamSound;
    }
}
