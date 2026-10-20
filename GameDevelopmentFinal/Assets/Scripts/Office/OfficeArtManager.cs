using System;
using UnityEngine;

namespace Tylerland.Office
{
    /// <summary>
    /// Holds the 2D hand-drawn sprite assets and scene references for one direction of the office
    /// (Left / West, Center / North, Right / East, Back / South).
    /// </summary>
    [Serializable]
    public class DirectionalWallArtSlot
    {
        public string directionName = "Center (North - Tyler)";

        [Header("2D Artist Sprites (Drag & Drop PNGs Here)")]
        [Tooltip("Hand-drawn 2D office wall with a transparent cutout in the center for the doorway")]
        public Sprite wallSprite;

        [Tooltip("Hand-drawn 2D corridor background seen through the open doorway")]
        public Sprite corridorBackdropSprite;

        [Tooltip("Optional separate sprite for the corridor when the 'F' Hallway Light is turned ON")]
        public Sprite corridorLitSprite;

        [Tooltip("Optional test/preview sprite for an animatronic standing in the doorway blindspot")]
        public Sprite blindspotAnimatronicSprite;

        [Header("Target World Dimensions (Auto-Fit)")]
        [Tooltip("Width and height in meters that the wall sprite will automatically fit to")]
        public Vector2 wallTargetSize = new Vector2(10.0f, 3.5f);

        [Tooltip("Width and height in meters that the corridor backdrop sprite will fit to")]
        public Vector2 corridorTargetSize = new Vector2(2.6f, 3.5f);

        [Tooltip("Width and height in meters that the doorway animatronic sprite will fit to")]
        public Vector2 animatronicTargetSize = new Vector2(2.0f, 2.8f);

        [Header("Scene Renderer References (Auto-Wired)")]
        public SpriteRenderer wallSpriteRenderer;
        public SpriteRenderer corridorSpriteRenderer;
        public SpriteRenderer blindspotAnimatronicRenderer;

        [Tooltip("3D gray placeholder cubes that automatically hide once a 2D wallSprite is assigned")]
        public GameObject[] placeholder3DWallObjects;

        [Tooltip("3D gray placeholder corridor cubes that automatically hide once a 2D corridorBackdropSprite is assigned")]
        public GameObject placeholder3DCorridorObject;
    }

    /// <summary>
    /// Central manager for switching the 3D blockout office into a 100% 2D Hand-Drawn Sprite environment.
    /// Automatically fits any sprite resolution to the exact room dimensions, prevents horizontal mirroring,
    /// hides 3D placeholder cubes when 2D sprites are assigned, and syncs corridor sprites with the 'F' hallway light.
    /// </summary>
    [ExecuteAlways]
    public class OfficeArtManager : MonoBehaviour
    {
        [Header("4 Cardinal Office Walls & Corridors (0=Left, 1=Center, 2=Right, 3=Back)")]
        [SerializeField] private DirectionalWallArtSlot[] _wallSlots = new DirectionalWallArtSlot[4];

        [Header("Office Desk, Floor & Ceiling 2D Sprites")]
        [Tooltip("2D hand-drawn security desk sprite (with fan/monitor/props)")]
        [SerializeField] private Sprite _deskSprite;
        [SerializeField] private Vector2 _deskTargetSize = new Vector2(2.8f, 1.4f);
        [SerializeField] private SpriteRenderer _deskSpriteRenderer;
        [SerializeField] private GameObject _placeholder3DDesk;

        [Tooltip("Optional 2D hand-drawn floor texture/sprite")]
        [SerializeField] private Sprite _floorSprite;
        [SerializeField] private Vector2 _floorTargetSize = new Vector2(10.0f, 10.0f);
        [SerializeField] private SpriteRenderer _floorSpriteRenderer;
        [SerializeField] private GameObject _placeholder3DFloor;

        [Tooltip("Optional 2D hand-drawn ceiling texture/sprite")]
        [SerializeField] private Sprite _ceilingSprite;
        [SerializeField] private Vector2 _ceilingTargetSize = new Vector2(10.0f, 10.0f);
        [SerializeField] private SpriteRenderer _ceilingSpriteRenderer;
        [SerializeField] private GameObject _placeholder3DCeiling;

        [Header("2D Lighting & Atmosphere Tints (URP Compatible)")]
        [Tooltip("Tint applied to the 2D office walls & desk inside the room")]
        [SerializeField] private Color _officeInteriorTint = new Color(0.88f, 0.85f, 0.80f, 1f);

        [Tooltip("Dark tint applied to the 2D corridor & doorway blindspot when the 'F' Hallway Light is OFF")]
        [SerializeField] private Color _corridorDarkTint = new Color(0.04f, 0.04f, 0.07f, 1f);

