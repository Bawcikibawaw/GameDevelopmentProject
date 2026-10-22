using System;
using UnityEngine;
using Tylerland.Core;
using Tylerland.Config;
using Tylerland.Office;

namespace Tylerland.Animatronics
{
    /// <summary>
    /// Section 4 GDD: Individual Animatronic AI Unit & Nightmare State Handler (4-Room 2D Sprite System).
    /// Implements exact GDD mechanics for:
    /// - Erle the Eagle (Left Door 0 / CAM 01): Night 3+, 5 stages, 15s window, Electric Gun -> Nightmare Good Cop / Bad Cop & Broken Authority (12s window)
    /// - Tyler the Lion (Center Door 1 / CAM 02): Passive Nights 1-3, attacks Night 4+, 4 stages, 10s window, Shotgun -> Nightmare Destroys Camera (audio-only)
    /// - Orville the Orca (Right Door 2 / CAM 03): Night 2+, 6 stages, 20s window, 80% retreat / 20% +2 advance on camera view, Harpoon -> Nightmare Reversal (watching advances him + 30s/60s unattended timers)
    /// - Sandy the Snake (Rear Door 3 / CAM 04): Night 1+, 4 stages, 20s window, Spiked Mace -> Nightmare 5 stages, Shedding Decoy Shells & 8s window
    /// </summary>
    public class AnimatronicUnit : MonoBehaviour
    {
        [Header("Configuration")]
        [SerializeField] private AnimatronicConfig _config;

        [Header("Runtime AI State (Read Only)")]
        [SerializeField] private int _baseAILevel = 0;
        [SerializeField] private int _effectiveAILevel = 0;
        [SerializeField] private int _currentStage = 0;
        [SerializeField] private int _damageLevel = 0;
        [SerializeField] private bool _isNightmare = false;
        [SerializeField] private bool _isPassiveObserver = false;
        [SerializeField] private bool _preventAttacksRestOfNight = false;
        [SerializeField] private float _tickTimer = 0f;
        [SerializeField] private bool _isAtDoorBlindspot = false;
        [SerializeField] private bool _isStalledByCamera = false;
        [SerializeField] private int _dynamicDoorOverride = -1;

        [Header("Character-Specific GDD States (Read Only)")]
        [Tooltip("Tyler Nightmare (§4.2.5): True when Nightmare Tyler has destroyed his room camera (CAM 02)")]
        [SerializeField] private bool _isCameraDestroyed = false;

        [Tooltip("Orville (§4.3.2 & §4.3.4): Tracks whether the player was viewing Orville's camera last frame and unattended duration")]
        [SerializeField] private bool _wasWatchedLastFrame = false;
        [SerializeField] private float _orvilleUnattendedTimer = 0f;
        [SerializeField] private bool _orvilleRetreatedAt30s = false;

        [Tooltip("Sandy Nightmare (§4.4.4): Tracks whether Nightmare Sandy has left a shed decoy shell in CAM 04")]
        [SerializeField] private bool _hasActiveDecoyShell = false;
        [SerializeField] private int _decoyStageIndex = 1;

        [Tooltip("Erle (§4.5.3 - §4.5.6): Identity scanner cooldown & Nightmare Good Cop / Bad Cop mode")]
        [SerializeField] private bool _isBadCopMode = false;
        [SerializeField] private float _erleModeSwitchTimer = 18f;
        [SerializeField] private float _erleScanCooldownTimer = 0f;
        [SerializeField] private string _statusCallout = "";

        private AudioSource _audioSource;

        public AnimatronicConfig Config => _config;
        public string AnimatronicId => _config != null ? _config.animatronicId : "unknown";
        public string DisplayName => _config != null ? _config.displayName : "Unknown";
        public int BaseAILevel => _baseAILevel;
        public int EffectiveAILevel => _effectiveAILevel;
        public int CurrentStage => _currentStage;

