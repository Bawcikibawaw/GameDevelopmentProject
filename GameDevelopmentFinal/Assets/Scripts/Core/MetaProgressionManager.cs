using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace Tylerland.Core
{
    /// <summary>
    /// Player settings that persist across all runs and sessions.
    /// </summary>
    [Serializable]
    public class PlayerSettingsData
    {
        [Range(0f, 1f)] public float masterVolume = 1f;
        [Range(0f, 1f)] public float sfxVolume = 1f;
        [Range(0f, 1f)] public float musicVolume = 0.8f;
        [Range(0.8f, 1.2f)] public float brightness = 1.0f;
        public bool subtitlesEnabled = true;
        public bool reduceFlashing = false;
        public bool screenShakeEnabled = true;
    }

    /// <summary>
    /// Meta progression state that survives player death and game restarts.
    /// Contains Soul Coins, permanent unlocks, statistics, and settings.
    /// </summary>
    [Serializable]
    public class MetaState
    {
        public int soulCoins = 0;
        public List<string> unlockedSoulItems = new List<string>();
        public int bestNightReached = 1;
        public int totalRuns = 0;
        public int totalDeaths = 0;
        public List<string> unlockedLoreFlags = new List<string>();
        public PlayerSettingsData settings = new PlayerSettingsData();
    }

    /// <summary>
    /// JSON serialization wrapper with save versioning for future-proof migrations.
    /// </summary>
    [Serializable]
    public class SaveDataWrapper
    {
        public int saveVersion = 1;
        public string saveTimestamp;
        public MetaState metaState;
    }

    /// <summary>
    /// Section 3.4: Meta Progression & Save/Load System.
    /// Manages persistent progress (Soul Coins, permanent unlocks, settings) saved to disk.
    /// Auto-saves at NightEnd, ShopExit, and RunReset. Never mid-night!
    /// </summary>
    public class MetaProgressionManager : Singleton<MetaProgressionManager>
    {
        private const int CURRENT_SAVE_VERSION = 1;
        private const string SAVE_FILE_NAME = "tylerland_meta.json";

        [Header("Meta State (Read Only)")]
        [SerializeField] private MetaState _metaState = new MetaState();

        public MetaState State => _metaState;
        public int SoulCoins => _metaState.soulCoins;
        public int BestNight => _metaState.bestNightReached;

        private string SaveFilePath => Path.Combine(Application.persistentDataPath, SAVE_FILE_NAME);

        protected override void Awake()
        {
            base.Awake();
            LoadFromDisk();
        }

        private void Start()
        {
            GameEvents.OnNightCompleted += HandleNightCompleted;
            GameEvents.OnPlayerDied += HandlePlayerDied;
            GameEvents.OnRunReset += HandleRunReset;
            GameEvents.OnGameStateChanged += HandleGameStateChanged;
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
            GameEvents.OnNightCompleted -= HandleNightCompleted;
            GameEvents.OnPlayerDied -= HandlePlayerDied;
            GameEvents.OnRunReset -= HandleRunReset;
            GameEvents.OnGameStateChanged -= HandleGameStateChanged;
        }

        #region Soul Coins & Unlocks
        public void AddSoulCoins(int amount)
        {
            if (amount <= 0) return;
            _metaState.soulCoins += amount;
            Debug.Log($"[MetaProgression] Added {amount} Soul Coins. Total={_metaState.soulCoins}");
            GameEvents.TriggerSoulCoinsChanged(_metaState.soulCoins, amount);
        }

        public bool SpendSoulCoins(int amount)
        {
            if (amount < 0 || _metaState.soulCoins < amount)
            {
                Debug.LogWarning($"[MetaProgression] Insufficient Soul Coins ({_metaState.soulCoins}) to spend {amount}.");
                return false;
            }

            _metaState.soulCoins -= amount;
            Debug.Log($"[MetaProgression] Spent {amount} Soul Coins. Remaining={_metaState.soulCoins}");
            GameEvents.TriggerSoulCoinsChanged(_metaState.soulCoins, -amount);
            return true;
        }

        public void UnlockSoulItem(string itemId)
        {
            if (!_metaState.unlockedSoulItems.Contains(itemId))
            {
                _metaState.unlockedSoulItems.Add(itemId);
                Debug.Log($"[MetaProgression] Permanently unlocked: {itemId}");
                SaveToDisk();
            }
        }

        public bool HasUnlockedSoulItem(string itemId)
        {
            return _metaState.unlockedSoulItems.Contains(itemId);
        }

        public void UnlockLoreFlag(string flag)
        {
            if (!_metaState.unlockedLoreFlags.Contains(flag))
            {
                _metaState.unlockedLoreFlags.Add(flag);
                Debug.Log($"[MetaProgression] Discovered lore flag: {flag}");
            }
        }
        #endregion

        #region Save / Load Serialization
        /// <summary>
        /// Serializes MetaState to JSON and writes to Application.persistentDataPath.
        /// </summary>
        public void SaveToDisk()
        {
            try
            {
                var wrapper = new SaveDataWrapper
                {
                    saveVersion = CURRENT_SAVE_VERSION,
                    saveTimestamp = DateTime.UtcNow.ToString("o"),
                    metaState = _metaState
                };

                string json = JsonUtility.ToJson(wrapper, true);
                File.WriteAllText(SaveFilePath, json);
                Debug.Log($"[MetaProgression] Saved meta data to: {SaveFilePath}");
                GameEvents.TriggerMetaSaved();
            }
            catch (Exception ex)
            {
                Debug.LogError($"[MetaProgression] Failed to save meta progress: {ex.Message}");
            }
        }

        /// <summary>
        /// Loads MetaState from JSON file on disk, handling version migrations if required.
        /// </summary>
        public void LoadFromDisk()
        {
            try
            {
                if (!File.Exists(SaveFilePath))
                {
                    Debug.Log("[MetaProgression] No existing save file found. Initialized with fresh MetaState.");
                    _metaState = new MetaState();
                    return;
                }

                string json = File.ReadAllText(SaveFilePath);
                var wrapper = JsonUtility.FromJson<SaveDataWrapper>(json);

                if (wrapper != null && wrapper.metaState != null)
                {
                    // Version migration check
                    if (wrapper.saveVersion < CURRENT_SAVE_VERSION)
                    {
                        Debug.Log($"[MetaProgression] Migrating save from v{wrapper.saveVersion} to v{CURRENT_SAVE_VERSION}");
                        // Perform any future migrations here
                        wrapper.saveVersion = CURRENT_SAVE_VERSION;
                    }

                    _metaState = wrapper.metaState;
                    Debug.Log($"[MetaProgression] Loaded save data successfully. Soul Coins={_metaState.soulCoins}, Best Night={_metaState.bestNightReached}");
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[MetaProgression] Error loading save data: {ex.Message}. Using default state.");
                _metaState = new MetaState();
            }
        }

        public void DeleteSaveFile()
        {
            if (File.Exists(SaveFilePath))
            {
                File.Delete(SaveFilePath);
                _metaState = new MetaState();
                Debug.Log("[MetaProgression] Save file deleted.");
            }
        }
        #endregion

        #region Autosave Handlers (Roadmap 3.4)
        private void HandleNightCompleted(int night)
        {
            // Update best night reached
            if (night > _metaState.bestNightReached)
            {
                _metaState.bestNightReached = night;
            }

            // Award Soul Coins based on animatronic damage dealt (up to night cap)
            if (RunManager.HasInstance)
            {
                int damageDealt = RunManager.Instance.State.GetTotalDamageDealt();
                int coins = Mathf.Min(damageDealt, 3); // 3 cap per night
                if (coins > 0)
                {
                    AddSoulCoins(coins);
                }
            }

            SaveToDisk();
        }

        private void HandlePlayerDied(string killer)
        {
            _metaState.totalDeaths++;
            SaveToDisk();
        }

        private void HandleRunReset()
        {
            _metaState.totalRuns++;
            SaveToDisk();
        }

        private void HandleGameStateChanged(GameState oldState, GameState newState)
        {
            // Autosave triggers: NightEnd, ShopExit, RunReset
            if (oldState == GameState.Shop && newState != GameState.Shop)
            {
                SaveToDisk();
            }
        }

        protected override void OnApplicationQuit()
        {
            base.OnApplicationQuit();
            SaveToDisk();
        }
        #endregion
    }
}
