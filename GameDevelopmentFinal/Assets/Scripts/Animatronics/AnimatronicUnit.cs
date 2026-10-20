using System;
using UnityEngine;
using Tylerland.Core;
using Tylerland.Config;
using Tylerland.Office;

namespace Tylerland.Animatronics
{
    /// <summary>
    /// Section 5.1 & 5.6: Individual Animatronic AI Unit & Damage State Handler.
    /// Executes FNAF 1 style periodic d20 movement rolls (1-20 <= EffectiveAI),
    /// traverses camera rooms toward its assigned office door, obeys camera stalling rules,
    /// applies 4-tier damage behavior modifiers, and triggers doorway AttackReady windows.
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
        [SerializeField] private float _tickTimer = 0f;
        [SerializeField] private bool _isAtDoorBlindspot = false;
        [SerializeField] private bool _isStalledByCamera = false;
        [SerializeField] private int _dynamicDoorOverride = -1;

        private AudioSource _audioSource;
        private GameObject _proceduralBlindspotFigure;

        public AnimatronicConfig Config => _config;
        public string AnimatronicId => _config != null ? _config.animatronicId : "unknown";
        public string DisplayName => _config != null ? _config.displayName : "Unknown";
        public int BaseAILevel => _baseAILevel;
        public int EffectiveAILevel => _effectiveAILevel;
        public int CurrentStage => _currentStage;
        public int MaxStage => (_config != null && _config.cameraRouteIndices != null) ? _config.cameraRouteIndices.Length : 4;
        public int DamageLevel => _damageLevel;
        public bool IsAtDoorBlindspot => _isAtDoorBlindspot;
        public bool IsStalledByCamera => _isStalledByCamera;
        public int TargetDoorIndex => _dynamicDoorOverride >= 0 ? _dynamicDoorOverride : (_config != null ? _config.targetDoorIndex : 1);

        /// <summary>
        /// Returns the current camera room index (0-9) this animatronic occupies,
        /// or -1 if it has exited the cameras into the office doorway blindspot.
        /// </summary>
        public int CurrentCameraRoomIndex
        {
            get
            {
                if (_isAtDoorBlindspot || _config == null || _config.cameraRouteIndices == null || _config.cameraRouteIndices.Length == 0)
                    return -1;
                int clamped = Mathf.Clamp(_currentStage, 0, _config.cameraRouteIndices.Length - 1);
                return _config.cameraRouteIndices[clamped];
            }
        }

        private void Awake()
        {
            _audioSource = gameObject.AddComponent<AudioSource>();
            _audioSource.playOnAwake = false;
            _audioSource.spatialBlend = 0.8f; // 3D directional audio
        }

        /// <summary>
        /// Initializes this animatronic for the start of a night shift.
        /// </summary>
        public void Initialize(AnimatronicConfig config, int startingAILevel)
        {
            _config = config;
            _baseAILevel = Mathf.Clamp(startingAILevel, 0, 20);
            _currentStage = 0;
            _isAtDoorBlindspot = false;
            _isStalledByCamera = false;
            _dynamicDoorOverride = -1;

            RefreshDamageState();
            RecalculateEffectiveAI(0f);
            ResetTickTimer();
            UpdateDoorwayVisuals();
        }

        public void SetDynamicDoorOverride(int doorIndex)
        {
            if (_isAtDoorBlindspot) return; // Don't swap doors mid-attack
            _dynamicDoorOverride = Mathf.Clamp(doorIndex, 0, 3);
        }

        public void RefreshDamageState()
        {
            if (RunManager.HasInstance && _config != null)
            {
                _damageLevel = RunManager.Instance.State.GetAnimatronicDamage(_config.animatronicId);
            }
            else
            {
                _damageLevel = 0;
            }
        }

