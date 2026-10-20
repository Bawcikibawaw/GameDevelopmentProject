using UnityEngine;
using UnityEngine.InputSystem;
using Tylerland.Core;
using Tylerland.Office;

namespace Tylerland.Animatronics
{
    /// <summary>
    /// Section 5.5: One-Use Special Weapons System.
    /// Allows the player to counter an animatronic lurking in a doorway blindspot using its matching weapon:
    /// - Door 0 (Left - Shark):   "Harpoon"
    /// - Door 1 (Center - Tyler): "Shears"
    /// - Door 2 (Right - Eagle):  "Net"
    /// - Door 3 (Back - Snake):   "Prod"
    /// Inflicts +1 Damage Level on the animatronic (which increases future repair costs & awards Soul Coins)
    /// and forces an immediate retreat to Stage 0 without draining door energy.
    /// </summary>
    public class SpecialWeaponSystem : Singleton<SpecialWeaponSystem>
    {
        [Header("Settings")]
        [Tooltip("If true, grants 1 of each weapon at the start of Night 1 for easy testing")]
        [SerializeField] private bool _grantStarterWeaponsForTesting = true;

        private string _lastStatusMessage = "";
        private float _statusMessageTimer = 0f;

        public string LastStatusMessage => _statusMessageTimer > 0f ? _lastStatusMessage : "";

        private void Start()
        {
            if (_grantStarterWeaponsForTesting && RunManager.HasInstance)
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

            // 'Q' Key: Deploy Special Weapon at the currently faced doorway!
            if (keyboard.qKey.wasPressedThisFrame)
            {
                int facedDoor = PlayerViewController.HasInstance
                    ? (int)PlayerViewController.Instance.CurrentDirection
                    : 1;
                TryUseWeaponAtDoor(facedDoor);
            }

            // 'G' Key (Debug/Testing): Grant all 4 counter-weapons
            if (keyboard.gKey.wasPressedThisFrame)
            {
                GrantAllWeapons();
                ShowStatus("Granted all 4 Special Weapons (Harpoon, Shears, Net, Prod)!");
            }
        }

        [ContextMenu("Grant All 4 Weapons")]
        public void GrantAllWeapons()
        {
            if (!RunManager.HasInstance) return;
            string[] weapons = { "Harpoon", "Shears", "Net", "Prod" };
            foreach (var w in weapons)
            {
                if (!RunManager.Instance.State.weaponsOwned.Contains(w))
                {
                    RunManager.Instance.AddWeapon(w);
                }
            }
        }

        /// <summary>
        /// Attempts to deploy the matching counter-weapon against an animatronic in the specified doorway blindspot.
        /// </summary>
        public bool TryUseWeaponAtDoor(int doorIndex)
        {
            if (!AnimatronicManager.HasInstance || !RunManager.HasInstance) return false;

            var unit = AnimatronicManager.Instance.GetAnimatronicAtDoorBlindspot(doorIndex);
            if (unit == null)
            {
                ShowStatus("No animatronic in this doorway blindspot to use a weapon on!");
                return false;
            }

            string requiredWeapon = unit.Config != null && !string.IsNullOrEmpty(unit.Config.counterWeaponId)
                ? unit.Config.counterWeaponId
                : GetDefaultWeaponForDoor(doorIndex);

            if (!RunManager.Instance.State.weaponsOwned.Contains(requiredWeapon))
            {
                ShowStatus($"Missing counter-weapon '{requiredWeapon}' for {unit.DisplayName}! (Press 'G' to grant test weapons)");
                return false;
            }

            // Consume weapon
            RunManager.Instance.UseWeapon(requiredWeapon);

            // Cancel active kill countdown in AttackResolver
            if (AttackResolver.HasInstance)
            {
                AttackResolver.Instance.CancelThreatForAnimatronic(unit.AnimatronicId);
            }

            // Inflict +1 Damage Level on this animatronic in RunState
            RunManager.Instance.InflictDamage(unit.AnimatronicId, 1);
            unit.RefreshDamageState();
            unit.RecalculateEffectiveAI(NightClock.HasInstance ? NightClock.Instance.HourBlendFactor : 0f);

            // Award +1 Soul Coin for damaging an animatronic (Roadmap 6.3)
            if (MetaProgressionManager.HasInstance)
            {
                MetaProgressionManager.Instance.AddSoulCoins(1);
            }

            // Force animatronic all the way back to Stage 0!
            unit.ForceRetreatToStage(0);

            string msg = $"⚡ Used {requiredWeapon} on {unit.DisplayName}! Stunned to Stage 0! (Damage Lv.{unit.DamageLevel}/3, +1 Soul Coin)";
            ShowStatus(msg);
            Debug.Log($"[SpecialWeaponSystem] {msg}");
            return true;
        }

        public static string GetDefaultWeaponForDoor(int doorIndex)
        {
            switch (doorIndex)
            {
                case 0: return "Harpoon"; // Left (Shark)
                case 1: return "Shears";  // Center (Tyler)
                case 2: return "Net";     // Right (Eagle)
                case 3: return "Prod";    // Back (Snake)
                default: return "Shears";
            }
        }

        private void ShowStatus(string msg)
        {
            _lastStatusMessage = msg;
            _statusMessageTimer = 3.5f;
        }
    }
}
