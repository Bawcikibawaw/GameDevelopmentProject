using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Tylerland.Core;
using Tylerland.Animatronics;

namespace Tylerland.Office
{
    /// <summary>
    /// Configuration and 2D hand-drawn art for one of the 4 Animatronic Camera Rooms.
    /// Holds the empty room 2D sprite, the stage 2D sprites when the animatronic is inside the room,
    /// and coordinates on the 4-room tablet map.
    /// </summary>
    [Serializable]
    public class HandDrawnCameraFeed
    {
        public string roomId = "CAM_01";
        public string cameraName = "CAM 01 - Erle the Eagle (Left)";

        [Tooltip("Position (X: 0-1, Y: 0-1) on the security tablet 4-room map")]
        public Vector2 mapNormalizedPosition = new Vector2(0.08f, 0.50f);

        [Tooltip("Hand-drawn 2D sprite of this room when EMPTY (when the animatronic has left for your office door!)")]
        public Sprite emptyRoomSprite;

        [Tooltip("Hand-drawn 2D sprites when the animatronic is inside this room (optional override per stage)")]
        public Sprite[] animatronicPresentSprites = new Sprite[6];

        [Tooltip("Whether this room is audio-only (e.g. after Nightmare Tyler destroys CAM 02)")]
        public bool isAudioOnly = false;

        [Tooltip("Audio clip playing while viewing this camera")]
        public AudioClip roomAudioClip;
    }

    /// <summary>
    /// Section 4.2 & GDD Section 4: 4-Room 2D Sprite Surveillance Monitor System.
    /// Monitors the 4 dedicated Animatronic Rooms surrounding the Security Office:
    /// - Room 0 (CAM 01 - Left):   Erle the Eagle (Sheriff / Security Mascot)
    /// - Room 1 (CAM 02 - Center): Tyler the Lion (Lead Singer / Central Mascot — destroys camera in Nightmare form!)
    /// - Room 2 (CAM 03 - Right):  Orville the Orca (Medical Attendant — 80% retreat / 20% +2 advance in Normal; +1 advance when watched in Nightmare!)
    /// - Room 3 (CAM 04 - Rear):   Sandy the Snake (Maintenance Mascot — 4 stages Normal; 5 stages + shed decoy shells in Nightmare!)
    /// </summary>
    public class CameraMonitorSystem : Singleton<CameraMonitorSystem>
    {
        [Header("Monitor Tablet Timings (Roadmap 4.2)")]
        [Tooltip("Time in seconds to raise the security tablet")]
        [SerializeField] private float _monitorOpenTime = 0.35f;

        [Tooltip("Time in seconds to lower the security tablet")]
        [SerializeField] private float _monitorCloseTime = 0.25f;

        [Tooltip("Static noise burst duration when switching between the 4 camera rooms")]
        [SerializeField] private float _switchStaticDuration = 0.20f;

        [Header("The 4 Animatronic Camera Rooms (0=Erle, 1=Tyler, 2=Orville, 3=Sandy)")]
        [SerializeField] private HandDrawnCameraFeed[] _rooms = new HandDrawnCameraFeed[4];

        [Header("Active Channel State")]
        [Tooltip("Current active camera room index (0=Erle, 1=Tyler, 2=Orville, 3=Sandy)")]
        [SerializeField] private int _currentCameraIndex = 1; // Default: CAM 02 Tyler's Room

        [Tooltip("Last viewed camera index remembered when reopening monitor")]
        [SerializeField] private int _lastViewedCamera = 1;

        [Header("Feed Atmosphere & Static")]
        [Range(0f, 1f)]
        [SerializeField] private float _baseFeedNoiseLevel = 0.12f;

        [Tooltip("Static noise level boost applied by Nightmare Sandy interference")]
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
        public HandDrawnCameraFeed[] Rooms => _rooms;
        public HandDrawnCameraFeed CurrentRoom => (_rooms != null && _currentCameraIndex >= 0 && _currentCameraIndex < _rooms.Length)
            ? _rooms[_currentCameraIndex]
            : null;

