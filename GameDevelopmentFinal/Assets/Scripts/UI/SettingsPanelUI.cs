using System;
using UnityEngine;
using UnityEngine.UI;
using Tylerland.Core;

namespace Tylerland.UI
{
    /// <summary>
    /// Reusable UGUI Settings panel used in BOTH the dedicated MainMenu scene and the in-game Pause Menu.
    /// Provides Master, Voice/Phone, SFX, Music/Ambience, and Brightness sliders + accessibility/display toggles.
    /// </summary>
    public class SettingsPanelUI
    {
        public GameObject Root { get; private set; }

        private Slider _masterSlider;
        private Slider _voiceSlider;
        private Slider _sfxSlider;
        private Slider _musicSlider;
        private Slider _brightnessSlider;

        private Toggle _subtitlesToggle;
        private Toggle _fullscreenToggle;
        private Toggle _fpsToggle;
        private Toggle _reduceFlashToggle;
        private Toggle _shakeToggle;

        public SettingsPanelUI(Transform parent, Action onClose)
        {
            AudioSettingsManager.EnsureLoaded();

            var panel = TylerlandUIKit.CreatePanel(parent, "SettingsPanel", TylerlandUIKit.PanelDark, false);
            panel.anchorMin = new Vector2(0.18f, 0.10f);
            panel.anchorMax = new Vector2(0.82f, 0.90f);
            panel.offsetMin = Vector2.zero;
            panel.offsetMax = Vector2.zero;
            TylerlandUIKit.AddBorder(panel.gameObject, TylerlandUIKit.AccentCrimson, 2.5f);
            Root = panel.gameObject;

            var vlg = panel.gameObject.AddComponent<VerticalLayoutGroup>();
            vlg.padding = new RectOffset(40, 40, 30, 30);
            vlg.spacing = 12f;
            vlg.childForceExpandWidth = true;
            vlg.childForceExpandHeight = false;

            // Header
            var title = TylerlandUIKit.CreateText(panel, "⚙ SETTINGS — AUDIO, VOICE & DISPLAY", 32, TylerlandUIKit.AccentGold, TextAnchor.MiddleCenter, FontStyle.Bold);
            title.gameObject.AddComponent<LayoutElement>().preferredHeight = 46f;

            var div = TylerlandUIKit.CreatePanel(panel, "Div", TylerlandUIKit.BorderSubtle, false);
            div.gameObject.AddComponent<LayoutElement>().preferredHeight = 2f;

            // Audio Section Header
            var audioHdr = TylerlandUIKit.CreateText(panel, "AUDIO & VOICE CHANNELS", 18, TylerlandUIKit.AccentCrimsonHover, TextAnchor.MiddleLeft, FontStyle.Bold);
            audioHdr.gameObject.AddComponent<LayoutElement>().preferredHeight = 28f;

            _masterSlider = TylerlandUIKit.CreateLabeledSlider(panel, "Master Volume", 0f, 1f, AudioSettingsManager.MasterVolume, true, v => AudioSettingsManager.SetMasterVolume(v), out _);
            _voiceSlider = TylerlandUIKit.CreateLabeledSlider(panel, "Voice / Phone Volume", 0f, 1f, AudioSettingsManager.VoiceVolume, true, v => AudioSettingsManager.SetVoiceVolume(v), out _);
            _sfxSlider = TylerlandUIKit.CreateLabeledSlider(panel, "SFX Volume", 0f, 1f, AudioSettingsManager.SfxVolume, true, v => AudioSettingsManager.SetSfxVolume(v), out _);
            _musicSlider = TylerlandUIKit.CreateLabeledSlider(panel, "Music / Ambience", 0f, 1f, AudioSettingsManager.MusicVolume, true, v => AudioSettingsManager.SetMusicVolume(v), out _);

            // Quick Audio Test Row
            var testRow = new GameObject("AudioTestRow", typeof(RectTransform));
            testRow.transform.SetParent(panel, false);
            var testHlg = testRow.AddComponent<HorizontalLayoutGroup>();
            testHlg.spacing = 16f;
            testHlg.childForceExpandWidth = true;
            testRow.AddComponent<LayoutElement>().preferredHeight = 40f;

            TylerlandUIKit.CreateButton(testRow.transform, "🔊 TEST VOICE CHANNEL", TylerlandUIKit.AccentBlue, () => AudioSettingsManager.PlayTestTone(true), 38f, 17);
            TylerlandUIKit.CreateButton(testRow.transform, "🔊 TEST SFX CHANNEL", TylerlandUIKit.BtnNeutral, () => AudioSettingsManager.PlayTestTone(false), 38f, 17);

            // Display & Accessibility Header
            var dispHdr = TylerlandUIKit.CreateText(panel, "DISPLAY & ACCESSIBILITY", 18, TylerlandUIKit.AccentCrimsonHover, TextAnchor.MiddleLeft, FontStyle.Bold);
            dispHdr.gameObject.AddComponent<LayoutElement>().preferredHeight = 28f;

            _brightnessSlider = TylerlandUIKit.CreateLabeledSlider(panel, "Brightness", 0.7f, 1.3f, AudioSettingsManager.Brightness, true, v => AudioSettingsManager.SetBrightness(v), out _);

            // 2-Column Toggles Grid
            var toggleGrid = new GameObject("ToggleGrid", typeof(RectTransform));
            toggleGrid.transform.SetParent(panel, false);
            var glg = toggleGrid.AddComponent<GridLayoutGroup>();
            glg.cellSize = new Vector2(520f, 44f);
            glg.spacing = new Vector2(20f, 8f);
            glg.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            glg.constraintCount = 2;
            toggleGrid.AddComponent<LayoutElement>().preferredHeight = 156f;

            _subtitlesToggle = TylerlandUIKit.CreateLabeledToggle(toggleGrid.transform, "Show Phone & Voice Subtitles", AudioSettingsManager.SubtitlesEnabled, v => AudioSettingsManager.SetSubtitlesEnabled(v));
            _fullscreenToggle = TylerlandUIKit.CreateLabeledToggle(toggleGrid.transform, "Fullscreen Display", AudioSettingsManager.Fullscreen, v => AudioSettingsManager.SetFullscreen(v));
            _fpsToggle = TylerlandUIKit.CreateLabeledToggle(toggleGrid.transform, "Show FPS Performance Counter", AudioSettingsManager.ShowFpsCounter, v => AudioSettingsManager.SetShowFpsCounter(v));
            _reduceFlashToggle = TylerlandUIKit.CreateLabeledToggle(toggleGrid.transform, "Reduce Hallway Fluorescent Flicker", AudioSettingsManager.ReduceFlashing, v => AudioSettingsManager.SetReduceFlashing(v));
            _shakeToggle = TylerlandUIKit.CreateLabeledToggle(toggleGrid.transform, "Camera Screen Shake", AudioSettingsManager.ScreenShakeEnabled, v => AudioSettingsManager.SetScreenShakeEnabled(v));

            // Spacer
            var spacer = new GameObject("Spacer", typeof(RectTransform));
            spacer.transform.SetParent(panel, false);
            spacer.AddComponent<LayoutElement>().flexibleHeight = 1f;

            // Footer Buttons
            var footer = new GameObject("SettingsFooter", typeof(RectTransform));
            footer.transform.SetParent(panel, false);
            var footerHlg = footer.AddComponent<HorizontalLayoutGroup>();
            footerHlg.spacing = 20f;
            footerHlg.childForceExpandWidth = true;
            footer.AddComponent<LayoutElement>().preferredHeight = 56f;

            TylerlandUIKit.CreateButton(footer.transform, "↺ RESET DEFAULTS", TylerlandUIKit.BtnNeutral, () =>
            {
                AudioSettingsManager.ResetToDefaults();
                RefreshControlsFromManager();
            }, 52f, 19);

            TylerlandUIKit.CreateButton(footer.transform, "✓ SAVE & BACK [ESC]", TylerlandUIKit.AccentEmerald, () =>
            {
                SetVisible(false);
                onClose?.Invoke();
            }, 52f, 20);

            Root.SetActive(false);
        }