        /// <summary>
        /// Total room stages before entering the doorway blindspot (Tyler=4, Orville=6, Sandy=4 Normal / 5 Nightmare, Erle=5).
        /// </summary>
        public int MaxStage
        {
            get
            {
                if (_config == null) return 4;
                return _isNightmare ? Mathf.Max(1, _config.nightmareStageCount) : Mathf.Max(1, _config.normalStageCount);
            }
        }

        public int DamageLevel => _damageLevel;
        public bool IsNightmare => _isNightmare;
        public bool IsPassiveObserver => _isPassiveObserver;
        public bool IsPassiveObserverOnly => _isPassiveObserver;
        public bool IsTemporarilyNeutralizedTonight => _preventAttacksRestOfNight;
        public bool IsAtDoorBlindspot => _isAtDoorBlindspot;
        public bool IsStalledByCamera => _isStalledByCamera;
        public bool IsCameraDestroyed => _isCameraDestroyed;
        public bool HasActiveDecoyShell => _hasActiveDecoyShell;
        public int DecoyStageIndex => _decoyStageIndex;
        public int DecoyShellStage => _decoyStageIndex;
        public bool IsBadCopMode => _isBadCopMode;
        public string StatusCallout => _statusCallout;
        public string LastVoiceLine => _statusCallout;
        public int TargetDoorIndex => _dynamicDoorOverride >= 0 ? _dynamicDoorOverride : (_config != null ? _config.targetDoorIndex : 1);

        /// <summary>
        /// Each animatronic occupies its dedicated Camera Room (0=CAM 01 Left/Erle, 1=CAM 02 Center/Tyler, 2=CAM 03 Right/Orville, 3=CAM 04 Rear/Sandy)
        /// while in stages 0..(MaxStage-1), and returns -1 once it reaches the Office Doorway Blindspot (MaxStage).
        /// </summary>
        public int CurrentCameraRoomIndex
        {
            get
            {
                if (_isAtDoorBlindspot) return -1;
                return Mathf.Clamp(TargetDoorIndex, 0, 3);
            }
        }

        private void Awake()
        {
            _audioSource = gameObject.AddComponent<AudioSource>();
            _audioSource.playOnAwake = false;
            _audioSource.spatialBlend = 0.8f;
        }

        private void OnEnable()
        {
            GameEvents.OnDoorClosed += HandleDoorClosed;
        }

        private void OnDisable()
        {
            GameEvents.OnDoorClosed -= HandleDoorClosed;
        }

        /// <summary>
        /// Initializes this animatronic for the start of a night shift according to GDD Section 4.
        /// </summary>
        public void Initialize(AnimatronicConfig config, int startingAILevel, int currentNight = 1)
        {
            _config = config;
            _baseAILevel = Mathf.Clamp(startingAILevel, 0, 20);
            _currentStage = 0;
            _isAtDoorBlindspot = false;
            _isStalledByCamera = false;
            _dynamicDoorOverride = -1;
            _preventAttacksRestOfNight = false;
            _wasWatchedLastFrame = false;
            _orvilleUnattendedTimer = 0f;
            _orvilleRetreatedAt30s = false;
            _hasActiveDecoyShell = false;
            _isBadCopMode = false;
            _erleModeSwitchTimer = 18f;
            _erleScanCooldownTimer = 0f;
            _statusCallout = "";

            // GDD §4.1 & §4.2.2: Tyler is present and watches the camera during Nights 1-3, but chooses not to attack until Night 4!
            if (_config != null && _config.passiveObserverBeforeActiveNight && currentNight < _config.activeFromNight)
            {
                _isPassiveObserver = true;
                _baseAILevel = 0;
                _statusCallout = "Passive Observer (Watching Camera)";
            }
            else
            {
                _isPassiveObserver = false;
            }

            RefreshDamageAndNightmareState();
            RecalculateEffectiveAI(0f);
            ResetTickTimer();
            UpdateDoorway2DSprite();
        }

        public void SetDynamicDoorOverride(int doorIndex)
        {
            if (_isAtDoorBlindspot) return;
            _dynamicDoorOverride = Mathf.Clamp(doorIndex, 0, 3);
        }

