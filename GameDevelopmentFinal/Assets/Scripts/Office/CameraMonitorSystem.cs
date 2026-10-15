using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using Tylerland.Core;

namespace Tylerland.Office
{
    /// <summary>
    /// Configuration and 2D hand-drawn art for a single room/corridor camera feed.
    /// Exactly like FNAF 1: holds empty room backgrounds, animatronic presence layers,
    /// audio-only states, and coordinates on the facility map.
    /// </summary>
    [Serializable]
    public class HandDrawnCameraFeed
    {
        public string roomId = "CAM_1A";
        public string cameraName = "CAM 1A - Main Stage";

        [Tooltip("Position (X: 0-1, Y: 0-1) on the security tablet floorplan map")]
        public Vector2 mapNormalizedPosition = new Vector2(0.5f, 0.8f);

        [Tooltip("Hand-drawn 2D background of this room when empty")]
        public Sprite emptyRoomSprite;

        [Tooltip("Hand-drawn 2D illustrations when animatronics are present (e.g. Far, Mid, Near)")]
        public Sprite[] animatronicPresentSprites = new Sprite[3];

        [Tooltip("Whether this room is audio-only (like the Kitchen in FNAF 1)")]
        public bool isAudioOnly = false;

        [Tooltip("Audio clip playing while viewing this camera (e.g. kitchen pots, music box)")]
        public AudioClip roomAudioClip;
    }

    /// <summary>
    /// Section 4.2: FNAF 1 Style Room & Corridor Surveillance Monitor System.
    /// Features:
    /// - 10 discrete facility rooms and hallways (Main Stage, Dining Area, West Hall,
    ///   West Corner, Pirate Cove, East Hall, East Corner, Backstage, Kitchen, Service Vent).
    /// - Interactive facility floorplan tablet map with clickable camera buttons.
    /// - 2D hand-drawn illustration display with static bursts and scanlines.
    /// - Kitchen audio-only mode.
    /// - Snake camera manipulation hooks.
    /// </summary>
    public class CameraMonitorSystem : Singleton<CameraMonitorSystem>
    {
        [Header("Monitor Tablet Timings (Roadmap 4.2)")]
        [Tooltip("Time in seconds to raise the security tablet")]
        [SerializeField] private float _monitorOpenTime = 0.4f;

        [Tooltip("Time in seconds to lower the security tablet")]
        [SerializeField] private float _monitorCloseTime = 0.3f;

        [Tooltip("Static noise burst duration when switching between camera channels")]
        [SerializeField] private float _switchStaticDuration = 0.25f;

        [Header("FNAF 1 Rooms & Corridors (10 Feeds)")]
        [SerializeField] private HandDrawnCameraFeed[] _rooms = new HandDrawnCameraFeed[10];

        [Header("Active Channel State")]
        [Tooltip("Current active camera index (0 to 9)")]
        [SerializeField] private int _currentCameraIndex = 0; // Default: CAM 1A Main Stage

        [Tooltip("Last viewed camera index remembered when reopening monitor")]
        [SerializeField] private int _lastViewedCamera = 0;

        [Header("Feed Atmosphere & Static")]
        [Range(0f, 1f)]
        [SerializeField] private float _baseFeedNoiseLevel = 0.15f;

        [Tooltip("Static noise level boost applied by Snake interference")]
        [Range(0f, 1f)]
        [SerializeField] private float _snakeNoiseBoost = 0f;

        [Header("Audio")]
        [SerializeField] private AudioSource _cameraAudioSource;

        [Header("Runtime State (Read Only)")]
        [SerializeField] private bool _isMonitorRaised = false;
        [SerializeField] private bool _isStaticBursting = false;

        private Coroutine _monitorTransitionRoutine;
        private Coroutine _staticRoutine;
        private float _animProgress = 0f; // 0 = down, 1 = up

        public bool IsMonitorRaised => _isMonitorRaised;
        public int CurrentCameraIndex => _currentCameraIndex;
        public float TotalNoiseLevel => Mathf.Clamp01(_baseFeedNoiseLevel + _snakeNoiseBoost);
        public bool IsStaticBursting => _isStaticBursting;
        public HandDrawnCameraFeed CurrentRoom => (_rooms != null && _currentCameraIndex >= 0 && _currentCameraIndex < _rooms.Length) ? _rooms[_currentCameraIndex] : null;

