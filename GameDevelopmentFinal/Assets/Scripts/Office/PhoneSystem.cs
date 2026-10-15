using System;
using System.Collections;
using UnityEngine;
using Tylerland.Core;
using Tylerland.Config;

namespace Tylerland.Office
{
    public enum PhoneCallState
    {
        Idle,
        Ringing,
        Active,
        Muted,
        Completed
    }

    /// <summary>
    /// Section 4.5: Phone System.
    /// Manages the introductory and lore telephone calls at the start of each night.
    /// Features auto-answer after 4s, audio playback with ducking, mute option (from Night 2),
    /// subtitles, and lore flag unlock triggers.
    /// </summary>
    public class PhoneSystem : Singleton<PhoneSystem>
    {
        [Header("Audio Source")]
        [SerializeField] private AudioSource _audioSource;

        [Header("Timings (Roadmap 4.5)")]
        [Tooltip("Ringing duration before auto-answering the phone")]
        [SerializeField] private float _ringDuration = 4.0f;

        [Tooltip("Delay in seconds after 12:00 AM shift start before phone starts ringing")]
        [SerializeField] private float _startDelay = 2.0f;

        [Header("Runtime State (Read Only)")]
        [SerializeField] private PhoneCallState _callState = PhoneCallState.Idle;
        [SerializeField] private bool _muteAllowed = false;
        [SerializeField] private string _currentSubtitle = "";

        private Coroutine _callRoutine;

        public PhoneCallState CallState => _callState;
        public bool IsCallActive => _callState == PhoneCallState.Active;
        public string CurrentSubtitle => _currentSubtitle;
        public bool MuteAllowed => _muteAllowed;

        protected override void Awake()
        {
            base.Awake();
            if (_audioSource == null)
            {
                _audioSource = gameObject.AddComponent<AudioSource>();
                _audioSource.playOnAwake = false;
            }
        }

        private void Start()
        {
            GameEvents.OnNightStarted += HandleNightStarted;
            GameEvents.OnRunReset += HandleRunReset;
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
            GameEvents.OnNightStarted -= HandleNightStarted;
            GameEvents.OnRunReset -= HandleRunReset;
        }

        private void HandleNightStarted(int nightNumber)
        {
            // Fetch night config
            NightConfig config = null;
            if (TylerlandBootstrap.HasInstance)
            {
                config = TylerlandBootstrap.Instance.GetConfigForNight(nightNumber);
            }

            if (config != null)
            {
                TriggerNightCall(config);
            }
        }

        public void TriggerNightCall(NightConfig config)
        {
            if (_callRoutine != null) StopCoroutine(_callRoutine);
            _callRoutine = StartCoroutine(NightCallRoutine(config));
        }

        private IEnumerator NightCallRoutine(NightConfig config)
        {
            _callState = PhoneCallState.Idle;
            _muteAllowed = config.muteAllowed;
            _currentSubtitle = "";

            // Wait after shift begins
            yield return new WaitForSeconds(_startDelay);

            // Ringing state
            _callState = PhoneCallState.Ringing;
            Debug.Log("[PhoneSystem] *RING* *RING* Phone is ringing...");
            yield return new WaitForSeconds(_ringDuration);

            // Auto-answer call
            _callState = PhoneCallState.Active;
            _currentSubtitle = config.phoneCallSubtitles;
            Debug.Log($"[PhoneSystem] Call answered! Message: \"{_currentSubtitle}\"");

            // Play voice clip if available
            if (config.phoneCallClip != null && _audioSource != null)
            {
                _audioSource.clip = config.phoneCallClip;
                _audioSource.Play();

                while (_audioSource.isPlaying)
                {
                    yield return null;
                }
            }
            else
            {
                // Fallback simulation if no audio clip provided yet
                yield return new WaitForSeconds(6.0f);
            }

            CompleteCall(config.nightNumber);
        }

        public void MuteCall()
        {
            if (!_muteAllowed || _callState != PhoneCallState.Active)
            {
                Debug.LogWarning("[PhoneSystem] Cannot mute: call not active or mute not permitted on this night.");
                return;
            }

            Debug.Log("[PhoneSystem] Call muted by player.");
            _callState = PhoneCallState.Muted;
            if (_audioSource != null && _audioSource.isPlaying)
            {
                _audioSource.Stop();
            }

            _currentSubtitle = "";
        }

        private void CompleteCall(int night)
        {
            _callState = PhoneCallState.Completed;
            _currentSubtitle = "";

            // Unlock environmental lore flag if meta progression manager exists
            if (MetaProgressionManager.HasInstance)
            {
                MetaProgressionManager.Instance.UnlockLoreFlag($"LORE_CALL_NIGHT_{night}");
            }

            Debug.Log($"[PhoneSystem] Night {night} call completed.");
        }

        private void HandleRunReset()
        {
            if (_callRoutine != null) StopCoroutine(_callRoutine);
            if (_audioSource != null) _audioSource.Stop();
            _callState = PhoneCallState.Idle;
            _currentSubtitle = "";
        }
    }
}
