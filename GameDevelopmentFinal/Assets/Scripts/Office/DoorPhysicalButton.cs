using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using Tylerland.Core;

namespace Tylerland.Office
{
    /// <summary>
    /// Section 4.3: Physical In-World Door & Light Buttons.
    /// Replaces screen-space OnGUI buttons with physical 2D/3D interactive objects mounted
    /// on the wall directly to the right of each doorway:
    /// - WHITE button for Hallway Blindspot LIGHT (toggles 'F' light on/off)
    /// - RED button for DOOR Shutter (toggles 'E' door open/closed)
    /// Features tactile click depression animation, hover highlight, and live state visualization.
    /// </summary>
    [RequireComponent(typeof(BoxCollider))]
    public class DoorPhysicalButton : MonoBehaviour
    {
        [Header("Button Identity")]
        [Tooltip("Door index: 0 = Left (Erle), 1 = Center (Tyler), 2 = Right (Orville), 3 = Rear (Sandy)")]
        [Range(0, 3)]
        [SerializeField] private int _doorIndex = 1;

        [Tooltip("True = WHITE Button for LIGHT, False = RED Button for DOOR")]
        [SerializeField] private bool _isLightButton = true;

        [Header("Visual Components")]
        [SerializeField] private SpriteRenderer _buttonRenderer;
        [SerializeField] private Sprite _customDefaultSprite;
        [SerializeField] private Sprite _customActiveSprite;

        [Header("Tactile Click Feedback")]
        [SerializeField] private float _pressDepressionDepth = 0.018f;
        [SerializeField] private float _pressDuration = 0.09f;

        private BoxCollider _boxCollider;
        private Vector3 _originalLocalPos;
        private bool _isPressedAnimRunning = false;
        private Camera _mainCam;

        public int DoorIndex => _doorIndex;
        public bool IsLightButton => _isLightButton;

        public void Setup(int doorIndex, bool isLightButton, SpriteRenderer renderer = null)
        {
            _doorIndex = doorIndex;
            _isLightButton = isLightButton;
            if (renderer != null) _buttonRenderer = renderer;
        }

        private void Awake()
        {
            _boxCollider = GetComponent<BoxCollider>();
            if (_boxCollider == null)
            {
                _boxCollider = gameObject.AddComponent<BoxCollider>();
            }
            _boxCollider.size = new Vector3(0.40f, 0.32f, 0.12f);
            _boxCollider.isTrigger = false;

            if (_buttonRenderer == null)
            {
                _buttonRenderer = GetComponent<SpriteRenderer>();
                if (_buttonRenderer == null)
                {
                    _buttonRenderer = gameObject.AddComponent<SpriteRenderer>();
                }
            }

            _buttonRenderer.sortingOrder = 15; // in front of wall and backplate
            _originalLocalPos = transform.localPosition;
            _mainCam = Camera.main;
        }

        private void Start()
        {
            UpdateVisuals(true);
        }

        private void Update()
        {
            UpdateVisuals(false);
            HandleDirectRaycastInput();
        }

        private void HandleDirectRaycastInput()
        {
            // Do not interact if monitor is actively raised
            bool isMonitorBlocking = (PlayerViewController.HasInstance && PlayerViewController.Instance.IsMonitorOpen)
                || (CameraMonitorSystem.HasInstance && CameraMonitorSystem.Instance.IsMonitorRaised && CameraMonitorSystem.Instance.AnimProgress > 0.15f);
            if (isMonitorBlocking) return;

            Vector2 mouseScreenPos = Vector2.zero;
            bool wasClicked = false;

            var mouse = Mouse.current;
            if (mouse != null)
            {
                mouseScreenPos = mouse.position.ReadValue();
                wasClicked = mouse.leftButton.wasPressedThisFrame;
            }
#if !ENABLE_INPUT_SYSTEM || ENABLE_LEGACY_INPUT_MANAGER
            else if (Input.GetMouseButtonDown(0))
            {
                mouseScreenPos = Input.mousePosition;
                wasClicked = true;
            }
#endif

            if (wasClicked)
            {
                if (_mainCam == null) _mainCam = Camera.main;
                if (_mainCam != null)
                {
                    Ray ray = _mainCam.ScreenPointToRay(mouseScreenPos);
                    if (Physics.Raycast(ray, out RaycastHit hit, 12f))
                    {
                        if (hit.collider == _boxCollider || hit.transform == transform)
                        {
                            TriggerPress();
                        }
                    }
                }
            }
        }