        public void RefreshDamageAndNightmareState()
        {
            if (RunManager.HasInstance && _config != null)
            {
                _damageLevel = RunManager.Instance.State.GetAnimatronicDamage(_config.animatronicId);
            }
            else
            {
                _damageLevel = 0;
            }

            _isNightmare = _damageLevel > 0;

            // GDD §4.2.5: Nightmare Tyler destroys his own camera!
            if (_config != null && _config.cameraBehavior == CameraStallBehavior.TylerDestroysCameraInNightmare)
            {
                _isCameraDestroyed = _isNightmare;
                if (_isCameraDestroyed)
                {
                    _statusCallout = "NIGHTMARE: Camera Destroyed (Track by Sound!)";
                }
            }
            else
            {
                _isCameraDestroyed = false;
            }
        }

        /// <summary>
        /// Backward-compatible alias for RefreshDamageAndNightmareState().
        /// </summary>
        public void RefreshDamageState() => RefreshDamageAndNightmareState();

        /// <summary>
        /// GDD §4.1.2 & §4.2.4: Called when hit by this character's counter-weapon at the doorway.
        /// Interrupts the attack, triggers Nightmare transformation, and for Tyler prevents another attack that night.
        /// </summary>
        public void OnHitBySpecialWeapon()
        {
            RefreshDamageAndNightmareState();

            // GDD §4.2.4: Shotgun hit on Tyler breaks part of his head, interrupts his attack, and prevents another Tyler attack that night!
            if (_config != null && _config.cameraBehavior == CameraStallBehavior.TylerDestroysCameraInNightmare)
            {
                _preventAttacksRestOfNight = true;
                _statusCallout = "NIGHTMARE TRIGGERED (Retreated for rest of tonight; CAM 02 Destroyed!)";
            }
            else if (_config != null && _config.cameraBehavior == CameraStallBehavior.ErleScannerAndGoodBadCop)
            {
                _isBadCopMode = false;
                _statusCallout = "NIGHTMARE: Good Cop ('Howdy, partner!')";
            }
            else if (_config != null && _config.cameraBehavior == CameraStallBehavior.OrvilleProbabilisticAndReversal)
            {
                _orvilleUnattendedTimer = 0f;
                _orvilleRetreatedAt30s = false;
                _statusCallout = "NIGHTMARE: Watching CAM 03 Advances Him!";
            }
            else if (_config != null && _config.cameraBehavior == CameraStallBehavior.SandyNightmareDecoys)
            {
                _statusCallout = "NIGHTMARE: 5 Stages + Decoy Shells (8s Door Window)";
            }

            ForceRetreatToStage(0);
        }

        public void RecalculateEffectiveAI(float hourBlendFactor)
        {
            if (_baseAILevel <= 0 || _isPassiveObserver)
            {
                _effectiveAILevel = 0;
                return;
            }

            int rampBonus = _config != null ? Mathf.FloorToInt(hourBlendFactor * _config.lateNightAIRamp) : 0;
            int nightmareBonus = _isNightmare ? 2 : 0;

            _effectiveAILevel = Mathf.Clamp(_baseAILevel + rampBonus + nightmareBonus, 0, 20);
        }

        private void Update()
        {
            if (_config == null) return;

            var flowState = GameFlowManager.HasInstance ? GameFlowManager.Instance.CurrentState : GameState.NightActive;
            if (!GameStateRules.IsGameplayActive(flowState) || Time.timeScale <= 0f) return;

            // Passive observer (Tyler on Nights 1-3) or neutralized for the rest of tonight
            if (_isPassiveObserver || _preventAttacksRestOfNight || _effectiveAILevel <= 0) return;

            // When already at the doorway blindspot, AttackResolver handles the countdown
            if (_isAtDoorBlindspot) return;

            // 1. Handle Orville the Orca camera observation & Nightmare 30s/60s unattended timers (§4.3.2 & §4.3.4)
            if (_config.cameraBehavior == CameraStallBehavior.OrvilleProbabilisticAndReversal)
            {
                UpdateOrvilleCameraMechanics();
            }

            // 2. Handle Erle the Eagle Identity Scanner cooldown & Nightmare Good Cop / Bad Cop alternation (§4.5.3 - §4.5.6)
            if (_config.cameraBehavior == CameraStallBehavior.ErleScannerAndGoodBadCop)
            {
                if (!UpdateErleMechanics()) return; // In scanner cooldown
            }

            // 3. Check standard camera stalling
            _isStalledByCamera = CheckCameraStall();
            if (_isStalledByCamera) return;

            _tickTimer -= Time.deltaTime;
            if (_tickTimer <= 0f)
            {
                ResetTickTimer();
                AttemptMovementRoll();
            }
        }