        public void RefreshControlsFromManager()
        {
            AudioSettingsManager.EnsureLoaded();
            if (_masterSlider != null) _masterSlider.SetValueWithoutNotify(AudioSettingsManager.MasterVolume);
            if (_voiceSlider != null) _voiceSlider.SetValueWithoutNotify(AudioSettingsManager.VoiceVolume);
            if (_sfxSlider != null) _sfxSlider.SetValueWithoutNotify(AudioSettingsManager.SfxVolume);
            if (_musicSlider != null) _musicSlider.SetValueWithoutNotify(AudioSettingsManager.MusicVolume);
            if (_brightnessSlider != null) _brightnessSlider.SetValueWithoutNotify(AudioSettingsManager.Brightness);

            if (_subtitlesToggle != null) _subtitlesToggle.SetIsOnWithoutNotify(AudioSettingsManager.SubtitlesEnabled);
            if (_fullscreenToggle != null) _fullscreenToggle.SetIsOnWithoutNotify(AudioSettingsManager.Fullscreen);
            if (_fpsToggle != null) _fpsToggle.SetIsOnWithoutNotify(AudioSettingsManager.ShowFpsCounter);
            if (_reduceFlashToggle != null) _reduceFlashToggle.SetIsOnWithoutNotify(AudioSettingsManager.ReduceFlashing);
            if (_shakeToggle != null) _shakeToggle.SetIsOnWithoutNotify(AudioSettingsManager.ScreenShakeEnabled);
        }

        public void SetVisible(bool visible)
        {
            if (visible) RefreshControlsFromManager();
            if (Root != null) Root.SetActive(visible);
        }

        public bool IsVisible => Root != null && Root.activeSelf;
    }
}
