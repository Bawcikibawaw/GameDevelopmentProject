using UnityEngine;
using Tylerland.Core;
using Tylerland.Animatronics;

namespace Tylerland.Office
{
    /// <summary>
    /// Section 7.4 & GDD Section 4: Office Diegetic HUD, Directional Threat Pointers & Diagnostics.
    /// Draws the on-screen shift stats:
    /// - Night & Clock display
    /// - 4 Door Energy, Durability & Live Animatronic Location Pointers
    /// - On-screen Directional Arrow Pointers (Left/Top/Right/Bottom) & Reaction Time Countdown Bars whenever an animatronic leaves its room for a door
    /// - Special Counter-Weapons status ('Q': Electric Gun, Shotgun, Harpoon, Spiked Mace)
    /// - Monitor status & Subtitles for phone calls
    /// </summary>
    public class OfficeHUD : MonoBehaviour
    {
        [Header("HUD Visibility")]
        [SerializeField] private bool _showHUD = true;

        private void OnGUI()
        {
            if (!_showHUD) return;

            int screenW = Screen.width;
            int screenH = Screen.height;

            var state = GameFlowManager.HasInstance ? GameFlowManager.Instance.CurrentState : GameState.NightActive;
            if (state == GameState.Menu || state == GameState.Boot)
            {
                DrawMainMenuScreen(screenW, screenH);
                return;
            }

            if (state != GameState.NightActive) return;

            // 1. TOP RIGHT: Night & Clock Display
            DrawClockWidget(screenW);

            // 2. BOTTOM LEFT: Door Energy, Durability & Live Animatronic Location Pointer per Door
            DrawDoorStatusWidget(screenH);

            // 3. BOTTOM RIGHT: Special Counter-Weapons & Nightmare Status
            DrawWeaponsWidget(screenW, screenH);

            // 4. BOTTOM CENTER: Compass / View Direction & Controls
            DrawViewCompassWidget(screenW, screenH);

            // 5. DIRECTIONAL THREAT POINTERS & REACTION TIME BARS (Always shows where animatronics are when they leave their room!)
            DrawDirectionalThreatPointers(screenW, screenH);

            // 6. TOP CENTER: Subtitles (if phone call is active)
            DrawSubtitlesWidget(screenW);
        }

        private void DrawClockWidget(int screenW)
        {
            int w = 215;
            int h = 84;
            Rect rect = new Rect(screenW - w - 20, 20, w, h);
            GUI.Box(rect, "");

            GUILayout.BeginArea(new Rect(screenW - w - 10, 24, w - 20, h - 8));
            int night = RunManager.HasInstance ? RunManager.Instance.CurrentNight : 1;
            string time = NightClock.HasInstance ? NightClock.Instance.GetDisplayTimeString() : "12 AM";

            GUIStyle timeStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 20,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter
            };
            timeStyle.normal.textColor = Color.white;

            GUIStyle nightStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 11,
                alignment = TextAnchor.MiddleCenter,
                richText = true
            };
            nightStyle.normal.textColor = new Color(0.85f, 0.85f, 0.85f);

            bool allNightmares = RunManager.HasInstance && RunManager.Instance.State.AllFourNightmaresActive;
            string trueEndingBadge = allNightmares
                ? "<color=#ff44aa>★ ALL 4 NIGHTMARES (TRUE ENDING)</color>"
                : $"<color=#aaaaaa>NIGHT {night}</color>";