        [Tooltip("Bright tint applied to the 2D corridor & doorway blindspot when the 'F' Hallway Light is ON")]
        [SerializeField] private Color _corridorLitTint = Color.white;

        [Tooltip("Subtle FNAF fluorescent flicker when hallway light is held on")]
        [SerializeField] private bool _enableFluorescentFlicker = true;

        public DirectionalWallArtSlot[] WallSlots => _wallSlots;

        private void Awake()
        {
            EnsureSlotsInitialized();
            ApplyAllArt();
        }

        private void OnValidate()
        {
            EnsureSlotsInitialized();
            ApplyAllArt();
        }

        private void Update()
        {
            UpdateCorridorLighting();
        }

        private void EnsureSlotsInitialized()
        {
            if (_wallSlots == null || _wallSlots.Length != 4)
            {
                _wallSlots = new DirectionalWallArtSlot[4];
            }

            string[] defaultNames =
            {
                "0. Left Wall & West Corridor (Shark)",
                "1. Center Wall & North Corridor (Tyler)",
                "2. Right Wall & East Corridor (Eagle)",
                "3. Back Wall & South Vent Corridor (Snake)"
            };

            for (int i = 0; i < 4; i++)
            {
                if (_wallSlots[i] == null)
                {
                    _wallSlots[i] = new DirectionalWallArtSlot();
                }
                if (string.IsNullOrEmpty(_wallSlots[i].directionName) || _wallSlots[i].directionName.StartsWith("Center"))
                {
                    _wallSlots[i].directionName = defaultNames[i];
                }
            }
        }

        /// <summary>
        /// Applies all assigned 2D sprites, auto-scales them to match the 3D room dimensions,
        /// and toggles the visibility of 3D gray placeholder meshes.
        /// </summary>
        [ContextMenu("Apply & Fit All 2D Sprites")]
        public void ApplyAllArt()
        {
            if (_wallSlots != null)
            {
                for (int i = 0; i < _wallSlots.Length; i++)
                {
                    var slot = _wallSlots[i];
                    if (slot == null) continue;

                    // 1. Office Wall Sprite
                    if (slot.wallSpriteRenderer != null)
                    {
                        slot.wallSpriteRenderer.sprite = slot.wallSprite;
                        slot.wallSpriteRenderer.color = _officeInteriorTint;
                        if (slot.wallSprite != null)
                        {
                            FitSpriteRendererToSize(slot.wallSpriteRenderer, slot.wallTargetSize);
                        }
                    }

                    // Auto-hide 3D wall cubes if 2D wall sprite is provided
                    if (slot.placeholder3DWallObjects != null)
                    {
                        bool show3DWall = (slot.wallSprite == null);
                        foreach (var obj in slot.placeholder3DWallObjects)
                        {
                            if (obj != null)
                            {
                                var mr = obj.GetComponent<MeshRenderer>();
                                if (mr != null) mr.enabled = show3DWall;
                            }
                        }
                    }

                    // 2. Corridor Backdrop Sprite
                    if (slot.corridorSpriteRenderer != null)
                    {
                        slot.corridorSpriteRenderer.sprite = slot.corridorBackdropSprite;
                        if (slot.corridorBackdropSprite != null)
                        {
                            FitSpriteRendererToSize(slot.corridorSpriteRenderer, slot.corridorTargetSize);
                        }
                    }

                    // Auto-hide 3D corridor tunnel meshes if 2D corridor sprite is provided
                    if (slot.placeholder3DCorridorObject != null)
                    {
                        bool show3DCorridor = (slot.corridorBackdropSprite == null);
                        var renderers = slot.placeholder3DCorridorObject.GetComponentsInChildren<MeshRenderer>(true);
                        foreach (var mr in renderers)
                        {
                            mr.enabled = show3DCorridor;
                        }
                    }

                    // 3. Blindspot Animatronic Sprite
                    if (slot.blindspotAnimatronicRenderer != null)
                    {
                        slot.blindspotAnimatronicRenderer.sprite = slot.blindspotAnimatronicSprite;
                        if (slot.blindspotAnimatronicSprite != null)
                        {
                            FitSpriteRendererToSize(slot.blindspotAnimatronicRenderer, slot.animatronicTargetSize);
                        }
                    }
                }
            }

            // 4. Desk 2D Sprite
            if (_deskSpriteRenderer != null)
            {
                _deskSpriteRenderer.sprite = _deskSprite;
                _deskSpriteRenderer.color = _officeInteriorTint;
                if (_deskSprite != null)
                {
                    FitSpriteRendererToSize(_deskSpriteRenderer, _deskTargetSize);
                }
            }
            if (_placeholder3DDesk != null)
            {
                bool show3DDesk = (_deskSprite == null);
                var renderers = _placeholder3DDesk.GetComponentsInChildren<MeshRenderer>(true);
                foreach (var mr in renderers) mr.enabled = show3DDesk;
            }

            // 5. Floor 2D Sprite
            if (_floorSpriteRenderer != null)
            {
                _floorSpriteRenderer.sprite = _floorSprite;
                _floorSpriteRenderer.color = _officeInteriorTint;
                if (_floorSprite != null)
                {
                    FitSpriteRendererToSize(_floorSpriteRenderer, _floorTargetSize);
                }
            }
            if (_placeholder3DFloor != null)
            {
                var mr = _placeholder3DFloor.GetComponent<MeshRenderer>();
                if (mr != null) mr.enabled = (_floorSprite == null);
            }

            // 6. Ceiling 2D Sprite
            if (_ceilingSpriteRenderer != null)
            {
                _ceilingSpriteRenderer.sprite = _ceilingSprite;
                _ceilingSpriteRenderer.color = _officeInteriorTint;
                if (_ceilingSprite != null)
                {
                    FitSpriteRendererToSize(_ceilingSpriteRenderer, _ceilingTargetSize);
                }
            }
            if (_placeholder3DCeiling != null)
            {
                var mr = _placeholder3DCeiling.GetComponent<MeshRenderer>();
                if (mr != null) mr.enabled = (_ceilingSprite == null);
            }
        }