        protected override void Awake()
        {
            base.Awake();
            InitializeDefaultFNAF1Rooms();
            if (_cameraAudioSource == null)
            {
                _cameraAudioSource = gameObject.AddComponent<AudioSource>();
                _cameraAudioSource.playOnAwake = false;
                _cameraAudioSource.loop = true;
            }
        }

        private void InitializeDefaultFNAF1Rooms()
        {
            if (_rooms == null || _rooms.Length != 10)
            {
                _rooms = new HandDrawnCameraFeed[10];
            }

            // Authentic FNAF 1 Building Layout Setup
            string[] ids = { "CAM_1A", "CAM_1B", "CAM_2A", "CAM_2B", "CAM_3", "CAM_4A", "CAM_4B", "CAM_5", "CAM_6", "CAM_7" };
            string[] names = {
                "CAM 1A - Main Stage",
                "CAM 1B - Dining Area",
                "CAM 2A - West Corridor (Left)",
                "CAM 2B - West Hall Corner (Left Door)",
                "CAM 3 - Pirate Cove (Shark)",
                "CAM 4A - East Corridor (Right)",
                "CAM 4B - East Hall Corner (Right Door)",
                "CAM 5 - Backstage & Parts",
                "CAM 6 - Kitchen (Audio Only)",
                "CAM 7 - Service Vent (Snake / Back Door)"
            };

            Vector2[] mapCoords = {
                new Vector2(0.50f, 0.85f), // 1A: Stage (Top center)
                new Vector2(0.50f, 0.65f), // 1B: Dining (Center)
                new Vector2(0.20f, 0.45f), // 2A: West Hall (Left corridor)
                new Vector2(0.20f, 0.20f), // 2B: West Corner (Outside Left Door)
                new Vector2(0.12f, 0.65f), // 3: Pirate Cove (Far left)
                new Vector2(0.80f, 0.45f), // 4A: East Hall (Right corridor)
                new Vector2(0.80f, 0.20f), // 4B: East Corner (Outside Right Door)
                new Vector2(0.12f, 0.85f), // 5: Backstage (Top left)
                new Vector2(0.88f, 0.65f), // 6: Kitchen (Far right)
                new Vector2(0.50f, 0.15f)  // 7: Service Vent (Bottom center outside Back Door)
            };

            for (int i = 0; i < 10; i++)
            {
                if (_rooms[i] == null) _rooms[i] = new HandDrawnCameraFeed();
                _rooms[i].roomId = ids[i];
                _rooms[i].cameraName = names[i];
                _rooms[i].mapNormalizedPosition = mapCoords[i];
                if (i == 8) _rooms[i].isAudioOnly = true; // CAM 6 Kitchen
            }
        }

        #region Monitor Controls
        public void ToggleMonitor()
        {
            SetMonitorRaised(!_isMonitorRaised);
        }

        public void SetMonitorRaised(bool raised, bool immediate = false)
        {
            _isMonitorRaised = raised;

            if (immediate)
            {
                _animProgress = raised ? 1f : 0f;
                return;
            }

            if (_monitorTransitionRoutine != null) StopCoroutine(_monitorTransitionRoutine);
            float duration = raised ? _monitorOpenTime : _monitorCloseTime;
            _monitorTransitionRoutine = StartCoroutine(AnimateMonitorRoutine(raised, duration));

            // Stop/Start room audio
            if (!raised && _cameraAudioSource != null)
            {
                _cameraAudioSource.Stop();
            }
            else if (raised)
            {
                PlayRoomAudio(CurrentRoom);
            }
        }

        private IEnumerator AnimateMonitorRoutine(bool raise, float duration)
        {
            float start = _animProgress;
            float target = raise ? 1f : 0f;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.SmoothStep(0f, 1f, elapsed / duration);
                _animProgress = Mathf.Lerp(start, target, t);
                yield return null;
            }

            _animProgress = target;
            _monitorTransitionRoutine = null;
        }
        #endregion