        protected override void Awake()
        {
            base.Awake();
            InitializeFourAnimatronicRooms();
            if (_cameraAudioSource == null)
            {
                _cameraAudioSource = gameObject.AddComponent<AudioSource>();
                _cameraAudioSource.playOnAwake = false;
                _cameraAudioSource.loop = true;
            }
        }

        private void OnValidate()
        {
            InitializeFourAnimatronicRooms();
        }

        /// <summary>
        /// Ensures there are strictly 4 Camera Rooms corresponding to the 4 GDD Animatronics' Rooms.
        /// </summary>
        public void InitializeFourAnimatronicRooms()
        {
            if (_rooms == null || _rooms.Length != 4)
            {
                var old = _rooms;
                _rooms = new HandDrawnCameraFeed[4];
                if (old != null)
                {
                    for (int i = 0; i < Mathf.Min(4, old.Length); i++)
                    {
                        _rooms[i] = old[i];
                    }
                }
            }

            string[] ids = { "CAM_01", "CAM_02", "CAM_03", "CAM_04" };
            string[] names =
            {
                "CAM 01 - Erle the Eagle (Left / Sheriff)",
                "CAM 02 - Tyler the Lion (Center / Main Stage)",
                "CAM 03 - Orville the Orca (Right / Medical Bay)",
                "CAM 04 - Sandy the Snake (Rear / Maintenance)"
            };

            // 4-Room Cross Layout surrounding the Office in the center (0.50, 0.50)
            Vector2[] mapCoords =
            {
                new Vector2(0.08f, 0.50f), // 0: Left (Erle the Eagle)
                new Vector2(0.50f, 0.86f), // 1: Center (Tyler the Lion)
                new Vector2(0.92f, 0.50f), // 2: Right (Orville the Orca)
                new Vector2(0.50f, 0.14f)  // 3: Rear (Sandy the Snake)
            };

            for (int i = 0; i < 4; i++)
            {
                if (_rooms[i] == null) _rooms[i] = new HandDrawnCameraFeed();
                _rooms[i].roomId = ids[i];
                _rooms[i].cameraName = names[i];
                _rooms[i].mapNormalizedPosition = mapCoords[i];
            }

            _currentCameraIndex = Mathf.Clamp(_currentCameraIndex, 0, 3);
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

        #region 4-Room 2D Sprite Security Tablet GUI (OnGUI)
        private void OnGUI()
        {
            if (_animProgress <= 0.01f) return;

            int sw = Screen.width;
            int sh = Screen.height;

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

            // 1. Draw 2D Room Artwork & 2D Animatronic Stage Sprite
            if (room != null)
            {
                DrawRoom2DArtwork(screenRect, room);
            }

            // 2. Static / Noise Burst overlay
            if (_isStaticBursting || TotalNoiseLevel > 0.05f)
            {
                DrawStaticNoiseOverlay(screenRect);
            }

            // 3. Top-Left: Channel Name & Blinking REC
            DrawCameraHeader(screenRect, room);

            // 4. Bottom-Right: Interactive 4-Room Cross Map with Live Animatronic Pointers
            DrawFourRoomTabletMap(screenRect);
        }

        private void DrawRoom2DArtwork(Rect screenRect, HandDrawnCameraFeed room)
        {
            // Check if Nightmare Tyler has destroyed CAM 02 (GDD Section 4.2.3)
            var tylerUnit = AnimatronicManager.HasInstance ? AnimatronicManager.Instance.GetAnimatronic("tyler") : null;
            if (_currentCameraIndex == 1 && tylerUnit != null && tylerUnit.IsCameraDestroyed)
            {
                DrawDestroyedCameraScreen(screenRect, tylerUnit);
                return;
            }

            bool isSpoofed = SnakeCameraManipulator.HasInstance
                && SnakeCameraManipulator.Instance.IsFeedCurrentlySpoofed
                && SnakeCameraManipulator.Instance.SpoofedCameraIndex == _currentCameraIndex;

            List<AnimatronicUnit> occupants = (!isSpoofed && AnimatronicManager.HasInstance)
                ? AnimatronicManager.Instance.GetAnimatronicsInCameraRoom(_currentCameraIndex)
                : new List<AnimatronicUnit>();

            var sandyUnit = AnimatronicManager.HasInstance ? AnimatronicManager.Instance.GetAnimatronic("sandy") : null;
            bool showSandyDecoy = (_currentCameraIndex == 3 && sandyUnit != null && sandyUnit.HasActiveDecoyShell);

            string[] ownerIds = { "erle", "tyler", "orville", "sandy" };
            var roomOwner = AnimatronicManager.HasInstance
                ? AnimatronicManager.Instance.GetAnimatronic(ownerIds[Mathf.Clamp(_currentCameraIndex, 0, 3)])
                : null;

            Sprite customSpriteToDraw = room.emptyRoomSprite;

            if (occupants.Count > 0)
            {
                var primary = occupants[0];
                Sprite[] stageSprites = (primary.IsNightmare && primary.Config != null && primary.Config.nightmareCameraStageSprites != null && primary.Config.nightmareCameraStageSprites.Length > 0)
                    ? primary.Config.nightmareCameraStageSprites
                    : (primary.Config != null ? primary.Config.cameraStageSprites : null);

                if (stageSprites != null && stageSprites.Length > 0)
                {
                    int stageIdx = Mathf.Clamp(primary.CurrentStage, 0, stageSprites.Length - 1);
                    if (stageSprites[stageIdx] != null)
                    {
                        customSpriteToDraw = stageSprites[stageIdx];
                    }
                }
                else if (room.animatronicPresentSprites != null && room.animatronicPresentSprites.Length > 0)
                {
                    int idx = Mathf.Clamp(primary.CurrentStage, 0, room.animatronicPresentSprites.Length - 1);
                    if (room.animatronicPresentSprites[idx] != null)
                    {
                        customSpriteToDraw = room.animatronicPresentSprites[idx];
                    }
                }
            }
            else if (showSandyDecoy && sandyUnit.Config != null && sandyUnit.Config.decoyShellSprite != null)
            {
                customSpriteToDraw = sandyUnit.Config.decoyShellSprite;
            }
            else if (roomOwner != null && roomOwner.Config != null && roomOwner.Config.emptyRoomSprite != null)
            {
                customSpriteToDraw = roomOwner.Config.emptyRoomSprite;
            }

            // If artist has assigned a 2D room sprite, draw it full-screen on the tablet!
            if (customSpriteToDraw != null && customSpriteToDraw.texture != null)
            {
                GUI.DrawTexture(screenRect, customSpriteToDraw.texture, ScaleMode.ScaleToFit);
            }
            else
            {
                // Otherwise, draw our generated 2D Room Backdrop Sprite + 2D Animatronic Character Sprite!
                Color[] roomAccents =
                {
                    new Color(0.95f, 0.75f, 0.20f, 1f), // CAM 01 Erle the Eagle (Left)
                    new Color(0.85f, 0.35f, 0.25f, 1f), // CAM 02 Tyler the Lion (Center)
                    new Color(0.20f, 0.65f, 0.90f, 1f), // CAM 03 Orville the Orca (Right)
                    new Color(0.35f, 0.85f, 0.40f, 1f)  // CAM 04 Sandy the Snake (Rear)
                };
                Sprite roomBg = Procedural2DSpriteFactory.GetOrCreateCorridorSprite(_currentCameraIndex, roomAccents[Mathf.Clamp(_currentCameraIndex, 0, 3)]);
                if (roomBg != null && roomBg.texture != null)
                {
                    GUI.color = new Color(0.65f, 0.68f, 0.72f, 1f);
                    GUI.DrawTexture(screenRect, roomBg.texture, ScaleMode.StretchToFill);
                    GUI.color = Color.white;
                }

                // If Nightmare Sandy shed a decoy shell in CAM 04, draw the translucent shed shell first
                if (showSandyDecoy)
                {
                    float decoyProgress = sandyUnit.MaxStage > 1 ? (float)sandyUnit.DecoyShellStage / (sandyUnit.MaxStage - 1) : 0f;
                    float decoyScale = Mathf.Lerp(0.40f, 0.85f, decoyProgress);
                    Sprite decoySprite = Procedural2DSpriteFactory.GetOrCreateAnimatronic2DSprite("sandy", new Color(0.55f, 0.65f, 0.50f), 1, 1.0f);
                    if (decoySprite != null && decoySprite.texture != null)
                    {
                        float spriteH = screenRect.height * decoyScale;
                        float spriteW = spriteH * (160f / 224f);
                        Rect decoyRect = new Rect(
                            screenRect.x + (screenRect.width - spriteW) * 0.5f - 95f,
                            screenRect.y + screenRect.height - spriteH - 20f,
                            spriteW,
                            spriteH
                        );
                        GUI.color = new Color(0.70f, 0.85f, 0.70f, 0.55f); // Hollow shed skin tint
                        GUI.DrawTexture(decoyRect, decoySprite.texture, ScaleMode.ScaleToFit);
                        GUI.color = Color.white;
                    }
                }

                // Draw each present animatronic as a 2D Sprite scaled by its Stage (Stage 1 = Far -> Final Stage = Close to lens!)
                if (occupants.Count > 0)
                {
                    for (int i = 0; i < occupants.Count; i++)
                    {
                        var u = occupants[i];
                        float stageProgress = u.MaxStage > 1 ? (float)u.CurrentStage / (u.MaxStage - 1) : 0f;
                        float scale = Mathf.Lerp(0.38f, 0.90f, stageProgress);

                        Sprite charSprite = Procedural2DSpriteFactory.GetOrCreateAnimatronic2DSprite(
                            u.AnimatronicId,
                            u.Config != null ? u.Config.themeColor : Color.red,
                            u.DamageLevel,
                            1.0f
                        );

                        if (charSprite != null && charSprite.texture != null)
                        {
                            float spriteH = screenRect.height * scale;
                            float spriteW = spriteH * (160f / 224f);
                            float offsetX = (i - (occupants.Count - 1) * 0.5f) * 140f;
                            Rect charRect = new Rect(
                                screenRect.x + (screenRect.width - spriteW) * 0.5f + offsetX,
                                screenRect.y + screenRect.height - spriteH - 20f,
                                spriteW,
                                spriteH
                            );
                            GUI.DrawTexture(charRect, charSprite.texture, ScaleMode.ScaleToFit);
                        }
                    }
                }
            }

            // If the room is empty because the animatronic has left for your office door, draw a huge Central Pointer Box!
            if (occupants.Count == 0 && roomOwner != null && roomOwner.IsAtDoorBlindspot)
            {
                DrawLeftRoomDoorPointerBanner(screenRect, roomOwner);
            }

            // Status overlay text at bottom-left of feed
            GUIStyle statusStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 16,
                alignment = TextAnchor.LowerLeft,
                fontStyle = FontStyle.Bold,
                richText = true
            };
            statusStyle.normal.textColor = Color.white;

            string statusText;
            if (isSpoofed)
            {
                statusText = "<color=#ff4444>⚠ SIGNAL ANOMALY (NIGHTMARE SANDY SPOOFING FEED)</color>";
            }
            else if (occupants.Count > 0)
            {
                statusText = "";
                foreach (var u in occupants)
                {
                    string hex = ColorUtility.ToHtmlStringRGB(u.Config != null ? u.Config.themeColor : Color.red);
                    string formTag = u.IsNightmare ? "<color=#ff3366>[NIGHTMARE]</color> " : "";
                    string passiveTag = u.IsPassiveObserverOnly
                        ? " <color=#ffcc44>[PASSIVE OBSERVER — WATCHING LENS]</color>"
                        : (u.EffectiveAILevel <= 0 ? " <color=#888888>[DORMANT — NOT ACTIVE THIS NIGHT]</color>" : "");
                    string copTag = (!string.IsNullOrEmpty(u.AnimatronicId) && u.AnimatronicId.ToLowerInvariant().Contains("erle") && u.IsNightmare)
                        ? (u.IsBadCopMode ? " <color=#ff3333>[BAD COP - FAST]</color>" : " <color=#55ddff>[GOOD COP - SLOW]</color>")
                        : "";
                    string voiceLine = !string.IsNullOrEmpty(u.LastVoiceLine) ? $"\n   <color=#dddddd><i>{u.LastVoiceLine}</i></color>" : "";

                    statusText += $"\n<color=#{hex}>● {formTag}{u.DisplayName.ToUpper()}: Stage {u.CurrentStage + 1}/{u.MaxStage}{passiveTag}{copTag}</color>{voiceLine}";
                }
                if (showSandyDecoy)
                {
                    statusText += $"\n<color=#88cc88>○ SHED DECOY SHELL DETECTED AT STAGE {sandyUnit.DecoyShellStage + 1}/{sandyUnit.MaxStage} (Hollow Eyes)</color>";
                }
            }
            else if (showSandyDecoy)
            {
                statusText = $"<color=#ff9944>⚠ SHED DECOY SHELL AT STAGE {sandyUnit.DecoyShellStage + 1}/{sandyUnit.MaxStage} — REAL NIGHTMARE SANDY IS AT YOUR REAR DOOR!</color>";
            }
            else
            {
                string[] doorNames = { "LEFT DOOR (Key 'A' / '1')", "CENTER DOOR (Key 'W' / '2')", "RIGHT DOOR (Key 'D' / '3')", "REAR DOOR (Key 'S' / '4')" };
                string dName = doorNames[Mathf.Clamp(_currentCameraIndex, 0, 3)];
                statusText = $"<color=#ff4444>🚨 ROOM IS EMPTY — ANIMATRONIC IS AT YOUR {dName}!</color>";
            }

            GUI.Label(new Rect(screenRect.x + 24, screenRect.y + screenRect.height - 135, screenRect.width - 375, 115), statusText, statusStyle);
        }

