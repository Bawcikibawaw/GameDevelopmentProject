using System.IO;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Tylerland.Core;

namespace Tylerland.UI
{
    /// <summary>
    /// Controller for the dedicated MainMenu scene (Assets/Scenes/MainMenu.unity).
    /// Renders an atmospheric UGUI main menu with New 5-Night Shift, Night Select,
    /// Settings (Voice/Audio/Display sliders), persistent Soul Coin stats, and Quit.
    /// </summary>
    public class MainMenuController : MonoBehaviour
    {
        [Header("Scene Navigation")]
        [SerializeField] private string _gameplaySceneName = "SampleScene";

        private Canvas _canvas;
        private GameObject _mainPanel;
        private SettingsPanelUI _settingsPanel;
        private Text _statsText;
        private Image _vignetteGlow;
        private Text _statusNotice;

        private void Awake()
        {
            Time.timeScale = 1f;
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;

            AudioSettingsManager.EnsureLoaded();
            BuildMenuUI();
            RefreshPersistentStats();
        }

        private void Update()
        {
            // Subtle horror title ambience pulse
            if (_vignetteGlow != null)
            {
                float pulse = 0.08f + 0.05f * Mathf.Sin(Time.unscaledTime * 1.4f);
                _vignetteGlow.color = new Color(0.55f, 0.06f, 0.08f, pulse);
            }

            var kb = Keyboard.current;
            bool escPressed = kb != null && kb.escapeKey.wasPressedThisFrame;
#if !ENABLE_INPUT_SYSTEM || ENABLE_LEGACY_INPUT_MANAGER
            if (!escPressed && Input.GetKeyDown(KeyCode.Escape)) escPressed = true;
#endif
            if (escPressed && _settingsPanel != null && _settingsPanel.IsVisible)
            {
                _settingsPanel.SetVisible(false);
                if (_mainPanel != null) _mainPanel.SetActive(true);
            }
        }

