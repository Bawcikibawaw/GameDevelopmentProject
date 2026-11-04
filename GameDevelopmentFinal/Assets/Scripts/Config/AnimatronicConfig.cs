using UnityEngine;

namespace Tylerland.Config
{
    /// <summary>
    /// Defines how an animatronic reacts when watched on its dedicated 2D camera room feed (GDD Section 4).
    /// </summary>
    public enum CameraStallBehavior
    {
        None = 0,
        /// <summary>Stalls movement ticks while the player is actively viewing the animatronic's room camera.</summary>
        StalledWhenWatchedOnCamera = 1,
        /// <summary>Orville the Orca (§4.3.2 & §4.3.4): Normal = 80% retreat 1 stage / 20% advance 2 stages when watched; Nightmare = watching advances him 1 stage + 30s/60s unattended timers.</summary>
        OrvilleProbabilisticAndReversal = 2,
        /// <summary>Sandy the Snake (§4.4.2 & §4.4.4): Normal = straightforward progression (no fake feeds); Nightmare = 5 stages + sheds decoy shells on camera.</summary>
        SandyNightmareDecoys = 3,
        /// <summary>Tyler the Lion (§4.2.2 & §4.2.5): Passive observer Nights 1-3; attacks Night 4+; Nightmare = destroys his camera (audio-only tracking).</summary>
        TylerDestroysCameraInNightmare = 4,
        /// <summary>Erle the Eagle (§4.5.3 - §4.5.6): Identity scanner check; Nightmare = Good Cop / Bad Cop + Broken Authority door misuse penalty.</summary>
        ErleScannerAndGoodBadCop = 5
    }

    /// <summary>
    /// Section 4 GDD: Animatronic Character & Threat Design ScriptableObject.
    /// Consolidates the designs of the 4 main animatronics (Tyler the Lion, Orville the Orca,
    /// Sandy the Snake, Erle the Eagle) and the True Final Boss, their dedicated 2D camera rooms,
    /// door defenses, minigame weapons, and Nightmare transformations.
    /// </summary>
    [CreateAssetMenu(fileName = "AnimatronicConfig", menuName = "Tylerland/Animatronic Config", order = 3)]
    public class AnimatronicConfig : ScriptableObject
    {
        [Header("Identity (GDD §4.1)")]
        [Tooltip("Internal ID ('erle', 'tyler', 'orville', 'sandy', 'boss')")]
        public string animatronicId = "tyler";

        [Tooltip("Full display name shown on UI and logs (e.g. 'Tyler the Lion', 'Orville the Orca', 'Sandy the Snake', 'Erle the Eagle')")]
        public string displayName = "Tyler the Lion";

        [Tooltip("Mascot role from GDD §4.1 (e.g. 'Lead singer / central mascot')")]
        public string formerRole = "Lead singer / central mascot";

        [Tooltip("Theme color used for 2D placeholder sprites and UI accents")]
        public Color themeColor = new Color(0.85f, 0.25f, 0.25f, 1f);

        [Header("Activation & Door Assignment (GDD §4.1)")]
        [Tooltip("Target office door & dedicated camera room index:\n" +
                 "0 = LEFT Door / CAM 01 (Erle the Eagle)\n" +
                 "1 = CENTER Door / CAM 02 (Tyler the Lion)\n" +
                 "2 = RIGHT Door / CAM 03 (Orville the Orca)\n" +
                 "3 = REAR Door / CAM 04 (Sandy the Snake)")]
        [Range(0, 3)]
        public int targetDoorIndex = 1;

        [Tooltip("Night number when this animatronic begins attacking (Sandy=1, Orville=2, Erle=3, Tyler=4)")]
        [Range(1, 6)]
        public int activeFromNight = 4;

        [Tooltip("If true (Tyler the Lion), the character is present in its room watching the camera on earlier nights (Nights 1-3) but chooses not to attack until activeFromNight")]
        public bool passiveObserverBeforeActiveNight = true;

        [Header("Room Stages & Reaction Windows (GDD §4.6)")]
        [Tooltip("Number of progression stages inside their room in Normal form (Tyler=4, Orville=6, Sandy=4, Erle=5)")]
        [Range(1, 8)]
        public int normalStageCount = 3;

