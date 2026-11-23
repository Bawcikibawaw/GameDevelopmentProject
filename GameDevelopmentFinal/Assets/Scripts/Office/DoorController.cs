using System;
using System.Collections;
using UnityEngine;
using Tylerland.Core;

namespace Tylerland.Office
{
    public enum DoorMotionStyle
    {
        VerticalRopeDrop,
        HorizontalSlideLeft,
        HorizontalSlideRight,
        CustomLocalOffsets
    }

    public enum DoorState
    {
        Open,
        Closing,
        Closed,
        Opening,
        Broken
    }

    /// <summary>
    /// Section 4.3: Door System (Energy + Durability).
    /// Manages an individual office security door with resource drain, durability loss,
    /// lockout mechanics, and 3D physical movement.
    /// </summary>
    [ExecuteAlways]
    public class DoorController : MonoBehaviour
    {
        [Header("Door Identification")]
        [Tooltip("Door index: 0 = Left (Shark), 1 = Center (Tyler), 2 = Right (Eagle), 3 = Back (Snake)")]
        [Range(0, 3)]
        [SerializeField] private int _doorIndex = 1;

        [Tooltip("Descriptive name (e.g. 'Center Door - Tyler')")]
        [SerializeField] private string _doorName = "Center Door";

        [Header("2D Hand-Drawn Sprite Support")]
        [Tooltip("Optional 2D SpriteRenderer for the hand-drawn door visual")]
        [SerializeField] private SpriteRenderer _doorSpriteRenderer;

        [Tooltip("3D gray placeholder MeshRenderer that automatically hides once a 2D sprite is assigned")]
        [SerializeField] private MeshRenderer _placeholder3DSlabRenderer;

        [Tooltip("Target width and height in meters for the 2D door sprite")]
        [SerializeField] private Vector2 _doorSpriteTargetSize = new Vector2(2.35f, 2.85f);

        [Tooltip("Hand-drawn sprite for the door shutter (used when closed or while sliding)")]
        [SerializeField] private Sprite _closedSprite;

        [Tooltip("Hand-drawn sprite for damaged door shutter (used when durability is <= 50%)")]
        [SerializeField] private Sprite _damagedSprite;

        [Tooltip("Optional separate sprite when door is fully OPEN (leave empty if using sliding shutter)")]
        [SerializeField] private Sprite _openSprite;

        [Tooltip("Hand-drawn sprite when door is jammed/BROKEN")]
        [SerializeField] private Sprite _brokenSprite;

        [Header("Door Motion Style")]
        [Tooltip("How the door physically moves: Vertical (drops down like a rope/roll-down shutter) or Horizontal (slides sideways)")]
        [SerializeField] private DoorMotionStyle _motionStyle = DoorMotionStyle.VerticalRopeDrop;

        [Header("3D Physical Animation & Positioning")]
        [Tooltip("Transform of the moving door slab/shutter (works with 2D Sprite or 3D Quad)")]
        [SerializeField] private Transform _doorMeshTransform;

        [Tooltip("Local offset when the door is fully OPEN (e.g. raised high)")]
        [SerializeField] private Vector3 _openLocalPosition = new Vector3(0, 4.5f, 0);

        [Tooltip("Local offset when the door is fully CLOSED (slammed shut)")]
        [SerializeField] private Vector3 _closedLocalPosition = new Vector3(0, 1.4f, 0);

        [Header("FNAF 1 Hallway Blindspot Light")]
        [Tooltip("Optional 3D light shining down the corridor outside this door")]
        [SerializeField] private Light _hallwayLight;
        [Tooltip("Energy drain rate per second while hallway light is on")]
        [SerializeField] private float _energyDrainLight = 1.0f;
        [SerializeField] private bool _isLightOn = false;

        public bool IsLightOn => _isLightOn;

        [Header("Energy Parameters (Roadmap 4.3)")]
        [SerializeField] private float _energyMax = 100f;
        [SerializeField] private float _currentEnergy = 100f;
        [Tooltip("Energy drain rate per second while closed")]
        [SerializeField] private float _energyDrainClosed = 1.6f;
        [Tooltip("Energy recharge rate per second while open")]
        [SerializeField] private float _energyRegenOpen = 1.8f;
        [Tooltip("Whether door energy reloads/recharges during the active night.")]
        [SerializeField] private bool _rechargeDuringNight = true;
        [Tooltip("Delay in seconds before recharge begins after opening door")]
        [SerializeField] private float _regenDelay = 1.5f;
        [Tooltip("Minimum energy required to close door")]
        [SerializeField] private float _minEnergyToClose = 5.0f;
        [Tooltip("Lockout penalty duration when energy reaches 0")]
        [SerializeField] private float _lockoutDuration = 3.0f;