            GUILayout.Label(time, timeStyle);
            GUILayout.Label(trueEndingBadge, nightStyle);
            GUILayout.EndArea();
        }

        private void DrawDoorStatusWidget(int screenH)
        {
            int w = 430;
            int h = 195;
            Rect rect = new Rect(20, screenH - h - 20, w, h);
            GUI.Box(rect, "<b>DOOR DEFENSES & LIVE TRACKER (Keys 1 - 4 or 'E')</b>");

            GUILayout.BeginArea(new Rect(30, screenH - h + 5, w - 20, h - 15));
            GUILayout.Space(15);

            string[] names =
            {
                "1. Left (Erle)",
                "2. Center (Tyler)",
                "3. Right (Orville)",
                "4. Rear (Sandy)"
            };

            string[] animIds = { "erle", "tyler", "orville", "sandy" };

            for (int i = 0; i < 4; i++)
            {
                var door = DoorSystemManager.HasInstance ? DoorSystemManager.Instance.GetDoor(i) : null;
                var unit = AnimatronicManager.HasInstance ? AnimatronicManager.Instance.GetAnimatronic(animIds[i]) : null;

                string locPointer = "";
                if (unit != null)
                {
                    if (unit.IsAtDoorBlindspot)
                    {
                        float rem = AttackResolver.HasInstance ? AttackResolver.Instance.GetRemainingWindowForDoor(i) : 0f;
                        locPointer = $" <color=#ff3333><b>[🚨 AT DOOR! {rem:F1}s]</b></color>";
                    }
                    else if (unit.IsPassiveObserverOnly)
                    {
                        locPointer = $" <color=#888888>[CAM {i + 1:00}: Watching]</color>";
                    }
                    else if (unit.EffectiveAILevel <= 0)
                    {
                        locPointer = $" <color=#666666>[CAM {i + 1:00}: Inactive]</color>";
                    }
                    else if (unit.CurrentStage >= unit.MaxStage - 1)
                    {
                        locPointer = $" <color=#ffcc33>[⚠ CAM {i + 1:00}: Stg {unit.CurrentStage + 1}/{unit.MaxStage} Leaving!]</color>";
                    }
                    else
                    {
                        locPointer = $" <color=#77cc88>[CAM {i + 1:00}: Stg {unit.CurrentStage + 1}/{unit.MaxStage}]</color>";
                    }
                }

                if (door != null)
                {
                    float energy = door.CurrentEnergy;
                    float dura = door.CurrentDurability;
                    string state = door.IsClosed ? "<color=cyan>[CLOSED]</color>" : "<color=gray>[OPEN]</color>";
                    if (door.IsBroken) state = "<color=red>[BROKEN]</color>";
                    else if (door.IsLockedOut) state = "<color=yellow>[LOCKOUT]</color>";

                    Color barColor = energy > 35f ? Color.green : (energy > 10f ? Color.yellow : Color.red);
                    string colorHex = ColorUtility.ToHtmlStringRGB(barColor);
                    string lightStr = door.IsLightOn ? " <color=yellow>[💡]</color>" : "";

                    GUILayout.Label($"<b>{names[i]}</b> {state}{lightStr} <color=#{colorHex}>P:{energy:F0}%</color> H:{dura:F0}%{locPointer}");
                }
                else
                {
                    GUILayout.Label($"<b>{names[i]}</b> - Ready{locPointer}");
                }
            }

            GUILayout.EndArea();
        }

        private void DrawWeaponsWidget(int screenW, int screenH)
        {
            int w = 355;
            int h = 165;
            Rect rect = new Rect(screenW - w - 20, screenH - h - 20, w, h);
            GUI.Box(rect, "<b>SPECIAL WEAPONS ('Q' to Use | 'G' Grant All)</b>");

            GUILayout.BeginArea(new Rect(screenW - w - 10, screenH - h + 5, w - 20, h - 15));
            GUILayout.Space(14);

            string[,] weaponTable =
            {
                { "Electric Gun", "Left (Erle)",    "erle" },
                { "Shotgun",      "Center (Tyler)", "tyler" },
                { "Harpoon",      "Right (Orville)","orville" },
                { "Spiked Mace",  "Rear (Sandy)",   "sandy" }
            };

            for (int i = 0; i < 4; i++)
            {
                string wId = weaponTable[i, 0];
                string target = weaponTable[i, 1];
                string animId = weaponTable[i, 2];

                bool owned = RunManager.HasInstance && RunManager.Instance.State.weaponsOwned.Contains(wId);
                bool used = RunManager.HasInstance && RunManager.Instance.State.weaponsUsed.Contains(wId);
                bool isNightmare = RunManager.HasInstance && RunManager.Instance.State.IsNightmareState(animId);

                string status = owned
                    ? "<color=#44ff66>[1 USE READY]</color>"
                    : (used ? "<color=#ff5555>[USED]</color>" : "<color=#888888>[LOCKED]</color>");

                string nmBadge = isNightmare ? " <color=#ff3366>[NIGHTMARE]</color>" : "";
                GUILayout.Label($"• <b>{wId}</b> ({target}): {status}{nmBadge}");
            }

            GUILayout.EndArea();
        }

        /// <summary>
        /// Draws unmistakable Directional Pointers and Reaction Time Countdown Bars whenever any animatronic
        /// has left its camera room and is standing at an Office Doorway!
        /// </summary>
        private void DrawDirectionalThreatPointers(int screenW, int screenH)
        {
            // 1. Show Weapon or Door Block confirmation banner if recent
            string statusBanner = "";
            if (SpecialWeaponSystem.HasInstance && !string.IsNullOrEmpty(SpecialWeaponSystem.Instance.LastStatusMessage))
            {
                statusBanner = SpecialWeaponSystem.Instance.LastStatusMessage;
            }
            else if (AttackResolver.HasInstance && !string.IsNullOrEmpty(AttackResolver.Instance.LastBlockStatusMessage))
            {
                statusBanner = AttackResolver.Instance.LastBlockStatusMessage;
            }

            if (!string.IsNullOrEmpty(statusBanner))
            {
                Rect msgRect = new Rect((screenW - 640) / 2f, 150, 640, 42);
                GUI.color = new Color(0.08f, 0.28f, 0.12f, 0.94f);
                GUI.Box(msgRect, "");
                GUI.color = Color.white;

                GUIStyle msgStyle = new GUIStyle(GUI.skin.label)
                {
                    fontSize = 14,
                    fontStyle = FontStyle.Bold,
                    alignment = TextAnchor.MiddleCenter
                };
                msgStyle.normal.textColor = new Color(0.4f, 1f, 0.55f);
                GUI.Label(msgRect, statusBanner, msgStyle);
            }

            if (!AnimatronicManager.HasInstance) return;

            int currentFacing = PlayerViewController.HasInstance ? (int)PlayerViewController.Instance.CurrentDirection : 1;
            bool isMonitorOpen = PlayerViewController.HasInstance && PlayerViewController.Instance.IsMonitorOpen;

            string[] doorDirLabels =
            {
                "LEFT DOOR (Key 'A' or '1')",
                "CENTER DOOR (Key 'W' or '2')",
                "RIGHT DOOR (Key 'D' or '3')",
                "REAR DOOR (Key 'S' or '4')"
            };

            bool pulse = (Time.time % 0.5f) < 0.3f;

            for (int doorIdx = 0; doorIdx < 4; doorIdx++)
            {
                var attacker = AnimatronicManager.Instance.GetAnimatronicAtDoorBlindspot(doorIdx);
                if (attacker == null || attacker.Config == null) continue;

                var threat = AttackResolver.HasInstance ? AttackResolver.Instance.GetActiveThreatAtDoor(doorIdx) : null;
                float remTime = threat != null ? Mathf.Max(0f, threat.remainingWindow) : 0f;
                float maxTime = (threat != null && threat.attackWindowDuration > 0.01f) ? threat.attackWindowDuration : 6f;
                float pct = Mathf.Clamp01(remTime / maxTime);

                bool doorClosed = DoorSystemManager.HasInstance && DoorSystemManager.Instance.IsDoorClosed(doorIdx);
                string weaponName = SpecialWeaponSystem.GetDefaultWeaponForDoor(doorIdx);
                string formPrefix = attacker.IsNightmare ? "NIGHTMARE " : "";

                // If player is currently facing this door (and monitor is down), draw a big Pointer Box right over the doorway!
                if (!isMonitorOpen && currentFacing == doorIdx)
                {
                    Rect warnRect = new Rect((screenW - 680) / 2f, 82, 680, 62);
                    GUI.color = doorClosed
                        ? new Color(0.10f, 0.35f, 0.45f, 0.94f)
                        : (pulse ? new Color(0.65f, 0.06f, 0.06f, 0.95f) : new Color(0.40f, 0.04f, 0.04f, 0.95f));
                    GUI.Box(warnRect, "");
                    GUI.color = Color.white;

                    // Reaction time progress bar at bottom of banner
                    Rect barBg = new Rect(warnRect.x + 12, warnRect.y + warnRect.height - 12, warnRect.width - 24, 6);
                    GUI.color = new Color(0.15f, 0.15f, 0.15f, 1f);
                    GUI.DrawTexture(barBg, Texture2D.whiteTexture);
                    GUI.color = doorClosed ? Color.cyan : Color.Lerp(Color.red, Color.yellow, pct);
                    GUI.DrawTexture(new Rect(barBg.x, barBg.y, barBg.width * pct, barBg.height), Texture2D.whiteTexture);
                    GUI.color = Color.white;

                    GUIStyle warnStyle = new GUIStyle(GUI.skin.label)
                    {
                        fontSize = 14,
                        fontStyle = FontStyle.Bold,
                        alignment = TextAnchor.MiddleCenter,
                        richText = true
                    };
                    warnStyle.normal.textColor = Color.white;

                    string actionHint = doorClosed
                        ? "<color=cyan>DOOR IS CLOSED — BLOCKING ATTACK...</color>"
                        : $"Reaction Time: <color=yellow>{remTime:F1}s</color> — Press <b>[E] Close Door</b>, <b>[F] Light</b>, or <b>[Q] Fire {weaponName}</b>!";

                    GUI.Label(
                        new Rect(warnRect.x, warnRect.y + 4, warnRect.width, 44),
                        $"▼ ⚠ {formPrefix}{attacker.Config.displayName.ToUpper()} IS RIGHT OUTSIDE THIS DOOR! ▼\n{actionHint}",
                        warnStyle
                    );
                }
                else
                {
                    // Off-screen / Edge-of-screen Directional Arrow Pointer so the player ALWAYS knows which door the animatronic went to!
                    Rect edgeRect;
                    string arrowText;

                    if (doorIdx == 0) // Left Door (Erle)
                    {
                        edgeRect = new Rect(20, screenH * 0.42f, 290, 58);
                        arrowText = $"◄◄ LEFT DOOR: {attacker.Config.displayName.ToUpper()}!\n[Press 'A' to Look | '1' Close Door] ({remTime:F1}s)";
                    }
                    else if (doorIdx == 1) // Center Door (Tyler)
                    {
                        edgeRect = new Rect((screenW - 360) * 0.5f, 20, 360, 56);
                        arrowText = $"▲▲ CENTER DOOR: {attacker.Config.displayName.ToUpper()}!\n[Press 'W' to Look | '2' Close Door] ({remTime:F1}s)";
                    }
                    else if (doorIdx == 2) // Right Door (Orville)
                    {
                        edgeRect = new Rect(screenW - 310, screenH * 0.42f, 290, 58);
                        arrowText = $"►► RIGHT DOOR: {attacker.Config.displayName.ToUpper()}!\n[Press 'D' to Look | '3' Close Door] ({remTime:F1}s)";
                    }
                    else // Rear Door (Sandy)
                    {
                        edgeRect = new Rect((screenW - 380) * 0.5f, screenH - 140, 380, 56);
                        arrowText = $"▼▼ REAR DOOR (BEHIND YOU): {attacker.Config.displayName.ToUpper()}!\n[Press 'S' to Turn Back | '4' Close Door] ({remTime:F1}s)";
                    }

                    GUI.color = pulse ? new Color(0.80f, 0.08f, 0.08f, 0.95f) : new Color(0.45f, 0.05f, 0.05f, 0.92f);
                    GUI.Box(edgeRect, "");
                    GUI.color = Color.white;

                    GUIStyle edgeStyle = new GUIStyle(GUI.skin.label)
                    {
                        fontSize = 13,
                        fontStyle = FontStyle.Bold,
                        alignment = TextAnchor.MiddleCenter
                    };
                    edgeStyle.normal.textColor = new Color(1f, 0.9f, 0.4f);

                    if (GUI.Button(edgeRect, arrowText, edgeStyle))
                    {
                        if (PlayerViewController.HasInstance)
                        {
                            PlayerViewController.Instance.SetMonitorState(false);
                            PlayerViewController.Instance.LookInDirection((OfficeViewDirection)doorIdx);
                        }
                    }
                }
            }
        }

        private void DrawViewCompassWidget(int screenW, int screenH)
        {
            int w = 500;
            int h = 62;
            Rect rect = new Rect((screenW - w) / 2, screenH - h - 15, w, h);
            GUI.Box(rect, "");

            GUILayout.BeginArea(new Rect((screenW - w) / 2 + 10, screenH - h - 10, w - 20, h));

            string facing = "CENTER (Tyler the Lion)";
            if (PlayerViewController.HasInstance)
            {
                switch (PlayerViewController.Instance.CurrentDirection)
                {
                    case OfficeViewDirection.Left:   facing = "LEFT (Erle the Eagle)"; break;
                    case OfficeViewDirection.Center: facing = "CENTER (Tyler the Lion)"; break;
                    case OfficeViewDirection.Right:  facing = "RIGHT (Orville the Orca)"; break;
                    case OfficeViewDirection.Back:   facing = "REAR (Sandy the Snake)"; break;
                }
            }

            bool mon = PlayerViewController.HasInstance && PlayerViewController.Instance.IsMonitorOpen;
            string monState = mon ? "<color=green>[TABLET UP]</color>" : "<color=gray>[TABLET DOWN]</color>";

            GUIStyle centerStyle = new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = 12
            };
            centerStyle.normal.textColor = Color.white;

            GUILayout.Label($"<b>Facing:</b> <color=cyan>{facing}</color> | <b>Monitor:</b> {monState}", centerStyle);
            GUILayout.Label("<color=#aaaaaa>A/D: Turn | S: Rear | W: Center | Space: Tablet | E: Door | F: Light | Q: Weapon | B: Summon</color>", centerStyle);
            GUILayout.EndArea();
        }

        private void DrawSubtitlesWidget(int screenW)
        {
            if (!PhoneSystem.HasInstance || !PhoneSystem.Instance.IsCallActive) return;

            string sub = PhoneSystem.Instance.CurrentSubtitle;
            if (string.IsNullOrEmpty(sub)) return;

            int w = 600;
            int h = 60;
            Rect rect = new Rect((screenW - w) / 2, 80, w, h);
            GUI.Box(rect, "");

            GUILayout.BeginArea(new Rect((screenW - w) / 2 + 10, 85, w - 20, h - 10));
            GUIStyle subStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 13,
                fontStyle = FontStyle.Italic,
                alignment = TextAnchor.MiddleCenter,
                wordWrap = true
            };
            subStyle.normal.textColor = Color.yellow;
            GUILayout.Label($"📞 \"{sub}\"", subStyle);
            GUILayout.EndArea();
        }

        private bool _autoPlayDemoActive = false;
        private float _autoPlayDecisionTimer = 0f;

        private void Update()
        {
            var keyboard = UnityEngine.InputSystem.Keyboard.current;
            var state = GameFlowManager.HasInstance ? GameFlowManager.Instance.CurrentState : GameState.NightActive;

            if (keyboard != null && keyboard.vKey.wasPressedThisFrame)
            {
                if (state == GameState.Menu)
                {
                    StartAutoPlayNight4Demo();
                    return;
                }
                else
                {
                    _autoPlayDemoActive = !_autoPlayDemoActive;
                    Debug.Log($"[OfficeHUD] Auto-Play Demo Bot: {(_autoPlayDemoActive ? "ENABLED" : "DISABLED")}");
                }
            }

            if (state == GameState.Menu)
            {
                if (keyboard != null && (keyboard.enterKey.wasPressedThisFrame || keyboard.numpadEnterKey.wasPressedThisFrame))
                {
                    StartNightGame();
                }
            }
            else if (state == GameState.NightActive && _autoPlayDemoActive)
            {
                RunAutoPlayDemoBot();
            }
        }

        private void RunAutoPlayDemoBot()
        {
            _autoPlayDecisionTimer -= Time.deltaTime;
            if (_autoPlayDecisionTimer > 0f) return;
            _autoPlayDecisionTimer = 0.65f;

            if (!AnimatronicManager.HasInstance) return;

            // 1. Check if any animatronic is at a door blindspot
            int urgentDoor = -1;
            for (int i = 0; i < 4; i++)
            {
                var attacker = AnimatronicManager.Instance.GetAnimatronicAtDoorBlindspot(i);
                if (attacker != null)
                {
                    urgentDoor = i;
                    break;
                }
            }

            if (urgentDoor >= 0)
            {
                // Lower tablet, face the threatened door, turn on light, and either fire weapon or close door!
                if (PlayerViewController.HasInstance)
                {
                    if (PlayerViewController.Instance.IsMonitorOpen)
                        PlayerViewController.Instance.SetMonitorState(false);
                    if ((int)PlayerViewController.Instance.CurrentDirection != urgentDoor)
                        PlayerViewController.Instance.LookInDirection((OfficeViewDirection)urgentDoor);
                }

                if (DoorSystemManager.HasInstance)
                {
                    var door = DoorSystemManager.Instance.GetDoor(urgentDoor);
                    if (door != null)
                    {
                        if (!door.IsLightOn) door.SetLight(true);

                        string wId = SpecialWeaponSystem.GetDefaultWeaponForDoor(urgentDoor);
                        bool hasWeapon = RunManager.HasInstance && RunManager.Instance.State.weaponsOwned.Contains(wId);
                        if (hasWeapon && SpecialWeaponSystem.HasInstance)
                        {
                            SpecialWeaponSystem.Instance.TryUseWeaponAtDoor(urgentDoor);
                        }
                        else if (!door.IsClosed)
                        {
                            door.CloseDoor();
                        }
                    }
                }
                return;
            }

            // 2. If no animatronic is at a door, reopen any closed doors to save energy and turn off lights
            if (DoorSystemManager.HasInstance)
            {
                for (int i = 0; i < 4; i++)
                {
                    var d = DoorSystemManager.Instance.GetDoor(i);
                    if (d != null)
                    {
                        if (d.IsClosed) d.OpenDoor();
                        if (d.IsLightOn) d.SetLight(false);
                    }
                }
            }

            // 3. Periodically check the 4-room camera tablet
            if (PlayerViewController.HasInstance && CameraMonitorSystem.HasInstance)
            {
                if (!PlayerViewController.Instance.IsMonitorOpen && Random.value < 0.45f)
                {
                    PlayerViewController.Instance.SetMonitorState(true);
                    CameraMonitorSystem.Instance.SwitchCameraFeed(Random.Range(0, 4));
                }
                else if (PlayerViewController.Instance.IsMonitorOpen)
                {
                    if (Random.value < 0.55f)
                        CameraMonitorSystem.Instance.SwitchCameraFeed((CameraMonitorSystem.Instance.CurrentCameraIndex + 1) % 4);
                    else
                        PlayerViewController.Instance.SetMonitorState(false);
                }
            }
        }

        private void StartAutoPlayNight4Demo()
        {
            _autoPlayDemoActive = true;
            StartSpecificNight(4);
            if (SpecialWeaponSystem.HasInstance)
            {
                SpecialWeaponSystem.Instance.GrantAllWeapons();
            }
        }

        private void DrawMainMenuScreen(int screenW, int screenH)
        {
            float w = 500;
            float h = 460;
            Rect menuRect = new Rect((screenW - w) / 2f, (screenH - h) / 2f, w, h);

            GUI.color = new Color(0.08f, 0.08f, 0.14f, 0.95f);
            GUI.Box(menuRect, "");
            GUI.color = Color.white;

            GUILayout.BeginArea(new Rect(menuRect.x + 25, menuRect.y + 25, w - 50, h - 50));

            // Title
            GUIStyle titleStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 36,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter
            };
            titleStyle.normal.textColor = new Color(0.95f, 0.25f, 0.25f);
            GUILayout.Label("TYLERLAND", titleStyle);

            GUIStyle subStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 12,
                fontStyle = FontStyle.Italic,
                alignment = TextAnchor.MiddleCenter
            };
            subStyle.normal.textColor = new Color(0.7f, 0.7f, 0.8f);
            GUILayout.Label("ANIMAL KINGDOM — 2D SPRITE SURVIVAL HORROR", subStyle);

            GUILayout.Space(20);

            // Persistent Stats
            int bestNight = MetaProgressionManager.HasInstance ? MetaProgressionManager.Instance.BestNight : 1;
            int soulCoins = MetaProgressionManager.HasInstance ? MetaProgressionManager.Instance.SoulCoins : 0;
            int currentNight = RunManager.HasInstance ? RunManager.Instance.CurrentNight : 1;

            GUIStyle statStyle = new GUIStyle(GUI.skin.label) { fontSize = 12, alignment = TextAnchor.MiddleCenter };
            statStyle.normal.textColor = Color.yellow;
            GUILayout.Label($"✦ Soul Coins: <b>{soulCoins}</b>   |   ★ Best Night: <b>Night {bestNight}</b>", statStyle);

            GUILayout.Space(16);

            // Big Start Button
            GUI.backgroundColor = new Color(0.2f, 0.8f, 0.3f);
            string startLabel = currentNight > 1 ? $"▶ CONTINUE NIGHT {currentNight}" : "▶ START NIGHT 1";
            if (GUILayout.Button(startLabel, GUILayout.Height(48)))
            {
                _autoPlayDemoActive = false;
                StartNightGame();
            }

            GUILayout.Space(8);
            GUI.backgroundColor = new Color(0.95f, 0.35f, 0.25f);
            if (GUILayout.Button("🎬 AUTO-PLAY NIGHT 4 DEMO (Press 'V')", GUILayout.Height(38)))
            {
                StartAutoPlayNight4Demo();
            }
            GUI.backgroundColor = Color.white;

            GUILayout.Space(12);
            GUIStyle hintStyle = new GUIStyle(GUI.skin.label) { fontSize = 11, alignment = TextAnchor.MiddleCenter };
            hintStyle.normal.textColor = new Color(0.6f, 0.6f, 0.6f);
            GUILayout.Label("Jump to Night (Testing):", hintStyle);

            // Quick Night Jumps
            GUILayout.BeginHorizontal();
            for (int i = 1; i <= 6; i++)
            {
                if (GUILayout.Button($"Night {i}", GUILayout.Height(30)))
                {
                    _autoPlayDemoActive = false;
                    StartSpecificNight(i);
                }
            }
            GUILayout.EndHorizontal();

            GUILayout.Space(10);
            GUILayout.Label("Press [ENTER] to start shift | Press [V] anytime to toggle Auto-Play Bot", hintStyle);

            GUILayout.EndArea();
        }

        private void StartNightGame()
        {
            if (GameFlowManager.HasInstance)
            {
                GameFlowManager.Instance.StartNightRun();
            }
        }

        private void StartSpecificNight(int night)
        {
            if (RunManager.HasInstance)
            {
                RunManager.Instance.State.currentNight = night;
            }
            if (TylerlandBootstrap.HasInstance && NightClock.HasInstance)
            {
                var cfg = TylerlandBootstrap.Instance.GetConfigForNight(night);
                if (cfg != null) NightClock.Instance.Initialize(cfg);
            }
            if (GameFlowManager.HasInstance)
            {
                GameFlowManager.Instance.StartNightRun();
            }
        }
    }
}