        private void BuildMenuUI()
        {
            _canvas = TylerlandUIKit.CreateOverlayCanvas("MainMenu_Canvas", 100, transform);

            // Deep obsidian background
            var bg = TylerlandUIKit.CreatePanel(_canvas.transform, "Background", new Color(0.025f, 0.028f, 0.045f, 1f), true);

            // Atmospheric crimson glow band across top
            var glowRt = TylerlandUIKit.CreatePanel(bg, "CrimsonAtmosphere", new Color(0.55f, 0.06f, 0.08f, 0.10f), false);
            glowRt.anchorMin = new Vector2(0f, 0.55f);
            glowRt.anchorMax = new Vector2(1f, 1f);
            glowRt.offsetMin = Vector2.zero;
            glowRt.offsetMax = Vector2.zero;
            _vignetteGlow = glowRt.GetComponent<Image>();

            // Decorative subtle side security frame
            var sideAccent = TylerlandUIKit.CreatePanel(bg, "LeftRedBar", TylerlandUIKit.AccentCrimson, false);
            sideAccent.anchorMin = new Vector2(0.06f, 0.12f);
            sideAccent.anchorMax = new Vector2(0.065f, 0.88f);
            sideAccent.offsetMin = Vector2.zero;
            sideAccent.offsetMax = Vector2.zero;

            // Main Left-Center Menu Column
            var mainContainer = TylerlandUIKit.CreatePanel(bg, "MainMenuPanel", TylerlandUIKit.PanelDark, false);
            mainContainer.anchorMin = new Vector2(0.09f, 0.10f);
            mainContainer.anchorMax = new Vector2(0.56f, 0.90f);
            mainContainer.offsetMin = Vector2.zero;
            mainContainer.offsetMax = Vector2.zero;
            TylerlandUIKit.AddBorder(mainContainer.gameObject, TylerlandUIKit.BorderSubtle, 2f);
            _mainPanel = mainContainer.gameObject;

            var vlg = mainContainer.gameObject.AddComponent<VerticalLayoutGroup>();
            vlg.padding = new RectOffset(46, 46, 40, 36);
            vlg.spacing = 16f;
            vlg.childForceExpandWidth = true;
            vlg.childForceExpandHeight = false;

            // Title & Subtitle
            var title = TylerlandUIKit.CreateText(mainContainer, "TYLERLAND", 74, TylerlandUIKit.AccentCrimsonHover, TextAnchor.MiddleLeft, FontStyle.Bold);
            title.gameObject.AddComponent<LayoutElement>().preferredHeight = 84f;

            var subtitle = TylerlandUIKit.CreateText(mainContainer, "ANIMAL KINGDOM  •  5-NIGHT SURVIVAL HORROR", 20, TylerlandUIKit.TextMuted, TextAnchor.MiddleLeft, FontStyle.Italic);
            subtitle.gameObject.AddComponent<LayoutElement>().preferredHeight = 30f;

            var div1 = TylerlandUIKit.CreatePanel(mainContainer, "Div1", TylerlandUIKit.AccentCrimson, false);
            div1.gameObject.AddComponent<LayoutElement>().preferredHeight = 3f;

            // Persistent Meta Stats Bar
            _statsText = TylerlandUIKit.CreateText(mainContainer, "", 19, TylerlandUIKit.AccentGold, TextAnchor.MiddleLeft, FontStyle.Bold);
            _statsText.gameObject.AddComponent<LayoutElement>().preferredHeight = 36f;

            // Primary Play Button
            TylerlandUIKit.CreateButton(mainContainer, "▶  START NEW 5-NIGHT SHIFT", TylerlandUIKit.AccentEmerald, () => LaunchGameplay(newRun: true, night: 1), 66f, 24);

            // Quick Night Select Row (Nights 1 - 5)
            var nightLabel = TylerlandUIKit.CreateText(mainContainer, "OR JUMP DIRECTLY TO NIGHT (1–5):", 15, TylerlandUIKit.TextMuted, TextAnchor.MiddleLeft, FontStyle.Bold);
            nightLabel.gameObject.AddComponent<LayoutElement>().preferredHeight = 22f;

            var nightRow = new GameObject("NightSelectRow", typeof(RectTransform));
            nightRow.transform.SetParent(mainContainer, false);
            var nightHlg = nightRow.AddComponent<HorizontalLayoutGroup>();
            nightHlg.spacing = 10f;
            nightHlg.childForceExpandWidth = true;
            nightRow.AddComponent<LayoutElement>().preferredHeight = 46f;

            for (int n = 1; n <= 5; n++)
            {
                int nightIdx = n;
                TylerlandUIKit.CreateButton(nightRow.transform, $"NIGHT {n}", TylerlandUIKit.BtnNeutral, () => LaunchGameplay(newRun: true, night: nightIdx), 44f, 18);
            }

            // Settings Button
            TylerlandUIKit.CreateButton(mainContainer, "⚙  SETTINGS (VOICE, AUDIO & DISPLAY)", TylerlandUIKit.AccentBlue, () =>
            {
                _mainPanel.SetActive(false);
                _settingsPanel.SetVisible(true);
            }, 58f, 21);

            // Quit Button
            TylerlandUIKit.CreateButton(mainContainer, "✕  QUIT GAME", TylerlandUIKit.AccentCrimson, QuitGame, 54f, 20);

            // Notice line
            _statusNotice = TylerlandUIKit.CreateText(mainContainer, "", 16, new Color(1f, 0.55f, 0.55f), TextAnchor.MiddleLeft);
            _statusNotice.gameObject.AddComponent<LayoutElement>().preferredHeight = 26f;

            // Right-Side Briefing Card (New Mechanics & Controls Overview)
            var intelCard = TylerlandUIKit.CreatePanel(bg, "BriefingCard", TylerlandUIKit.CardBg, false);
            intelCard.anchorMin = new Vector2(0.60f, 0.16f);
            intelCard.anchorMax = new Vector2(0.94f, 0.84f);
            intelCard.offsetMin = Vector2.zero;
            intelCard.offsetMax = Vector2.zero;
            TylerlandUIKit.AddBorder(intelCard.gameObject, TylerlandUIKit.BorderSubtle, 1.5f);

            var intelVlg = intelCard.gameObject.AddComponent<VerticalLayoutGroup>();
            intelVlg.padding = new RectOffset(30, 30, 28, 28);
            intelVlg.spacing = 14f;
            intelVlg.childForceExpandWidth = true;

            var intelTitle = TylerlandUIKit.CreateText(intelCard, "SECURITY GUARD BRIEFING", 24, TylerlandUIKit.AccentGold, TextAnchor.MiddleLeft, FontStyle.Bold);
            intelTitle.gameObject.AddComponent<LayoutElement>().preferredHeight = 36f;

            var intelDiv = TylerlandUIKit.CreatePanel(intelCard, "IntelDiv", TylerlandUIKit.BorderSubtle, false);
            intelDiv.gameObject.AddComponent<LayoutElement>().preferredHeight = 2f;

            string briefingBody =
                "<b><color=#f87171>• ONE-USE WEAPONS & PERMANENT NIGHTMARE MODE</color></b>\n" +
                "Each special gun has strictly <b>1 use per 5-night cycle</b>. Shooting an animatronic stops their attack, but locks them into <b>PERMANENT NIGHTMARE MODE</b> — their aggression will surge drastically every subsequent night!\n\n" +
                "<b><color=#c084fc>• BETWEEN-NIGHTS HUB</color></b>\n" +
                "After surviving to 6:00 AM, visit the <b>Soul Coin Shop</b>, <b>Security Shop</b>, and <b>Signal Calibration Minigame</b> before starting the next shift.\n\n" +
                "<b><color=#38bdf8>• OFFICE CONTROLS</color></b>\n" +
                "<b>A / D / W / S</b> — Look Left, Right, Center, Rear\n" +
                "<b>E / Red Button</b> — Toggle Security Door Shutter\n" +
                "<b>F / White Button</b> — Toggle Hallway Blindspot Light\n" +
                "<b>Q</b> — Fire Counter-Weapon at Doorway Threat\n" +
                "<b>SPACE / Desk Monitor</b> — Toggle Security Cameras\n" +
                "<b>ESC / P</b> — Pause Menu & Audio/Voice Settings";

            var bodyTxt = TylerlandUIKit.CreateText(intelCard, briefingBody, 17, TylerlandUIKit.TextPrimary, TextAnchor.UpperLeft);
            bodyTxt.gameObject.AddComponent<LayoutElement>().flexibleHeight = 1f;

            // Settings Overlay Panel (shared UGUI SettingsPanelUI)
            _settingsPanel = new SettingsPanelUI(bg, () =>
            {
                if (_mainPanel != null) _mainPanel.SetActive(true);
            });
        }