        [Header("Durability Parameters (Roadmap 4.3)")]
        [SerializeField] private float _durabilityMax = 100f;
        [SerializeField] private float _currentDurability = 100f;
        [Tooltip("Durability lost each time an animatronic attack is blocked at closed door")]
        [SerializeField] private float _durabilityLossPerBlock = 3.5f;
        [Tooltip("Wear and tear durability lost on each cycle")]
        [SerializeField] private float _durabilityLossPerCycle = 0.2f;

        [Header("Motion Timings")]
        [SerializeField] private float _closeDuration = 0.25f;
        [SerializeField] private float _openDuration = 0.35f;
        [SerializeField] private float _closeSlamWindow = 0.1f;

        [Header("Runtime State (Read Only)")]
        [SerializeField] private DoorState _state = DoorState.Open;
        [SerializeField] private bool _isLockedOut = false;
        [SerializeField] private float _lockoutTimer = 0f;
        [SerializeField] private float _regenCooldownTimer = 0f;

        private Coroutine _movementRoutine;

        public int DoorIndex => _doorIndex;
        public string DoorName => _doorName;
        public DoorState State => _state;
        public float CurrentEnergy => _currentEnergy;
        public float EnergyMax => _energyMax;
        public float CurrentDurability => _currentDurability;
        public float DurabilityMax => _durabilityMax;
        public bool IsClosed => _state == DoorState.Closed;
        public bool IsBroken => _state == DoorState.Broken;
        public bool IsLockedOut => _isLockedOut;

        // Upgrade multipliers (from Shop upgrades)
        private float _energyMaxMultiplier = 1f;
        private float _drainMultiplier = 1f;
        private float _regenMultiplier = 1f;
        private float _durabilityMaxMultiplier = 1f;

        private void OnEnable()
        {
            LoadShutterSpritesIfMissing();
            UpdateSpriteForState(_state);
        }

        private void OnValidate()
        {
            LoadShutterSpritesIfMissing();
            UpdateSpriteForState(_state);
        }

        private void Start()
        {
            if (!Application.isPlaying)
            {
                LoadShutterSpritesIfMissing();
                UpdateSpriteForState(_state);
                return;
            }

            if (_doorMeshTransform == null)
            {
                _doorMeshTransform = transform;
            }

            LoadShutterSpritesIfMissing();
            ApplyMotionStylePositions();
            ResetDoor();
            ApplyUpgradeModifiers();
            UpdateSpriteForState(_state);
        }

        private void LoadShutterSpritesIfMissing()
        {
            if (_doorSpriteTargetSize.x < 2.25f)
            {
                _doorSpriteTargetSize = new Vector2(2.35f, 2.85f);
            }
#if UNITY_EDITOR
            const string folder = "Assets/Sprites/the rest of the office/";
            if (_closedSprite == null)
                _closedSprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>(folder + "spr_door_shutter_01.png");
            if (_damagedSprite == null)
                _damagedSprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>(folder + "spr_door_shutter_02.png");
            if (_brokenSprite == null)
                _brokenSprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>(folder + "spr_door_shutter_03.png");
#endif
        }

        public void ApplyMotionStylePositions()
        {
            switch (_motionStyle)
            {
                case DoorMotionStyle.VerticalRopeDrop:
                    // Drops down from above like a rope or roll-down shutter
                    _closedLocalPosition = new Vector3(0, 1.4f, 0);
                    _openLocalPosition = new Vector3(0, 4.5f, 0);
                    break;
                case DoorMotionStyle.HorizontalSlideLeft:
                    // Slides horizontally to the left
                    _closedLocalPosition = new Vector3(0, 1.4f, 0);
                    _openLocalPosition = new Vector3(-2.6f, 1.4f, 0);
                    break;
                case DoorMotionStyle.HorizontalSlideRight:
                    // Slides horizontally to the right
                    _closedLocalPosition = new Vector3(0, 1.4f, 0);
                    _openLocalPosition = new Vector3(2.6f, 1.4f, 0);
                    break;
            }
        }