        public void RecalculateEffectiveAI(float hourBlendFactor)
        {
            if (_baseAILevel <= 0)
            {
                _effectiveAILevel = 0;
                return;
            }

            int rampBonus = _config != null ? Mathf.FloorToInt(hourBlendFactor * _config.lateNightAIRamp) : 0;

            // Section 5.6 Damage System Modifiers:
            // Level 0 (Intact): Normal
            // Level 1 (Cracked): Slightly slower tick (+15% interval), +0 AI
            // Level 2 (Broken): +1 AI aggression, erratic stage surge chance
            // Level 3 (Critical): Enraged (+3 AI aggression, faster attack window)
            int damageBonus = 0;
            if (_damageLevel == 2) damageBonus = 1;
            else if (_damageLevel >= 3) damageBonus = 3;

            _effectiveAILevel = Mathf.Clamp(_baseAILevel + rampBonus + damageBonus, 0, 20);
        }

        private void Update()
        {
            if (_config == null || _effectiveAILevel <= 0) return;

            var flowState = GameFlowManager.HasInstance ? GameFlowManager.Instance.CurrentState : GameState.NightActive;
            if (!GameStateRules.IsGameplayActive(flowState) || Time.timeScale <= 0f) return;

            // Update blindspot figure visibility based on whether the player's 'F' Hallway Light is ON
            UpdateBlindspotFigureVisibility();

            // When already at the doorway blindspot, AttackResolver handles the countdown
            if (_isAtDoorBlindspot) return;

            // Check camera stalling
            _isStalledByCamera = CheckCameraStall();
            if (_isStalledByCamera)
            {
                // Stalled on camera: pause or slow tick countdown
                return;
            }

            _tickTimer -= Time.deltaTime;
            if (_tickTimer <= 0f)
            {
                ResetTickTimer();
                AttemptMovementRoll();
            }
        }

        private bool CheckCameraStall()
        {
            if (_config == null || !CameraMonitorSystem.HasInstance) return false;
            var monitor = CameraMonitorSystem.Instance;
            if (!monitor.IsMonitorRaised) return false;

            if (_config.cameraBehavior == CameraStallBehavior.StalledWheneverMonitorOpen)
            {
                return true;
            }

            if (_config.cameraBehavior == CameraStallBehavior.StalledWhenWatchedOnCamera)
            {
                return monitor.CurrentCameraIndex == CurrentCameraRoomIndex;
            }

            return false;
        }

        private void ResetTickTimer()
        {
            float interval = _config != null ? _config.baseTickInterval : 4.8f;

            // Damage Level 1 (Cracked): Servo limp slows interval by 15%
            if (_damageLevel == 1) interval *= 1.15f;
            // Damage Level 3 (Critical): Overclocked servos speed up interval by 15%
            else if (_damageLevel >= 3) interval *= 0.85f;

            // Slight random jitter (+/- 0.25s) so all 4 animatronics don't roll on the exact same frame
            _tickTimer = Mathf.Max(1.0f, interval + UnityEngine.Random.Range(-0.25f, 0.25f));
        }

        private void AttemptMovementRoll()
        {
            // Authentic FNAF 1 d20 roll: 1..20 <= EffectiveAILevel
            int roll = UnityEngine.Random.Range(1, 21);
            if (roll > _effectiveAILevel)
            {
                return; // Failed roll, stays in current stage
            }

            AdvanceStage();
        }

        /// <summary>
        /// Advances this animatronic along its camera route toward the office doorway.
        /// Can also be triggered manually via debug keys.
        /// </summary>
        public void AdvanceStage()
        {
            if (_isAtDoorBlindspot) return;

            int oldStage = _currentStage;
            int step = 1;

            // Damage Level 2 (Broken): 20% chance to surge 2 stages at once (never skipping the final blindspot stage)
            if (_damageLevel == 2 && _currentStage + 2 < MaxStage && UnityEngine.Random.value < 0.20f)
            {
                step = 2;
                Debug.Log($"[Animatronic AI] {DisplayName} (BROKEN damage state) surged ahead 2 stages!");
            }

            int nextStage = Mathf.Min(_currentStage + step, MaxStage);

            // If entering the final stage (Doorway Blindspot / AttackReady), register with AttackResolver!
            if (nextStage >= MaxStage)
            {
                TryEnterDoorwayBlindspot(oldStage);
                return;
            }

            _currentStage = nextStage;
            PlayFootstepCue();

            Debug.Log($"[Animatronic AI] {DisplayName} moved from Stage {oldStage} -> Stage {_currentStage} (Room Index {CurrentCameraRoomIndex}) [AI={_effectiveAILevel}/20]");
            GameEvents.TriggerAnimatronicStageChanged(AnimatronicId, oldStage, _currentStage);
        }