        #region Feed Switching
        public void SwitchCameraFeed(int newIndex, bool triggerStatic = true)
        {
            if (newIndex < 0 || newIndex >= _rooms.Length) return;

            _lastViewedCamera = _currentCameraIndex;
            _currentCameraIndex = newIndex;

            if (triggerStatic && _switchStaticDuration > 0f)
            {
                if (_staticRoutine != null) StopCoroutine(_staticRoutine);
                _staticRoutine = StartCoroutine(TriggerStaticBurstRoutine(_switchStaticDuration));
            }

            var room = CurrentRoom;
            Debug.Log($"[CameraMonitor] Switched to {room.cameraName}");
            PlayRoomAudio(room);
        }

        private void PlayRoomAudio(HandDrawnCameraFeed room)
        {
            if (_cameraAudioSource == null) return;
            if (room != null && room.roomAudioClip != null)
            {
                _cameraAudioSource.clip = room.roomAudioClip;
                _cameraAudioSource.Play();
            }
            else
            {
                _cameraAudioSource.Stop();
            }
        }

        private IEnumerator TriggerStaticBurstRoutine(float duration)
        {
            _isStaticBursting = true;
            yield return new WaitForSeconds(duration);
            _isStaticBursting = false;
            _staticRoutine = null;
        }

        public void SetSnakeInterference(float noiseBoost)
        {
            _snakeNoiseBoost = Mathf.Clamp01(noiseBoost);
        }
        #endregion

        #region FNAF 1 Security Tablet GUI (OnGUI)
        private void OnGUI()
        {
            if (_animProgress <= 0.01f) return;

            int sw = Screen.width;
            int sh = Screen.height;

            // Slide tablet in from bottom
            float panelW = sw * 0.90f;
            float panelH = sh * 0.90f;
            float startY = sh;
            float targetY = (sh - panelH) / 2f;
            float currentY = Mathf.Lerp(startY, targetY, _animProgress);

            Rect tabletRect = new Rect((sw - panelW) / 2f, currentY, panelW, panelH);

            // Dark CRT Tablet Bezel
            GUI.color = new Color(0.06f, 0.06f, 0.10f, 0.98f);
            GUI.Box(tabletRect, "");
            GUI.color = Color.white;

            // Inner Viewport Screen
            Rect screenRect = new Rect(tabletRect.x + 16, tabletRect.y + 16, tabletRect.width - 32, tabletRect.height - 32);
            GUI.color = Color.black;
            GUI.DrawTexture(screenRect, Texture2D.whiteTexture);
            GUI.color = Color.white;

            var room = CurrentRoom;

            // 1. Draw 2D Room Artwork or Audio-Only Screen
            if (room != null)
            {
                if (room.isAudioOnly)
                {
                    DrawAudioOnlyScreen(screenRect);
                }
                else
                {
                    DrawRoom2DArtwork(screenRect, room);
                }
            }

            // 2. Static / Noise Burst overlay
            if (_isStaticBursting || TotalNoiseLevel > 0.05f)
            {
                DrawStaticNoiseOverlay(screenRect);
            }

            // 3. Top-Left: Channel Name & Blinking REC
            DrawCameraHeader(screenRect, room);

            // 4. Bottom-Right: Interactive FNAF 1 Facility Floorplan Map
            DrawFacilityTabletMap(screenRect);
        }

        private void DrawRoom2DArtwork(Rect screenRect, HandDrawnCameraFeed room)
        {
            Sprite spriteToDraw = room.emptyRoomSprite;

            // Draw Sprite texture if assigned
            if (spriteToDraw != null && spriteToDraw.texture != null)
            {
                GUI.DrawTexture(screenRect, spriteToDraw.texture, ScaleMode.ScaleToFit);
            }
            else
            {
                // Stylized Blueprint Mockup when artist has not delivered PNG yet
                GUIStyle mockStyle = new GUIStyle(GUI.skin.label)
                {
                    fontSize = 22,
                    alignment = TextAnchor.MiddleCenter,
                    fontStyle = FontStyle.Bold
                };
                mockStyle.normal.textColor = new Color(0.2f, 0.8f, 0.4f, 0.85f);
                GUI.Label(screenRect, $"[2D HAND-DRAWN ROOM FEED]\n<b>{room.cameraName}</b>\n<size=13><color=#aaaaaa>Drop 2D art into CameraMonitorSystem -> Rooms[{_currentCameraIndex}]</color></size>", mockStyle);
            }
        }

