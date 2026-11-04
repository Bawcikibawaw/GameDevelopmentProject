using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using Tylerland.Core;
using Tylerland.Config;
using Tylerland.Office;

namespace Tylerland.Animatronics
{
    /// <summary>
    /// Section 5.1 & 5.2 (Updated for Section 4 Animatronic GDD):
    /// Central Coordinator for the 4 Main Animatronics + True Final Boss:
    /// - Door 0 (LEFT / CAM 01):   Erle the Eagle (Sheriff, Active Night 3, 5 stages, Electric Gun)
    /// - Door 1 (CENTER / CAM 02): Tyler the Lion (Lead Singer, Passive Nights 1-3, Attacks Night 4, 4 stages, Shotgun)
    /// - Door 2 (RIGHT / CAM 03):  Orville the Orca (Medical Attendant, Active Night 2, 6 stages, Harpoon)
    /// - Door 3 (REAR / CAM 04):   Sandy the Snake (Maintenance Mascot, Active Night 1, 4 stages Normal / 5 Nightmare, Spiked Mace)
    /// </summary>
    public class AnimatronicManager : Singleton<AnimatronicManager>
    {
        [Header("Character Configurations (GDD Section 4)")]
        [Tooltip("Door 1 (Center / CAM 02): Tyler the Lion")]
        [SerializeField] private AnimatronicConfig _tylerConfig;

        [Tooltip("Door 2 (Right / CAM 03): Orville the Orca")]
        [SerializeField] private AnimatronicConfig _sharkConfig;

        [Tooltip("Door 0 (Left / CAM 01): Erle the Eagle")]
        [SerializeField] private AnimatronicConfig _eagleConfig;

        [Tooltip("Door 3 (Rear / CAM 04): Sandy the Snake")]
        [SerializeField] private AnimatronicConfig _snakeConfig;

        [Tooltip("True Final Boss (Requires all 4 Nightmares active in the same run)")]
        [SerializeField] private AnimatronicConfig _bossConfig;

        [Header("Runtime Units (Read Only)")]
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
            var kb = Keyboard.current;
            if (kb == null) return;

            // 'T' Key (Testing): Advance all active animatronics by +1 Stage
            if (kb.tKey.wasPressedThisFrame)
            {
                StepAllActiveAnimatronics();
            }

            // 'B' Key (Testing): Summon the corresponding animatronic directly to the currently faced doorway!
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
            // 1. Tyler the Lion (Center Door 1 / CAM 02) - Passive Nights 1-3, Attacks Night 4+, 4 stages, Shotgun
            if (_tylerConfig == null) _tylerConfig = ScriptableObject.CreateInstance<AnimatronicConfig>();
            ApplyGDDConfig(
                _tylerConfig,
                id: "tyler", displayName: "Tyler the Lion", role: "Lead singer / central mascot",
                color: new Color(0.88f, 0.22f, 0.20f),
                targetDoor: 1, activeFromNight: 4, passiveBeforeActive: true,
                normalStages: 4, nightmareStages: 4, retreatStage: 0,
                normalWindow: 7.0f, nightmareWindow: 6.0f,
                extraDuraLoss: 2.5f, normalEnergyDrain: 0f, nightmareEnergyDrain: 8f,
                interval: 5.2f, weapon: "Shotgun",
                behavior: CameraStallBehavior.TylerDestroysCameraInNightmare
            );

            // 2. Orville the Orca (Right Door 2 / CAM 03) - Active Night 2+, 4 stages, Harpoon
            if (_sharkConfig == null) _sharkConfig = ScriptableObject.CreateInstance<AnimatronicConfig>();
            ApplyGDDConfig(
                _sharkConfig,
                id: "orville", displayName: "Orville the Orca", role: "Medical attendant",
                color: new Color(0.20f, 0.68f, 0.85f),
                targetDoor: 2, activeFromNight: 2, passiveBeforeActive: false,
                normalStages: 4, nightmareStages: 4, retreatStage: 0,
                normalWindow: 8.0f, nightmareWindow: 7.0f,
                extraDuraLoss: 1.5f, normalEnergyDrain: 6f, nightmareEnergyDrain: 12f,
                interval: 5.0f, weapon: "Harpoon",
                behavior: CameraStallBehavior.OrvilleProbabilisticAndReversal
            );

