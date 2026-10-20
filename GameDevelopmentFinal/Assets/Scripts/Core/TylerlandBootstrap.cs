using System.Collections.Generic;
using UnityEngine;
using Tylerland.Config;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Tylerland.Core
{
    /// <summary>
    /// Master bootstrap component for Tylerland.
    /// Place this component on a single GameObject in your starting scene (e.g. named '[GAME_CORE]').
    /// It automatically initializes and wires up all Core Run Systems.
    /// </summary>
    [RequireComponent(typeof(GameFlowManager))]
    [RequireComponent(typeof(NightClock))]
    [RequireComponent(typeof(RunManager))]
    [RequireComponent(typeof(MetaProgressionManager))]
    [RequireComponent(typeof(CoreDebugOverlay))]
    [RequireComponent(typeof(ScreenFader))]
    public class TylerlandBootstrap : Singleton<TylerlandBootstrap>
    {
        [Header("Configuration Assets")]
        [Tooltip("Array of NightConfigs for Nights 1 through 6")]
        [SerializeField] private NightConfig[] _nightConfigs;

        [Tooltip("Economy Configuration asset for salaries and repairs")]
        [SerializeField] private EconomyConfig _economyConfig;

        [Header("Auto-Start Options")]
        [Tooltip("If true, automatically transitions from Boot -> Menu -> NightIntro -> NightActive for testing")]
        [SerializeField] private bool _autoStartNight1OnPlay = true;

        public NightConfig[] NightConfigs => _nightConfigs;
        public EconomyConfig Economy => _economyConfig;

        protected override void Awake()
        {
            base.Awake();
        }

        private void Start()
        {
            GameEvents.OnNightStarted += HandleNightStarted;

            // Initialize Night Clock with Night 1 config if provided
            var n1Config = GetConfigForNight(1);
            if (n1Config != null && NightClock.HasInstance)
            {
                NightClock.Instance.Initialize(n1Config);
            }

            if (_autoStartNight1OnPlay)
            {
                StartCoroutine(AutoStartSequence());
            }
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
            GameEvents.OnNightStarted -= HandleNightStarted;
        }

        private void HandleNightStarted(int nightNumber)
        {
            var config = GetConfigForNight(nightNumber);
            if (config != null && NightClock.HasInstance)
            {
                NightClock.Instance.Initialize(config);
                Debug.Log($"[TylerlandBootstrap] Initialized Night {nightNumber} Clock with {config.realSecondsPerGameHour}s per hour.");
            }
        }

        /// <summary>
        /// Retrieves the NightConfig corresponding to a specific night (1 to 6).
        /// </summary>
        public NightConfig GetConfigForNight(int nightNumber)
        {
            if (_nightConfigs == null || _nightConfigs.Length == 0) return null;
            int index = Mathf.Clamp(nightNumber - 1, 0, _nightConfigs.Length - 1);
            return _nightConfigs[index];
        }

        private System.Collections.IEnumerator AutoStartSequence()
        {
            // Wait for GameFlowManager to boot into Menu
            yield return new WaitForSeconds(0.6f);

            if (GameFlowManager.Instance.CurrentState == GameState.Menu)
            {
                Debug.Log("[TylerlandBootstrap] Auto-starting Night 1 gameplay test...");
                GameFlowManager.Instance.StartNightRun();
            }
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            // Auto-populate configs in editor if array is empty
            if (_nightConfigs == null || _nightConfigs.Length == 0 || _nightConfigs[0] == null)
            {
                AutoFindAndAssignConfigs();
            }
        }

        [ContextMenu("Auto-Find & Assign Config Assets")]
        public void AutoFindAndAssignConfigs()
        {
            // Find Night configs
            List<NightConfig> list = new List<NightConfig>();
            for (int i = 1; i <= 6; i++)
            {
                var cfg = AssetDatabase.LoadAssetAtPath<NightConfig>($"Assets/ScriptableObjects/Nights/Night_{i}.asset");
                if (cfg != null)
                {
                    list.Add(cfg);
                }
            }

            if (list.Count > 0)
            {
                _nightConfigs = list.ToArray();
                Debug.Log($"[TylerlandBootstrap] Auto-assigned {list.Count} Night Configs!");
                EditorUtility.SetDirty(this);
            }

            // Find Economy config
            if (_economyConfig == null)
            {
                _economyConfig = AssetDatabase.LoadAssetAtPath<EconomyConfig>("Assets/ScriptableObjects/EconomyConfig.asset");
                if (_economyConfig != null)
                {
                    Debug.Log("[TylerlandBootstrap] Auto-assigned EconomyConfig!");
                    EditorUtility.SetDirty(this);
                }
            }
        }
#endif
    }
}
