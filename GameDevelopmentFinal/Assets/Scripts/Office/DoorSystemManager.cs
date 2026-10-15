using UnityEngine;
using UnityEngine.InputSystem;
using Tylerland.Core;

namespace Tylerland.Office
{
    /// <summary>
    /// Section 4.3: Door System Coordinator.
    /// Manages the 4 office security doors:
    /// Door 0: Left (Shark)
    /// Door 1: Center (Tyler)
    /// Door 2: Right (Eagle)
    /// Door 3: Back (Snake)
    /// Provides hotkey mapping (1-4 or 'E' for currently faced door).
    /// </summary>
    public class DoorSystemManager : Singleton<DoorSystemManager>
    {
        [Header("Door References (Left=0, Center=1, Right=2, Back=3)")]
        [Tooltip("The 4 DoorController instances in the scene")]
        [SerializeField] private DoorController[] _doors = new DoorController[4];

        [Header("Interaction Settings")]
        [Tooltip("Allow pressing 'E' to toggle the door the player is currently facing in 3D")]
        [SerializeField] private bool _allowFacingInteraction = true;

        private void Start()
        {
            AutoFindDoorsIfEmpty();
        }

        private void Update()
        {
            if (!GameStateRules.IsGameplayActive(GameFlowManager.HasInstance ? GameFlowManager.Instance.CurrentState : GameState.NightActive))
                return;
            if (Time.timeScale <= 0f)
                return;

            HandleDoorHotkeys();
        }

        private void HandleDoorHotkeys()
        {
            var keyboard = Keyboard.current;
            if (keyboard == null) return;

            // Direct door hotkeys 1, 2, 3, 4
            if (keyboard.digit1Key.wasPressedThisFrame) ToggleDoor(0); // Left (Shark)
            if (keyboard.digit2Key.wasPressedThisFrame) ToggleDoor(1); // Center (Tyler)
            if (keyboard.digit3Key.wasPressedThisFrame) ToggleDoor(2); // Right (Eagle)
            if (keyboard.digit4Key.wasPressedThisFrame) ToggleDoor(3); // Back (Snake)

            // Contextual 'E' key: Toggle the door you are currently looking at!
            if (_allowFacingInteraction && keyboard.eKey.wasPressedThisFrame)
            {
                if (PlayerViewController.HasInstance)
                {
                    int facingIdx = (int)PlayerViewController.Instance.CurrentDirection;
                    ToggleDoor(facingIdx);
                }
            }

            // Contextual 'F' key: Toggle Hallway Blindspot Light (like FNAF 1)!
            if (keyboard.fKey.wasPressedThisFrame)
            {
                if (PlayerViewController.HasInstance)
                {
                    int facingIdx = (int)PlayerViewController.Instance.CurrentDirection;
                    ToggleDoorLight(facingIdx);
                }
            }
        }

        public void ToggleDoorLight(int doorIndex)
        {
            var door = GetDoor(doorIndex);
            if (door != null)
            {
                door.ToggleLight();
            }
        }

        public void ToggleDoor(int doorIndex)
        {
            var door = GetDoor(doorIndex);
            if (door != null)
            {
                door.ToggleDoor();
            }
        }

        public DoorController GetDoor(int index)
        {
            if (_doors != null && index >= 0 && index < _doors.Length)
            {
                return _doors[index];
            }
            return null;
        }

        public bool IsDoorClosed(int doorIndex)
        {
            var door = GetDoor(doorIndex);
            return door != null && door.IsClosed;
        }

        public bool TryBlockAnimatronic(int doorIndex, string animatronicId)
        {
            var door = GetDoor(doorIndex);
            if (door != null)
            {
                return door.TryBlockAttack(animatronicId);
            }
            return false;
        }

        [ContextMenu("Auto-Find Doors in Scene")]
        public void AutoFindDoorsIfEmpty()
        {
            var foundDoors = FindObjectsByType<DoorController>(FindObjectsSortMode.None);
            foreach (var d in foundDoors)
            {
                if (d.DoorIndex >= 0 && d.DoorIndex < _doors.Length)
                {
                    _doors[d.DoorIndex] = d;
                }
            }
        }
    }
}