        private void Update()
        {
            if (!Application.isPlaying) return;
            if (!GameStateRules.IsGameplayActive(GameFlowManager.HasInstance ? GameFlowManager.Instance.CurrentState : GameState.NightActive))
                return;
            if (Time.timeScale <= 0f)
                return;

            HandleLockoutTimer();
            HandleEnergyDrainAndRegen();
        }

        #region Resource Logic
        private void HandleLockoutTimer()
        {
            if (_isLockedOut)
            {
                _lockoutTimer -= Time.deltaTime;
                if (_lockoutTimer <= 0f)
                {
                    _isLockedOut = false;
                    Debug.Log($"[Door {_doorIndex}] Lockout ended. Door controls restored.");
                }
            }
        }

        private void HandleEnergyDrainAndRegen()
        {
            if (_state == DoorState.Broken) return;

            // DRAIN while closed or closing
            if (_state == DoorState.Closed || _state == DoorState.Closing)
            {
                float drain = _energyDrainClosed * _drainMultiplier * Time.deltaTime;
                _currentEnergy -= drain;

                if (_currentEnergy <= 0f)
                {
                    _currentEnergy = 0f;
                    TriggerEnergyDepletionLockout();
                }

                GameEvents.TriggerDoorStatsChanged(_doorIndex, _currentEnergy, _currentDurability);
            }
            // RECHARGE while open (Disabled during active night by default: door charges do NOT load in the night)
            else if (_rechargeDuringNight && _state == DoorState.Open && !_isLockedOut)
            {
                if (_regenCooldownTimer > 0f)
                {
                    _regenCooldownTimer -= Time.deltaTime;
                }
                else if (_currentEnergy < (_energyMax * _energyMaxMultiplier))
                {
                    float regen = _energyRegenOpen * _regenMultiplier * Time.deltaTime;
                    _currentEnergy = Mathf.Min(_energyMax * _energyMaxMultiplier, _currentEnergy + regen);
                    GameEvents.TriggerDoorStatsChanged(_doorIndex, _currentEnergy, _currentDurability);
                }
            }

            // DRAIN while hallway light is shining
            if (_isLightOn)
            {
                float lightDrain = _energyDrainLight * Time.deltaTime;
                _currentEnergy -= lightDrain;
                if (_currentEnergy <= 0f)
                {
                    _currentEnergy = 0f;
                    SetLight(false);
                    TriggerEnergyDepletionLockout();
                }
                GameEvents.TriggerDoorStatsChanged(_doorIndex, _currentEnergy, _currentDurability);
            }
        }

        public void ToggleLight()
        {
            SetLight(!_isLightOn);
        }

        public void SetLight(bool on)
        {
            if (_isLockedOut && on) return;
            _isLightOn = on;
            if (_hallwayLight != null)
            {
                _hallwayLight.enabled = _isLightOn;
            }
            Debug.Log($"[Door {_doorIndex}] Hallway blindspot light: {(_isLightOn ? "ON" : "OFF")}");
        }

        private void TriggerEnergyDepletionLockout()
        {
            Debug.LogWarning($"[Door {_doorIndex}] Energy DEPLETED! Auto-opening with {_lockoutDuration}s lockout!");
            SetLight(false);
            _isLockedOut = true;
            _lockoutTimer = _lockoutDuration;
            OpenDoorInternal(true);
        }
        #endregion

        #region Door Actions
        public void ToggleDoor()
        {
            if (_state == DoorState.Open || _state == DoorState.Opening)
            {
                CloseDoor();
            }
            else if (_state == DoorState.Closed || _state == DoorState.Closing)
            {
                OpenDoor();
            }
        }

        public bool CloseDoor()
        {
            if (_state == DoorState.Broken)
            {
                Debug.LogWarning($"[Door {_doorIndex}] Cannot close: door is BROKEN!");
                return false;
            }
            if (_isLockedOut)
            {
                Debug.LogWarning($"[Door {_doorIndex}] Cannot close: door is in LOCKOUT ({_lockoutTimer:F1}s remaining)!");
                return false;
            }
            if (_currentEnergy < _minEnergyToClose)
            {
                Debug.LogWarning($"[Door {_doorIndex}] Insufficient energy to close ({_currentEnergy:F1} < {_minEnergyToClose})!");
                return false;
            }
            if (_state == DoorState.Closed || _state == DoorState.Closing)
            {
                return true; // Already closed/closing
            }

            // Durability wear per cycle
            ApplyDurabilityWear(_durabilityLossPerCycle);

            if (_movementRoutine != null) StopCoroutine(_movementRoutine);
            _movementRoutine = StartCoroutine(MoveDoorRoutine(_closedLocalPosition, _closeDuration, DoorState.Closed, () =>
            {
                GameEvents.TriggerDoorClosed(_doorIndex);
            }));

            _state = DoorState.Closing;
            UpdateSpriteForState(DoorState.Closing);
            return true;
        }