        private void OnMouseDown()
        {
            // Unity standard click callback as robust fallback
            bool isMonitorBlocking = (PlayerViewController.HasInstance && PlayerViewController.Instance.IsMonitorOpen)
                || (CameraMonitorSystem.HasInstance && CameraMonitorSystem.Instance.IsMonitorRaised && CameraMonitorSystem.Instance.AnimProgress > 0.15f);
            if (isMonitorBlocking) return;

            TriggerPress();
        }

        public void TriggerPress()
        {
            if (_isLightButton)
            {
                if (DoorSystemManager.HasInstance)
                {
                    DoorSystemManager.Instance.ToggleDoorLight(_doorIndex);
                }
                else
                {
                    var door = FindDoorByIndex(_doorIndex);
                    door?.ToggleLight();
                }
            }
            else
            {
                if (DoorSystemManager.HasInstance)
                {
                    DoorSystemManager.Instance.ToggleDoor(_doorIndex);
                }
                else
                {
                    var door = FindDoorByIndex(_doorIndex);
                    door?.ToggleDoor();
                }
            }

            if (!_isPressedAnimRunning && gameObject.activeInHierarchy)
            {
                StartCoroutine(TactileDepressionRoutine());
            }
        }

        private IEnumerator TactileDepressionRoutine()
        {
            _isPressedAnimRunning = true;
            // Press button inwards into wall (along local +Z)
            Vector3 pressedPos = _originalLocalPos + new Vector3(0, 0, _pressDepressionDepth);
            transform.localPosition = pressedPos;

            yield return new WaitForSeconds(_pressDuration);

            transform.localPosition = _originalLocalPos;
            _isPressedAnimRunning = false;
        }

        private void UpdateVisuals(bool force)
        {
            var door = DoorSystemManager.HasInstance ? DoorSystemManager.Instance.GetDoor(_doorIndex) : FindDoorByIndex(_doorIndex);

            if (_isLightButton)
            {
                // WHITE BUTTON FOR LIGHT (User Requirement: white for light)
                bool isLightOn = door != null && door.IsLightOn;

                if (_customDefaultSprite != null && _customActiveSprite != null)
                {
                    _buttonRenderer.sprite = isLightOn ? _customActiveSprite : _customDefaultSprite;
                }
                else
                {
                    _buttonRenderer.sprite = Procedural2DSpriteFactory.GetOrCreateLightButtonSprite(isLightOn);
                }

                _buttonRenderer.color = isLightOn
                    ? new Color(1.0f, 1.0f, 0.94f, 1.0f) // Bright luminous warm white glow
                    : new Color(0.88f, 0.90f, 0.94f, 1.0f); // Clean industrial titanium white
            }
            else
            {
                // RED BUTTON FOR DOOR (User Requirement: red for door)
                bool isClosed = door != null && door.IsClosed;
                bool isBrokenOrLocked = door != null && (door.IsBroken || door.IsLockedOut);

                if (_customDefaultSprite != null && _customActiveSprite != null)
                {
                    _buttonRenderer.sprite = isClosed ? _customActiveSprite : _customDefaultSprite;
                }
                else
                {
                    _buttonRenderer.sprite = Procedural2DSpriteFactory.GetOrCreateDoorButtonSprite(isClosed, isBrokenOrLocked);
                }

                if (isBrokenOrLocked)
                {
                    float pulse = (Mathf.Sin(Time.time * 8f) + 1f) * 0.5f;
                    _buttonRenderer.color = Color.Lerp(new Color(1f, 0.45f, 0f, 1f), new Color(1f, 0.8f, 0.2f, 1f), pulse);
                }
                else if (isClosed)
                {
                    _buttonRenderer.color = new Color(1.0f, 0.18f, 0.18f, 1.0f); // High-voltage vivid red (Closed)
                }
                else
                {
                    _buttonRenderer.color = new Color(0.68f, 0.12f, 0.12f, 1.0f); // Industrial dark crimson (Open)
                }
            }
        }

        private DoorController FindDoorByIndex(int index)
        {
            var doors = FindObjectsByType<DoorController>(FindObjectsSortMode.None);
            for (int i = 0; i < doors.Length; i++)
            {
                if (doors[i].DoorIndex == index) return doors[i];
            }
            return null;
        }
    }
}
