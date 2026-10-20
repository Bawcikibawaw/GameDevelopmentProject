using UnityEngine;
using UnityEngine.InputSystem;
using Tylerland.Animatronics;

namespace Tylerland.Core
{
    /// <summary>
    /// Section 8.2: Debug & Balance Tools.
    /// Toggleable runtime overlay and hotkey cheats for rapid playtesting.
    /// Uses Unity's New Input System (Keyboard.current) and standard letter keys.
    /// </summary>
    public class CoreDebugOverlay : MonoBehaviour
    {
        [Header("Debug Settings")]
        [Tooltip("Whether the debug HUD box is displayed on screen")]
        [SerializeField] private bool _showOverlay = true;

        private void Update()
        {
            var keyboard = Keyboard.current;
            if (keyboard == null) return;

            // 'H' or 'O': Toggle debug overlay HUD
            if (keyboard.hKey.wasPressedThisFrame || keyboard.oKey.wasPressedThisFrame)
            {
                _showOverlay = !_showOverlay;
            }

            // 'N': Force 6:00 AM Night Completion & Advance to Next Night
            if (keyboard.nKey.wasPressedThisFrame)
            {
                Debug.Log("[DEBUG CHEAT] 'N' pressed: Forcing 6:00 AM Night Completion & Advancing Night!");
                if (RunManager.HasInstance)
                {
                    int currentNight = RunManager.Instance.CurrentNight;
                    GameEvents.TriggerNightCompleted(currentNight);
                    RunManager.Instance.AdvanceToNextNight();
                }
                else
                {
                    GameEvents.TriggerNightCompleted(1);
                }

                if (NightClock.HasInstance)
                {
                    NightClock.Instance.ResetClock();
                    NightClock.Instance.StartClock();
                }
            }

            // 'M': Give $100 Run Money
            if (keyboard.mKey.wasPressedThisFrame)
            {
                if (RunManager.HasInstance)
                {
                    RunManager.Instance.AddMoney(100);
                    Debug.Log("[DEBUG CHEAT] 'M' pressed: Granted $100 run money!");
                }
            }

            // 'C': Give 5 Soul Coins
            if (keyboard.cKey.wasPressedThisFrame)
            {
                if (MetaProgressionManager.HasInstance)
                {
                    MetaProgressionManager.Instance.AddSoulCoins(5);
                    Debug.Log("[DEBUG CHEAT] 'C' pressed: Granted 5 Soul Coins!");
                }
            }

            // 'J': Trigger Player Death (Jumpscare)
            if (keyboard.jKey.wasPressedThisFrame)
            {
                Debug.Log("[DEBUG CHEAT] 'J' pressed: Triggering jumpscare kill!");
                GameEvents.TriggerJumpscare("Tyler");
                GameEvents.TriggerPlayerDied("Tyler");
            }

            // 'R': Force Run Reset
            if (keyboard.rKey.wasPressedThisFrame)
            {
                Debug.Log("[DEBUG CHEAT] 'R' pressed: Triggering full run reset!");
                GameEvents.TriggerRunReset();
            }

            // 'P' or 'Escape': Toggle Pause (when in NightActive)
            if (keyboard.pKey.wasPressedThisFrame || keyboard.escapeKey.wasPressedThisFrame)
            {
                if (GameFlowManager.HasInstance)
                {
                    GameFlowManager.Instance.TogglePause();
                }
            }
        }

        private void OnGUI()
        {
            if (!_showOverlay) return;

            // Semi-transparent debug box
            GUI.color = Color.white;
            int boxWidth = 420;
            int boxHeight = 390;
            Rect rect = new Rect(20, 20, boxWidth, boxHeight);
            GUI.Box(rect, "TYLERLAND DEBUG OVERLAY [Toggle: 'H' or 'O']");

            GUILayout.BeginArea(new Rect(30, 45, boxWidth - 20, boxHeight - 30));

            // Game State
            string stateStr = GameFlowManager.HasInstance ? GameFlowManager.Instance.CurrentState.ToString() : "N/A";
            bool paused = GameFlowManager.HasInstance && GameFlowManager.Instance.IsPaused;
            GUILayout.Label($"<b>Game Flow State:</b> {stateStr} {(paused ? "<color=yellow>[PAUSED]</color>" : "")}");

            // Clock & Shift
            if (NightClock.HasInstance)
            {
                string timeStr = NightClock.Instance.GetDisplayTimeString();
                float blend = NightClock.Instance.HourBlendFactor;
                GUILayout.Label($"<b>Shift Clock:</b> {timeStr} | Blend: {blend:P0} | Active: {NightClock.Instance.IsClockRunning}");
            }

            // Run & Meta State
            if (RunManager.HasInstance)
            {
                var run = RunManager.Instance;
                int sc = MetaProgressionManager.HasInstance ? MetaProgressionManager.Instance.SoulCoins : 0;
                GUILayout.Label($"<b>Night:</b> {run.CurrentNight}/{run.TotalNights} | <b>Money:</b> ${run.Money} | <b>Soul Coins:</b> {sc}");
            }

            // Live Animatronic Progression Table
            if (AnimatronicManager.HasInstance && AnimatronicManager.Instance.Units != null)
            {
                GUILayout.Space(4);
                GUILayout.Label("<b><color=#ff9966>LIVE ANIMATRONICS (Stage / Room / AI / Dmg):</color></b>");
                foreach (var unit in AnimatronicManager.Instance.Units)
                {
                    if (unit == null || unit.Config == null) continue;
                    string statusColor = unit.IsAtDoorBlindspot ? "#ff4444" : (unit.EffectiveAILevel > 0 ? "#88ff88" : "#888888");
                    string loc = unit.IsAtDoorBlindspot
                        ? $"DOOR {unit.TargetDoorIndex + 1} BLINDSPOT!"
                        : $"CAM {unit.CurrentCameraRoomIndex + 1:00} (Stg {unit.CurrentStage}/{unit.MaxStage})";

                    GUILayout.Label($"<color={statusColor}>• <b>{unit.Config.displayName}:</b> {loc} | AI:{unit.EffectiveAILevel} | Dmg:{unit.DamageLevel}</color>");
                }
            }

            GUILayout.Space(6);
            GUILayout.Label("<color=cyan><b>Letter Cheats:</b>\n[T] Step Animatronics +1 Stage | [B] Summon to Faced Door\n[G] Grant All 4 Weapons       | [Q] Use Counter-Weapon\n[N] Win Night (6 AM)          | [J] Test Jumpscare\n[M] +$100  [C] +5 Coins        | [R] Reset  [P] Pause</color>");

            GUILayout.EndArea();
        }
    }
}
