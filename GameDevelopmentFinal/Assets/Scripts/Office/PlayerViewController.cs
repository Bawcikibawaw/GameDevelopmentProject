using System;
using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using Tylerland.Core;

namespace Tylerland.Office
{
    public enum OfficeViewDirection
    {
        Left = 0,    // -90 degrees (facing Shark door)
        Center = 1,  // 0 degrees (facing Tyler door)
        Right = 2,   // +90 degrees (facing Eagle door)
        Back = 3     // 180 degrees (facing Snake door)
    }

    /// <summary>
    /// Section 4.1: Player View & Input in 3D Unity.
    /// Controls the 3D first-person office view with 4 fixed cardinal angles,
    /// smooth turn interpolation, mouse edge-panning, and hotkey support.
    /// </summary>
    public class PlayerViewController : Singleton<PlayerViewController>
    {
        [Header("Camera & Orientation (3D)")]
        [Tooltip("The main camera transform to rotate. Defaults to Camera.main if unassigned.")]
        [SerializeField] private Transform _cameraTransform;

        [Tooltip("Current cardinal view direction of the player")]
        [SerializeField] private OfficeViewDirection _currentDirection = OfficeViewDirection.Center;

        [Header("Turn Timings (Roadmap 4.1)")]
        [Tooltip("Rotation duration between adjacent views (Left/Center/Right)")]
        [SerializeField] private float _turnDuration = 0.35f;

        [Tooltip("Rotation duration when turning to or from the Back door (deliberately slower)")]
        [SerializeField] private float _backTurnDuration = 0.55f;

        [Tooltip("Input buffer duration to forgive rapid clicks")]
        [SerializeField] private float _inputBuffer = 0.1f;

        [Header("Edge Panning")]
        [Tooltip("Enable mouse edge panning")]
        [SerializeField] private bool _enableEdgePan = true;

        [Tooltip("Percentage of screen width on edges that triggers view turning (default 12%)")]
        [Range(0.05f, 0.25f)]
        [SerializeField] private float _edgePanZone = 0.12f;

        [Header("Monitor State")]
        [SerializeField] private bool _isMonitorOpen = false;

        private bool _isRotating = false;
        private Coroutine _rotationRoutine;
        private float _bufferedTurnInput = 0f;
        private float _bufferTimer = 0f;

        public OfficeViewDirection CurrentDirection => _currentDirection;
        public bool IsRotating => _isRotating;
        public bool IsMonitorOpen => _isMonitorOpen;

        // Angle mapping for Y-axis rotation in 3D
        private readonly float[] _targetYRotations = new float[]
        {
            -90f,  // Left (Shark)
            0f,    // Center (Tyler)
            90f,   // Right (Eagle)
            180f   // Back (Snake)
        };

        protected override void Awake()
        {
            base.Awake();
            if (_cameraTransform == null)
            {
                if (Camera.main != null) _cameraTransform = Camera.main.transform;
                else _cameraTransform = transform;
            }
        }

        private void Start()
        {
            ApplyOrientationImmediate(_currentDirection);
        }

        private void Update()
        {
            // Only process view input during active gameplay and when game is not paused
            if (!GameStateRules.IsGameplayActive(GameFlowManager.HasInstance ? GameFlowManager.Instance.CurrentState : GameState.NightActive))
                return;
            if (Time.timeScale <= 0f)
                return;

            HandleMonitorInput();

            // When monitor is raised, player cannot pan the camera view
            if (_isMonitorOpen) return;

            HandleKeyboardInput();
            HandleEdgePanInput();
            ProcessBufferedInput();
        }

        #region Input Handlers
        private void HandleMonitorInput()
        {
            var keyboard = Keyboard.current;
            if (keyboard != null && keyboard.spaceKey.wasPressedThisFrame)
            {
                ToggleMonitor();
            }
        }

        public void ToggleMonitor()
        {
            SetMonitorState(!_isMonitorOpen);
        }

        public void SetMonitorState(bool open)
        {
            if (_isMonitorOpen == open) return;
            _isMonitorOpen = open;

            if (CameraMonitorSystem.HasInstance)
            {
                CameraMonitorSystem.Instance.SetMonitorRaised(_isMonitorOpen);
            }
        }