        private void DrawAudioOnlyScreen(Rect screenRect)
        {
            GUIStyle kitchenStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 24,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter
            };
            kitchenStyle.normal.textColor = new Color(0.9f, 0.2f, 0.2f, 0.9f);
            GUI.Label(screenRect, "● CAMERA DISABLED\n<size=16><color=white>[AUDIO FEED ONLY]</color></size>\n<size=12><color=#888888>*clattering pots & distant metallic hum*</color></size>", kitchenStyle);
        }

        private void DrawCameraHeader(Rect screenRect, HandDrawnCameraFeed room)
        {
            GUILayout.BeginArea(new Rect(screenRect.x + 20, screenRect.y + 20, 500, 80));
            bool blink = (Time.time % 1.0f) < 0.6f;
            string recDot = blink ? "<color=red>● REC</color>" : "<color=#440000>○ REC</color>";

            GUIStyle titleStyle = new GUIStyle(GUI.skin.label) { fontSize = 18, fontStyle = FontStyle.Bold };
            titleStyle.normal.textColor = Color.white;

            string roomTitle = room != null ? room.cameraName : "UNKNOWN CHANNEL";
            GUILayout.Label($"{recDot}  <b>{roomTitle}</b>", titleStyle);
            GUILayout.Label("<color=#888888>TYLERLAND ENTERTAINMENT FACILITY MAP</color>");
            GUILayout.EndArea();
        }

        private void DrawFacilityTabletMap(Rect screenRect)
        {
            // Map container in bottom-right
            float mapW = 340;
            float mapH = 240;
            Rect mapRect = new Rect(screenRect.x + screenRect.width - mapW - 16, screenRect.y + screenRect.height - mapH - 16, mapW, mapH);

            // Semi-transparent map backdrop
            GUI.color = new Color(0.04f, 0.08f, 0.12f, 0.85f);
            GUI.Box(mapRect, "");
            GUI.color = Color.white;

            // Office marker (You Are Here)
            GUIStyle officeTagStyle = new GUIStyle(GUI.skin.label) { fontSize = 10, alignment = TextAnchor.MiddleCenter, fontStyle = FontStyle.Bold };
            officeTagStyle.normal.textColor = Color.green;
            GUI.Label(new Rect(mapRect.x + mapW * 0.42f, mapRect.y + mapH * 0.78f, 60, 20), "[ YOU ]", officeTagStyle);

            // Draw camera buttons on the map floorplan
            for (int i = 0; i < _rooms.Length; i++)
            {
                var r = _rooms[i];
                if (r == null) continue;

                float btnW = 58;
                float btnH = 24;

                // Compute button position from normalized map coordinates
                float btnX = mapRect.x + (r.mapNormalizedPosition.x * (mapW - btnW - 20)) + 10;
                float btnY = mapRect.y + ((1f - r.mapNormalizedPosition.y) * (mapH - btnH - 30)) + 15;

                Rect btnRect = new Rect(btnX, btnY, btnW, btnH);

                bool isCurrent = (i == _currentCameraIndex);
                if (isCurrent) GUI.backgroundColor = new Color(0.2f, 0.9f, 0.4f);
                else GUI.backgroundColor = new Color(0.15f, 0.2f, 0.28f);

                string shortName = r.roomId.Replace("_", " ");
                if (GUI.Button(btnRect, shortName))
                {
                    SwitchCameraFeed(i);
                }
            }

            GUI.backgroundColor = Color.white;
        }

        private void DrawStaticNoiseOverlay(Rect screenRect)
        {
            float alpha = _isStaticBursting ? 0.65f : TotalNoiseLevel * 0.22f;
            GUI.color = new Color(1f, 1f, 1f, alpha);
            GUI.Label(screenRect, "");
            GUI.color = Color.white;
        }
        #endregion
    }
}