            // 3. Erle the Eagle (Left Door 0 / CAM 01) - Active Night 3+, 4 stages, Electric Gun
            if (_eagleConfig == null) _eagleConfig = ScriptableObject.CreateInstance<AnimatronicConfig>();
            ApplyGDDConfig(
                _eagleConfig,
                id: "erle", displayName: "Erle the Eagle", role: "Sheriff / security mascot",
                color: new Color(0.92f, 0.72f, 0.18f),
                targetDoor: 0, activeFromNight: 3, passiveBeforeActive: false,
                normalStages: 4, nightmareStages: 4, retreatStage: 0,
                normalWindow: 7.5f, nightmareWindow: 6.0f,
                extraDuraLoss: 1.5f, normalEnergyDrain: 0f, nightmareEnergyDrain: 8f,
                interval: 5.2f, weapon: "Electric Gun",
                behavior: CameraStallBehavior.ErleScannerAndGoodBadCop
            );

            // 4. Sandy the Snake (Rear Door 3 / CAM 04) - Active Night 1+, 4 stages, Spiked Mace
            if (_snakeConfig == null) _snakeConfig = ScriptableObject.CreateInstance<AnimatronicConfig>();
            ApplyGDDConfig(
                _snakeConfig,
                id: "sandy", displayName: "Sandy the Snake", role: "Maintenance / repair mascot",
                color: new Color(0.32f, 0.82f, 0.35f),
                targetDoor: 3, activeFromNight: 1, passiveBeforeActive: false,
                normalStages: 4, nightmareStages: 4, retreatStage: 0,
                normalWindow: 8.0f, nightmareWindow: 6.0f,
                extraDuraLoss: 1.5f, normalEnergyDrain: 0f, nightmareEnergyDrain: 12f,
                interval: 4.8f, weapon: "Spiked Mace",
                behavior: CameraStallBehavior.SandyNightmareDecoys
            );

            // 5. True Final Boss (Unlocked when all 4 Nightmares are triggered in the same run!)
            if (_bossConfig == null) _bossConfig = ScriptableObject.CreateInstance<AnimatronicConfig>();
            ApplyGDDConfig(
                _bossConfig,
                id: "boss", displayName: "The Amalgam (True Final Boss)", role: "True Ending Culmination",
                color: new Color(0.75f, 0.15f, 0.90f),
                targetDoor: 1, activeFromNight: 6, passiveBeforeActive: false,
                normalStages: 4, nightmareStages: 4, retreatStage: 1,
                normalWindow: 6.0f, nightmareWindow: 5.5f,
                extraDuraLoss: 3.5f, normalEnergyDrain: 8f, nightmareEnergyDrain: 12f,
                interval: 4.5f, weapon: "Shotgun",
                behavior: CameraStallBehavior.None
            );
        }

        private void ApplyGDDConfig(
            AnimatronicConfig cfg,
            string id, string displayName, string role, Color color,
            int targetDoor, int activeFromNight, bool passiveBeforeActive,
            int normalStages, int nightmareStages, int retreatStage,
            float normalWindow, float nightmareWindow,
            float extraDuraLoss, float normalEnergyDrain, float nightmareEnergyDrain,
            float interval, string weapon, CameraStallBehavior behavior)
        {
            cfg.animatronicId = id;
            cfg.displayName = displayName;
            cfg.formerRole = role;
            cfg.themeColor = color;
            cfg.targetDoorIndex = targetDoor;
            cfg.activeFromNight = activeFromNight;
            cfg.passiveObserverBeforeActiveNight = passiveBeforeActive;
            cfg.normalStageCount = normalStages;
            cfg.nightmareStageCount = nightmareStages;
            cfg.retreatStageIndex = retreatStage;
            cfg.attackWindowSeconds = normalWindow;
            cfg.nightmareAttackWindowSeconds = nightmareWindow;
            cfg.extraDurabilityLossOnBlock = extraDuraLoss;
            cfg.energyDrainOnNormalBlock = normalEnergyDrain;
            cfg.energyDrainOnNightmareBlock = nightmareEnergyDrain;
            cfg.baseTickInterval = interval;
            cfg.lateNightAIRamp = 3;
            cfg.counterWeaponId = weapon;
            cfg.cameraBehavior = behavior;
        }

