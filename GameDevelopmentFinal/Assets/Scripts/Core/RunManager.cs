using System;
using System.Collections.Generic;
using UnityEngine;
using Tylerland.Config;

namespace Tylerland.Core
{
    /// <summary>
    /// Per-night performance statistics recorded at the end of each shift.
    /// </summary>
    [Serializable]
    public struct NightHistoryEntry
    {
        public int nightNumber;
        public int damageDealt;
        public float powerUsed;
        public int doorsClosed;
        public bool survived;

        public NightHistoryEntry(int night, int damage, float power, int doors, bool survived)
        {
            this.nightNumber = night;
            this.damageDealt = damage;
            this.powerUsed = power;
            this.doorsClosed = doors;
            this.survived = survived;
        }
    }

    /// <summary>
    /// Run state holding all roguelite data for the current active run.
    /// Crucial architecture rule: RunState is wiped on death / reset!
    /// </summary>
    [Serializable]
    public class RunState
    {
        public int currentNight = 1;
        public int money = 0;
        public List<string> upgrades = new List<string>();
        public List<string> weaponsOwned = new List<string>();
        public List<string> weaponsUsed = new List<string>();
        
        // Animatronic damage levels (0 = Intact, 1 = Cracked, 2 = Broken, 3 = Critical)
        public int damageTyler = 0;
        public int damageShark = 0;
        public int damageEagle = 0;
        public int damageSnake = 0;

        // Night on which each animatronic was shot (0 = never shot, 1..6 = permanent Nightmare since that night)
        public int shotNightTyler = 0;
        public int shotNightShark = 0;
        public int shotNightEagle = 0;
        public int shotNightSnake = 0;

        public List<NightHistoryEntry> nightHistory = new List<NightHistoryEntry>();
        public int rngSeed;

        public void Reset(int newSeed)
        {
            currentNight = 1;
            money = 0;
            upgrades.Clear();
            weaponsOwned.Clear();
            weaponsUsed.Clear();
            damageTyler = 0;
            damageShark = 0;
            damageEagle = 0;
            damageSnake = 0;
            shotNightTyler = 0;
            shotNightShark = 0;
            shotNightEagle = 0;
            shotNightSnake = 0;
            nightHistory.Clear();
            rngSeed = newSeed;
        }

        public int GetTotalDamageDealt()
        {
            return damageTyler + damageShark + damageEagle + damageSnake;
        }

        public int GetAnimatronicDamage(string animatronicId)
        {
            if (string.IsNullOrEmpty(animatronicId)) return 0;
            switch (animatronicId.ToLower())
            {
                case "tyler": return damageTyler;
                case "orville":
                case "shark": return damageShark;
                case "erle":
                case "eagle": return damageEagle;
                case "sandy":
                case "snake": return damageSnake;
                default: return 0;
            }
        }

        public void SetAnimatronicDamage(string animatronicId, int level)
        {
            if (string.IsNullOrEmpty(animatronicId)) return;
            level = Mathf.Clamp(level, 0, 3);
            switch (animatronicId.ToLower())
            {
                case "tyler": damageTyler = level; break;
                case "orville":
                case "shark": damageShark = level; break;
                case "erle":
                case "eagle": damageEagle = level; break;
                case "sandy":
                case "snake": damageSnake = level; break;
            }
        }

        public bool IsNightmareState(string animatronicId)
        {
            return GetAnimatronicDamage(animatronicId) > 0 || GetShotNight(animatronicId) > 0;
        }

        public int GetShotNight(string animatronicId)
        {
            if (string.IsNullOrEmpty(animatronicId)) return 0;
            switch (animatronicId.ToLower())
            {
                case "tyler": return shotNightTyler;
                case "orville":
                case "shark": return shotNightShark;
                case "erle":
                case "eagle": return shotNightEagle;
                case "sandy":
                case "snake": return shotNightSnake;
                default: return 0;
            }
        }

        public void MarkShotOnNight(string animatronicId, int nightNumber)
        {
            if (string.IsNullOrEmpty(animatronicId)) return;
            nightNumber = Mathf.Max(1, nightNumber);
            switch (animatronicId.ToLower())
            {
                case "tyler":
                    if (shotNightTyler <= 0) shotNightTyler = nightNumber;
                    if (damageTyler <= 0) damageTyler = 1;
                    break;
                case "orville":
                case "shark":
                    if (shotNightShark <= 0) shotNightShark = nightNumber;
                    if (damageShark <= 0) damageShark = 1;
                    break;
                case "erle":
                case "eagle":
                    if (shotNightEagle <= 0) shotNightEagle = nightNumber;
                    if (damageEagle <= 0) damageEagle = 1;
                    break;
                case "sandy":
                case "snake":
                    if (shotNightSnake <= 0) shotNightSnake = nightNumber;
                    if (damageSnake <= 0) damageSnake = 1;
                    break;
            }
        }

