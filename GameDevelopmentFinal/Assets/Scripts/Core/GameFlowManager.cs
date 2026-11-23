using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Tylerland.Core
{
    /// <summary>
    /// Cross-scene launch parameters passed from MainMenu scene to SampleScene.
    /// </summary>
    public static class SceneLaunchRequest
    {
        public static bool LaunchedFromMainMenu = false;
        public static bool StartNewRun = true;
        public static int RequestedNight = 1;
    }

    /// <summary>
    /// Section 3.1: Game Flow State Machine.
    /// Central manager owning the active game mode/state and coordinating transitions.
    /// Transitions follow strict rules with black fade transitions.
    /// </summary>
    public class GameFlowManager : Singleton<GameFlowManager>
    {
        [Header("Timing Parameters (Roadmap 3.1)")]
        [Tooltip("Black fade duration between states")]
        [SerializeField] private float _fadeDuration = 0.6f;

        [Tooltip("Duration of 'Night N, 12:00 AM' intro card")]
        [SerializeField] private float _nightIntroDuration = 3.0f;

        [Header("State Tracking (Read Only)")]
        [SerializeField] private GameState _currentState = GameState.Boot;
        [SerializeField] private bool _isPaused = false;
        [SerializeField] private bool _isTransitioning = false;

        [Header("Optional References")]
        [SerializeField] private ScreenFader _screenFader;

        // Transition validation lookup table
        private readonly Dictionary<GameState, HashSet<GameState>> _allowedTransitions =
            new Dictionary<GameState, HashSet<GameState>>
            {
                { GameState.Boot,       new HashSet<GameState> { GameState.Menu, GameState.NightIntro } },
                { GameState.Menu,       new HashSet<GameState> { GameState.NightIntro } },
                { GameState.NightIntro, new HashSet<GameState> { GameState.NightActive, GameState.Menu } },
                { GameState.NightActive,new HashSet<GameState> { GameState.NightEnd, GameState.Dead, GameState.Menu, GameState.NightIntro } },
                { GameState.NightEnd,   new HashSet<GameState> { GameState.Salary, GameState.Choice, GameState.Shop, GameState.Minigame, GameState.NightIntro, GameState.Menu, GameState.Victory } },
                { GameState.Salary,     new HashSet<GameState> { GameState.Choice, GameState.Shop, GameState.Minigame, GameState.Victory, GameState.NightIntro, GameState.Menu } },
                { GameState.Choice,     new HashSet<GameState> { GameState.Shop, GameState.Minigame, GameState.NightIntro, GameState.Victory, GameState.Menu } },
                { GameState.Shop,       new HashSet<GameState> { GameState.Choice, GameState.Minigame, GameState.NightIntro, GameState.Victory, GameState.Menu } },
                { GameState.Minigame,   new HashSet<GameState> { GameState.Choice, GameState.Shop, GameState.NightIntro, GameState.Victory, GameState.Menu } },
                { GameState.Dead,       new HashSet<GameState> { GameState.RunReset, GameState.NightIntro, GameState.Menu } },
                { GameState.RunReset,   new HashSet<GameState> { GameState.Menu, GameState.NightIntro } },
                { GameState.Victory,    new HashSet<GameState> { GameState.RunReset, GameState.Menu } }
            };

        public GameState CurrentState => _currentState;
        public bool IsPaused => _isPaused;
        public bool IsTransitioning => _isTransitioning;

        protected override void Awake()
        {
            base.Awake();
            if (_screenFader == null)
            {
                _screenFader = GetComponentInChildren<ScreenFader>();
                if (_screenFader == null)
                {
                    var faderObj = new GameObject("ScreenFader");
                    faderObj.transform.SetParent(transform);
                    _screenFader = faderObj.AddComponent<ScreenFader>();
                }
            }
        }

        private void Start()
        {
            // Subscribe to external events that trigger transitions
            GameEvents.OnNightCompleted += HandleNightCompleted;
            GameEvents.OnNightStarted += HandleNightStarted;
            GameEvents.OnPlayerDied += HandlePlayerDied;

            // Boot state initial progression
            if (_currentState == GameState.Boot)
            {
                StartCoroutine(BootSequence());
            }
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
            GameEvents.OnNightCompleted -= HandleNightCompleted;
            GameEvents.OnNightStarted -= HandleNightStarted;
            GameEvents.OnPlayerDied -= HandlePlayerDied;
        }

        private IEnumerator BootSequence()
        {
            Debug.Log("[GameFlowManager] Booting Tylerland Systems...");
            yield return new WaitForSeconds(0.5f);
            TransitionTo(GameState.Menu);
        }

        #region Public Transition Triggers
        /// <summary>
        /// Request a state transition. Validates transition rules and executes with fade.
        /// </summary>
        public bool TransitionTo(GameState targetState)
        {
            if (_isTransitioning)
            {
                Debug.LogWarning($"[GameFlowManager] Cannot transition to {targetState}; already transitioning.");
                return false;
            }

            if (!_allowedTransitions.TryGetValue(_currentState, out var validTargets) || !validTargets.Contains(targetState))
            {
                Debug.LogError($"[GameFlowManager] Invalid transition: {_currentState} -> {targetState}. Ignored.");
                return false;
            }

            StartCoroutine(ExecuteTransition(targetState));
            return true;
        }

        /// <summary>
        /// Starts a new run or continues to the night intro from the Menu.
        /// </summary>
        public void StartNightRun()
        {
            if (_currentState == GameState.Menu)
            {
                TransitionTo(GameState.NightIntro);
            }
        }

        /// <summary>
        /// Restarts the currently active night cleanly from Pause or Game Over.
        /// </summary>
        public void RestartCurrentNight()
        {
            if (_isPaused)
            {
                _isPaused = false;
                Time.timeScale = 1f;
                GameEvents.TriggerPauseToggled(false);
            }
            if (Tylerland.Office.CameraMonitorSystem.HasInstance)
            {
                Tylerland.Office.CameraMonitorSystem.Instance.SetMonitorRaised(false, immediate: true);
            }
            if (Tylerland.Office.PlayerViewController.HasInstance)
            {
                Tylerland.Office.PlayerViewController.Instance.SetMonitorState(false);
            }
            if (Tylerland.Office.AttackResolver.HasInstance)
            {
                Tylerland.Office.AttackResolver.Instance.ClearAllThreats();
            }

            StopAllCoroutines();
            _isTransitioning = false;
            StartCoroutine(ExecuteTransition(GameState.NightIntro));
        }

        /// <summary>
        /// Cleanly exits the office scene and loads the dedicated MainMenu scene if present in Build Settings.
        /// </summary>
        public void ReturnToMainMenuScene()
        {
            if (_isPaused)
            {
                _isPaused = false;
                Time.timeScale = 1f;
                GameEvents.TriggerPauseToggled(false);
            }
            StopAllCoroutines();
            _isTransitioning = false;
            _currentState = GameState.Menu;
            GameEvents.TriggerGameStateChanged(_currentState, GameState.Menu);

            if (Application.CanStreamedLevelBeLoaded("MainMenu"))
            {
                SceneLaunchRequest.LaunchedFromMainMenu = false;
                SceneManager.LoadScene("MainMenu");
            }
            else
            {
                Debug.LogWarning("[GameFlowManager] 'MainMenu' scene not in Build Settings yet; staying in Menu state.");
            }
        }

        /// <summary>
        /// Launches the gameplay scene cleanly from MainMenu or starts NightIntro if already in SampleScene.
        /// </summary>
        public void BeginGameplayFromMenu(bool newRun, int targetNight = 1)
        {
            if (_isPaused)
            {
                _isPaused = false;
                Time.timeScale = 1f;
            }
            StopAllCoroutines();
            _isTransitioning = false;

            if (RunManager.HasInstance)
            {
                if (newRun)
                {
                    RunManager.Instance.StartNewRun();
                }
                RunManager.Instance.State.currentNight = Mathf.Max(1, targetNight);
            }

            _currentState = GameState.Menu;
            StartCoroutine(ExecuteTransition(GameState.NightIntro));
        }

        /// <summary>
        /// Toggles pause state. Only allowed during NightActive.
        /// Freezes clock, AI tick, and audio timers.
        /// </summary>
        public void TogglePause()
        {
            if (!GameStateRules.CanPause(_currentState))
            {
                Debug.LogWarning($"[GameFlowManager] Pause not allowed in state {_currentState}.");
                return;
            }

            _isPaused = !_isPaused;
            Time.timeScale = _isPaused ? 0f : 1f;
            GameEvents.TriggerPauseToggled(_isPaused);
            Debug.Log($"[GameFlowManager] Game {(_isPaused ? "PAUSED" : "RESUMED")}");
        }
        #endregion

        #region Internal Transition Coroutine
        private IEnumerator ExecuteTransition(GameState targetState)
        {
            _isTransitioning = true;
            GameState previousState = _currentState;
            Debug.Log($"[GameFlowManager] Transition START: [{previousState}] -> [{targetState}]");

            // 1. Unpause if transitioning out of paused state
            if (_isPaused)
            {
                _isPaused = false;
                Time.timeScale = 1f;
                GameEvents.TriggerPauseToggled(false);
            }

            // 2. Fade to black
            if (_screenFader != null && _fadeDuration > 0f)
            {
                yield return _screenFader.FadeToBlack(_fadeDuration);
            }

            // 3. Update state
            _currentState = targetState;
            GameEvents.TriggerGameStateChanged(previousState, targetState);

            // 4. Special state handling during black screen
            if (targetState == GameState.NightIntro)
            {
                // Announce the night that is actually about to be played (e.g. Night 4 picked from the menu)
                // so AnimatronicManager, NightClock, PhoneSystem etc. all initialize for THAT night.
                // Skipped if this transition was itself triggered by an OnNightStarted event (avoids double init).
                if (_suppressNextNightStartedBroadcast)
                {
                    _suppressNextNightStartedBroadcast = false;
                }
                else
                {
                    int night = RunManager.HasInstance ? RunManager.Instance.CurrentNight : 1;
                    Debug.Log($"[GameFlowManager] Broadcasting OnNightStarted({night}) for NightIntro.");
                    GameEvents.TriggerNightStarted(night);
                }

                // Show intro card while black/faded
                yield return new WaitForSeconds(_nightIntroDuration);
                // Auto-advance from NightIntro to NightActive
                _currentState = GameState.NightActive;
                GameEvents.TriggerGameStateChanged(GameState.NightIntro, GameState.NightActive);
            }
            else if (targetState == GameState.NightEnd)
            {
                // Ensure monitor tablet is fully lowered and camera view is reset
                if (Tylerland.Office.CameraMonitorSystem.HasInstance)
                {
                    Tylerland.Office.CameraMonitorSystem.Instance.SetMonitorRaised(false, immediate: true);
                }
                if (Tylerland.Office.PlayerViewController.HasInstance)
                {
                    Tylerland.Office.PlayerViewController.Instance.SetMonitorState(false);
                }
                if (Tylerland.Office.AttackResolver.HasInstance)
                {
                    Tylerland.Office.AttackResolver.Instance.ClearAllThreats();
                }
                if (Tylerland.Animatronics.AnimatronicManager.HasInstance)
                {
                    Tylerland.Animatronics.AnimatronicManager.Instance.ResetAllToHome();
                }
            }
            else if (targetState == GameState.RunReset)
            {
                GameEvents.TriggerRunReset();
                yield return new WaitForSeconds(0.5f);
                // Auto-advance to Menu
                _currentState = GameState.Menu;
                GameEvents.TriggerGameStateChanged(GameState.RunReset, GameState.Menu);
            }

            // 5. Fade from black
            if (_screenFader != null && _fadeDuration > 0f)
            {
                yield return _screenFader.FadeFromBlack(_fadeDuration);
            }

            _isTransitioning = false;
            Debug.Log($"[GameFlowManager] Transition COMPLETE: Now in [{_currentState}]");
        }
        #endregion

        #region Event Callbacks
        private void HandleNightCompleted(int nightNumber)
        {
            if (_currentState == GameState.NightActive)
            {
                TransitionTo(GameState.NightEnd);
            }
        }

        private bool _suppressNextNightStartedBroadcast = false;

        private void HandleNightStarted(int nightNumber)
        {
            if (_currentState != GameState.NightActive && _currentState != GameState.NightIntro)
            {
                _suppressNextNightStartedBroadcast = true;
                if (!TransitionTo(GameState.NightIntro))
                {
                    _suppressNextNightStartedBroadcast = false;
                }
            }
        }

        private void HandlePlayerDied(string killer)
        {
            if (_currentState == GameState.NightActive)
            {
                TransitionTo(GameState.Dead);
            }
        }
        #endregion
    }
}
