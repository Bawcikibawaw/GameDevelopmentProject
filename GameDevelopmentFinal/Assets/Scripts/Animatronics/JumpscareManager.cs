using System.Collections;
using UnityEngine;
using Tylerland.Core;
using Tylerland.Office;

namespace Tylerland.Animatronics
{
    /// <summary>
    /// Section 5.4: Attack & Jumpscare Presentation System.
    /// Forces down the security tablet, snaps the 3D camera toward the attacker's door,
    /// shakes the camera, renders full-screen 2D hand-drawn jumpscare frames,
    /// and transitions into the Game Over / Run Reset flow.
    /// </summary>
    public class JumpscareManager : Singleton<JumpscareManager>
    {
        [Header("Jumpscare Timings & Intensity (Roadmap 5.4)")]
        [Tooltip("Duration of the active jumpscare scream and animation in seconds")]
        [SerializeField] private float _jumpscareDuration = 1.6f;

        [Tooltip("Intensity of the 3D camera shake during a jumpscare")]
        [SerializeField] private float _cameraShakeMagnitude = 6.5f;

        [Tooltip("Frame rate when cycling through multiple 2D hand-drawn jumpscare sprites")]
        [SerializeField] private float _spriteAnimationFps = 14f;

        [Header("Runtime State (Read Only)")]
        [SerializeField] private bool _isJumpscarePlaying = false;
        [SerializeField] private bool _showDeathScreen = false;
        [SerializeField] private string _killerId = "";
        [SerializeField] private string _killerDisplayName = "";

        private float _jumpscareElapsed = 0f;
        private Color _killerThemeColor = Color.red;
        private Sprite[] _activeJumpscareSprites;
        private AudioSource _audioSource;

        public bool IsJumpscarePlaying => _isJumpscarePlaying;

        protected override void Awake()
        {
            base.Awake();
            _audioSource = gameObject.AddComponent<AudioSource>();
            _audioSource.playOnAwake = false;
            _audioSource.spatialBlend = 0f; // Full 2D stereo scream
        }

        private void Start()
        {
            GameEvents.OnJumpscare += HandleJumpscare;
            GameEvents.OnNightStarted += HandleNightStarted;
            GameEvents.OnGameStateChanged += HandleGameStateChanged;
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
            GameEvents.OnJumpscare -= HandleJumpscare;
            GameEvents.OnNightStarted -= HandleNightStarted;
            GameEvents.OnGameStateChanged -= HandleGameStateChanged;
        }

        private void HandleNightStarted(int night)
        {
            _isJumpscarePlaying = false;
            _showDeathScreen = false;
        }

        private void HandleGameStateChanged(GameState oldState, GameState newState)
        {
            if (newState == GameState.Menu || newState == GameState.NightIntro || newState == GameState.NightActive)
            {
                _isJumpscarePlaying = false;
                _showDeathScreen = false;
            }
        }

        private void HandleJumpscare(string animatronicId)
        {
            if (_isJumpscarePlaying) return;

            _killerId = animatronicId;
            _killerDisplayName = string.IsNullOrEmpty(animatronicId) ? "UNKNOWN" : animatronicId.ToUpper();
            _killerThemeColor = new Color(0.9f, 0.15f, 0.15f);
            _activeJumpscareSprites = null;

            int targetDoor = 1;
            if (AnimatronicManager.HasInstance)
            {
                var unit = AnimatronicManager.Instance.GetAnimatronic(animatronicId);
                if (unit != null && unit.Config != null)
                {
                    _killerDisplayName = unit.Config.displayName.ToUpper();
                    _killerThemeColor = unit.Config.themeColor;
                    _activeJumpscareSprites = unit.Config.jumpscareFrames;
                    targetDoor = unit.TargetDoorIndex;

                    if (unit.Config.jumpscareScreamSound != null && _audioSource != null)
                    {
                        _audioSource.PlayOneShot(unit.Config.jumpscareScreamSound, 1.0f);
                    }
                }
            }

            // Force monitor down and snap player view to the attacking door
            if (PlayerViewController.HasInstance)
            {
                PlayerViewController.Instance.SetMonitorState(false);
                PlayerViewController.Instance.ApplyOrientationImmediate((OfficeViewDirection)Mathf.Clamp(targetDoor, 0, 3));
            }

            StartCoroutine(JumpscareSequenceRoutine());
        }

        private IEnumerator JumpscareSequenceRoutine()
        {
            _isJumpscarePlaying = true;
            _showDeathScreen = false;
            _jumpscareElapsed = 0f;

            Transform camTransform = Camera.main != null ? Camera.main.transform : null;
            Quaternion baseRot = camTransform != null ? camTransform.localRotation : Quaternion.identity;

            bool shakeEnabled = !MetaProgressionManager.HasInstance || MetaProgressionManager.Instance.State.settings.screenShakeEnabled;

            while (_jumpscareElapsed < _jumpscareDuration)
            {
                _jumpscareElapsed += Time.unscaledDeltaTime;

                if (camTransform != null && shakeEnabled)
                {
                    float pitch = Random.Range(-_cameraShakeMagnitude, _cameraShakeMagnitude);
                    float yaw = Random.Range(-_cameraShakeMagnitude, _cameraShakeMagnitude);
                    float roll = Random.Range(-_cameraShakeMagnitude * 0.6f, _cameraShakeMagnitude * 0.6f);
                    camTransform.localRotation = baseRot * Quaternion.Euler(pitch, yaw, roll);
                }

                yield return null;
            }

            if (camTransform != null)
            {
                camTransform.localRotation = baseRot;
            }

            _isJumpscarePlaying = false;
            _showDeathScreen = true;
        }

