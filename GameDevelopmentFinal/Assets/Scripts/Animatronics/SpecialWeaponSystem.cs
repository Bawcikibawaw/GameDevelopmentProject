using UnityEngine;
using UnityEngine.InputSystem;
using Tylerland.Core;
using Tylerland.Office;

namespace Tylerland.Animatronics
{
    /// <summary>
    /// Section 4.1.2 & 4.7 GDD: Permanent Weapons & Nightmare Transformations.
    /// Each character-specific minigame permanently unlocks a special weapon (stored in MetaProgressionManager),
    /// with 1 use restored every run:
    /// - Door 0 (LEFT  - Erle the Eagle):   "Electric Gun" -> Triggers Nightmare Erle (Good Cop / Bad Cop, 12s window)
    /// - Door 1 (CENTER - Tyler the Lion):  "Shotgun"      -> Triggers Nightmare Tyler (Destroys CAM 02, halts attacks tonight)
    /// - Door 2 (RIGHT - Orville the Orca): "Harpoon"      -> Triggers Nightmare Orville (Watching CAM 03 advances him)
    /// - Door 3 (REAR  - Sandy the Snake):  "Spiked Mace"  -> Triggers Nightmare Sandy (5 stages, Decoy Shells, 8s window)
    /// TRUE ENDING RULE: Transform all FOUR regular animatronics into Nightmare forms within the SAME run!
    /// </summary>
    public class SpecialWeaponSystem : Singleton<SpecialWeaponSystem>
    {
        [Header("Settings")]
        [Tooltip("If true, permanently unlocks all 4 weapons on Start for immediate playtesting")]
        [SerializeField] private bool _unlockStarterWeaponsForTesting = true;

        private string _lastStatusMessage = "";
        private float _statusMessageTimer = 0f;

        public string LastStatusMessage => _statusMessageTimer > 0f ? _lastStatusMessage : "";

        private void Start()
        {
            if (_unlockStarterWeaponsForTesting && RunManager.HasInstance)
            {
                GrantAllWeapons();
            }
        }

        private void Update()
        {
            if (_statusMessageTimer > 0f)
            {
                _statusMessageTimer -= Time.deltaTime;
            }

            var flowState = GameFlowManager.HasInstance ? GameFlowManager.Instance.CurrentState : GameState.NightActive;
            if (!GameStateRules.IsGameplayActive(flowState) || Time.timeScale <= 0f) return;

            var keyboard = Keyboard.current;
            if (keyboard == null) return;

            // 'Q' Key: Fire Special Weapon at the currently faced doorway!
            if (keyboard.qKey.wasPressedThisFrame)
            {
                int facedDoor = PlayerViewController.HasInstance
                    ? (int)PlayerViewController.Instance.CurrentDirection
                    : 1;
                TryUseWeaponAtDoor(facedDoor);
            }

            // 'G' Key (Testing): Permanently unlock & restore all 4 counter-weapons
            if (keyboard.gKey.wasPressedThisFrame)
            {
                GrantAllWeapons();
                ShowStatus("Unlocked & restored all 4 Special Weapons (Electric Gun, Shotgun, Harpoon, Spiked Mace)!");
            }
        }

        [ContextMenu("Unlock & Grant All 4 Weapons")]
        public void GrantAllWeapons()
        {
            if (!RunManager.HasInstance) return;
            string[] weapons = { "Electric Gun", "Shotgun", "Harpoon", "Spiked Mace" };
            foreach (var w in weapons)
            {
                if (MetaProgressionManager.HasInstance)
                {
                    MetaProgressionManager.Instance.UnlockSoulItem(w);
                }
                if (!RunManager.Instance.State.weaponsOwned.Contains(w) && !RunManager.Instance.State.weaponsUsed.Contains(w))
                {
                    RunManager.Instance.AddWeapon(w);
                }
            }
        }

        /// <summary>
        /// Permanently unlocks a weapon from a post-night minigame so it is available (1 use per run) in all future runs.
        /// </summary>
        public void UnlockWeaponFromMinigame(string weaponId)
        {
            if (string.IsNullOrEmpty(weaponId)) return;
            if (MetaProgressionManager.HasInstance)
            {
                MetaProgressionManager.Instance.UnlockSoulItem(weaponId);
            }
            if (RunManager.HasInstance && !RunManager.Instance.State.weaponsOwned.Contains(weaponId) && !RunManager.Instance.State.weaponsUsed.Contains(weaponId))
            {
                RunManager.Instance.AddWeapon(weaponId);
            }
            ShowStatus($"★ Permanently Unlocked Weapon: {weaponId} (1 use per run)!");
        }

        /// <summary>
        /// Deploys the matching counter-weapon against an animatronic attacking the specified doorway.
        /// Interrupts the attack, causes visible damage (reducing salary), and triggers its Nightmare state!
        /// </summary>
        public bool TryUseWeaponAtDoor(int doorIndex)
        {
            if (!AnimatronicManager.HasInstance || !RunManager.HasInstance) return false;

            var unit = AnimatronicManager.Instance.GetAnimatronicAtDoorBlindspot(doorIndex);
            if (unit == null)
            {
                ShowStatus("No animatronic attacking this doorway to use a weapon on!");
                return false;
            }

            string requiredWeapon = unit.Config != null && !string.IsNullOrEmpty(unit.Config.counterWeaponId)
                ? unit.Config.counterWeaponId
                : GetDefaultWeaponForDoor(doorIndex);

            if (!RunManager.Instance.State.weaponsOwned.Contains(requiredWeapon))
            {
                ShowStatus($"Missing '{requiredWeapon}' for {unit.DisplayName}! (Press 'G' to unlock/restore weapons)");
                return false;
            }

            // 1. Consume the 1-per-run weapon use
            RunManager.Instance.UseWeapon(requiredWeapon);

            // 2. Interrupt active kill countdown in AttackResolver
            if (AttackResolver.HasInstance)
            {
                AttackResolver.Instance.CancelThreatForAnimatronic(unit.AnimatronicId);
            }

            // 3. Inflict damage & trigger Nightmare state in RunState
            RunManager.Instance.InflictDamage(unit.AnimatronicId, 1);
            unit.OnHitBySpecialWeapon();
            unit.RecalculateEffectiveAI(NightClock.HasInstance ? NightClock.Instance.HourBlendFactor : 0f);

            // 4. Award +1 Soul Coin
            if (MetaProgressionManager.HasInstance)
            {
                MetaProgressionManager.Instance.AddSoulCoins(1);
            }

            string msg = $"⚡ Fired {requiredWeapon} at {unit.DisplayName}! Attack interrupted -> NIGHTMARE FORM TRIGGERED!";
            if (RunManager.Instance.State.AllFourNightmaresActive)
            {
                msg = "🔥 ALL 4 NIGHTMARES TRIGGERED IN THIS RUN! TRUE ENDING BOSS ROUTE UNLOCKED!";
            }

            ShowStatus(msg);
            Debug.LogWarning($"[SpecialWeaponSystem] {msg}");
            return true;
        }

        public static string GetDefaultWeaponForDoor(int doorIndex)
        {
            switch (doorIndex)
            {
                case 0: return "Electric Gun"; // Left Door (Erle the Eagle)
                case 1: return "Shotgun";      // Center Door (Tyler the Lion)
                case 2: return "Harpoon";      // Right Door (Orville the Orca)
                case 3: return "Spiked Mace";  // Rear Door (Sandy the Snake)
                default: return "Shotgun";
            }
        }

        private void ShowStatus(string msg)
        {
            _lastStatusMessage = msg;
            _statusMessageTimer = 4.5f;
        }
    }
}
