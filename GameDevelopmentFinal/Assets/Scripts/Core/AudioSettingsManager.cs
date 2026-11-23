using System;
using UnityEngine;

namespace Tylerland.Core
{
    /// <summary>
    /// Centralized audio & user settings controller that works in both MainMenu and SampleScene.
    /// Synchronizes sliders (Master, Voice, SFX, Music/Ambience, Brightness) and toggles
    /// (Subtitles, Fullscreen, FPS Counter, Reduce Flashing, Screen Shake) with PlayerPrefs,
    /// MetaProgressionManager, AudioListener, and live AudioSources.
    /// </summary>
    public static class AudioSettingsManager
    {
        public static event Action OnSettingsChanged;

        private const string KEY_MASTER = "Tylerland_MasterVol";
        private const string KEY_VOICE = "Tylerland_VoiceVol";
        private const string KEY_SFX = "Tylerland_SfxVol";
        private const string KEY_MUSIC = "Tylerland_MusicVol";
        private const string KEY_BRIGHTNESS = "Tylerland_Brightness";
        private const string KEY_SUBTITLES = "Tylerland_Subtitles";
        private const string KEY_REDUCE_FLASH = "Tylerland_ReduceFlash";
        private const string KEY_SHAKE = "Tylerland_ScreenShake";
        private const string KEY_FPS = "Tylerland_ShowFps";
        private const string KEY_FULLSCREEN = "Tylerland_Fullscreen";

        private static bool _initialized = false;
        private static float _masterVolume = 1f;
        private static float _voiceVolume = 1f;
        private static float _sfxVolume = 1f;
        private static float _musicVolume = 0.8f;
        private static float _brightness = 1f;
        private static bool _subtitlesEnabled = true;
        private static bool _reduceFlashing = false;
        private static bool _screenShakeEnabled = true;
        private static bool _showFpsCounter = true;
        private static bool _fullscreen = true;

        private static AudioSource _previewSource;

        public static float MasterVolume { get { EnsureLoaded(); return _masterVolume; } }
        public static float VoiceVolume { get { EnsureLoaded(); return _voiceVolume; } }
        public static float SfxVolume { get { EnsureLoaded(); return _sfxVolume; } }
        public static float MusicVolume { get { EnsureLoaded(); return _musicVolume; } }
        public static float Brightness { get { EnsureLoaded(); return _brightness; } }
        public static bool SubtitlesEnabled { get { EnsureLoaded(); return _subtitlesEnabled; } }
        public static bool ReduceFlashing { get { EnsureLoaded(); return _reduceFlashing; } }
        public static bool ScreenShakeEnabled { get { EnsureLoaded(); return _screenShakeEnabled; } }
        public static bool ShowFpsCounter { get { EnsureLoaded(); return _showFpsCounter; } }
        public static bool Fullscreen { get { EnsureLoaded(); return _fullscreen; } }

        public static void EnsureLoaded()
        {
            if (_initialized) return;
            _initialized = true;

            if (MetaProgressionManager.HasInstance && MetaProgressionManager.Instance.State?.settings != null)
            {
                var s = MetaProgressionManager.Instance.State.settings;
                _masterVolume = PlayerPrefs.GetFloat(KEY_MASTER, s.masterVolume);
                _voiceVolume = PlayerPrefs.GetFloat(KEY_VOICE, s.voiceVolume);
                _sfxVolume = PlayerPrefs.GetFloat(KEY_SFX, s.sfxVolume);
                _musicVolume = PlayerPrefs.GetFloat(KEY_MUSIC, s.musicVolume);
                _brightness = PlayerPrefs.GetFloat(KEY_BRIGHTNESS, s.brightness);
                _subtitlesEnabled = PlayerPrefs.GetInt(KEY_SUBTITLES, s.subtitlesEnabled ? 1 : 0) == 1;
                _reduceFlashing = PlayerPrefs.GetInt(KEY_REDUCE_FLASH, s.reduceFlashing ? 1 : 0) == 1;
                _screenShakeEnabled = PlayerPrefs.GetInt(KEY_SHAKE, s.screenShakeEnabled ? 1 : 0) == 1;
                _showFpsCounter = PlayerPrefs.GetInt(KEY_FPS, s.showFpsCounter ? 1 : 0) == 1;
                _fullscreen = PlayerPrefs.GetInt(KEY_FULLSCREEN, Screen.fullScreen ? 1 : 0) == 1;
            }
            else
            {
                _masterVolume = PlayerPrefs.GetFloat(KEY_MASTER, 1f);
                _voiceVolume = PlayerPrefs.GetFloat(KEY_VOICE, 1f);
                _sfxVolume = PlayerPrefs.GetFloat(KEY_SFX, 1f);
                _musicVolume = PlayerPrefs.GetFloat(KEY_MUSIC, 0.8f);
                _brightness = PlayerPrefs.GetFloat(KEY_BRIGHTNESS, 1f);
                _subtitlesEnabled = PlayerPrefs.GetInt(KEY_SUBTITLES, 1) == 1;
                _reduceFlashing = PlayerPrefs.GetInt(KEY_REDUCE_FLASH, 0) == 1;
                _screenShakeEnabled = PlayerPrefs.GetInt(KEY_SHAKE, 1) == 1;
                _showFpsCounter = PlayerPrefs.GetInt(KEY_FPS, 1) == 1;
                _fullscreen = PlayerPrefs.GetInt(KEY_FULLSCREEN, Screen.fullScreen ? 1 : 0) == 1;
            }

            ApplyLive();
        }