        private void DrawLeftRoomDoorPointerBanner(Rect screenRect, AnimatronicUnit roomOwner)
        {
            string[] doorDirs = { "LEFT OFFICE DOOR (Press 'A')", "CENTER OFFICE DOOR (Press 'W')", "RIGHT OFFICE DOOR (Press 'D')", "REAR OFFICE DOOR (Press 'S')" };
            string[] doorHotkeys = { "1", "2", "3", "4" };
            int doorIdx = Mathf.Clamp(roomOwner.TargetDoorIndex, 0, 3);
            float rem = AttackResolver.HasInstance ? AttackResolver.Instance.GetRemainingWindowForDoor(doorIdx) : 0f;

            float boxW = 580f;
            float boxH = 155f;
            Rect alertRect = new Rect(
                screenRect.x + (screenRect.width - boxW) * 0.5f,
                screenRect.y + (screenRect.height - boxH) * 0.45f,
                boxW,
                boxH
            );

            bool pulse = (Time.time % 0.5f) < 0.3f;
            GUI.color = pulse ? new Color(0.60f, 0.05f, 0.05f, 0.94f) : new Color(0.35f, 0.03f, 0.03f, 0.94f);
            GUI.Box(alertRect, "");
            GUI.color = Color.white;

            GUIStyle alertStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 17,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter,
                richText = true
            };
            alertStyle.normal.textColor = Color.white;

