using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Tylerland.Core;

namespace Tylerland.Office
{
    /// <summary>
    /// Pending attack request state tracked by the AttackResolver.
    /// </summary>
    public class ActiveThreat
    {
        public string animatronicId;
        public int doorIndex;
        public float attackWindowDuration;
        public float remainingWindow;
        public Action onBlockedRetreat;
        public bool isBlocked = false;

        public ActiveThreat(string id, int door, float window, Action onRetreat)
        {
            animatronicId = id;
            doorIndex = door;
            attackWindowDuration = window;
            remainingWindow = window;
            onBlockedRetreat = onRetreat;
        }
    }

    /// <summary>
    /// Section 4.4: Attack Window & Door Resolution.
    /// The universal arbiter deciding whether an animatronic reaching AttackReady
    /// is blocked by the door or kills the player.
    /// </summary>
    public class AttackResolver : Singleton<AttackResolver>
    {
        [Header("Default Timings (Roadmap 4.4)")]
        [Tooltip("Default attack window duration if unspecified by character config")]
        [SerializeField] private float _defaultAttackWindow = 3.0f;

        [Tooltip("Warning cue lead time before window countdown starts")]
        [SerializeField] private float _warningCueLead = 1.0f;

        [Tooltip("Min and Max duration animatronic lingers at closed door before retreating")]
        [SerializeField] private Vector2 _retreatDelayRange = new Vector2(3.0f, 6.0f);

        [Header("Fairness Guards (Roadmap 5.1)")]
        [Tooltip("Maximum simultaneous attack windows allowed to prevent impossible deaths")]
        [SerializeField] private int _simultaneousThreatCap = 2;

        [Tooltip("Minimum time gap between two AttackReady events on different doors")]
        [SerializeField] private float _fairnessMinGap = 1.5f;

        private readonly List<ActiveThreat> _activeThreats = new List<ActiveThreat>();
        private float _lastAttackReadyTimestamp = -10f;

        public int ActiveThreatCount => _activeThreats.Count;

        private void Update()
        {
            if (!GameStateRules.IsGameplayActive(GameFlowManager.HasInstance ? GameFlowManager.Instance.CurrentState : GameState.NightActive))
                return;
            if (Time.timeScale <= 0f)
                return;

            ProcessActiveThreats();
        }

        private void ProcessActiveThreats()
        {
            for (int i = _activeThreats.Count - 1; i >= 0; i--)
            {
                var threat = _activeThreats[i];

                // 1. Check if the corresponding door is currently closed
                bool doorClosed = DoorSystemManager.HasInstance && DoorSystemManager.Instance.IsDoorClosed(threat.doorIndex);

                if (doorClosed && !threat.isBlocked)
                {
                    // BLOCKED!
                    threat.isBlocked = true;
                    DoorSystemManager.Instance.TryBlockAnimatronic(threat.doorIndex, threat.animatronicId);
                    StartCoroutine(ExecuteRetreatRoutine(threat));
                    _activeThreats.RemoveAt(i);
                    continue;
                }

                // 2. Countdown window timer
                threat.remainingWindow -= Time.deltaTime;

                // 3. If timer expires and door remains open -> JUMPSCARE & DEATH!
                if (threat.remainingWindow <= 0f)
                {
                    ExecuteKill(threat);
                    _activeThreats.RemoveAt(i);
                    return;
                }
            }
        }

        /// <summary>
        /// Called by Animatronic AI when reaching AttackReady at a door.
        /// Returns true if accepted into the attack queue; false if rejected by fairness guard.
        /// </summary>
        public bool RegisterAttackArrival(string animatronicId, int doorIndex, float attackWindow, Action onRetreatCallback)
        {
            // Fairness Guard: Simultaneous threat cap
            if (_activeThreats.Count >= _simultaneousThreatCap)
            {
                Debug.Log($"[AttackResolver] Fairness guard rejected '{animatronicId}': Active threat cap ({_simultaneousThreatCap}) reached.");
                return false;
            }

            // Fairness Guard: Minimum gap between arrivals
            if (Time.time - _lastAttackReadyTimestamp < _fairnessMinGap)
            {
                Debug.Log($"[AttackResolver] Fairness guard delayed '{animatronicId}': Min gap ({_fairnessMinGap}s) not yet elapsed.");
                return false;
            }

            _lastAttackReadyTimestamp = Time.time;
            float window = attackWindow > 0f ? attackWindow : _defaultAttackWindow;

            var threat = new ActiveThreat(animatronicId, doorIndex, window, onRetreatCallback);
            _activeThreats.Add(threat);

            Debug.LogWarning($"[AttackResolver] THREAT ARRIVED: '{animatronicId}' at Door {doorIndex}! Window: {window}s to close door!");
            return true;
        }

        private IEnumerator ExecuteRetreatRoutine(ActiveThreat threat)
        {
            float linger = UnityEngine.Random.Range(_retreatDelayRange.x, _retreatDelayRange.y);
            Debug.Log($"[AttackResolver] '{threat.animatronicId}' blocked at Door {threat.doorIndex}. Lingering for {linger:F1}s before retreat.");
            yield return new WaitForSeconds(linger);

            threat.onBlockedRetreat?.Invoke();
            Debug.Log($"[AttackResolver] '{threat.animatronicId}' has retreated back to stage.");
        }

        private void ExecuteKill(ActiveThreat threat)
        {
            Debug.LogError($"[AttackResolver] ATTACK RESOLUTION: Door {threat.doorIndex} was OPEN! '{threat.animatronicId}' kills the player!");
            GameEvents.TriggerJumpscare(threat.animatronicId);
            GameEvents.TriggerPlayerDied(threat.animatronicId);
        }

        public void ClearAllThreats()
        {
            StopAllCoroutines();
            _activeThreats.Clear();
        }
    }
}