        private void OnGUI()
        {
            int sw = Screen.width;
            int sh = Screen.height;

            if (_isJumpscarePlaying)
            {
                DrawJumpscareOverlay(sw, sh);
            }
            else if (_showDeathScreen)
            {
                DrawDeathScreenOverlay(sw, sh);
            }
        }

        private void DrawJumpscareOverlay(int sw, int sh)
        {
            bool reduceFlash = MetaProgressionManager.HasInstance && MetaProgressionManager.Instance.State.settings.reduceFlashing;
            bool flashFrame = !reduceFlash && (Mathf.FloorToInt(_jumpscareElapsed * 22f) % 2 == 0);

            // Background flash
            GUI.color = flashFrame ? new Color(_killerThemeColor.r * 0.35f, 0.02f, 0.02f, 0.96f) : new Color(0.02f, 0.02f, 0.02f, 0.96f);
            GUI.DrawTexture(new Rect(0, 0, sw, sh), Texture2D.whiteTexture);
            GUI.color = Color.white;

            // Shake offset for 2D sprite
            float shakeX = Random.Range(-18f, 18f);
            float shakeY = Random.Range(-18f, 18f);
            float scaleZoom = Mathf.Lerp(0.85f, 1.25f, Mathf.Clamp01(_jumpscareElapsed / (_jumpscareDuration * 0.4f)));

            float boxW = sw * 0.75f * scaleZoom;
            float boxH = sh * 0.85f * scaleZoom;
            Rect spriteRect = new Rect((sw - boxW) * 0.5f + shakeX, (sh - boxH) * 0.5f + shakeY, boxW, boxH);

            // Draw 2D Hand-Drawn Jumpscare Sprite if assigned by artist
            if (_activeJumpscareSprites != null && _activeJumpscareSprites.Length > 0)
            {
                int frameIdx = Mathf.FloorToInt(_jumpscareElapsed * _spriteAnimationFps) % _activeJumpscareSprites.Length;
                Sprite s = _activeJumpscareSprites[frameIdx] != null ? _activeJumpscareSprites[frameIdx] : _activeJumpscareSprites[0];
                if (s != null && s.texture != null)
                {
                    GUI.DrawTexture(spriteRect, s.texture, ScaleMode.ScaleToFit);
                    return;
                }
            }

            // Procedural Fallback Jumpscare Card (when artist PNGs are not assigned yet)
            GUIStyle screamStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 54,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter
            };
            screamStyle.normal.textColor = _killerThemeColor;

            GUI.Label(
                spriteRect,
                $"<b>[ ◉ _ ◉ ]</b>\n{_killerDisplayName} JUMPSCARE!\n<size=18><color=#cccccc>(Assign 2D Jumpscare Frames in {_killerDisplayName}'s AnimatronicConfig)</color></size>",
                screamStyle
            );
        }

        private void DrawDeathScreenOverlay(int sw, int sh)
        {
            GUI.color = new Color(0.04f, 0.01f, 0.01f, 0.96f);
            GUI.DrawTexture(new Rect(0, 0, sw, sh), Texture2D.whiteTexture);
            GUI.color = Color.white;

            int boxW = 480;
            int boxH = 290;
            Rect rect = new Rect((sw - boxW) / 2f, (sh - boxH) / 2f, boxW, boxH);
            GUI.Box(rect, "");

            GUILayout.BeginArea(new Rect(rect.x + 25, rect.y + 25, boxW - 50, boxH - 50));

            GUIStyle titleStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 34,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter
            };
            titleStyle.normal.textColor = new Color(0.95f, 0.2f, 0.2f);
            GUILayout.Label("YOU DID NOT SURVIVE", titleStyle);

            GUILayout.Space(8);
            GUIStyle subStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 15,
                alignment = TextAnchor.MiddleCenter
            };
            subStyle.normal.textColor = Color.white;
            GUILayout.Label($"Killed by: <b>{_killerDisplayName}</b>", subStyle);
            GUILayout.Label("<color=#aaaaaa>Run progress wiped. Soul Coins & permanent unlocks retained.</color>", subStyle);

            GUILayout.Space(28);

            GUI.backgroundColor = new Color(0.85f, 0.25f, 0.25f);
            if (GUILayout.Button("↻ START NEW RUN (NIGHT 1)", GUILayout.Height(46)))
            {
                _showDeathScreen = false;
                if (RunManager.HasInstance) RunManager.Instance.StartNewRun();
                if (GameFlowManager.HasInstance)
                {
                    GameFlowManager.Instance.TransitionTo(GameState.RunReset);
                    GameFlowManager.Instance.StartNightRun();
                }
            }

            GUILayout.Space(10);
            GUI.backgroundColor = new Color(0.3f, 0.35f, 0.45f);
            if (GUILayout.Button("⌂ RETURN TO MAIN MENU", GUILayout.Height(36)))
            {
                _showDeathScreen = false;
                if (RunManager.HasInstance) RunManager.Instance.StartNewRun();
                if (GameFlowManager.HasInstance) GameFlowManager.Instance.TransitionTo(GameState.RunReset);
            }
            GUI.backgroundColor = Color.white;

            GUILayout.EndArea();
        }
    }
}
