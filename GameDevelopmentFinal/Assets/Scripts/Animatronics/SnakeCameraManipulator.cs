using UnityEngine;
using Tylerland.Core;
using Tylerland.Office;

namespace Tylerland.Animatronics
{
    /// <summary>
    /// Section 5.3: Snake Camera Manipulation System.
    /// As Snake advances through the back service vents toward the Back Door (Door 3),
    /// it boosts static noise on the security tablet and periodically spoofs ("lies" on)
    /// camera feeds to disorient the player.
    /// </summary>
    public class SnakeCameraManipulator : Singleton<SnakeCameraManipulator>
    {
        [Header("Snake Interference Settings (Roadmap 5.3)")]
        [Tooltip("Maximum camera static noise boost when Snake reaches the Back Door")]
        [Range(0f, 0.85f)]
        [SerializeField] private float _maxStaticBoost = 0.60f;

        [Tooltip("Seconds between Snake attempting to spoof/corrupt a camera feed")]
        [SerializeField] private float _spoofAttemptInterval = 8.0f;

        [Tooltip("Duration in seconds that a spoofed camera feed lies to the player")]
        [SerializeField] private float _spoofDuration = 4.5f;

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
            if (animatronicId == null || !animatronicId.ToLower().Contains("snake")) return;

            if (!AnimatronicManager.HasInstance) return;
            var snake = AnimatronicManager.Instance.GetAnimatronic("snake");
            if (snake == null) return;

            float progress = snake.MaxStage > 0 ? Mathf.Clamp01((float)newStage / snake.MaxStage) : 0f;

            // Shop Upgrade: Signal Filter reduces Snake interference by 50%
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
            var snake = AnimatronicManager.Instance.GetAnimatronic("snake");
            if (snake == null || snake.EffectiveAILevel <= 0 || snake.CurrentStage < 1) return;

            _attemptTimer -= Time.deltaTime;
            if (_attemptTimer <= 0f)
            {
                _attemptTimer = _spoofAttemptInterval;
                // Chance scales with Snake stage and AI level
                float chance = (snake.EffectiveAILevel / 20f) * 0.45f;
                if (Random.value < chance)
                {
                    TriggerCameraSpoof();
                }
            }
        }

        public void TriggerCameraSpoof()
        {
            _isFeedCurrentlySpoofed = true;
            _spoofedCameraIndex = CameraMonitorSystem.HasInstance ? CameraMonitorSystem.Instance.CurrentCameraIndex : 0;
            _spoofTimer = _spoofDuration;
            Debug.Log($"[SnakeManipulator] Snake spoofed camera feed CAM index {_spoofedCameraIndex} for {_spoofDuration}s!");
        }

        public void ClearSpoof()
        {
            _isFeedCurrentlySpoofed = false;
            _spoofedCameraIndex = -1;
            _spoofTimer = 0f;
        }
    }
}