        #region Character-Specific GDD Mechanics (Orville, Erle, Sandy, Tyler)
        private void UpdateOrvilleCameraMechanics()
        {
            bool isCurrentlyWatched = CameraMonitorSystem.HasInstance
                && CameraMonitorSystem.Instance.IsMonitorRaised
                && CameraMonitorSystem.Instance.CurrentCameraIndex == CurrentCameraRoomIndex;

            // Trigger immediate reaction on the frame the player opens/switches to Orville's camera (CAM 03)!
            if (isCurrentlyWatched && !_wasWatchedLastFrame)
            {
                _orvilleUnattendedTimer = 0f;
                _orvilleRetreatedAt30s = false;

                if (!_isNightmare)
                {
                    // GDD §4.3.2 Normal Camera Response: 80% chance to retreat 1 stage or 20% chance to advance 2 stages!
                    int oldStage = _currentStage;
                    if (UnityEngine.Random.value < 0.80f)
                    {
                        _currentStage = Mathf.Max(0, _currentStage - 1);
                        _statusCallout = "Camera Response: Retreated 1 Stage (80%)";
                        Debug.Log($"[Orville AI] Watched on CAM 03 (Normal): 80% roll -> Retreated from Stage {oldStage + 1} to Stage {_currentStage + 1}");
                    }
                    else
                    {
                        int next = Mathf.Min(MaxStage - 1, _currentStage + 2);
                        _currentStage = next;
                        _statusCallout = "Camera Response: Surged +2 Stages! (20%)";
                        Debug.Log($"[Orville AI] Watched on CAM 03 (Normal): 20% roll -> Advanced +2 to Stage {_currentStage + 1}/{MaxStage}!");
                    }
                    GameEvents.TriggerAnimatronicStageChanged(AnimatronicId, oldStage, _currentStage);
                }
                else
                {
                    // GDD §4.3.4 Nightmare Reversal: Checking his camera now moves him 1 stage forward!
                    _statusCallout = "NIGHTMARE REVERSAL: Watching CAM 03 Moved Him Forward!";
                    Debug.LogWarning("[Nightmare Orville] Player checked CAM 03 -> Nightmare Reversal moved Orville 1 stage forward!");
                    AdvanceStage();
                }
            }

            _wasWatchedLastFrame = isCurrentlyWatched;

            // GDD §4.3.4 Nightmare Unattended Camera Timers (30s retreat / 60s automatic attack)
            if (_isNightmare && !isCurrentlyWatched)
            {
                _orvilleUnattendedTimer += Time.deltaTime;

                if (_orvilleUnattendedTimer >= 30f && !_orvilleRetreatedAt30s)
                {
                    _orvilleRetreatedAt30s = true;
                    if (_currentStage > 0)
                    {
                        int old = _currentStage;
                        _currentStage = 0;
                        _statusCallout = "30s Unattended: Retreated to Stage 1";
                        GameEvents.TriggerAnimatronicStageChanged(AnimatronicId, old, _currentStage);
                    }
                }
                else if (_orvilleUnattendedTimer >= 60f)
                {
                    _orvilleUnattendedTimer = 0f;
                    _orvilleRetreatedAt30s = false;
                    _statusCallout = "60s Unattended: AUTOMATIC NIGHTMARE ATTACK!";
                    Debug.LogWarning("[Nightmare Orville] Left unattended for 60s -> Automatic Attack triggered!");
                    TryEnterDoorwayBlindspot(_currentStage);
                }
            }
        }