        private void HandleKeyboardInput()
        {
            var keyboard = Keyboard.current;
            if (keyboard == null) return;

            // 'A' or Left Arrow: Turn Left
            if (keyboard.aKey.wasPressedThisFrame || keyboard.leftArrowKey.wasPressedThisFrame)
            {
                RequestTurn(-1);
            }
            // 'D' or Right Arrow: Turn Right
            else if (keyboard.dKey.wasPressedThisFrame || keyboard.rightArrowKey.wasPressedThisFrame)
            {
                RequestTurn(1);
            }
            // 'S' or Down Arrow: Turn Back
            else if (keyboard.sKey.wasPressedThisFrame || keyboard.downArrowKey.wasPressedThisFrame)
            {
                TurnToDirection(OfficeViewDirection.Back);
            }
            // 'W' or Up Arrow: Turn Center
            else if (keyboard.wKey.wasPressedThisFrame || keyboard.upArrowKey.wasPressedThisFrame)
            {
                TurnToDirection(OfficeViewDirection.Center);
            }
        }

        private void HandleEdgePanInput()
        {
            if (!_enableEdgePan || _isRotating) return;

            var mouse = Mouse.current;
            if (mouse == null) return;

            Vector2 mousePos = mouse.position.ReadValue();
            float screenWidth = Screen.width;

            float leftThreshold = screenWidth * _edgePanZone;
            float rightThreshold = screenWidth * (1f - _edgePanZone);

            if (mousePos.x <= leftThreshold)
            {
                // Cursor in left zone
                RequestTurn(-1);
            }
            else if (mousePos.x >= rightThreshold)
            {
                // Cursor in right zone
                RequestTurn(1);
            }
        }

        private void RequestTurn(int delta)
        {
            if (_isRotating)
            {
                _bufferedTurnInput = delta;
                _bufferTimer = _inputBuffer;
                return;
            }

            int currentIdx = (int)_currentDirection;
            int targetIdx;

            if (delta < 0) // Turn Left (counter-clockwise)
            {
                if (_currentDirection == OfficeViewDirection.Center) targetIdx = (int)OfficeViewDirection.Left;
                else if (_currentDirection == OfficeViewDirection.Right) targetIdx = (int)OfficeViewDirection.Center;
                else if (_currentDirection == OfficeViewDirection.Left) targetIdx = (int)OfficeViewDirection.Back;
                else targetIdx = (int)OfficeViewDirection.Right; // From Back, turning Left turns to the Right wall
            }
            else // Turn Right (clockwise)
            {
                if (_currentDirection == OfficeViewDirection.Center) targetIdx = (int)OfficeViewDirection.Right;
                else if (_currentDirection == OfficeViewDirection.Left) targetIdx = (int)OfficeViewDirection.Center;
                else if (_currentDirection == OfficeViewDirection.Right) targetIdx = (int)OfficeViewDirection.Back;
                else targetIdx = (int)OfficeViewDirection.Left; // From Back, turning Right turns to the Left wall
            }

            TurnToDirection((OfficeViewDirection)targetIdx);
        }

        private void ProcessBufferedInput()
        {
            if (_bufferTimer > 0f)
            {
                _bufferTimer -= Time.deltaTime;
                if (!_isRotating && _bufferedTurnInput != 0f)
                {
                    RequestTurn((int)_bufferedTurnInput);
                    _bufferedTurnInput = 0f;
                }
            }
        }
        #endregion

        #region Rotation Logic
        public void TurnToDirection(OfficeViewDirection direction)
        {
            if (_currentDirection == direction && !_isRotating) return;

            OfficeViewDirection from = _currentDirection;
            _currentDirection = direction;

            float targetY = _targetYRotations[(int)direction];
            float duration = (from == OfficeViewDirection.Back || direction == OfficeViewDirection.Back)
                ? _backTurnDuration
                : _turnDuration;

            if (_rotationRoutine != null) StopCoroutine(_rotationRoutine);
            _rotationRoutine = StartCoroutine(SmoothRotateRoutine(targetY, duration));
        }

        private IEnumerator SmoothRotateRoutine(float targetY, float duration)
        {
            _isRotating = true;
            Quaternion startRot = _cameraTransform.localRotation;
            Quaternion targetRot = Quaternion.Euler(0f, targetY, 0f);

            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.SmoothStep(0f, 1f, elapsed / duration);
                _cameraTransform.localRotation = Quaternion.Slerp(startRot, targetRot, t);
                yield return null;
            }

            _cameraTransform.localRotation = targetRot;
            _isRotating = false;
            _rotationRoutine = null;
        }

        public void ApplyOrientationImmediate(OfficeViewDirection direction)
        {
            _currentDirection = direction;
            float targetY = _targetYRotations[(int)direction];
            if (_cameraTransform != null)
            {
                _cameraTransform.localRotation = Quaternion.Euler(0f, targetY, 0f);
            }
        }
        #endregion
    }
}
