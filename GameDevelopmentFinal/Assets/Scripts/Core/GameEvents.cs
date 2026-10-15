using System;
using UnityEngine;

namespace Tylerland.Core
{
    /// <summary>
    /// Breakdown of salary calculation at NightEnd according to section 6.1.
    /// </summary>
    [Serializable]
    public struct SalaryBreakdown
    {
        public int nightNumber;
        public int baseSalary;
        public int cleanNightBonus;
        public int repairDeductions;
        public int totalSalary;

        public SalaryBreakdown(int night, int @base, int bonus, int deductions, int total)
        {
            nightNumber = night;
            baseSalary = @base;
            cleanNightBonus = bonus;
            repairDeductions = deductions;
            totalSalary = total;
        }

        public override string ToString()
        {
            return $"Night {nightNumber} Salary: Base=${baseSalary} + CleanBonus=${cleanNightBonus} - Repairs=${repairDeductions} = Total=${totalSalary}";
        }
    }

    /// <summary>
    /// Central Event Bus for decoupled communication between all systems.
    /// UI, audio, and save code listen to these events.
    /// </summary>
    public static class GameEvents
    {
        #region Game Flow & State
        public static event Action<GameState, GameState> OnGameStateChanged;
        public static void TriggerGameStateChanged(GameState oldState, GameState newState) => OnGameStateChanged?.Invoke(oldState, newState);

        public static event Action<bool> OnPauseToggled;
        public static void TriggerPauseToggled(bool isPaused) => OnPauseToggled?.Invoke(isPaused);
        #endregion

        #region Night & Shift Clock
        public static event Action<int> OnNightStarted;
        public static void TriggerNightStarted(int nightNumber) => OnNightStarted?.Invoke(nightNumber);

        public static event Action<int> OnNightCompleted;
        public static void TriggerNightCompleted(int nightNumber) => OnNightCompleted?.Invoke(nightNumber);

        public static event Action<int, float> OnHourChanged;
        public static void TriggerHourChanged(int hour, float hourBlendFactor) => OnHourChanged?.Invoke(hour, hourBlendFactor);
        #endregion

        #region Office & Doors
        public static event Action<int> OnDoorClosed;
        public static void TriggerDoorClosed(int doorIndex) => OnDoorClosed?.Invoke(doorIndex);

        public static event Action<int> OnDoorOpened;
        public static void TriggerDoorOpened(int doorIndex) => OnDoorOpened?.Invoke(doorIndex);

        public static event Action<int, float, float> OnDoorStatsChanged; // doorIndex, energy, durability
        public static void TriggerDoorStatsChanged(int doorIndex, float energy, float durability) => OnDoorStatsChanged?.Invoke(doorIndex, energy, durability);
        #endregion

        #region Animatronics & Combat
        public static event Action<string, int, int> OnAnimatronicStageChanged;
        public static void TriggerAnimatronicStageChanged(string animatronicId, int oldStage, int newStage) => OnAnimatronicStageChanged?.Invoke(animatronicId, oldStage, newStage);

        public static event Action<string> OnJumpscare;
        public static void TriggerJumpscare(string animatronicId) => OnJumpscare?.Invoke(animatronicId);

        public static event Action<string> OnPlayerDied;
        public static void TriggerPlayerDied(string killedBy) => OnPlayerDied?.Invoke(killedBy);
        #endregion

        #region Economy & Roguelite Progression
        public static event Action<SalaryBreakdown> OnSalaryCalculated;
        public static void TriggerSalaryCalculated(SalaryBreakdown breakdown) => OnSalaryCalculated?.Invoke(breakdown);

        public static event Action<int, int> OnMoneyChanged; // newAmount, delta
        public static void TriggerMoneyChanged(int newAmount, int delta) => OnMoneyChanged?.Invoke(newAmount, delta);

        public static event Action<int, int> OnSoulCoinsChanged; // newAmount, delta
        public static void TriggerSoulCoinsChanged(int newAmount, int delta) => OnSoulCoinsChanged?.Invoke(newAmount, delta);

        public static event Action OnRunReset;
        public static void TriggerRunReset() => OnRunReset?.Invoke();

        public static event Action OnRunWon;
        public static void TriggerRunWon() => OnRunWon?.Invoke();

        public static event Action OnMetaSaved;
        public static void TriggerMetaSaved() => OnMetaSaved?.Invoke();
        #endregion

        /// <summary>
        /// Clears all event subscribers. Useful when testing or reloading domain without restart.
        /// </summary>
        public static void ClearAllSubscribers()
        {
            OnGameStateChanged = null;
            OnPauseToggled = null;
            OnNightStarted = null;
            OnNightCompleted = null;
            OnHourChanged = null;
            OnDoorClosed = null;
            OnDoorOpened = null;
            OnDoorStatsChanged = null;
            OnAnimatronicStageChanged = null;
            OnJumpscare = null;
            OnPlayerDied = null;
            OnSalaryCalculated = null;
            OnMoneyChanged = null;
            OnSoulCoinsChanged = null;
            OnRunReset = null;
            OnRunWon = null;
            OnMetaSaved = null;
        }
    }
}