        private void TryEnterDoorwayBlindspot(int oldStage)
        {
            if (!AttackResolver.HasInstance) return;

            float attackWindow = _config != null ? _config.attackWindowSeconds : 3.5f;

            // Damage Level 3 (Critical): Desperate lunge reduces window by 0.4s (minimum 1.5s)
            if (_damageLevel >= 3)
            {
                attackWindow = Mathf.Max(1.5f, attackWindow - 0.4f);
            }

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
                // Fairness guard held the animatronic at the corner camera outside the door
                _currentStage = Mathf.Max(0, MaxStage - 1);
                return;
            }

            _currentStage = MaxStage;
            _isAtDoorBlindspot = true;
            PlayBlindspotWarningCue();
            UpdateDoorwayVisuals();

            Debug.LogWarning($"[Animatronic AI] ⚠ {DisplayName} is in the DOOR {TargetDoorIndex} BLINDSPOT! Press 'F' to check light or close Door {TargetDoorIndex + 1} within {attackWindow:F1}s!");
            GameEvents.TriggerAnimatronicStageChanged(AnimatronicId, oldStage, _currentStage);
        }

        /// <summary>
        /// Callback invoked by AttackResolver when the player blocks the attack with a closed door.
        /// </summary>
        private void HandleBlockedRetreat()
        {
            PlayDoorBangCue();
            int retreatStage = _config != null ? Mathf.Clamp(_config.retreatStageIndex, 0, MaxStage - 1) : 0;
            ForceRetreatToStage(retreatStage);
        }

        /// <summary>
        /// Forces the animatronic back to a specified stage (used when blocked by a door or stunned by a Special Weapon).
        /// </summary>
        public void ForceRetreatToStage(int targetStage)
        {
            int oldStage = _currentStage;
            _isAtDoorBlindspot = false;
            _currentStage = Mathf.Clamp(targetStage, 0, MaxStage - 1);
            ResetTickTimer();
            UpdateDoorwayVisuals();

            GameEvents.TriggerAnimatronicStageChanged(AnimatronicId, oldStage, _currentStage);
        }

        #region Doorway 2D Sprite & Procedural Blindspot Silhouette
        private void UpdateDoorwayVisuals()
        {
            var artMgr = FindFirstObjectByType<OfficeArtManager>();
            if (artMgr != null)
            {
                if (_isAtDoorBlindspot && _config != null && _config.doorwaySpritesByDamage != null && _config.doorwaySpritesByDamage.Length > 0)
                {
                    int dmgIdx = Mathf.Clamp(_damageLevel, 0, _config.doorwaySpritesByDamage.Length - 1);
                    Sprite s = _config.doorwaySpritesByDamage[dmgIdx] != null
                        ? _config.doorwaySpritesByDamage[dmgIdx]
                        : _config.doorwaySpritesByDamage[0];
                    artMgr.SetBlindspotAnimatronicSprite(TargetDoorIndex, s);
                }
                else
                {
                    artMgr.SetBlindspotAnimatronicSprite(TargetDoorIndex, null);
                }
            }

            // Ensure procedural silhouette exists for playtesting before artist sprites are assigned
            EnsureProceduralBlindspotFigure();
            UpdateBlindspotFigureVisibility();
        }

        private void EnsureProceduralBlindspotFigure()
        {
            if (_proceduralBlindspotFigure != null) return;

            _proceduralBlindspotFigure = new GameObject($"BlindspotFigure_{DisplayName}");
            _proceduralBlindspotFigure.transform.SetParent(transform, false);

            // Body silhouette
            var body = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            body.name = "Silhouette_Body";
            body.transform.SetParent(_proceduralBlindspotFigure.transform, false);
            body.transform.localPosition = new Vector3(0f, 1.15f, 0f);
            body.transform.localScale = new Vector3(0.75f, 1.15f, 0.35f);
            Destroy(body.GetComponent<Collider>());

            // Head
            var head = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            head.name = "Silhouette_Head";
            head.transform.SetParent(_proceduralBlindspotFigure.transform, false);
            head.transform.localPosition = new Vector3(0f, 2.35f, 0f);
            head.transform.localScale = new Vector3(0.55f, 0.55f, 0.45f);
            Destroy(head.GetComponent<Collider>());

            // Glowing Left Eye
            var leftEye = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            leftEye.name = "GlowingEye_L";
            leftEye.transform.SetParent(head.transform, false);
            leftEye.transform.localPosition = new Vector3(-0.18f, 0.08f, 0.45f);
            leftEye.transform.localScale = new Vector3(0.14f, 0.14f, 0.14f);
            Destroy(leftEye.GetComponent<Collider>());

            // Glowing Right Eye
            var rightEye = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            rightEye.name = "GlowingEye_R";
            rightEye.transform.SetParent(head.transform, false);
            rightEye.transform.localPosition = new Vector3(0.18f, 0.08f, 0.45f);
            rightEye.transform.localScale = new Vector3(0.14f, 0.14f, 0.14f);
            Destroy(rightEye.GetComponent<Collider>());

            // Color the body dark and eyes glowing with theme color
            Color bodyCol = _config != null ? Color.Lerp(_config.themeColor, Color.black, 0.65f) : new Color(0.2f, 0.05f, 0.05f);
            Color eyeCol = _config != null ? _config.themeColor : Color.red;

            ApplyColorToRenderer(body.GetComponent<MeshRenderer>(), bodyCol);
            ApplyColorToRenderer(head.GetComponent<MeshRenderer>(), bodyCol);
            ApplyColorToRenderer(leftEye.GetComponent<MeshRenderer>(), eyeCol);
            ApplyColorToRenderer(rightEye.GetComponent<MeshRenderer>(), eyeCol);

            _proceduralBlindspotFigure.SetActive(false);
        }

        private void ApplyColorToRenderer(MeshRenderer mr, Color c)
        {
            if (mr == null) return;
            var mat = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard"));
            mat.color = c;
            if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", c);
            if (mat.HasProperty("_EmissionColor"))
            {
                mat.EnableKeyword("_EMISSION");
                mat.SetColor("_EmissionColor", c * 1.5f);
            }
            mr.sharedMaterial = mat;
        }

        private void UpdateBlindspotFigureVisibility()
        {
            if (_proceduralBlindspotFigure == null) return;

            // Hide procedural 3D figure if a 2D doorway sprite is already assigned for this animatronic
            bool has2DDoorwaySprite = _config != null
                && _config.doorwaySpritesByDamage != null
                && _config.doorwaySpritesByDamage.Length > 0
                && _config.doorwaySpritesByDamage[0] != null;

            if (!_isAtDoorBlindspot || has2DDoorwaySprite)
            {
                if (_proceduralBlindspotFigure.activeSelf) _proceduralBlindspotFigure.SetActive(false);
                return;
            }

            // Position the figure right outside the target door facing inward toward the player
            Vector3[] doorBlindspotPositions =
            {
                new Vector3(-5.55f, 0f, 0f), // 0 = Left (Shark)
                new Vector3(0f, 0f, 5.55f),  // 1 = Center (Tyler)
                new Vector3(5.55f, 0f, 0f),  // 2 = Right (Eagle)
                new Vector3(0f, 0f, -5.55f)  // 3 = Back (Snake)
            };
            Quaternion[] inwardRotations =
            {
                Quaternion.Euler(0, 90f, 0),
                Quaternion.Euler(0, 180f, 0),
                Quaternion.Euler(0, -90f, 0),
                Quaternion.Euler(0, 0f, 0)
            };

            int dIdx = Mathf.Clamp(TargetDoorIndex, 0, 3);
            _proceduralBlindspotFigure.transform.position = doorBlindspotPositions[dIdx];
            _proceduralBlindspotFigure.transform.rotation = inwardRotations[dIdx];

            // Only visible when the player shines the 'F' Hallway Light on this door!
            bool lightOn = false;
            if (DoorSystemManager.HasInstance)
            {
                var door = DoorSystemManager.Instance.GetDoor(dIdx);
                if (door != null) lightOn = door.IsLightOn;
            }

            if (_proceduralBlindspotFigure.activeSelf != lightOn)
            {
                _proceduralBlindspotFigure.SetActive(lightOn);
            }
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
