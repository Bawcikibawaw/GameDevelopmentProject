using UnityEngine;

namespace Tylerland.Config
{
    /// <summary>
    /// Configuration asset defining the rules, pacing, and AI difficulty for a single night.
    /// ScriptableObject so parameters can be tuned without recompiling.
    /// </summary>
    [CreateAssetMenu(fileName = "NightConfig_Night1", menuName = "Tylerland/Night Config", order = 1)]
    public class NightConfig : ScriptableObject
    {
        [Header("Night Overview")]
        [Tooltip("The night index (1 to 6)")]
        [Range(1, 6)]
        public int nightNumber = 1;

        [Tooltip("Real-time seconds per game hour. 90s = 9 minute night. Early nights can be faster (e.g. 60s).")]
        [Range(30f, 180f)]
        public float realSecondsPerGameHour = 90f;

        [Tooltip("Duration of the 'Night N, 12:00 AM' intro card before shift begins.")]
        [Range(1f, 10f)]
        public float nightIntroDuration = 3f;

        [Header("Animatronic AI Levels (0 = Inactive, 20 = Max Aggression)")]
        [Range(0, 20)]
        [Tooltip("Center door threat. Baseline teacher in Night 1.")]
        public int tylerAILevel = 2;

        [Range(0, 20)]
        [Tooltip("Left door threat. Dorsal-fin approach.")]
        public int sharkAILevel = 0;

        [Range(0, 20)]
        [Tooltip("Right door threat. Fast and aggressive.")]
        public int eagleAILevel = 0;

        [Range(0, 20)]
        [Tooltip("Back door threat. Manipulates camera feeds.")]
        public int snakeAILevel = 0;

        [Header("Boss Encounter")]
        [Tooltip("Whether the special Boss animatronic is active on this night (Night 6).")]
        public bool bossActive = false;

        [Header("Phone Call")]
        [Tooltip("Audio clip for the phone call playing at the start of this night.")]
        public AudioClip phoneCallClip;

        [Tooltip("Subtitle or text for the phone call.")]
        [TextArea(3, 8)]
        public string phoneCallSubtitles = "Hello? Hello! Welcome to your new summer job at Tylerland...";

        [Tooltip("Can the player mute this call? (Mute is allowed starting Night 2)")]
        public bool muteAllowed = false;

        /// <summary>
        /// Total night duration in real seconds (6 game hours).
        /// </summary>
        public float TotalNightDurationSeconds => realSecondsPerGameHour * 6f;
    }
}