        private void RefreshPersistentStats()
        {
            int soulCoins = 0;
            int bestNight = 1;
            int totalRuns = 0;

            if (MetaProgressionManager.HasInstance)
            {
                soulCoins = MetaProgressionManager.Instance.SoulCoins;
                bestNight = MetaProgressionManager.Instance.BestNight;
                totalRuns = MetaProgressionManager.Instance.State.totalRuns;
            }
            else
            {
                // Read saved JSON directly if MetaProgressionManager hasn't been booted yet in MainMenu scene
                string path = Path.Combine(Application.persistentDataPath, "tylerland_meta.json");
                if (File.Exists(path))
                {
                    try
                    {
                        var wrapper = JsonUtility.FromJson<SaveDataWrapper>(File.ReadAllText(path));
                        if (wrapper != null && wrapper.metaState != null)
                        {
                            soulCoins = wrapper.metaState.soulCoins;
                            bestNight = Mathf.Max(1, wrapper.metaState.bestNightReached);
                            totalRuns = wrapper.metaState.totalRuns;
                        }
                    }
                    catch { /* ignore corrupted preview read */ }
                }
            }

            if (_statsText != null)
            {
                _statsText.text = $"✦ SOUL COINS: <b>{soulCoins}</b>     |     ★ BEST NIGHT: <b>NIGHT {bestNight}</b>     |     SHIFTS LOGGED: <b>{totalRuns}</b>";
            }
        }

        private void LaunchGameplay(bool newRun, int night)
        {
            AudioSettingsManager.PlayTestTone(false);

            SceneLaunchRequest.LaunchedFromMainMenu = true;
            SceneLaunchRequest.StartNewRun = newRun;
            SceneLaunchRequest.RequestedNight = Mathf.Clamp(night, 1, 5);

            if (GameFlowManager.HasInstance && RunManager.HasInstance)
            {
                if (newRun) RunManager.Instance.StartNewRun();
                RunManager.Instance.State.currentNight = SceneLaunchRequest.RequestedNight;
            }

            if (Application.CanStreamedLevelBeLoaded(_gameplaySceneName))
            {
                SceneManager.LoadScene(_gameplaySceneName);
            }
            else if (_statusNotice != null)
            {
                _statusNotice.text = $"Scene '{_gameplaySceneName}' is not in Build Settings! Run Tylerland -> 4. Setup MainMenu & Build Settings.";
            }
        }

        private void QuitGame()
        {
            if (MetaProgressionManager.HasInstance) MetaProgressionManager.Instance.SaveToDisk();
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }
    }
}
