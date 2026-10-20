using UnityEngine;
using Tylerland.Core;
using Tylerland.Animatronics;

namespace Tylerland.Office
{
    /// <summary>
    /// Section 7.4: Office Diegetic HUD & Diagnostics.
    /// Draws the on-screen shift stats:
    /// - Night & Clock display
    /// - 4 Door Energy & Durability gauges (Green >35%, Amber 10-35%, Red <10%)
    /// - Facing direction indicator (Left, Center, Right, Back)
    /// - Blindspot animatronic warning when Hallway Light ('F') illuminates an attacker
    /// - Special Counter-Weapons status ('Q')
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

            // 2. BOTTOM LEFT: Door Energy & Durability Status Box
            DrawDoorStatusWidget(screenH);

            // 3. BOTTOM RIGHT: Special Counter-Weapons & Animatronic Status
            DrawWeaponsWidget(screenW, screenH);

            // 4. BOTTOM CENTER: Compass / View Direction & Controls
            DrawViewCompassWidget(screenW, screenH);

            // 5. CENTER TOP: Blindspot Warning when Hallway Light ('F') reveals an animatronic!
            DrawBlindspotThreatBanner(screenW, screenH);

            // 6. TOP CENTER: Subtitles (if phone call is active)
            DrawSubtitlesWidget(screenW);
        }

        private void DrawClockWidget(int screenW)
        {
            int w = 200;
            int h = 70;
            Rect rect = new Rect(screenW - w - 20, 20, w, h);
            GUI.Box(rect, "");

            GUILayout.BeginArea(new Rect(screenW - w - 10, 25, w - 20, h - 10));
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
                fontSize = 12,
                alignment = TextAnchor.MiddleCenter
            };
            nightStyle.normal.textColor = new Color(0.8f, 0.8f, 0.8f);

            GUILayout.Label(time, timeStyle);
            GUILayout.Label($"NIGHT {night}", nightStyle);
            GUILayout.EndArea();
        }

        private void DrawDoorStatusWidget(int screenH)
        {
            int w = 320;
            int h = 180;
            Rect rect = new Rect(20, screenH - h - 20, w, h);
            GUI.Box(rect, "<b>DOOR DEFENSES (Keys 1 - 4 or 'E')</b>");

            GUILayout.BeginArea(new Rect(30, screenH - h + 5, w - 20, h - 15));
            GUILayout.Space(15);

            string[] names = { "1. Left (Shark)", "2. Center (Tyler)", "3. Right (Eagle)", "4. Back (Snake)" };

            for (int i = 0; i < 4; i++)
            {
                var door = DoorSystemManager.HasInstance ? DoorSystemManager.Instance.GetDoor(i) : null;
                if (door != null)
                {
                    float energy = door.CurrentEnergy;
                    float dura = door.CurrentDurability;
                    string state = door.IsClosed ? "<color=cyan>[CLOSED]</color>" : "<color=gray>[OPEN]</color>";
                    if (door.IsBroken) state = "<color=red>[BROKEN]</color>";
                    else if (door.IsLockedOut) state = "<color=yellow>[LOCKOUT]</color>";

                    Color barColor = energy > 35f ? Color.green : (energy > 10f ? Color.yellow : Color.red);
                    string colorHex = ColorUtility.ToHtmlStringRGB(barColor);
                    string lightStr = door.IsLightOn ? " <color=yellow>[💡LIGHT]</color>" : "";

                    GUILayout.Label($"<b>{names[i]}</b> {state}{lightStr} - <color=#{colorHex}>PWR:{energy:F0}%</color> | HP:{dura:F0}%");
                }
                else
                {
                    GUILayout.Label($"<b>{names[i]}</b> - Ready");
                }
            }

            GUILayout.EndArea();
        }

        private void DrawWeaponsWidget(int screenW, int screenH)
        {
            int w = 310;
            int h = 150;
            Rect rect = new Rect(screenW - w - 20, screenH - h - 20, w, h);
            GUI.Box(rect, "<b>COUNTER-WEAPONS ('Q' to Use | 'G' Grant All)</b>");

            GUILayout.BeginArea(new Rect(screenW - w - 10, screenH - h + 5, w - 20, h - 15));
            GUILayout.Space(14);

            string[,] weaponTable =
            {
                { "Harpoon", "Left Door (Shark)" },
                { "Shears",  "Center Door (Tyler)" },
                { "Net",     "Right Door (Eagle)" },
                { "Prod",    "Back Door (Snake)" }
            };

            for (int i = 0; i < 4; i++)
            {
                string wId = weaponTable[i, 0];
                string target = weaponTable[i, 1];
                bool owned = RunManager.HasInstance && RunManager.Instance.State.weaponsOwned.Contains(wId);
                bool used = RunManager.HasInstance && RunManager.Instance.State.weaponsUsed.Contains(wId);

                string status = owned
                    ? "<color=#44ff66>[READY]</color>"
                    : (used ? "<color=#ff5555>[USED]</color>" : "<color=#888888>[NOT OWNED]</color>");

                GUILayout.Label($"• <b>{wId}</b> ({target}): {status}");
            }

            GUILayout.EndArea();
        }

        private void DrawBlindspotThreatBanner(int screenW, int screenH)
        {
            // Show weapon feedback message if recent
            if (SpecialWeaponSystem.HasInstance && !string.IsNullOrEmpty(SpecialWeaponSystem.Instance.LastStatusMessage))
            {
                Rect msgRect = new Rect((screenW - 560) / 2f, 145, 560, 42);
                GUI.color = new Color(0.1f, 0.25f, 0.1f, 0.92f);
                GUI.Box(msgRect, "");
                GUI.color = Color.white;

                GUIStyle msgStyle = new GUIStyle(GUI.skin.label)
                {
                    fontSize = 14,
                    fontStyle = FontStyle.Bold,
                    alignment = TextAnchor.MiddleCenter
                };
                msgStyle.normal.textColor = new Color(0.4f, 1f, 0.5f);
                GUI.Label(msgRect, SpecialWeaponSystem.Instance.LastStatusMessage, msgStyle);
            }

            if (!PlayerViewController.HasInstance || PlayerViewController.Instance.IsMonitorOpen) return;

            int doorIdx = (int)PlayerViewController.Instance.CurrentDirection;
            var door = DoorSystemManager.HasInstance ? DoorSystemManager.Instance.GetDoor(doorIdx) : null;
            if (door == null || !door.IsLightOn) return;

            var attacker = AnimatronicManager.HasInstance ? AnimatronicManager.Instance.GetAnimatronicAtDoorBlindspot(doorIdx) : null;
            if (attacker == null || attacker.Config == null) return;

            Rect warnRect = new Rect((screenW - 600) / 2f, 90, 600, 50);
            GUI.color = new Color(0.45f, 0.05f, 0.05f, 0.92f);
            GUI.Box(warnRect, "");
            GUI.color = Color.white;

            GUIStyle warnStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 15,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter
            };
            warnStyle.normal.textColor = new Color(1f, 0.35f, 0.35f);

            string weaponName = SpecialWeaponSystem.GetDefaultWeaponForDoor(doorIdx);
            GUI.Label(warnRect,
                $"⚠ {attacker.Config.displayName.ToUpper()} IS AT THE DOOR! (Dmg Lv.{attacker.DamageLevel}) — Press [E] Close Door or [Q] Use {weaponName}!",
                warnStyle);
        }

        private void DrawViewCompassWidget(int screenW, int screenH)
        {
            int w = 480;
            int h = 60;
            Rect rect = new Rect((screenW - w) / 2, screenH - h - 15, w, h);
            GUI.Box(rect, "");

            GUILayout.BeginArea(new Rect((screenW - w) / 2 + 10, screenH - h - 10, w - 20, h));

            string facing = "CENTER (Tyler)";
            if (PlayerViewController.HasInstance)
            {
                switch (PlayerViewController.Instance.CurrentDirection)
                {
                    case OfficeViewDirection.Left: facing = "LEFT (Shark Door)"; break;
                    case OfficeViewDirection.Center: facing = "CENTER (Tyler Door)"; break;
                    case OfficeViewDirection.Right: facing = "RIGHT (Eagle Door)"; break;
                    case OfficeViewDirection.Back: facing = "BACK (Snake Door)"; break;
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
            GUILayout.Label("<color=#aaaaaa>A/D: Turn | S: Back | W: Center | Space: Tablet | E: Door | F: Light | Q: Weapon | B: Summon</color>", centerStyle);
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

        private void Update()
        {
            var state = GameFlowManager.HasInstance ? GameFlowManager.Instance.CurrentState : GameState.NightActive;
            if (state == GameState.Menu)
            {
                var keyboard = UnityEngine.InputSystem.Keyboard.current;
                if (keyboard != null && (keyboard.enterKey.wasPressedThisFrame || keyboard.numpadEnterKey.wasPressedThisFrame))
                {
                    StartNightGame();
                }
            }
        }

        private void DrawMainMenuScreen(int screenW, int screenH)
        {
            float w = 500;
            float h = 420;
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
            GUILayout.Label("DEVELOPER SYSTEMS ROADMAP PROTOTYPE", subStyle);

            GUILayout.Space(25);

            // Persistent Stats
            int bestNight = MetaProgressionManager.HasInstance ? MetaProgressionManager.Instance.BestNight : 1;
            int soulCoins = MetaProgressionManager.HasInstance ? MetaProgressionManager.Instance.SoulCoins : 0;
            int currentNight = RunManager.HasInstance ? RunManager.Instance.CurrentNight : 1;

            GUIStyle statStyle = new GUIStyle(GUI.skin.label) { fontSize = 12, alignment = TextAnchor.MiddleCenter };
            statStyle.normal.textColor = Color.yellow;
            GUILayout.Label($"✦ Soul Coins: <b>{soulCoins}</b>   |   ★ Best Night: <b>Night {bestNight}</b>", statStyle);

            GUILayout.Space(20);

            // Big Start Button
            GUI.backgroundColor = new Color(0.2f, 0.8f, 0.3f);
            string startLabel = currentNight > 1 ? $"▶ CONTINUE NIGHT {currentNight}" : "▶ START NIGHT 1";
            if (GUILayout.Button(startLabel, GUILayout.Height(52)))
            {
                StartNightGame();
            }
            GUI.backgroundColor = Color.white;

            GUILayout.Space(15);
            GUIStyle hintStyle = new GUIStyle(GUI.skin.label) { fontSize = 11, alignment = TextAnchor.MiddleCenter };
            hintStyle.normal.textColor = new Color(0.6f, 0.6f, 0.6f);
            GUILayout.Label("Jump to Night (Testing):", hintStyle);

            // Quick Night Jumps
            GUILayout.BeginHorizontal();
            for (int i = 1; i <= 6; i++)
            {
                if (GUILayout.Button($"Night {i}", GUILayout.Height(30)))
                {
                    StartSpecificNight(i);
                }
            }
            GUILayout.EndHorizontal();

            GUILayout.Space(15);
            GUILayout.Label("Press [ENTER] to start shift", hintStyle);

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