        [Tooltip("Number of progression stages inside their room in Nightmare form (Sandy increases from 4 to 5 in Nightmare form!)")]
        [Range(1, 8)]
        public int nightmareStageCount = 3;

        [Tooltip("Legacy route array kept for backward compatibility")]
        public int[] cameraRouteIndices = new int[] { 1, 1, 1, 1 };

        [Tooltip("Stage index (0-based: 0 = Stage 1, 1 = Stage 2) the animatronic resets to after being blocked by a closed door (Orville resets to Stage 2 -> index 1; others reset to Stage 1 -> index 0)")]
        public int retreatStageIndex = 0;

        [Tooltip("Seconds the player has to close the door at final approach in Normal form (Tyler=10s, Orville=20s, Sandy=20s, Erle=15s)")]
        [Range(1.0f, 30.0f)]
        public float attackWindowSeconds = 10.0f;

        [Tooltip("Seconds the player has to close the door at final approach in Nightmare form (Tyler=10s, Orville=20s, Sandy=8s, Erle=12s)")]
        [Range(1.0f, 30.0f)]
        public float nightmareAttackWindowSeconds = 10.0f;

        [Header("Door Block Resource Impacts (GDD §4.2 - §4.5)")]
        [Tooltip("Extra door Durability lost when blocking this animatronic's attack (in addition to base block wear)")]
        public float extraDurabilityLossOnBlock = 5.0f;

        [Tooltip("Door Energy drained when blocking this animatronic in Normal form (Orville drains Energy on block; Tyler/Sandy/Erle = 0)")]
        public float energyDrainOnNormalBlock = 0.0f;

        [Tooltip("Door Energy drained when blocking this animatronic in Nightmare form (Nightmare Sandy causes heavy Energy drain on block!)")]
        public float energyDrainOnNightmareBlock = 0.0f;

        [Header("AI Movement Timings")]
        [Tooltip("Base seconds between movement opportunity rolls")]
        [Range(1.0f, 15.0f)]
        public float baseTickInterval = 4.8f;

        [Tooltip("Bonus AI levels added gradually from 12 AM to 6 AM via NightClock.HourBlendFactor")]
        [Range(0, 10)]
        public int lateNightAIRamp = 3;

        [Tooltip("Character-specific camera & Nightmare behavior rule from GDD Section 4")]
        public CameraStallBehavior cameraBehavior = CameraStallBehavior.TylerDestroysCameraInNightmare;

        [Header("Permanent Weapon & Nightmare Trigger (GDD §4.1.2)")]
        [Tooltip("ID of the one-use weapon that counters this animatronic and triggers its Nightmare form ('Shotgun', 'Harpoon', 'Spiked Mace', 'Electric Gun')")]
        public string counterWeaponId = "Shotgun";

        [Header("100% 2D Hand-Drawn Sprites (Normal & Nightmare)")]
        [Tooltip("2D room sprites for each stage in Normal form (Each phase has a FIXED anchor position in the room; animatronics NEVER zoom or creep closer to the camera lens)")]
        public Sprite[] cameraStageSprites = new Sprite[3];

        [Tooltip("2D room sprites for each stage in Nightmare / Damaged form (Fixed anchor positions per phase with damaged/nightmare form visual changes)")]
        public Sprite[] nightmareCameraStageSprites = new Sprite[3];

        [Tooltip("Normalized (X: 0..1, Y: 0..1) fixed anchor positions inside the camera feed for each room phase (prevents fake camera zoom/scaling; animatronic stays at fixed spots)")]
        public Vector2[] stageAnchorCoordinates = new Vector2[0];

        [Tooltip("2D sprite for Nightmare Sandy's shed decoy shell left behind in CAM 04")]
        public Sprite decoyShellSprite;

        [Tooltip("2D sprite for this animatronic's room when EMPTY (when the animatronic has left for your office door)")]
        public Sprite emptyRoomSprite;

        [Tooltip("2D sprites when standing in the office doorway blindspot (illuminated by 'F' light): Index 0 = Normal form, Index 1+ = Nightmare / Damaged form")]
        public Sprite[] doorwaySpritesByDamage = new Sprite[4];

        [Tooltip("Full-screen 2D hand-drawn jumpscare frames for Normal form")]
        public Sprite[] jumpscareFrames;