        public bool OpenDoor()
        {
            return OpenDoorInternal(false);
        }

        private bool OpenDoorInternal(bool forced)
        {
            if (_state == DoorState.Open || _state == DoorState.Opening) return true;

            // Start regen delay
            _regenCooldownTimer = _regenDelay;

            if (_movementRoutine != null) StopCoroutine(_movementRoutine);
            _movementRoutine = StartCoroutine(MoveDoorRoutine(_openLocalPosition, _openDuration, DoorState.Open, () =>
            {
                GameEvents.TriggerDoorOpened(_doorIndex);
            }));

            _state = DoorState.Opening;
            return true;
        }

        private IEnumerator MoveDoorRoutine(Vector3 targetPos, float duration, DoorState finalState, Action onComplete)
        {
            Vector3 startPos = _doorMeshTransform.localPosition;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                _doorMeshTransform.localPosition = Vector3.Lerp(startPos, targetPos, t);
                yield return null;
            }

            _doorMeshTransform.localPosition = targetPos;
            _state = finalState;
            UpdateSpriteForState(finalState);
            onComplete?.Invoke();
            _movementRoutine = null;
        }

        private void UpdateSpriteForState(DoorState state)
        {
            if (_placeholder3DSlabRenderer != null)
            {
                _placeholder3DSlabRenderer.enabled = false;
            }

            if (_doorSpriteRenderer == null) return;

            _doorSpriteRenderer.enabled = (state != DoorState.Open);

            float effectiveMaxDurability = Mathf.Max(1f, _durabilityMax * _durabilityMaxMultiplier);
            bool isDamaged = _currentDurability <= effectiveMaxDurability * 0.5f;

            Sprite activeSprite = (isDamaged && _damagedSprite != null)
                ? _damagedSprite
                : (_closedSprite != null
                    ? _closedSprite
                    : Procedural2DSpriteFactory.GetOrCreateDoorShutterSprite(_doorIndex));

            if (state == DoorState.Broken && _brokenSprite != null)
            {
                activeSprite = _brokenSprite;
            }
            else if (state == DoorState.Open && _openSprite != null)
            {
                activeSprite = _openSprite;
            }

            _doorSpriteRenderer.sprite = activeSprite;
            if (activeSprite != null)
            {
                OfficeArtManager.FitSpriteRendererToSize(_doorSpriteRenderer, _doorSpriteTargetSize);
            }
        }

        /// <summary>
        /// Called when an animatronic hits this closed door.
        /// Absorbs the blow, damages door durability, and returns true if blocked.
        /// </summary>
        public bool TryBlockAttack(string animatronicId)
        {
            // If closed or closing within the slam window, it counts as blocked!
            bool isBlocking = (_state == DoorState.Closed || (_state == DoorState.Closing && _closeSlamWindow > 0f));

            if (isBlocking)
            {
                Debug.Log($"[Door {_doorIndex}] BLOCKED attack from '{animatronicId}'!");
                ApplyDurabilityWear(_durabilityLossPerBlock);
                return true;
            }

            return false;
        }

        public void ApplyDurabilityWear(float amount)
        {
            if (_state == DoorState.Broken || amount <= 0f) return;

            _currentDurability = Mathf.Max(0f, _currentDurability - amount);
            GameEvents.TriggerDoorStatsChanged(_doorIndex, _currentEnergy, _currentDurability);

            if (_currentDurability <= 0f)
            {
                BreakDoor();
            }
            else
            {
                UpdateSpriteForState(_state);
            }
        }

        public void ApplyEnergyDrain(float amount)
        {
            if (_state == DoorState.Broken || amount <= 0f) return;

            _currentEnergy = Mathf.Max(0f, _currentEnergy - amount);
            GameEvents.TriggerDoorStatsChanged(_doorIndex, _currentEnergy, _currentDurability);

            if (_currentEnergy <= 0f && !_isLockedOut)
            {
                TriggerEnergyDepletionLockout();
            }
        }