        private bool UpdateErleMechanics()
        {
            if (_erleScanCooldownTimer > 0f)
            {
                _erleScanCooldownTimer -= Time.deltaTime;
                return false;
            }

            if (_isNightmare)
            {
                // GDD §4.5.5 Nightmare Erle: Alternates between Good Cop and Bad Cop
                _erleModeSwitchTimer -= Time.deltaTime;
                if (_erleModeSwitchTimer <= 0f)
                {
                    _isBadCopMode = !_isBadCopMode;
                    _erleModeSwitchTimer = UnityEngine.Random.Range(14f, 22f);
                    _statusCallout = _isBadCopMode
                        ? "BAD COP: \"You're under arrest!\" (Fast)"
                        : "GOOD COP: \"Howdy, partner!\" (Slow)";
                    Debug.Log($"[Nightmare Erle] Switched state -> {_statusCallout}");
                }
            }
            return true;
        }

        /// <summary>
        /// GDD §4.5.6 Broken Authority:
        /// If the player closes Erle's LEFT door (Door 0) without a legitimate approaching threat,
        /// Nightmare Erle interprets it as suspicious behavior and immediately switches from Good Cop to Bad Cop!
        /// </summary>
        private void HandleDoorClosed(int doorIndex)
        {
            if (_config == null || _config.cameraBehavior != CameraStallBehavior.ErleScannerAndGoodBadCop) return;
            if (!_isNightmare || doorIndex != TargetDoorIndex) return;

            // Legitimate threat is when Erle is at the doorway blindspot or on the verge of leaving his room (final room stage)
            bool isLegitimateThreat = _isAtDoorBlindspot || (_currentStage >= MaxStage - 1);
            if (!isLegitimateThreat && !_isBadCopMode)
            {
                _isBadCopMode = true;
                _erleModeSwitchTimer = 25f;
                _statusCallout = "BROKEN AUTHORITY: Unnecessary Door Close -> BAD COP! (\"You're under arrest!\")";
                Debug.LogWarning("[Nightmare Erle] Broken Authority triggered! Unnecessary Left Door close switched Erle to BAD COP!");
            }
        }
        #endregion

        private bool CheckCameraStall()
        {
            if (_config == null || !CameraMonitorSystem.HasInstance) return false;
            var monitor = CameraMonitorSystem.Instance;
            if (!monitor.IsMonitorRaised) return false;

            if (_config.cameraBehavior == CameraStallBehavior.StalledWhenWatchedOnCamera)
            {
                return monitor.CurrentCameraIndex == CurrentCameraRoomIndex;
            }

            return false;
        }

        private void ResetTickTimer()
        {
            float interval = _config != null ? _config.baseTickInterval : 4.8f;

            // GDD §4.5.5 Nightmare Erle: Good Cop is slower (1.25x), Bad Cop is faster (0.65x)
            if (_config != null && _config.cameraBehavior == CameraStallBehavior.ErleScannerAndGoodBadCop && _isNightmare)
            {
                interval *= _isBadCopMode ? 0.65f : 1.25f;
            }

            _tickTimer = Mathf.Max(1.0f, interval + UnityEngine.Random.Range(-0.35f, 0.35f));
        }

        private void AttemptMovementRoll()
        {
            int roll = UnityEngine.Random.Range(1, 21);
            if (roll > _effectiveAILevel) return;

            // GDD §4.5.3 Erle Identity Error: Malfunctioning Scanner probability check
            if (_config != null && _config.cameraBehavior == CameraStallBehavior.ErleScannerAndGoodBadCop && !_isNightmare)
            {
                // 25% chance scanner momentarily registers guard as Authorized Staff and enters a 6s scan cooldown
                if (UnityEngine.Random.value < 0.25f)
                {
                    _erleScanCooldownTimer = 6.0f;
                    _statusCallout = "Identity Scan: 'Authorized Staff' (6s Cooldown)";
                    Debug.Log("[Erle AI] Identity Scanner read 'Authorized Staff' -> 6s progression cooldown.");
                    return;
                }
                _statusCallout = "Identity Scan: 'INTRUDER DETECTED!'";
            }

            AdvanceStage();
        }