        public static void SetMasterVolume(float v) { EnsureLoaded(); _masterVolume = Mathf.Clamp01(v); SaveAndApply(); }
        public static void SetVoiceVolume(float v) { EnsureLoaded(); _voiceVolume = Mathf.Clamp01(v); SaveAndApply(); }
        public static void SetSfxVolume(float v) { EnsureLoaded(); _sfxVolume = Mathf.Clamp01(v); SaveAndApply(); }
        public static void SetMusicVolume(float v) { EnsureLoaded(); _musicVolume = Mathf.Clamp01(v); SaveAndApply(); }
        public static void SetBrightness(float v) { EnsureLoaded(); _brightness = Mathf.Clamp(v, 0.7f, 1.3f); SaveAndApply(); }
        public static void SetSubtitlesEnabled(bool on) { EnsureLoaded(); _subtitlesEnabled = on; SaveAndApply(); }
        public static void SetReduceFlashing(bool on) { EnsureLoaded(); _reduceFlashing = on; SaveAndApply(); }
        public static void SetScreenShakeEnabled(bool on) { EnsureLoaded(); _screenShakeEnabled = on; SaveAndApply(); }
        public static void SetShowFpsCounter(bool on) { EnsureLoaded(); _showFpsCounter = on; SaveAndApply(); }

        public static void SetFullscreen(bool on)
        {
            EnsureLoaded();
            _fullscreen = on;
            Screen.fullScreen = on;
            SaveAndApply();
        }

        public static void ResetToDefaults()
        {
            _initialized = true;
            _masterVolume = 1f;
            _voiceVolume = 1f;
            _sfxVolume = 1f;
            _musicVolume = 0.8f;
            _brightness = 1f;
            _subtitlesEnabled = true;
            _reduceFlashing = false;
            _screenShakeEnabled = true;
            _showFpsCounter = true;
            _fullscreen = true;
            Screen.fullScreen = true;
            SaveAndApply();
        }

        private static void SaveAndApply()
        {
            PlayerPrefs.SetFloat(KEY_MASTER, _masterVolume);
            PlayerPrefs.SetFloat(KEY_VOICE, _voiceVolume);
            PlayerPrefs.SetFloat(KEY_SFX, _sfxVolume);
            PlayerPrefs.SetFloat(KEY_MUSIC, _musicVolume);
            PlayerPrefs.SetFloat(KEY_BRIGHTNESS, _brightness);
            PlayerPrefs.SetInt(KEY_SUBTITLES, _subtitlesEnabled ? 1 : 0);
            PlayerPrefs.SetInt(KEY_REDUCE_FLASH, _reduceFlashing ? 1 : 0);
            PlayerPrefs.SetInt(KEY_SHAKE, _screenShakeEnabled ? 1 : 0);
            PlayerPrefs.SetInt(KEY_FPS, _showFpsCounter ? 1 : 0);
            PlayerPrefs.SetInt(KEY_FULLSCREEN, _fullscreen ? 1 : 0);
            PlayerPrefs.Save();

            if (MetaProgressionManager.HasInstance && MetaProgressionManager.Instance.State?.settings != null)
            {
                var s = MetaProgressionManager.Instance.State.settings;
                s.masterVolume = _masterVolume;
                s.voiceVolume = _voiceVolume;
                s.sfxVolume = _sfxVolume;
                s.musicVolume = _musicVolume;
                s.brightness = _brightness;
                s.subtitlesEnabled = _subtitlesEnabled;
                s.reduceFlashing = _reduceFlashing;
                s.screenShakeEnabled = _screenShakeEnabled;
                s.showFpsCounter = _showFpsCounter;
                s.fullscreen = _fullscreen;
                MetaProgressionManager.Instance.SaveToDisk();
            }

            ApplyLive();
            OnSettingsChanged?.Invoke();
        }

        public static void ApplyLive()
        {
            AudioListener.volume = _masterVolume;
        }

        /// <summary>
        /// Plays a synthesized short intercom/chime preview clip so the player can immediately test
        /// Voice or SFX slider volume even without external WAV files assigned yet.
        /// </summary>
        public static void PlayTestTone(bool isVoiceChannel)
        {
            EnsureLoaded();
            if (_previewSource == null)
            {
                var go = new GameObject("AudioSettings_Preview");
                UnityEngine.Object.DontDestroyOnLoad(go);
                _previewSource = go.AddComponent<AudioSource>();
                _previewSource.playOnAwake = false;
                _previewSource.ignoreListenerPause = true;
            }

            int sampleRate = 22050;
            float duration = isVoiceChannel ? 0.36f : 0.16f;
            int samples = Mathf.RoundToInt(sampleRate * duration);
            float[] data = new float[samples];

            for (int i = 0; i < samples; i++)
            {
                float t = (float)i / sampleRate;
                float env = Mathf.Sin(Mathf.PI * (float)i / samples);
                if (isVoiceChannel)
                {
                    // Warm dual-tone phone intercom beep-chime
                    float f = (t < 0.17f) ? 440f : 554.37f;
                    data[i] = (Mathf.Sin(2f * Mathf.PI * f * t) * 0.55f + Mathf.Sin(2f * Mathf.PI * f * 2f * t) * 0.15f) * env * 0.45f;
                }
                else
                {
                    // Crisp mechanical UI click/chime
                    data[i] = Mathf.Sin(2f * Mathf.PI * 820f * t) * env * 0.4f;
                }
            }

            var clip = AudioClip.Create(isVoiceChannel ? "VoiceTest" : "SfxTest", samples, 1, sampleRate, false);
            clip.SetData(data, 0);
            _previewSource.volume = isVoiceChannel ? _voiceVolume : _sfxVolume;
            _previewSource.PlayOneShot(clip);
        }
    }
}
