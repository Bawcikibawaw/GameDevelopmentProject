using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using Tylerland.Core;
using Tylerland.Config;
using Tylerland.Office;

namespace Tylerland.Animatronics
{
    /// <summary>
    /// Central Coordinator for all Animatronic Systems (Section 5.1 - 5.7).
    /// Spawns and initializes Tyler, Shark, Eagle, Snake, and the Night 6 Boss for each shift,
    /// updates AI scaling per hour, answers room occupancy queries for the Security Tablet,
    /// and provides quick playtest hotkeys ('T' to step AI, 'B' to summon to faced doorway).
    /// </summary>
    public class AnimatronicManager : Singleton<AnimatronicManager>
    {
        [Header("Cast Config Assets (Tyler, Shark, Eagle, Snake, Boss)")]
        [SerializeField] private AnimatronicConfig _tylerConfig;
        [SerializeField] private AnimatronicConfig _sharkConfig;
        [SerializeField] private AnimatronicConfig _eagleConfig;
        [SerializeField] private AnimatronicConfig _snakeConfig;
        [SerializeField] private AnimatronicConfig _bossConfig;

        [Header("Active Runtime Units (Read Only)")]
        [SerializeField] private List<AnimatronicUnit> _units = new List<AnimatronicUnit>();
        private BossController _bossController;

        public IReadOnlyList<AnimatronicUnit> Units => _units;

        protected override void Awake()
        {
            base.Awake();
            EnsureDefaultConfigsIfMissing();
            CreateUnitObjects();
        }

        private void Start()
        {
            GameEvents.OnNightStarted += HandleNightStarted;
            GameEvents.OnHourChanged += HandleHourChanged;
            GameEvents.OnRunReset += HandleRunReset;

            int currentNight = RunManager.HasInstance ? RunManager.Instance.CurrentNight : 1;
            InitializeForNight(currentNight);
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
            GameEvents.OnNightStarted -= HandleNightStarted;
            GameEvents.OnHourChanged -= HandleHourChanged;
            GameEvents.OnRunReset -= HandleRunReset;
        }

        private void Update()
        {
            var flowState = GameFlowManager.HasInstance ? GameFlowManager.Instance.CurrentState : GameState.NightActive;
            if (!GameStateRules.IsGameplayActive(flowState) || Time.timeScale <= 0f) return;

            var kb = Keyboard.current;
            if (kb == null) return;

            // 'T' Key (Testing): Advance all active animatronics by 1 stage immediately
            if (kb.tKey.wasPressedThisFrame)
            {
                StepAllActiveAnimatronics();
            }

            // 'B' Key (Testing): Summon the corresponding animatronic directly to the currently faced doorway blindspot!
            if (kb.bKey.wasPressedThisFrame)
            {
                int facedDoor = PlayerViewController.HasInstance
                    ? (int)PlayerViewController.Instance.CurrentDirection
                    : 1;
                SummonToDoorBlindspot(facedDoor);
            }
        }