        /// <summary>
        /// Advances this animatronic 1 stage inside its dedicated room, or into the Office Doorway Blindspot at final stage.
        /// </summary>
        public void AdvanceStage()
        {
            if (_isAtDoorBlindspot || _isPassiveObserver || _preventAttacksRestOfNight) return;

            int oldStage = _currentStage;
            int nextStage = Mathf.Min(_currentStage + 1, MaxStage);

            // GDD §4.4.4 Nightmare Sandy: Shedding Decoys
            if (_config != null && _config.cameraBehavior == CameraStallBehavior.SandyNightmareDecoys && _isNightmare)
            {
                // 45% chance when advancing to leave a shed decoy shell at the previous stage!
                if (UnityEngine.Random.value < 0.45f)
                {
                    _hasActiveDecoyShell = true;
                    _decoyStageIndex = oldStage;
                    _statusCallout = $"NIGHTMARE DECOY: Shed fake shell at Stage {oldStage + 1}!";
                }
            }

            // GDD §4.2.2 & §4.2.5 Tyler voice taunts / sound buildup as he nears the Center Door
            if (_config != null && _config.cameraBehavior == CameraStallBehavior.TylerDestroysCameraInNightmare)
            {
                string[] taunts =
                {
                    "\"You dare look upon a king?\"",
                    "\"Stay at your desk, little guard.\"",
                    "\"Your center door won't save you forever!\"",
                    "\"BOW BEFORE TYLER!\" *HEAVY METALLIC FOOTSTEPS*"
                };
                _statusCallout = taunts[Mathf.Clamp(nextStage, 0, taunts.Length - 1)];
            }

            // If reaching the final stage (Doorway Attack Approach), register with AttackResolver!
            if (nextStage >= MaxStage)
            {
                TryEnterDoorwayBlindspot(oldStage);
                return;
            }

            _currentStage = nextStage;
            PlayFootstepCue();

            Debug.Log($"[Animatronic AI] {DisplayName} advanced in CAM {CurrentCameraRoomIndex + 1:00}: Stage {_currentStage + 1}/{MaxStage} [AI={_effectiveAILevel}/20]");
            GameEvents.TriggerAnimatronicStageChanged(AnimatronicId, oldStage, _currentStage);
        }

        private void TryEnterDoorwayBlindspot(int oldStage)
        {
            if (!AttackResolver.HasInstance) return;

            // GDD §4.6 Reaction Window Table:
            // Normal:    Tyler=10s, Orville=20s, Sandy=20s, Erle=15s
            // Nightmare: Tyler=10s, Orville=20s, Sandy=8s,  Erle=12s
            float attackWindow = _config != null
                ? (_isNightmare ? _config.nightmareAttackWindowSeconds : _config.attackWindowSeconds)
                : 15.0f;

            // Shop Upgrade: Motion Sensor adds +0.75s warning window
            if (RunManager.HasInstance && RunManager.Instance.HasUpgrade("MOTION_SENSOR"))
            {
                attackWindow += 0.75f;
            }

            bool accepted = AttackResolver.Instance.RegisterAttackArrival(
                AnimatronicId,
                TargetDoorIndex,
                attackWindow,
                HandleBlockedRetreat
            );

            if (!accepted)
            {
                _currentStage = Mathf.Max(0, MaxStage - 1);
                return;
            }

            _currentStage = MaxStage;
            _isAtDoorBlindspot = true;
            PlayBlindspotWarningCue();
            UpdateDoorway2DSprite();

            Debug.LogWarning($"[Animatronic AI] ⚠ {DisplayName} ({(_isNightmare ? "NIGHTMARE" : "Normal")}) is attacking DOOR {TargetDoorIndex + 1}! Reaction Window: {attackWindow:F0}s!");
            GameEvents.TriggerAnimatronicStageChanged(AnimatronicId, oldStage, _currentStage);
        }

