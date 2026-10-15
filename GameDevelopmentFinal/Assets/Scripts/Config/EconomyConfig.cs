using UnityEngine;

namespace Tylerland.Config
{
    /// <summary>
    /// Economy rules and configuration for salaries, repairs, and Soul Coins.
    /// ScriptableObject so designers can tune rewards and costs without code changes.
    /// </summary>
    [CreateAssetMenu(fileName = "EconomyConfig", menuName = "Tylerland/Economy Config", order = 2)]
    public class EconomyConfig : ScriptableObject
    {
        [Header("Base Salary per Night")]
        [Tooltip("Base salary awarded upon completing each night (Index 0 = Night 1, Index 5 = Night 6)")]
        public int[] baseSalaryPerNight = new int[] { 100, 150, 200, 275, 350, 450 };

        [Header("Bonuses & Deductions")]
        [Tooltip("Bonus percentage applied if no animatronic was damaged during the night (economic playstyle)")]
        [Range(0f, 50f)]
        public float cleanNightBonusPercent = 10f;

        [Tooltip("Percentage deduction of base salary per damage level inflicted on animatronics during the shift")]
        [Range(5f, 50f)]
        public float repairDeductionPerLevelPercent = 25f;

        [Tooltip("Guaranteed minimum floor percentage of base salary so players never get zero")]
        [Range(10f, 50f)]
        public float minSalaryFractionPercent = 20f;

        [Header("Soul Coins (Persistent Roguelite Currency)")]
        [Tooltip("Soul coins awarded per damage level dealt when a night is survived")]
        public int soulCoinsPerDamageLevel = 1;

        [Tooltip("Maximum soul coins earnable in a single night")]
        public int soulCoinNightCap = 3;

        [Tooltip("Percentage of accumulated Soul Coins retained upon death (GDD states 100%)")]
        [Range(0f, 100f)]
        public float soulCoinKeptOnDeathPercent = 100f;

        /// <summary>
        /// Calculates salary breakdown for a given night and damage count.
        /// </summary>
        public Core.SalaryBreakdown CalculateSalary(int nightNumber, int totalDamageLevelsDealt)
        {
            int nightIndex = Mathf.Clamp(nightNumber - 1, 0, baseSalaryPerNight.Length - 1);
            int @base = baseSalaryPerNight[nightIndex];

            int cleanBonus = 0;
            int deductions = 0;

            if (totalDamageLevelsDealt <= 0)
            {
                cleanBonus = Mathf.RoundToInt(@base * (cleanNightBonusPercent / 100f));
            }
            else
            {
                deductions = Mathf.RoundToInt(@base * (repairDeductionPerLevelPercent / 100f) * totalDamageLevelsDealt);
            }

            int minFloor = Mathf.RoundToInt(@base * (minSalaryFractionPercent / 100f));
            int calculatedTotal = @base + cleanBonus - deductions;
            int finalTotal = Mathf.Max(minFloor, calculatedTotal);

            return new Core.SalaryBreakdown(nightNumber, @base, cleanBonus, deductions, finalTotal);
        }
    }
}