        private void EnsureDefaultConfigsIfMissing()
        {
            if (_tylerConfig == null)
            {
                _tylerConfig = CreateRuntimeConfig(
                    "tyler", "Tyler", new Color(0.90f, 0.25f, 0.20f),
                    targetDoor: 1, // Center Door
                    route: new int[] { 0, 1, 7, 1 }, // Stage -> Dining -> Backstage -> Dining -> Center Door
                    interval: 4.8f, window: 3.8f, weapon: "Shears",
                    behavior: CameraStallBehavior.StalledWhenWatchedOnCamera
                );
            }

            if (_sharkConfig == null)
            {
                _sharkConfig = CreateRuntimeConfig(
                    "shark", "Shark", new Color(0.20f, 0.65f, 0.95f),
                    targetDoor: 0, // Left Door
                    route: new int[] { 4, 4, 2, 3 }, // Pirate Cove -> Peeking -> West Hall -> West Corner -> Left Door
                    interval: 5.0f, window: 3.2f, weapon: "Harpoon",
                    behavior: CameraStallBehavior.StalledWhenWatchedOnCamera
                );
            }

            if (_eagleConfig == null)
            {
                _eagleConfig = CreateRuntimeConfig(
                    "eagle", "Eagle", new Color(0.95f, 0.75f, 0.15f),
                    targetDoor: 2, // Right Door
                    route: new int[] { 0, 1, 8, 5, 6 }, // Stage -> Dining -> Kitchen -> East Hall -> East Corner -> Right Door
                    interval: 3.4f, window: 2.8f, weapon: "Net",
                    behavior: CameraStallBehavior.None
                );
            }

            if (_snakeConfig == null)
            {
                _snakeConfig = CreateRuntimeConfig(
                    "snake", "Snake", new Color(0.30f, 0.85f, 0.35f),
                    targetDoor: 3, // Back Door
                    route: new int[] { 7, 8, 9, 9 }, // Backstage -> Kitchen -> Service Vent Far -> Service Vent Near -> Back Door
                    interval: 4.3f, window: 3.5f, weapon: "Prod",
                    behavior: CameraStallBehavior.ManipulatesCameraFeeds
                );
            }

            if (_bossConfig == null)
            {
                _bossConfig = CreateRuntimeConfig(
                    "boss", "The Amalgam (Boss)", new Color(0.75f, 0.15f, 0.90f),
                    targetDoor: 1,
                    route: new int[] { 0, 1, 2, 5, 9 },
                    interval: 3.1f, window: 2.6f, weapon: "Shears",
                    behavior: CameraStallBehavior.ManipulatesCameraFeeds
                );
            }
        }

        private AnimatronicConfig CreateRuntimeConfig(
            string id, string displayName, Color color,
            int targetDoor, int[] route, float interval, float window,
            string weapon, CameraStallBehavior behavior)
        {
            var cfg = ScriptableObject.CreateInstance<AnimatronicConfig>();
            cfg.animatronicId = id;
            cfg.displayName = displayName;
            cfg.themeColor = color;
            cfg.targetDoorIndex = targetDoor;
            cfg.cameraRouteIndices = route;
            cfg.retreatStageIndex = 1;
            cfg.baseTickInterval = interval;
            cfg.lateNightAIRamp = 3;
            cfg.attackWindowSeconds = window;
            cfg.counterWeaponId = weapon;
            cfg.cameraBehavior = behavior;
            return cfg;
        }

        private void CreateUnitObjects()
        {
            if (_units.Count > 0) return;

            AnimatronicConfig[] configs = { _tylerConfig, _sharkConfig, _eagleConfig, _snakeConfig, _bossConfig };
            foreach (var cfg in configs)
            {
                if (cfg == null) continue;
                var unitObj = new GameObject($"Animatronic_{cfg.displayName}");
                unitObj.transform.SetParent(transform, false);
                var unit = unitObj.AddComponent<AnimatronicUnit>();
                unit.Initialize(cfg, 0);
                _units.Add(unit);
            }

            _bossController = gameObject.GetComponent<BossController>();
            if (_bossController == null) _bossController = gameObject.AddComponent<BossController>();
        }

        private void HandleNightStarted(int nightNumber)
        {
            InitializeForNight(nightNumber);
        }

        private void HandleRunReset()
        {
            InitializeForNight(1);
        }

        private void HandleHourChanged(int hour, float hourBlendFactor)
        {
            foreach (var u in _units)
            {
                if (u != null) u.RecalculateEffectiveAI(hourBlendFactor);
            }
        }