        private void CreateUnitObjects()
        {
            if (_units.Count > 0) return;

            // Ordered by Door 0..3 + Boss:
            // Door 0 (Left): Erle | Door 1 (Center): Tyler | Door 2 (Right): Orville | Door 3 (Rear): Sandy
            AnimatronicConfig[] configs = { _eagleConfig, _tylerConfig, _sharkConfig, _snakeConfig, _bossConfig };
            foreach (var cfg in configs)
            {
                if (cfg == null) continue;
                var unitObj = new GameObject($"Animatronic_{cfg.displayName}");
                unitObj.transform.SetParent(transform, false);
                var unit = unitObj.AddComponent<AnimatronicUnit>();
                unit.Initialize(cfg, 0, 1);
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
        /// Configures all animatronic AI levels for the given night number according to GDD §4.1:
        /// - Night 1: Sandy the Snake active; Tyler passive observer on CAM 02.
        /// - Night 2: Sandy + Orville the Orca active; Tyler passive observer.
        /// - Night 3: Sandy + Orville + Erle the Eagle active; Tyler passive observer.
        /// - Night 4+: All 4 (Tyler, Orville, Sandy, Erle) active and attacking!
        /// - True Final Boss: Appears when all 4 regular animatronics are in Nightmare form in the same run (§4.1.2 & §4.7).
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

            // Enforce balanced progressive AI progression across all 6 Nights:
            // Night 1: Sandy(2), Tyler(Passive) -> Gentle tutorial pacing (~1-2 attacks)
            // Night 2: Sandy(3), Orville(3), Tyler(Passive) -> Dual threats (~5-6 attacks)
            // Night 3: Sandy(3), Orville(3), Erle(3), Tyler(Passive) -> Triple threats (~8 attacks)
            // Night 4: Sandy(4), Orville(4), Erle(4), Tyler(4) -> All 4 active! (~15 attacks)
            // Night 5: Sandy(6), Orville(6), Erle(6), Tyler(6) -> Climax shift (~25 attacks)
            // Night 6: Sandy(8), Orville(8), Erle(8), Tyler(8) -> Nightmare finale (~40 attacks)
            int sandyAI = 2;
            int orvilleAI = 0;
            int erleAI = 0;
            int tylerAI = 0;

            if (nightCfg != null && nightCfg.snakeAILevel > 0)
                sandyAI = nightCfg.snakeAILevel;
            else
                sandyAI = (nightNumber == 1) ? 2 : (nightNumber <= 3 ? 3 : (nightNumber == 4 ? 4 : (nightNumber == 5 ? 6 : 8)));

            if (nightNumber >= 2)
            {
                if (nightCfg != null && nightCfg.sharkAILevel > 0)
                    orvilleAI = nightCfg.sharkAILevel;
                else
                    orvilleAI = (nightNumber <= 3) ? 3 : (nightNumber == 4 ? 4 : (nightNumber == 5 ? 6 : 8));
            }

            if (nightNumber >= 3)
            {
                if (nightCfg != null && nightCfg.eagleAILevel > 0)
                    erleAI = nightCfg.eagleAILevel;
                else
                    erleAI = (nightNumber == 3) ? 3 : (nightNumber == 4 ? 4 : (nightNumber == 5 ? 6 : 8));
            }

            if (nightNumber >= 4)
            {
                if (nightCfg != null && nightCfg.tylerAILevel > 0)
                    tylerAI = nightCfg.tylerAILevel;
                else
                    tylerAI = (nightNumber == 4) ? 4 : (nightNumber == 5 ? 6 : 8);
            }

            // GDD §4.1.2 & §4.7 True Ending Rule: True Final Boss appears when all 4 regular animatronics are transformed into Nightmare forms in the same run
            bool allFourNightmares = RunManager.HasInstance && RunManager.Instance.State.AllFourNightmaresActive;
            bool bossActive = (nightNumber >= 6 && allFourNightmares) || (nightCfg != null && nightCfg.bossActive && allFourNightmares);

            GetAnimatronic("erle")?.Initialize(_eagleConfig, erleAI, nightNumber);
            GetAnimatronic("tyler")?.Initialize(_tylerConfig, tylerAI, nightNumber);
            GetAnimatronic("orville")?.Initialize(_sharkConfig, orvilleAI, nightNumber);
            GetAnimatronic("sandy")?.Initialize(_snakeConfig, sandyAI, nightNumber);

            var bossUnit = GetAnimatronic("boss");
            if (bossUnit != null)
            {
                bossUnit.Initialize(_bossConfig, bossActive ? 14 : 0, nightNumber);
                if (_bossController != null)
                {
                    _bossController.InitializeBoss(bossUnit, bossActive);
                }
            }

            Debug.Log($"[AnimatronicManager] Initialized Night {nightNumber} (GDD Schedule) -> Sandy(Rear):{sandyAI}, Orville(Right):{orvilleAI}, Erle(Left):{erleAI}, Tyler(Center):{(nightNumber < 4 ? "Passive Observer" : tylerAI.ToString())}, TrueBoss:{bossActive}");
        }

        #region Queries & Helpers
        public AnimatronicUnit GetAnimatronic(string id)
        {
            if (string.IsNullOrEmpty(id)) return null;
            string lower = id.ToLower();

            // Support both GDD names and legacy aliases
            if (lower == "eagle") lower = "erle";
            else if (lower == "shark") lower = "orville";
            else if (lower == "snake") lower = "sandy";

            for (int i = 0; i < _units.Count; i++)
            {
                var u = _units[i];
                if (u == null || u.Config == null) continue;
                string unitId = u.AnimatronicId.ToLower();
                if (unitId == lower) return u;
            }
            return null;
        }

        public List<AnimatronicUnit> GetAnimatronicsInCameraRoom(int cameraIndex)
        {
            var result = new List<AnimatronicUnit>();
            for (int i = 0; i < _units.Count; i++)
            {
                var u = _units[i];
                if (u != null && !u.IsAtDoorBlindspot && u.CurrentCameraRoomIndex == cameraIndex)
                {
                    // Skip the True Final Boss unless it is actually active this night
                    if (u.AnimatronicId == "boss" && u.EffectiveAILevel <= 0) continue;

                    // Every regular animatronic is ALWAYS physically in its own room while not at the door,
                    // even if it is not moving yet (AI 0) — AI only controls movement, not visibility.
                    result.Add(u);
                }
            }
            return result;
        }

        public AnimatronicUnit GetAnimatronicAtDoorBlindspot(int doorIndex)
        {
            for (int i = 0; i < _units.Count; i++)
            {
                var u = _units[i];
                if (u != null && u.IsAtDoorBlindspot && u.TargetDoorIndex == doorIndex)
                {
                    return u;
                }
            }
            return null;
        }

        public void RefreshAllDamageStates()
        {
            for (int i = 0; i < _units.Count; i++)
            {
                if (_units[i] != null) _units[i].RefreshDamageAndNightmareState();
            }
        }

        [ContextMenu("Debug: Step All Active Animatronics +1 Stage")]
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

        public void SummonToDoorBlindspot(int doorIndex)
        {
            doorIndex = Mathf.Clamp(doorIndex, 0, 3);
            string[] doorToAnimId = { "erle", "tyler", "orville", "sandy" };
            var unit = GetAnimatronic(doorToAnimId[doorIndex]);
            if (unit == null) return;

            if (unit.EffectiveAILevel <= 0 || unit.IsPassiveObserver)
            {
                unit.Initialize(unit.Config, 10, 4);
            }

            int safety = 0;
            while (!unit.IsAtDoorBlindspot && safety++ < 12)
            {
                unit.AdvanceStage();
            }

            Debug.Log($"[AnimatronicManager] Summoned {unit.DisplayName} directly to Door {doorIndex + 1} Blindspot!");
        }
        #endregion
    }
}
