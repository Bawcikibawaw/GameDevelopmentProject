using UnityEngine;
using Tylerland.Core;

namespace Tylerland.Animatronics
{
    /// <summary>
    /// Section 5.7: Night 6 Boss Animatronic Controller.
    /// Drives the 3-phase Night 6 Boss ("The Amalgam") which breaks normal single-lane rules
    /// by dynamically switching target office doors (0-3) and escalating aggression across 3 phases.
    /// </summary>
    public class BossController : MonoBehaviour
    {
        [Header("Boss State (Read Only)")]
        [SerializeField] private bool _isBossActive = false;
        [SerializeField] private int _currentPhase = 1; // 1, 2, or 3
        [SerializeField] private AnimatronicUnit _bossUnit;

        public bool IsBossActive => _isBossActive;
        public int CurrentPhase => _currentPhase;

        public void InitializeBoss(AnimatronicUnit bossUnit, bool activeOnThisNight)
        {
            _bossUnit = bossUnit;
            _isBossActive = activeOnThisNight;
            _currentPhase = 1;

            if (_bossUnit != null && _isBossActive)
            {
                PickNewRandomDoor();
                Debug.LogWarning("[BossController] ⚠ NIGHT 6 BOSS ACTIVE! Phase 1 initiated.");
            }
        }

        private void OnEnable()
        {
            GameEvents.OnHourChanged += HandleHourChanged;
            GameEvents.OnAnimatronicStageChanged += HandleStageChanged;
        }

        private void OnDisable()
        {
            GameEvents.OnHourChanged -= HandleHourChanged;
            GameEvents.OnAnimatronicStageChanged -= HandleStageChanged;
        }

        private void HandleHourChanged(int hour, float blend)
        {
            if (!_isBossActive || _bossUnit == null) return;

            int newPhase = blend < 0.33f ? 1 : (blend < 0.66f ? 2 : 3);
            if (newPhase != _currentPhase)
            {
                _currentPhase = newPhase;
                Debug.LogWarning($"[BossController] ⚠ BOSS ENTERED PHASE {_currentPhase}!");
            }
        }

        private void HandleStageChanged(string animatronicId, int oldStage, int newStage)
        {
            if (!_isBossActive || _bossUnit == null) return;
            if (!string.Equals(animatronicId, _bossUnit.AnimatronicId, System.StringComparison.OrdinalIgnoreCase)) return;

            // Whenever the Boss retreats after being blocked at a door, or advances in Phase 3, switch target door!
            if (newStage < oldStage || (_currentPhase >= 3 && !(_bossUnit.IsAtDoorBlindspot)))
            {
                PickNewRandomDoor();
            }
        }

        private void PickNewRandomDoor()
        {
            if (_bossUnit == null) return;
            int nextDoor = Random.Range(0, 4);
            _bossUnit.SetDynamicDoorOverride(nextDoor);
            Debug.Log($"[BossController] Boss shifted attack vector to Door {nextDoor}!");
        }
    }
}