            GUI.Label(
                new Rect(alertRect.x + 15, alertRect.y + 12, boxW - 30, 88),
                $"<size=20><color=#ff5555>🚨 {roomOwner.DisplayName.ToUpper()} HAS LEFT THIS ROOM! 🚨</color></size>\n" +
                $"➔ NOW STANDING AT YOUR <color=yellow>{doorDirs[doorIdx]}</color>!\n" +
                $"Reaction Time Remaining: <color=#44ff66>{rem:F1}s</color> (Close Door [{doorHotkeys[doorIdx]}] or [E])",
                alertStyle
            );

            Rect btnRect = new Rect(alertRect.x + 90, alertRect.y + 104, boxW - 180, 38);
            GUI.backgroundColor = new Color(1f, 0.35f, 0.25f);
            if (GUI.Button(btnRect, $"⬇ LOWER TABLET & LOOK AT {doorDirs[doorIdx]}"))
            {
                if (PlayerViewController.HasInstance)
                {
                    PlayerViewController.Instance.SetMonitorState(false);
                    PlayerViewController.Instance.LookInDirection((OfficeViewDirection)doorIdx);
                }
                else
                {
                    SetMonitorRaised(false);
                }
            }
            GUI.backgroundColor = Color.white;
        }

        private void DrawDestroyedCameraScreen(Rect screenRect, AnimatronicUnit tylerUnit)
        {
            GUI.color = new Color(0.05f, 0.01f, 0.02f, 1f);
            GUI.DrawTexture(screenRect, Texture2D.whiteTexture);
            GUI.color = Color.white;

            GUIStyle brokenStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 24,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter,
                richText = true
            };
            brokenStyle.normal.textColor = new Color(1f, 0.25f, 0.25f);