        [Tooltip("Full-screen 2D hand-drawn jumpscare frames for Nightmare form")]
        public Sprite[] nightmareJumpscareFrames;

        [Header("Audio Cues")]
        public AudioClip footstepSound;
        public AudioClip blindspotWarningSound;
        public AudioClip doorBlockedBangSound;
        public AudioClip jumpscareScreamSound;
        public AudioClip nightmareJumpscareScreamSound;

        /// <summary>
        /// Returns the custom 2D doorway sprite for the given damage level (0 = Normal, 1+ = Nightmare/Damaged), or null if none assigned.
        /// </summary>
        public Sprite GetBlindspotSpriteForDamage(int damageLevel)
        {
            if (doorwaySpritesByDamage == null || doorwaySpritesByDamage.Length == 0) return null;
            int idx = Mathf.Clamp(damageLevel, 0, doorwaySpritesByDamage.Length - 1);
            return doorwaySpritesByDamage[idx] != null ? doorwaySpritesByDamage[idx] : doorwaySpritesByDamage[0];
        }

        /// <summary>
        /// Returns the normalized fixed anchor position (X: 0..1, Y: 0..1) inside the camera feed for this stage phase.
        /// Animatronics remain at discrete fixed locations in every phase without zooming closer to the lens.
        /// </summary>
        public Vector2 GetStageAnchorCoordinate(int stageIndex, int totalStages)
        {
            if (stageAnchorCoordinates != null && stageIndex >= 0 && stageIndex < stageAnchorCoordinates.Length)
            {
                return stageAnchorCoordinates[stageIndex];
            }
            return GetDefaultStageAnchor(animatronicId, stageIndex, totalStages);
        }

        /// <summary>
        /// Default fixed room anchor coordinates (X: 0..1, Y: 0..1) per character phase.
        /// Enforces the rule that animatronics never scale towards the camera, but instead occupy discrete fixed anchor spots.
        /// </summary>
        public static Vector2 GetDefaultStageAnchor(string id, int stageIndex, int totalStages)
        {
            string cleanId = id != null ? id.ToLowerInvariant().Trim() : "";

            switch (cleanId)
            {
                case "erle": // CAM 01 (Left / Sheriff) - 3 Camera Positions
                    switch (stageIndex)
                    {
                        case 0: return new Vector2(0.24f, 0.38f); // Pos 1: Desk chair
                        case 1: return new Vector2(0.52f, 0.42f); // Pos 2: Jail cell bars
                        default: return new Vector2(0.80f, 0.32f); // Pos 3: Doorway archway exit
                    }

                case "tyler": // CAM 02 (Center / Main Stage) - 3 Camera Positions
                    switch (stageIndex)
                    {
                        case 0: return new Vector2(0.50f, 0.38f); // Pos 1: Center stage microphone stand
                        case 1: return new Vector2(0.74f, 0.36f); // Pos 2: Stage right curtain / spotlight
                        default: return new Vector2(0.26f, 0.36f); // Pos 3: Stage left exit arch
                    }

                case "orville": // CAM 03 (Right / Medical Bay) - 3 Camera Positions
                    switch (stageIndex)
                    {
                        case 0: return new Vector2(0.28f, 0.40f); // Pos 1: Recovery tank / gurney
                        case 1: return new Vector2(0.68f, 0.38f); // Pos 2: Medical monitors / pool deck
                        default: return new Vector2(0.50f, 0.30f); // Pos 3: Corridor viewing window
                    }

                case "sandy": // CAM 04 (Rear / Maintenance) - 3 Camera Positions
                    switch (stageIndex)
                    {
                        case 0: return new Vector2(0.50f, 0.58f); // Pos 1: Coiled on overhead ceiling pipe
                        case 1: return new Vector2(0.22f, 0.42f); // Pos 2: Hanging over electrical breaker
                        default: return new Vector2(0.76f, 0.32f); // Pos 3: Slithering by tunnel exit
                    }

                default:
                    float t = totalStages > 1 ? (float)stageIndex / (totalStages - 1) : 0.5f;
                    return new Vector2(Mathf.Lerp(0.25f, 0.75f, t), 0.35f);
            }
        }
    }
}
