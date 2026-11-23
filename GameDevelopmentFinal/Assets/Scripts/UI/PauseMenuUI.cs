using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using Tylerland.Core;
using Tylerland.Office;

namespace Tylerland.UI
{
    /// <summary>
    /// Proper UGUI Pause Menu overlay for SampleScene.
    /// Triggered by ESC (when camera monitor is lowered) or P during NightActive.
    /// Includes Resume, Settings (with Voice/Audio sliders), Restart Night, Main Menu, and Quit.
    /// </summary>
    public class PauseMenuUI : MonoBehaviour
    {
        private Canvas _canvas;
        private GameObject _backdrop;
        private GameObject _mainPausePanel;
        private SettingsPanelUI _settingsPanel;
        private Text _shiftSubtitle;

        private void Awake()
        {
            BuildUI();
            SetPauseOverlayVisible(false);
        }

        private void OnEnable()
        {
            GameEvents.OnPauseToggled += HandlePauseToggled;
            GameEvents.OnGameStateChanged += HandleGameStateChanged;
        }

        private void OnDisable()
        {
            GameEvents.OnPauseToggled -= HandlePauseToggled;
            GameEvents.OnGameStateChanged -= HandleGameStateChanged;
        }

        private void Update()
        {
            if (!GameFlowManager.HasInstance) return;
            if (GameFlowManager.Instance.CurrentState != GameState.NightActive) return;

            var kb = Keyboard.current;
            bool escPressed = kb != null && kb.escapeKey.wasPressedThisFrame;
            bool pPressed = kb != null && kb.pKey.wasPressedThisFrame;

#if !ENABLE_INPUT_SYSTEM || ENABLE_LEGACY_INPUT_MANAGER
            if (!escPressed && Input.GetKeyDown(KeyCode.Escape)) escPressed = true;
            if (!pPressed && Input.GetKeyDown(KeyCode.P)) pPressed = true;
#endif

            if (!escPressed && !pPressed) return;

            // If paused & inside settings sub-menu, ESC returns to pause menu first
            if (GameFlowManager.Instance.IsPaused)
            {
                if (escPressed && _settingsPanel != null && _settingsPanel.IsVisible)
                {
                    _settingsPanel.SetVisible(false);
                    if (_mainPausePanel != null) _mainPausePanel.SetActive(true);
                    return;
                }
                GameFlowManager.Instance.TogglePause();
                return;
            }

            // If unpaused and player presses ESC while camera tablet is open, let PlayerViewController close the monitor first
            if (escPressed && PlayerViewController.HasInstance && PlayerViewController.Instance.IsMonitorOpen)
            {
                return;
            }

            GameFlowManager.Instance.TogglePause();
        }

        private void HandlePauseToggled(bool isPaused)
        {
            SetPauseOverlayVisible(isPaused);
        }

        private void HandleGameStateChanged(GameState oldState, GameState newState)
        {
            if (newState != GameState.NightActive)
            {
                SetPauseOverlayVisible(false);
            }
        }

        private void SetPauseOverlayVisible(bool visible)
        {
            if (_backdrop != null) _backdrop.SetActive(visible);
            if (visible)
            {
                if (_mainPausePanel != null) _mainPausePanel.SetActive(true);
                if (_settingsPanel != null) _settingsPanel.SetVisible(false);
                UpdateShiftSubtitle();
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
            }
        }

        private void UpdateShiftSubtitle()
        {
            if (_shiftSubtitle == null) return;
            int night = RunManager.HasInstance ? RunManager.Instance.CurrentNight : 1;
            string time = NightClock.HasInstance ? NightClock.Instance.GetDisplayTimeString() : "12 AM";
            _shiftSubtitle.text = $"Night {night}  •  {time}  —  Clock, Doors & AI Suspended";
        }

        private void BuildUI()
        {
            _canvas = TylerlandUIKit.CreateOverlayCanvas("PauseMenu_Canvas", 500, transform);

            var bg = TylerlandUIKit.CreatePanel(_canvas.transform, "PauseBackdrop", new Color(0.01f, 0.015f, 0.03f, 0.84f), true);
            _backdrop = bg.gameObject;

            // Main Pause Card
            var card = TylerlandUIKit.CreatePanel(bg, "MainPausePanel", TylerlandUIKit.PanelDark, false);
            card.anchorMin = new Vector2(0.32f, 0.20f);
            card.anchorMax = new Vector2(0.68f, 0.80f);
            card.offsetMin = Vector2.zero;
            card.offsetMax = Vector2.zero;
            TylerlandUIKit.AddBorder(card.gameObject, TylerlandUIKit.AccentCrimson, 2.5f);
            _mainPausePanel = card.gameObject;

            var vlg = card.gameObject.AddComponent<VerticalLayoutGroup>();
            vlg.padding = new RectOffset(44, 44, 36, 36);
            vlg.spacing = 16f;
            vlg.childForceExpandWidth = true;
            vlg.childForceExpandHeight = false;

            var title = TylerlandUIKit.CreateText(card, "⏸  GAME PAUSED", 38, TylerlandUIKit.TextPrimary, TextAnchor.MiddleCenter, FontStyle.Bold);
            title.gameObject.AddComponent<LayoutElement>().preferredHeight = 52f;

            _shiftSubtitle = TylerlandUIKit.CreateText(card, "Surveillance Suspended", 18, TylerlandUIKit.TextMuted, TextAnchor.MiddleCenter, FontStyle.Italic);
            _shiftSubtitle.gameObject.AddComponent<LayoutElement>().preferredHeight = 28f;

            var div = TylerlandUIKit.CreatePanel(card, "Div", TylerlandUIKit.BorderSubtle, false);
            div.gameObject.AddComponent<LayoutElement>().preferredHeight = 2f;

            TylerlandUIKit.CreateButton(card, "▶  RESUME SHIFT  [ESC]", TylerlandUIKit.AccentEmerald, () =>
            {
                if (GameFlowManager.HasInstance && GameFlowManager.Instance.IsPaused)
                    GameFlowManager.Instance.TogglePause();
            }, 58f, 22);

            TylerlandUIKit.CreateButton(card, "⚙  SETTINGS (VOICE / AUDIO / DISPLAY)", TylerlandUIKit.AccentBlue, () =>
            {
                _mainPausePanel.SetActive(false);
                _settingsPanel.SetVisible(true);
            }, 56f, 20);

            TylerlandUIKit.CreateButton(card, "🔄  RESTART CURRENT NIGHT", TylerlandUIKit.BtnNeutral, () =>
            {
                if (GameFlowManager.HasInstance)
                    GameFlowManager.Instance.RestartCurrentNight();
            }, 52f, 20);

            TylerlandUIKit.CreateButton(card, "🏠  RETURN TO MAIN MENU", TylerlandUIKit.BtnNeutral, () =>
            {
                if (GameFlowManager.HasInstance)
                    GameFlowManager.Instance.ReturnToMainMenuScene();
            }, 52f, 20);

            TylerlandUIKit.CreateButton(card, "✕  QUIT GAME", TylerlandUIKit.AccentCrimson, () =>
            {
                if (MetaProgressionManager.HasInstance) MetaProgressionManager.Instance.SaveToDisk();
#if UNITY_EDITOR
                UnityEditor.EditorApplication.isPlaying = false;
#else
                Application.Quit();
#endif
            }, 50f, 19);

            // Settings Sub-Panel (shared component!)
            _settingsPanel = new SettingsPanelUI(bg, () =>
            {
                if (_mainPausePanel != null) _mainPausePanel.SetActive(true);
            });
        }
    }
}