            string audioHint = tylerUnit.IsAtDoorBlindspot
                ? "<color=#ff2222>🔊 HEAVY FOOTSTEPS STOP RIGHT OUTSIDE YOUR CENTER DOOR!</color>"
                : $"<color=#ffaa44>🔊 AUDIO TRACKING: Heavy footsteps & roars (Stage {tylerUnit.CurrentStage + 1}/{tylerUnit.MaxStage})</color>\n<color=#cccccc><i>{tylerUnit.LastVoiceLine}</i></color>";

            GUI.Label(
                screenRect,
                "<b>✖ CAM 02 VIDEO FEED DESTROYED BY NIGHTMARE TYLER ✖</b>\n\n" +
                "<size=16><color=#aaaaaa>Shotgun blast shattered Tyler's head casing — he smashed this camera lens.\nRely on spatial audio cues (roars, speech & footsteps) for the rest of this run!</color></size>\n\n" +
                audioHint,
                brokenStyle
            );
        }

        private void DrawCameraHeader(Rect screenRect, HandDrawnCameraFeed room)
        {
            GUILayout.BeginArea(new Rect(screenRect.x + 20, screenRect.y + 20, 620, 85));
            bool blink = (Time.time % 1.0f) < 0.6f;
            string recDot = blink ? "<color=red>● REC</color>" : "<color=#440000>○ REC</color>";

            GUIStyle titleStyle = new GUIStyle(GUI.skin.label) { fontSize = 20, fontStyle = FontStyle.Bold };
            titleStyle.normal.textColor = Color.white;

            string roomTitle = room != null ? room.cameraName : "UNKNOWN CHANNEL";
            GUILayout.Label($"{recDot}  <b>{roomTitle}</b>", titleStyle);
            GUILayout.Label("<color=#99aa99>TYLERLAND 4-ROOM 2D SURVEILLANCE SYSTEM (Click Map to Switch Rooms)</color>");
            GUILayout.EndArea();
        }

        private void DrawFourRoomTabletMap(Rect screenRect)
        {
            float mapW = 355;
            float mapH = 250;
            Rect mapRect = new Rect(screenRect.x + screenRect.width - mapW - 18, screenRect.y + screenRect.height - mapH - 18, mapW, mapH);

            // Semi-transparent map backdrop
            GUI.color = new Color(0.04f, 0.08f, 0.12f, 0.92f);
            GUI.Box(mapRect, "<b>4 ANIMATRONIC ROOMS & DOOR RADAR</b>");
            GUI.color = Color.white;

            // Central Office Box ([ YOU ])
            Rect officeRect = new Rect(mapRect.x + (mapW - 80) * 0.5f, mapRect.y + (mapH - 36) * 0.5f + 6, 80, 36);
            GUI.color = new Color(0.15f, 0.35f, 0.20f, 0.95f);
            GUI.Box(officeRect, "");
            GUI.color = Color.white;

            GUIStyle officeTagStyle = new GUIStyle(GUI.skin.label) { fontSize = 11, alignment = TextAnchor.MiddleCenter, fontStyle = FontStyle.Bold };
            officeTagStyle.normal.textColor = Color.green;
            GUI.Label(officeRect, "[ OFFICE ]", officeTagStyle);

            string[] ownerIds = { "erle", "tyler", "orville", "sandy" };
            string[] shortNames = { "CAM 01 Erle", "CAM 02 Tyler", "CAM 03 Orville", "CAM 04 Sandy" };

            Rect[] btnRects =
            {
                new Rect(mapRect.x + 10, mapRect.y + (mapH - 46) * 0.5f + 6, 98, 46),               // 0: Left (Erle)
                new Rect(mapRect.x + (mapW - 106) * 0.5f, mapRect.y + 26, 106, 46),                  // 1: Center (Tyler)
                new Rect(mapRect.x + mapW - 108, mapRect.y + (mapH - 46) * 0.5f + 6, 98, 46),       // 2: Right (Orville)
                new Rect(mapRect.x + (mapW - 106) * 0.5f, mapRect.y + mapH - 54, 106, 46)            // 3: Rear (Sandy)
            };

            // Corridor Doorway Marker positions between each Room and [ OFFICE ]
            Rect[] doorMarkerRects =
            {
                new Rect(officeRect.x - 32, officeRect.y + 8, 30, 20),                               // 0: Left Doorway
                new Rect(officeRect.x + 10, officeRect.y - 22, 60, 20),                              // 1: Center Doorway
                new Rect(officeRect.x + officeRect.width + 2, officeRect.y + 8, 30, 20),             // 2: Right Doorway
                new Rect(officeRect.x + 10, officeRect.y + officeRect.height + 2, 60, 20)            // 3: Rear Doorway
            };

            GUIStyle markerStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 10,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter,
                richText = true
            };

            for (int i = 0; i < 4; i++)
            {
                var unit = AnimatronicManager.HasInstance ? AnimatronicManager.Instance.GetAnimatronic(ownerIds[i]) : null;
                bool isAtDoor = unit != null && unit.IsAtDoorBlindspot;
                bool isCurrent = (i == _currentCameraIndex);

                string stageBadge = "";
                if (unit != null)
                {
                    if (isAtDoor)
                    {
                        stageBadge = "\n🚨 AT DOOR!";
                    }
                    else if (unit.IsPassiveObserverOnly)
                    {
                        stageBadge = "\n(Watching)";
                    }
                    else
                    {
                        stageBadge = $"\nStg {unit.CurrentStage + 1}/{unit.MaxStage}";
                    }
                }

                if (isAtDoor)
                {
                    GUI.backgroundColor = new Color(0.95f, 0.20f, 0.20f);
                }
                else
                {
                    GUI.backgroundColor = isCurrent ? new Color(0.2f, 0.9f, 0.4f) : new Color(0.18f, 0.24f, 0.34f);
                }

                if (GUI.Button(btnRects[i], $"{shortNames[i]}{stageBadge}"))
                {
                    SwitchCameraFeed(i);
                }

                // Draw Doorway Pointer Dot between the Room and [ OFFICE ] if the animatronic is at the door!
                if (isAtDoor)
                {
                    GUI.color = new Color(0.9f, 0.1f, 0.1f, 0.95f);
                    GUI.Box(doorMarkerRects[i], "");
                    GUI.color = Color.white;
                    markerStyle.normal.textColor = Color.yellow;
                    GUI.Label(doorMarkerRects[i], "⚠DOOR", markerStyle);
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