        /// <summary>
        /// Permanent Nightmare escalation bonus:
        /// Once shot on night S, the animatronic stays in permanent Nightmare Mode for the entire 5-night cycle
        /// and its aggressiveness grows drastically faster every subsequent night (+2 same night, +4, +7, +10, +13...).
        /// </summary>
        public int GetNightmareNightBonus(string animatronicId, int currentNight)
        {
            int shotNight = GetShotNight(animatronicId);
            if (shotNight <= 0)
            {
                return IsNightmareState(animatronicId) ? 4 : 0;
            }
            int nightsSinceShot = Mathf.Max(0, currentNight - shotNight);
            return nightsSinceShot == 0 ? 2 : (1 + nightsSinceShot * 3);
        }

        public bool HasUsedWeaponThisRun(string weaponId)
        {
            return !string.IsNullOrEmpty(weaponId) && weaponsUsed.Contains(weaponId);
        }

        /// <summary>
        /// GDD §4.1.2 & §4.7 True Ending Rule:
        /// Returns true if all FOUR regular animatronics (Tyler, Orville, Sandy, Erle)
        /// have been transformed into their Nightmare forms within the SAME run.
        /// </summary>
        public bool AllFourNightmaresActive =>
            IsNightmareState("tyler") && IsNightmareState("orville") && IsNightmareState("erle") && IsNightmareState("sandy");
    }

    /// <summary>
    /// Section 3.3: Run Manager (Roguelite Layer).
    /// Holds the active run data and manages death wipe vs night progression.
    /// </summary>
    public class RunManager : Singleton<RunManager>
    {
        [Header("Run Configuration")]
        [Tooltip("Standard 5-night shift cycle (extends to Night 6 True Boss if all 4 Nightmares are active)")]
        [SerializeField] private int _totalNights = 5;

        [Tooltip("Default Economy config for salary and repair calculations")]
        [SerializeField] private EconomyConfig _economyConfig;

        [Header("Current Run State (Read Only)")]
        [SerializeField] private RunState _runState = new RunState();

        public RunState State => _runState;
        public int CurrentNight => _runState.currentNight;
        public int TotalNights => _runState.AllFourNightmaresActive ? Mathf.Max(_totalNights, 6) : _totalNights;
        public int Money => _runState.money;
        public EconomyConfig Economy => _economyConfig;

        protected override void Awake()
        {
            base.Awake();
            if (_runState.rngSeed == 0)
            {
                StartNewRun();
            }
        }

        private void Start()
        {
            GameEvents.OnNightCompleted += HandleNightCompleted;
            GameEvents.OnPlayerDied += HandlePlayerDied;
            GameEvents.OnRunReset += HandleRunReset;
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
            GameEvents.OnNightCompleted -= HandleNightCompleted;
            GameEvents.OnPlayerDied -= HandlePlayerDied;
            GameEvents.OnRunReset -= HandleRunReset;
        }

        #region Run Controls
        /// <summary>
        /// Begins a brand new run from Night 1, resetting all run currency, damage, and Nightmare states,
        /// while restoring 1 use of each permanently unlocked special weapon (GDD §4.1.2 & §4.7).
        /// </summary>
        public void StartNewRun()
        {
            int seed = UnityEngine.Random.Range(100000, 999999);
            _runState.Reset(seed);
            UnityEngine.Random.InitState(seed);

            // Restore 1 use of every permanently unlocked special weapon from MetaProgressionManager
            if (MetaProgressionManager.HasInstance)
            {
                string[] allWeapons = { "Electric Gun", "Shotgun", "Harpoon", "Spiked Mace" };
                foreach (var w in allWeapons)
                {
                    if (MetaProgressionManager.Instance.HasUnlockedSoulItem(w))
                    {
                        _runState.weaponsOwned.Add(w);
                    }
                }

                // Permanent Soul Coin Shop bonus: starting shift stipend
                if (MetaProgressionManager.Instance.HasUnlockedSoulItem("SOUL_START_CASH"))
                {
                    _runState.money += 100;
                }
            }

            Debug.Log($"[RunManager] New 5-night run started. RNG Seed={seed}, Night={_runState.currentNight}, Restored Weapons={_runState.weaponsOwned.Count}, Cash=${_runState.money}");
        }

