using UnityEngine;
using Tylerland.Core;
using Tylerland.Office;

namespace Tylerland.Animatronics
{
    /// <summary>
    /// Section 4.4.2 & 4.4.4 GDD: Sandy the Snake Camera Manipulation & Shedding Decoys.
    /// Crucial GDD Rule:
    /// - In NORMAL form, Sandy does NOT falsify camera information (straightforward camera-and-door loop).
    /// - In NIGHTMARE form (after being hit by the Spiked Mace), Nightmare Sandy fabricates false camera evidence
    ///   by shedding decoy shells on CAM 04 and boosting static interference!
    /// </summary>
    public class SnakeCameraManipulator : Singleton<SnakeCameraManipulator>
    {
        [Header("Nightmare Sandy Interference Settings (GDD §4.4.4)")]
        [Tooltip("Maximum camera static noise boost when Nightmare Sandy approaches the Rear Door")]
        [Range(0f, 0.85f)]
        [SerializeField] private float _maxStaticBoost = 0.55f;

        [Tooltip("Seconds between Nightmare Sandy attempting to spoof/decoy CAM 04")]
        [SerializeField] private float _spoofAttemptInterval = 7.0f;

        [Tooltip("Duration in seconds that a decoy shell spoofs the camera feed")]
        [SerializeField] private float _spoofDuration = 5.0f;

        [Header("Runtime State (Read Only)")]
        [SerializeField] private bool _isFeedCurrentlySpoofed = false;
        [SerializeField] private int _spoofedCameraIndex = -1;
        [SerializeField] private float _spoofTimer = 0f;
        [SerializeField] private float _attemptTimer = 0f;

        public bool IsFeedCurrentlySpoofed => _isFeedCurrentlySpoofed;
        public int SpoofedCameraIndex => _spoofedCameraIndex;

        private void Start()
        {
            GameEvents.OnAnimatronicStageChanged += HandleAnimatronicStageChanged;
            GameEvents.OnNightStarted += HandleNightStarted;
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
            GameEvents.OnAnimatronicStageChanged -= HandleAnimatronicStageChanged;
            GameEvents.OnNightStarted -= HandleNightStarted;
        }

        private void HandleNightStarted(int night)
        {
            ClearSpoof();
            if (CameraMonitorSystem.HasInstance)
            {
                CameraMonitorSystem.Instance.SetSnakeInterference(0f);
            }
        }

        private void HandleAnimatronicStageChanged(string animatronicId, int oldStage, int newStage)
        {
            if (string.IsNullOrEmpty(animatronicId)) return;
            string lower = animatronicId.ToLower();
            if (!lower.Contains("sandy") && !lower.Contains("snake")) return;

            if (!AnimatronicManager.HasInstance) return;
            var sandy = AnimatronicManager.Instance.GetAnimatronic("sandy") ?? AnimatronicManager.Instance.GetAnimatronic("snake");
            if (sandy == null) return;

            // GDD §4.4.2: Normal Sandy does NOT falsify or distort camera feeds!
            if (!sandy.IsNightmare)
            {
                if (CameraMonitorSystem.HasInstance)
                {
                    CameraMonitorSystem.Instance.SetSnakeInterference(0f);
                }
                return;
            }

            float progress = sandy.MaxStage > 0 ? Mathf.Clamp01((float)newStage / sandy.MaxStage) : 0f;
            float filterMult = (RunManager.HasInstance && RunManager.Instance.HasUpgrade("SIGNAL_FILTER")) ? 0.5f : 1.0f;

            float boost = progress * _maxStaticBoost * filterMult;
            if (CameraMonitorSystem.HasInstance)
            {
                CameraMonitorSystem.Instance.SetSnakeInterference(boost);
            }
        }

        private void Update()
        {
            var flowState = GameFlowManager.HasInstance ? GameFlowManager.Instance.CurrentState : GameState.NightActive;
            if (!GameStateRules.IsGameplayActive(flowState) || Time.timeScale <= 0f) return;

            if (_isFeedCurrentlySpoofed)
            {
                _spoofTimer -= Time.deltaTime;
                if (_spoofTimer <= 0f)
                {
                    ClearSpoof();
                }
                return;
            }

            if (!AnimatronicManager.HasInstance) return;
            var sandy = AnimatronicManager.Instance.GetAnimatronic("sandy") ?? AnimatronicManager.Instance.GetAnimatronic("snake");

            // Only Nightmare Sandy fabricates false camera evidence (§4.4.4)
            if (sandy == null || !sandy.IsNightmare || sandy.EffectiveAILevel <= 0 || sandy.CurrentStage < 1) return;

            _attemptTimer -= Time.deltaTime;
            if (_attemptTimer <= 0f)
            {
                _attemptTimer = _spoofAttemptInterval;
                float chance = (sandy.EffectiveAILevel / 20f) * 0.50f;
                if (Random.value < chance)
                {
                    TriggerCameraSpoof();
                }
            }
        }

        public void TriggerCameraSpoof()
        {
            _isFeedCurrentlySpoofed = true;
            _spoofedCameraIndex = 3; // CAM 04 (Sandy's Room)
            _spoofTimer = _spoofDuration;
            Debug.Log($"[Sandy Decoy System] Nightmare Sandy left a shed decoy shell on CAM 04 for {_spoofDuration}s!");
        }

        public void ClearSpoof()
        {
            _isFeedCurrentlySpoofed = false;
            _spoofedCameraIndex = -1;
            _spoofTimer = 0f;
        }
    }
}
