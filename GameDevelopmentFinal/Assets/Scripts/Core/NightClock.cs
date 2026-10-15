using UnityEngine;
using Tylerland.Config;

namespace Tylerland.Core
{
    /// <summary>
    /// Section 3.2: Night Clock.
    /// Drives the 12:00 AM to 6:00 AM shift and tells every other system how far into the night it is.
    /// Fires OnHourChanged and OnNightCompleted.
    /// </summary>
    public class NightClock : Singleton<NightClock>
    {
        [Header("Clock Settings (Roadmap 3.2)")]
        [Tooltip("Starting hour (0 represents 12:00 AM)")]
        [SerializeField] private int _nightStartHour = 0;

        [Tooltip("Ending hour (6 represents 6:00 AM shift completion)")]
        [SerializeField] private int _nightEndHour = 6;

        [Tooltip("Default real seconds per game hour if no NightConfig is supplied (90s = 9 min night)")]
        [SerializeField] private float _defaultSecondsPerHour = 90f;

        [Header("Runtime State (Read Only)")]
        [SerializeField] private bool _isClockRunning = false;
        [SerializeField] private float _currentHourProgress = 0f; // 0.0 to 6.0
        [SerializeField] private int _currentHourIndex = 0; // 0=12AM, 1=1AM, ..., 6=6AM
        [SerializeField] private float _hourBlendFactor = 0f; // 0.0 at 12AM, 1.0 at 6AM
        [SerializeField] private float _activeSecondsPerHour = 90f;

        public float CurrentHourProgress => _currentHourProgress;
        public int CurrentDisplayHour => _currentHourIndex == 0 ? 12 : _currentHourIndex;
        public float HourBlendFactor => _hourBlendFactor;
        public bool IsClockRunning => _isClockRunning;

        public float TotalNightDuration => _activeSecondsPerHour * (_nightEndHour - _nightStartHour);
        public float ElapsedRealTime => _currentHourProgress * _activeSecondsPerHour;
        public float RemainingRealTime => Mathf.Max(0f, TotalNightDuration - ElapsedRealTime);

        private void Start()
        {
            GameEvents.OnGameStateChanged += HandleGameStateChanged;
            GameEvents.OnRunReset += ResetClock;
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
            GameEvents.OnGameStateChanged -= HandleGameStateChanged;
            GameEvents.OnRunReset -= ResetClock;
        }

        private void Update()
        {
            if (!_isClockRunning) return;

            // Only tick if gameplay is active and game is not paused (Time.timeScale > 0)
            if (Time.timeScale <= 0f) return;

            // Advance time
            float hourDelta = Time.deltaTime / _activeSecondsPerHour;
            _currentHourProgress += hourDelta;

            // Compute hour blend factor (0.0 to 1.0)
            _hourBlendFactor = Mathf.Clamp01(_currentHourProgress / (_nightEndHour - _nightStartHour));

            // Check if hour integer flipped
            int newHourIndex = Mathf.FloorToInt(_currentHourProgress);
            if (newHourIndex != _currentHourIndex && newHourIndex <= _nightEndHour)
            {
                _currentHourIndex = newHourIndex;
                int displayHour = _currentHourIndex == 0 ? 12 : _currentHourIndex;
                Debug.Log($"[NightClock] Clock chime: {GetDisplayTimeString()} (Blend={_hourBlendFactor:F2})");
                GameEvents.TriggerHourChanged(displayHour, _hourBlendFactor);
            }

            // Check for 6:00 AM shift completion
            if (_currentHourProgress >= (_nightEndHour - _nightStartHour))
            {
                CompleteNight();
            }
        }

        #region Clock Controls
        /// <summary>
        /// Initializes the clock with a specific NightConfig.
        /// </summary>
        public void Initialize(NightConfig config)
        {
            _activeSecondsPerHour = config != null ? config.realSecondsPerGameHour : _defaultSecondsPerHour;
            ResetClock();
        }

        public void StartClock()
        {
            _isClockRunning = true;
            Debug.Log($"[NightClock] Clock started at {GetDisplayTimeString()}. {_activeSecondsPerHour}s per game hour.");
        }

        public void StopClock()
        {
            _isClockRunning = false;
        }

        public void ResetClock()
        {
            _isClockRunning = false;
            _currentHourProgress = 0f;
            _currentHourIndex = 0;
            _hourBlendFactor = 0f;
        }

        private void CompleteNight()
        {
            _isClockRunning = false;
            _currentHourProgress = _nightEndHour - _nightStartHour;
            _currentHourIndex = _nightEndHour;
            _hourBlendFactor = 1f;

            Debug.Log("[NightClock] 6:00 AM! Shift survived!");
            int nightNumber = RunManager.HasInstance ? RunManager.Instance.CurrentNight : 1;
            GameEvents.TriggerNightCompleted(nightNumber);
        }
        #endregion

        #region Display Formatting
        /// <summary>
        /// Returns friendly string representation like '12:00 AM', '1:00 AM', etc.
        /// </summary>
        public string GetDisplayTimeString()
        {
            int hour = _currentHourIndex == 0 ? 12 : _currentHourIndex;
            return $"{hour} AM";
        }
        #endregion

        #region Event Handlers
        private void HandleGameStateChanged(GameState oldState, GameState newState)
        {
            if (newState == GameState.NightActive)
            {
                StartClock();
            }
            else
            {
                StopClock();
            }
        }
        #endregion
    }
}