        private void BreakDoor()
        {
            if (_movementRoutine != null)
            {
                StopCoroutine(_movementRoutine);
                _movementRoutine = null;
            }

            _state = DoorState.Broken;
            if (_doorMeshTransform != null)
            {
                _doorMeshTransform.localPosition = _closedLocalPosition;
            }
            SetLight(false);
            UpdateSpriteForState(DoorState.Broken);
            Debug.LogError($"[Door {_doorIndex}] DURABILITY BROKEN! Shutter breached — animatronics can pass through until repaired!");
            GameEvents.TriggerDoorOpened(_doorIndex);
        }

        public void RepairDoor()
        {
            _currentDurability = _durabilityMax * _durabilityMaxMultiplier;
            _currentEnergy = _energyMax * _energyMaxMultiplier;
            _state = DoorState.Open;
            _isLockedOut = false;
            _doorMeshTransform.localPosition = _openLocalPosition;
            UpdateSpriteForState(_state);
            Debug.Log($"[Door {_doorIndex}] Repaired to 100%!");
            GameEvents.TriggerDoorStatsChanged(_doorIndex, _currentEnergy, _currentDurability);
        }

        public void ResetDoor()
        {
            _currentEnergy = _energyMax * _energyMaxMultiplier;
            _currentDurability = _durabilityMax * _durabilityMaxMultiplier;
            _state = DoorState.Open;
            _isLockedOut = false;
            _lockoutTimer = 0f;
            _regenCooldownTimer = 0f;
            if (_doorMeshTransform != null) _doorMeshTransform.localPosition = _openLocalPosition;
        }
        #endregion

        #region Shop Upgrade Modifiers
        public void ApplyUpgradeModifiers()
        {
            // Reset to baseline before computing modifiers so re-applying after Between-Nights shopping is idempotent
            _energyMaxMultiplier = 1f;
            _drainMultiplier = 1f;
            _regenMultiplier = 1f;
            _durabilityMaxMultiplier = 1f;
            _closeDuration = 0.25f;
            _energyDrainLight = 1.0f;

            if (RunManager.HasInstance)
            {
                var run = RunManager.Instance;

                // "Door Battery" (+20% / +40% / +60% energyMax)
                if (run.HasUpgrade("DOOR_BATTERY_3")) _energyMaxMultiplier = 1.6f;
                else if (run.HasUpgrade("DOOR_BATTERY_2")) _energyMaxMultiplier = 1.4f;
                else if (run.HasUpgrade("DOOR_BATTERY_1")) _energyMaxMultiplier = 1.2f;

                // "Efficient Motor" (-20% / -40% energyDrainClosed)
                if (run.HasUpgrade("EFFICIENT_MOTOR_2")) _drainMultiplier = 0.6f;
                else if (run.HasUpgrade("EFFICIENT_MOTOR_1")) _drainMultiplier = 0.8f;

                // "Fast Recharge" (+25% / +50% energyRegenOpen)
                if (run.HasUpgrade("FAST_RECHARGE_2")) _regenMultiplier = 1.5f;
                else if (run.HasUpgrade("FAST_RECHARGE_1")) _regenMultiplier = 1.25f;

                // "Reinforced Plating" (+25% / +50% / +75% durabilityMax)
                if (run.HasUpgrade("REINFORCED_PLATING_3")) _durabilityMaxMultiplier = 1.75f;
                else if (run.HasUpgrade("REINFORCED_PLATING_2")) _durabilityMaxMultiplier = 1.5f;
                else if (run.HasUpgrade("REINFORCED_PLATING_1")) _durabilityMaxMultiplier = 1.25f;

                // "Quick Slam" (-20% / -36% closeDuration)
                if (run.HasUpgrade("QUICK_SLAM_2")) _closeDuration = 0.16f;
                else if (run.HasUpgrade("QUICK_SLAM_1")) _closeDuration = 0.20f;
            }

            // Permanent Soul Coin Shop meta-upgrades
            if (MetaProgressionManager.HasInstance)
            {
                var meta = MetaProgressionManager.Instance;
                if (meta.HasUnlockedSoulItem("SOUL_EXTRA_ENERGY")) _energyMaxMultiplier *= 1.15f;
                if (meta.HasUnlockedSoulItem("SOUL_EXTRA_DURABILITY")) _durabilityMaxMultiplier *= 1.20f;
                if (meta.HasUnlockedSoulItem("SOUL_LIGHT_EFFICIENCY")) _energyDrainLight = 0.5f;
            }
        }
        #endregion
    }
}
