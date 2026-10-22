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
        public float elapsedAtDoor;
        public Action onBlockedRetreat;
        public bool isBlocked = false;

        public ActiveThreat(string id, int door, float window, Action onRetreat)
        {
            animatronicId = id;
            doorIndex = door;
            attackWindowDuration = window;
            remainingWindow = window;
            elapsedAtDoor = 0f;
            onBlockedRetreat = onRetreat;
        }
    }

    /// <summary>
    /// Section 4.4: Attack Window & Door Resolution.
    /// The universal arbiter deciding whether an animatronic reaching the Office Doorway
    /// is blocked by the door or kills the player when the reaction time expires.
    /// </summary>
    public class AttackResolver : Singleton<AttackResolver>
    {
        [Header("Default Timings (Roadmap 4.4)")]
        [Tooltip("Default reaction time in seconds if unspecified by character config")]
        [SerializeField] private float _defaultAttackWindow = 6.0f;

        [Tooltip("Warning cue lead time before window countdown starts")]
        [SerializeField] private float _warningCueLead = 0.5f;

        [Tooltip("Short delay after hitting a closed door before the animatronic returns to its room")]
        [SerializeField] private float _retreatDelayAfterBlock = 0.8f;

        [Header("Fairness Guards (Roadmap 5.1)")]
        [Tooltip("Maximum simultaneous attack windows allowed to prevent impossible deaths")]
        [SerializeField] private int _simultaneousThreatCap = 3;

        [Tooltip("Minimum time gap between two AttackReady events on different doors")]
        [SerializeField] private float _fairnessMinGap = 1.0f;

        private readonly List<ActiveThreat> _activeThreats = new List<ActiveThreat>();
        private float _lastAttackReadyTimestamp = -10f;
        private string _lastBlockStatusMessage = "";
        private float _blockStatusTimer = 0f;

        public int ActiveThreatCount => _activeThreats.Count;
        public float WarningCueLead => _warningCueLead;
        public string LastBlockStatusMessage => _blockStatusTimer > 0f ? _lastBlockStatusMessage : "";

        private void Update()
        {
            if (_blockStatusTimer > 0f)
            {
                _blockStatusTimer -= Time.deltaTime;
            }

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
                threat.elapsedAtDoor += Time.deltaTime;

                // 1. Check if the corresponding door is currently closed
                bool doorClosed = DoorSystemManager.HasInstance && DoorSystemManager.Instance.IsDoorClosed(threat.doorIndex);

                // Give a 0.9s minimum visual presence if the door was already closed on arrival so the player sees the arrival & block
                if (doorClosed && !threat.isBlocked && threat.elapsedAtDoor >= 0.9f)
                {
                    // BLOCKED!
                    threat.isBlocked = true;
                    DoorSystemManager.Instance.TryBlockAnimatronic(threat.doorIndex, threat.animatronicId);

                    string[] doorNames = { "LEFT DOOR", "CENTER DOOR", "RIGHT DOOR", "REAR DOOR" };
                    string dName = doorNames[Mathf.Clamp(threat.doorIndex, 0, 3)];
                    _lastBlockStatusMessage = $"✔ BLOCKED {threat.animatronicId.ToUpper()} AT {dName}! (Retreated to CAM {threat.doorIndex + 1:00})";
                    _blockStatusTimer = 3.5f;

                    StartCoroutine(ExecuteRetreatRoutine(threat));
                    _activeThreats.RemoveAt(i);
                    continue;
                }

                // 2. Countdown reaction time window
                threat.remainingWindow -= Time.deltaTime;

                // 3. If reaction time expires and door remains open -> JUMPSCARE & DEATH!
                if (threat.remainingWindow <= 0f)
                {
                    ExecuteKill(threat);
                    _activeThreats.RemoveAt(i);
                    return;
                }
            }
        }

        /// <summary>
        /// Called by Animatronic AI when leaving its room and arriving at an Office Doorway.
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

            Debug.LogWarning($"[AttackResolver] THREAT ARRIVED: '{animatronicId}' at Door {doorIndex}! Reaction Time: {window:F1}s to close door!");
            return true;
        }

        private IEnumerator ExecuteRetreatRoutine(ActiveThreat threat)
        {
            Debug.Log($"[AttackResolver] '{threat.animatronicId}' blocked at Door {threat.doorIndex}. Returning to camera room in {_retreatDelayAfterBlock:F1}s.");
            yield return new WaitForSeconds(_retreatDelayAfterBlock);

            threat.onBlockedRetreat?.Invoke();
            Debug.Log($"[AttackResolver] '{threat.animatronicId}' has returned to its camera room.");
        }

        private void ExecuteKill(ActiveThreat threat)
        {
            Debug.LogError($"[AttackResolver] ATTACK RESOLUTION: Door {threat.doorIndex} was OPEN! '{threat.animatronicId}' kills the player!");
            GameEvents.TriggerJumpscare(threat.animatronicId);
            GameEvents.TriggerPlayerDied(threat.animatronicId);
        }

        public ActiveThreat GetActiveThreatAtDoor(int doorIndex)
        {
            for (int i = 0; i < _activeThreats.Count; i++)
            {
                if (_activeThreats[i].doorIndex == doorIndex && !_activeThreats[i].isBlocked)
                {
                    return _activeThreats[i];
                }
            }
            return null;
        }

        public float GetRemainingWindowForDoor(int doorIndex)
        {
            var t = GetActiveThreatAtDoor(doorIndex);
            return t != null ? Mathf.Max(0f, t.remainingWindow) : 0f;
        }

        public bool CancelThreatForAnimatronic(string animatronicId)
        {
            for (int i = _activeThreats.Count - 1; i >= 0; i--)
            {
                if (string.Equals(_activeThreats[i].animatronicId, animatronicId, StringComparison.OrdinalIgnoreCase))
                {
                    _activeThreats.RemoveAt(i);
                    return true;
                }
            }
            return false;
        }

        public void ClearAllThreats()
        {
            StopAllCoroutines();
            _activeThreats.Clear();
        }
    }
}