        /// <summary>
        /// Configures all animatronic AI levels for the given night number using NightConfig.
        /// </summary>
        public void InitializeForNight(int nightNumber)
        {
            if (AttackResolver.HasInstance)
            {
                AttackResolver.Instance.ClearAllThreats();
            }

            NightConfig nightCfg = null;
            if (TylerlandBootstrap.HasInstance)
            {
                nightCfg = TylerlandBootstrap.Instance.GetConfigForNight(nightNumber);
            }

            // Default AI table from Roadmap 8.1 if NightConfig is unassigned
            int tylerAI = nightCfg != null ? nightCfg.tylerAILevel : Mathf.Clamp(nightNumber * 2, 2, 12);
            int sharkAI = nightCfg != null ? nightCfg.sharkAILevel : (nightNumber >= 2 ? nightNumber * 2 : 0);
            int eagleAI = nightCfg != null ? nightCfg.eagleAILevel : (nightNumber >= 3 ? nightNumber * 2 : 0);
            int snakeAI = nightCfg != null ? nightCfg.snakeAILevel : (nightNumber >= 4 ? nightNumber * 2 : 0);
            bool bossActive = nightCfg != null ? nightCfg.bossActive : (nightNumber >= 6);

            GetAnimatronic("tyler")?.Initialize(_tylerConfig, tylerAI);
            GetAnimatronic("shark")?.Initialize(_sharkConfig, sharkAI);
            GetAnimatronic("eagle")?.Initialize(_eagleConfig, eagleAI);
            GetAnimatronic("snake")?.Initialize(_snakeConfig, snakeAI);

            var bossUnit = GetAnimatronic("boss");
            if (bossUnit != null)
            {
                bossUnit.Initialize(_bossConfig, bossActive ? 14 : 0);
                if (_bossController != null)
                {
                    _bossController.InitializeBoss(bossUnit, bossActive);
                }
            }

            Debug.Log($"[AnimatronicManager] Initialized Night {nightNumber} AI -> Tyler:{tylerAI}, Shark:{sharkAI}, Eagle:{eagleAI}, Snake:{snakeAI}, Boss:{bossActive}");
        }

        #region Queries & Helpers
        public AnimatronicUnit GetAnimatronic(string id)
        {
            if (string.IsNullOrEmpty(id)) return null;
            foreach (var u in _units)
            {
                if (u != null && string.Equals(u.AnimatronicId, id, StringComparison.OrdinalIgnoreCase))
                {
                    return u;
                }
            }
            return null;
        }

        /// <summary>
        /// Returns all active animatronics currently occupying the specified camera room index (0-9).
        /// </summary>
        public List<AnimatronicUnit> GetAnimatronicsInCameraRoom(int cameraRoomIndex)
        {
            List<AnimatronicUnit> present = new List<AnimatronicUnit>();
            foreach (var u in _units)
            {
                if (u != null && u.EffectiveAILevel > 0 && !u.IsAtDoorBlindspot && u.CurrentCameraRoomIndex == cameraRoomIndex)
                {
                    present.Add(u);
                }
            }
            return present;
        }

        /// <summary>
        /// Returns the animatronic currently lurking in the specified doorway blindspot (0=Left, 1=Center, 2=Right, 3=Back), or null.
        /// </summary>
        public AnimatronicUnit GetAnimatronicAtDoorBlindspot(int doorIndex)
        {
            foreach (var u in _units)
            {
                if (u != null && u.IsAtDoorBlindspot && u.TargetDoorIndex == doorIndex)
                {
                    return u;
                }
            }
            return null;
        }

        /// <summary>
        /// Testing Helper ('T' key): Forces all active animatronics to advance 1 stage along their route.
        /// </summary>
        public void StepAllActiveAnimatronics()
        {
            foreach (var u in _units)
            {
                if (u != null && u.EffectiveAILevel > 0 && !u.IsAtDoorBlindspot)
                {
                    u.AdvanceStage();
                }
            }
        }

        /// <summary>
        /// Testing Helper ('B' key): Summons the animatronic assigned to the given door directly into its doorway blindspot!
        /// </summary>
        public void SummonToDoorBlindspot(int doorIndex)
        {
            string[] doorAnimIds = { "shark", "tyler", "eagle", "snake" };
            int d = Mathf.Clamp(doorIndex, 0, 3);
            var unit = GetAnimatronic(doorAnimIds[d]);
            if (unit == null) return;

            // Ensure unit is active if summoned for testing
            if (unit.EffectiveAILevel <= 0)
            {
                unit.Initialize(unit.Config, 5);
            }

            while (!unit.IsAtDoorBlindspot)
            {
                int prev = unit.CurrentStage;
                unit.AdvanceStage();
                if (unit.CurrentStage == prev) break;
            }
        }
        #endregion
    }
}