        /// <summary>
        /// Advances to the next night after salary and between-nights hub phases.
        /// </summary>
        public void AdvanceToNextNight()
        {
            if (_runState.currentNight >= TotalNights)
            {
                Debug.Log("[RunManager] Final Night survived! Run victory achieved!");
                GameEvents.TriggerRunWon();
                if (GameFlowManager.HasInstance)
                {
                    GameFlowManager.Instance.TransitionTo(GameState.Victory);
                }
                return;
            }

            _runState.currentNight++;
            Debug.Log($"[RunManager] Advancing to Night {_runState.currentNight} / {TotalNights}");
            GameEvents.TriggerNightStarted(_runState.currentNight);
        }
        #endregion

        #region Economy Operations
        public void AddMoney(int amount)
        {
            if (amount <= 0) return;
            _runState.money += amount;
            Debug.Log($"[RunManager] Added ${amount}. Total Money=${_runState.money}");
            GameEvents.TriggerMoneyChanged(_runState.money, amount);
        }

        public bool SpendMoney(int amount)
        {
            if (amount < 0 || _runState.money < amount)
            {
                Debug.LogWarning($"[RunManager] Cannot spend ${amount}. Current balance=${_runState.money}");
                return false;
            }

            _runState.money -= amount;
            Debug.Log($"[RunManager] Spent ${amount}. Remaining Money=${_runState.money}");
            GameEvents.TriggerMoneyChanged(_runState.money, -amount);
            return true;
        }

        public void AddUpgrade(string upgradeId)
        {
            if (!_runState.upgrades.Contains(upgradeId))
            {
                _runState.upgrades.Add(upgradeId);
                Debug.Log($"[RunManager] Acquired upgrade: {upgradeId}");
            }
        }

        public bool HasUpgrade(string upgradeId)
        {
            return _runState.upgrades.Contains(upgradeId);
        }

        public void AddWeapon(string weaponId)
        {
            _runState.weaponsOwned.Add(weaponId);
            Debug.Log($"[RunManager] Acquired weapon: {weaponId}");
        }

        public bool UseWeapon(string weaponId)
        {
            if (_runState.weaponsOwned.Remove(weaponId))
            {
                _runState.weaponsUsed.Add(weaponId);
                Debug.Log($"[RunManager] Expended one-use weapon: {weaponId}");
                return true;
            }
            return false;
        }

        public void InflictDamage(string animatronicId, int levels = 1)
        {
            int current = _runState.GetAnimatronicDamage(animatronicId);
            int updated = Mathf.Clamp(current + levels, 0, 3);
            _runState.SetAnimatronicDamage(animatronicId, updated);
            Debug.Log($"[RunManager] {animatronicId} damage increased to level {updated}/3");
        }

        public void MarkAnimatronicShot(string animatronicId, int nightNumber)
        {
            _runState.MarkShotOnNight(animatronicId, nightNumber);
            Debug.LogWarning($"[RunManager] {animatronicId} SHOT on Night {nightNumber}! PERMANENT NIGHTMARE MODE active for remainder of this 5-night cycle.");
        }
        #endregion

        #region Event Callbacks
        private void HandleNightCompleted(int night)
        {
            int damageThisNight = _runState.GetTotalDamageDealt();

            // Calculate salary if Economy config is assigned
            SalaryBreakdown breakdown;
            if (_economyConfig != null)
            {
                breakdown = _economyConfig.CalculateSalary(night, damageThisNight);
            }
            else
            {
                // Fallback default
                int @base = 100 + (night - 1) * 50;
                breakdown = new SalaryBreakdown(night, @base, 0, 0, @base);
            }

            AddMoney(breakdown.totalSalary);
            GameEvents.TriggerSalaryCalculated(breakdown);

            // Record history
            _runState.nightHistory.Add(new NightHistoryEntry(night, damageThisNight, 0f, 0, true));
        }

        private void HandlePlayerDied(string killer)
        {
            _runState.nightHistory.Add(new NightHistoryEntry(_runState.currentNight, 0, 0f, 0, false));
        }

        private void HandleRunReset()
        {
            Debug.Log("[RunManager] Death / Reset event received. Clearing RunState!");
            StartNewRun();
        }
        #endregion
    }
}