        /// <summary>
        /// Callback invoked by AttackResolver when the player blocks the attack with a closed door.
        /// Applies character-specific Durability and Energy penalties from GDD Section 4!
        /// </summary>
        private void HandleBlockedRetreat()
        {
            PlayDoorBangCue();

            if (DoorSystemManager.HasInstance && _config != null)
            {
                var door = DoorSystemManager.Instance.GetDoor(TargetDoorIndex);
                if (door != null)
                {
                    if (_config.extraDurabilityLossOnBlock > 0f)
                    {
                        door.ApplyDurabilityWear(_config.extraDurabilityLossOnBlock);
                    }

                    float energyDrain = _isNightmare ? _config.energyDrainOnNightmareBlock : _config.energyDrainOnNormalBlock;
                    if (energyDrain > 0f)
                    {
                        door.ApplyEnergyDrain(energyDrain);
                        Debug.Log($"[Door Block] {DisplayName} drained {energyDrain:F0}% Energy from Door {TargetDoorIndex + 1}!");
                    }
                }
            }

            // GDD §4.3.3: Orville retreats to Stage 2 (index 1); Tyler, Sandy, and Erle retreat to Stage 1 (index 0)
            int retreatStage = _config != null ? Mathf.Clamp(_config.retreatStageIndex, 0, MaxStage - 1) : 0;

            // GDD §4.5.3: Erle enters a brief cooldown after an attack phase
            if (_config != null && _config.cameraBehavior == CameraStallBehavior.ErleScannerAndGoodBadCop)
            {
                _erleScanCooldownTimer = 5.0f;
            }

            ForceRetreatToStage(retreatStage);
        }

        /// <summary>
        /// Forces the animatronic back into its room at the specified 0-based stage index.
        /// </summary>
        public void ForceRetreatToStage(int targetStage)
        {
            int oldStage = _currentStage;
            _isAtDoorBlindspot = false;
            _hasActiveDecoyShell = false;
            _currentStage = Mathf.Clamp(targetStage, 0, MaxStage - 1);
            ResetTickTimer();
            UpdateDoorway2DSprite();

            GameEvents.TriggerAnimatronicStageChanged(AnimatronicId, oldStage, _currentStage);
        }

        #region 100% 2D Doorway Blindspot Sprite
        private void UpdateDoorway2DSprite()
        {
            var artMgr = FindFirstObjectByType<OfficeArtManager>();
            if (artMgr == null) return;

            if (!_isAtDoorBlindspot)
            {
                artMgr.SetBlindspotAnimatronicSprite(TargetDoorIndex, null);
                return;
            }

            Sprite doorwaySprite = null;
            if (_config != null && _config.doorwaySpritesByDamage != null && _config.doorwaySpritesByDamage.Length > 0)
            {
                int dmgIdx = Mathf.Clamp(_damageLevel, 0, _config.doorwaySpritesByDamage.Length - 1);
                doorwaySprite = _config.doorwaySpritesByDamage[dmgIdx] != null
                    ? _config.doorwaySpritesByDamage[dmgIdx]
                    : _config.doorwaySpritesByDamage[0];
            }

            if (doorwaySprite == null)
            {
                Color theme = _config != null ? _config.themeColor : Color.red;
                doorwaySprite = Procedural2DSpriteFactory.GetOrCreateAnimatronic2DSprite(AnimatronicId, theme, _damageLevel, 1.0f);
            }

            artMgr.SetBlindspotAnimatronicSprite(TargetDoorIndex, doorwaySprite);
        }
        #endregion

        #region Audio Cues
        private void PlayFootstepCue()
        {
            if (_audioSource != null && _config != null && _config.footstepSound != null)
            {
                _audioSource.PlayOneShot(_config.footstepSound, 0.7f);
            }
        }

        private void PlayBlindspotWarningCue()
        {
            if (_audioSource != null && _config != null && _config.blindspotWarningSound != null)
            {
                _audioSource.PlayOneShot(_config.blindspotWarningSound, 0.9f);
            }
        }

        private void PlayDoorBangCue()
        {
            if (_audioSource != null && _config != null && _config.doorBlockedBangSound != null)
            {
                _audioSource.PlayOneShot(_config.doorBlockedBangSound, 1.0f);
            }
        }
        #endregion
    }
}