        /// <summary>
        /// Updates the brightness/tint of the 2D corridor backdrops and blindspot animatronic sprites
        /// based on whether each door's 'F' Hallway Light is ON or OFF.
        /// Works seamlessly in URP even with Unlit Sprites-Default materials!
        /// </summary>
        private void UpdateCorridorLighting()
        {
            if (_wallSlots == null) return;

            for (int i = 0; i < _wallSlots.Length; i++)
            {
                var slot = _wallSlots[i];
                if (slot == null) continue;

                bool isLightOn = false;
                if (DoorSystemManager.HasInstance)
                {
                    var door = DoorSystemManager.Instance.GetDoor(i);
                    if (door != null) isLightOn = door.IsLightOn;
                }

                Color targetColor = _corridorDarkTint;
                if (isLightOn)
                {
                    float flicker = 1f;
                    if (_enableFluorescentFlicker && Application.isPlaying)
                    {
                        // Subtle FNAF hallway light buzz/flicker
                        flicker = (Mathf.PerlinNoise(Time.time * 24f, i * 10f) > 0.12f) ? 1f : 0.35f;
                    }
                    targetColor = Color.Lerp(_corridorDarkTint, _corridorLitTint, flicker);
                }

                if (slot.corridorSpriteRenderer != null)
                {
                    if (isLightOn && slot.corridorLitSprite != null)
                    {
                        slot.corridorSpriteRenderer.sprite = slot.corridorLitSprite;
                    }
                    else if (slot.corridorBackdropSprite != null)
                    {
                        slot.corridorSpriteRenderer.sprite = slot.corridorBackdropSprite;
                    }
                    slot.corridorSpriteRenderer.color = targetColor;
                }

                if (slot.blindspotAnimatronicRenderer != null)
                {
                    slot.blindspotAnimatronicRenderer.color = targetColor;
                }
            }
        }

        /// <summary>
        /// Sets the 2D animatronic sprite visible in the doorway blindspot for a given door index (0-3).
        /// Pass null when the animatronic leaves the doorway.
        /// </summary>
        public void SetBlindspotAnimatronicSprite(int doorIndex, Sprite animatronicSprite)
        {
            if (_wallSlots == null || doorIndex < 0 || doorIndex >= _wallSlots.Length) return;
            var slot = _wallSlots[doorIndex];
            slot.blindspotAnimatronicSprite = animatronicSprite;
            if (slot.blindspotAnimatronicRenderer != null)
            {
                slot.blindspotAnimatronicRenderer.sprite = animatronicSprite;
                if (animatronicSprite != null)
                {
                    FitSpriteRendererToSize(slot.blindspotAnimatronicRenderer, slot.animatronicTargetSize);
                }
            }
        }

        /// <summary>
        /// Automatically adjusts the localScale of a SpriteRenderer so its sprite fits the target width & height in meters,
        /// regardless of the PNG's pixel dimensions or Pixels Per Unit (PPU) setting.
        /// </summary>
        public static void FitSpriteRendererToSize(SpriteRenderer sr, Vector2 targetSize)
        {
            if (sr == null || sr.sprite == null) return;
            Vector2 spriteBoundsSize = sr.sprite.bounds.size;
            if (spriteBoundsSize.x <= 0.001f || spriteBoundsSize.y <= 0.001f) return;

            sr.transform.localScale = new Vector3(
                targetSize.x / spriteBoundsSize.x,
                targetSize.y / spriteBoundsSize.y,
                1f
            );
        }
    }
}
