using System;
using UnityEngine;
using Tylerland.Animatronics;

namespace Tylerland.Office
{
    /// <summary>
    /// Holds the 2D SpriteRenderers and custom Sprite overrides for one of the 4 viewing directions
    /// (0 = Left/Erle, 1 = Center/Tyler, 2 = Right/Orville, 3 = Rear/Sandy).
    /// Everything in the project is rendered as 100% 2D Sprites (zero 3D cube primitives).
    /// </summary>
    [Serializable]
    public class DirectionalWallArtSlot
    {
        public string directionName = "Center";

        [Header("1. Office Wall 2D Sprite (Foreground Frame with Doorway Cutout)")]
        [Tooltip("Drop your artist's 2D Wall Sprite here (with a transparent cutout in the middle for the doorway). Leave empty to use the generated 2D Wall Sprite.")]
        public Sprite wallSprite;

        [Tooltip("The 2D SpriteRenderer placed on this wall")]
        public SpriteRenderer wallSpriteRenderer;

        [Tooltip("World width and height (in meters) to auto-fit the 2D wall sprite")]
        public Vector2 wallTargetSize = new Vector2(10.0f, 3.5f);

        [Tooltip("Legacy 3D wall objects (always hidden so the scene is 100% 2D sprites)")]
        public GameObject[] placeholder3DWallObjects;

        [Header("2. Corridor Backdrop 2D Sprite (Behind the Doorway)")]
        [Tooltip("Drop your artist's 2D Corridor Sprite here (dark/default). Leave empty to use the generated 2D Corridor Sprite.")]
        public Sprite corridorBackdropSprite;

        [Tooltip("Optional: Drop a separate 'Lit' 2D Corridor Sprite here if your artist drew a hand-lit version for when 'F' is pressed")]
        public Sprite corridorLitSprite;

        [Tooltip("The 2D SpriteRenderer placed behind the doorway opening")]
        public SpriteRenderer corridorSpriteRenderer;

        [Tooltip("World width and height (in meters) to auto-fit the 2D corridor backdrop")]
        public Vector2 corridorTargetSize = new Vector2(2.54f, 3.30f);

        [Tooltip("Legacy 3D corridor tunnel object (always hidden so the scene is 100% 2D sprites)")]
        public GameObject placeholder3DCorridorObject;

        [Header("3. Doorway Blindspot Animatronic 2D Sprite")]
        [Tooltip("Drop a 2D Animatronic Sprite here to preview or display an animatronic standing in the doorway blindspot")]
        public Sprite blindspotAnimatronicSprite;

        [Tooltip("The 2D SpriteRenderer positioned inside the corridor right behind the door shutter")]
        public SpriteRenderer blindspotAnimatronicRenderer;

        [Tooltip("World width and height (in meters) to auto-fit the animatronic sprite")]
        public Vector2 animatronicTargetSize = new Vector2(1.85f, 2.55f);
    }

    /// <summary>
    /// Section 4.1 / 7.2: 100% 2D Hand-Drawn Sprite Office Art Manager.
    /// Ensures EVERY visual surface in the Office (4 Walls with doorway cutouts, 4 Corridors,
    /// 4 Door Shutters, 4 Doorway Animatronics, Desk, Floor, Ceiling) is rendered strictly
    /// as a 2D SpriteRenderer with zero 3D cube primitives.
    /// </summary>
    [ExecuteAlways]
    public class OfficeArtManager : MonoBehaviour
    {
        [Header("Directional 2D Wall & Corridor Slots (0=Left, 1=Center, 2=Right, 3=Rear)")]
        [SerializeField] private DirectionalWallArtSlot[] _wallSlots = new DirectionalWallArtSlot[4];

        [Header("Desk, Floor & Ceiling 2D Sprites")]
        [Tooltip("Optional 2D Sprite for the security desk")]
        [SerializeField] private Sprite _deskSprite;
        [SerializeField] private SpriteRenderer _deskSpriteRenderer;
        [SerializeField] private Vector2 _deskTargetSize = new Vector2(2.30f, 0.44f);
        [SerializeField] private GameObject _placeholder3DDesk;

        [Header("Tabletop Props (office.table.1)")]
        [SerializeField] private Sprite _deskLampSprite;
        [SerializeField] private SpriteRenderer _deskLampRenderer;
        [SerializeField] private Sprite _deskClockSprite;
        [SerializeField] private SpriteRenderer _deskClockRenderer;
        [SerializeField] private Sprite _deskPlushieSprite;
        [SerializeField] private SpriteRenderer _deskPlushieRenderer;
        [SerializeField] private Sprite _deskMonitorSprite;
        [SerializeField] private SpriteRenderer _deskMonitorRenderer;

        [Tooltip("Optional 2D Sprite for the office floor")]
        [SerializeField] private Sprite _floorSprite;
        [SerializeField] private SpriteRenderer _floorSpriteRenderer;
        [SerializeField] private Vector2 _floorTargetSize = new Vector2(10.0f, 10.0f);
        [SerializeField] private GameObject _placeholder3DFloor;

        [Tooltip("Optional 2D Sprite for the office ceiling")]
        [SerializeField] private Sprite _ceilingSprite;
        [SerializeField] private SpriteRenderer _ceilingSpriteRenderer;
        [SerializeField] private Vector2 _ceilingTargetSize = new Vector2(10.0f, 10.0f);
        [SerializeField] private GameObject _placeholder3DCeiling;

        [Header("2D Lighting & Hallway Flashlight ('F' Key) Tint")]
        [Tooltip("Color tint of the corridor when the 'F' Hallway Light is OFF")]
        [SerializeField] private Color _corridorDarkTint = new Color(0.18f, 0.20f, 0.26f, 1.0f);

        [Tooltip("Color tint of the doorway animatronic silhouette when the 'F' Hallway Light is OFF (visible silhouette in doorway!)")]
        [SerializeField] private Color _animatronicSilhouetteTint = new Color(0.55f, 0.58f, 0.68f, 1.0f);

        [Tooltip("Color tint of the corridor & blindspot animatronic when the 'F' Hallway Light is ON")]
        [SerializeField] private Color _corridorLitTint = Color.white;

        [Tooltip("Subtle FNAF-style fluorescent flicker when the hallway light is held on")]
        [SerializeField] private bool _enableFluorescentFlicker = true;

        [Tooltip("Ambient brightness of the 4 inner office walls")]
        [SerializeField] private Color _officeInteriorTint = new Color(0.88f, 0.88f, 0.94f, 1.0f);

        public DirectionalWallArtSlot[] WallSlots => _wallSlots;

        private void OnEnable()
        {
            EnsureFourSlotsInitialized();
            ApplyAllArt();
        }

        private void Awake()
        {
            EnsureFourSlotsInitialized();
            if (Application.isPlaying)
            {
                EnsureRuntime2DSpriteRenderersExist();
            }
            ApplyAllArt();
        }

        private void Start()
        {
            if (Application.isPlaying)
            {
                EnsureRuntime2DSpriteRenderersExist();
            }
            ApplyAllArt();
        }

        private void OnValidate()
        {
            EnsureFourSlotsInitialized();
            ApplyAllArt();
        }

        private void Update()
        {
            if (Application.isPlaying)
            {
                SyncDoorwayAnimatronicSprites();
            }
            UpdateCorridorLighting();
        }

        private void EnsureFourSlotsInitialized()
        {
            if (_wallSlots == null || _wallSlots.Length != 4)
            {
                var old = _wallSlots;
                _wallSlots = new DirectionalWallArtSlot[4];
                if (old != null)
                {
                    for (int i = 0; i < Mathf.Min(4, old.Length); i++)
                    {
                        _wallSlots[i] = old[i];
                    }
                }
            }

            string[] names =
            {
                "0. Left Wall & Corridor (Erle the Eagle)",
                "1. Center Wall & Corridor (Tyler the Lion)",
                "2. Right Wall & Corridor (Orville the Orca)",
                "3. Rear Wall & Corridor (Sandy the Snake)"
            };

            for (int i = 0; i < 4; i++)
            {
                if (_wallSlots[i] == null) _wallSlots[i] = new DirectionalWallArtSlot();
                _wallSlots[i].directionName = names[i];
            }
        }

        /// <summary>
        /// Ensures that even if the user runs an older scene without re-running the Setup Menu,
        /// all 4 directional 2D SpriteRenderers (Wall, Corridor, and Doorway Blindspot Animatronic)
        /// exist and are properly positioned so animatronics are ALWAYS visible at the doors.
        /// </summary>
        private void EnsureRuntime2DSpriteRenderersExist()
        {
            Transform root = transform.parent != null ? transform.parent : transform;
            Transform layerRoot = root.Find("Office_2DSprite_Layers");
            if (layerRoot == null)
            {
                var go = new GameObject("Office_2DSprite_Layers");
                go.transform.SetParent(root, false);
                layerRoot = go.transform;
            }

            Quaternion[] viewRotations =
            {
                Quaternion.Euler(0, -90f, 0),
                Quaternion.Euler(0, 0f, 0),
                Quaternion.Euler(0, 90f, 0),
                Quaternion.Euler(0, 180f, 0)
            };

            Vector3[] wall2DPositions =
            {
                new Vector3(-4.82f, 1.75f, 0f),
                new Vector3(0f, 1.75f, 4.82f),
                new Vector3(4.82f, 1.75f, 0f),
                new Vector3(0f, 1.75f, -4.82f)
            };

            Vector3[] animatronic2DPositions =
            {
                new Vector3(-5.15f, 1.42f, 0f),
                new Vector3(0f, 1.42f, 5.15f),
                new Vector3(5.15f, 1.42f, 0f),
                new Vector3(0f, 1.42f, -5.15f)
            };

            Vector3[] corridor2DPositions =
            {
                new Vector3(-5.65f, 1.29f, 0f),
                new Vector3(0f, 1.29f, 5.65f),
                new Vector3(5.65f, 1.29f, 0f),
                new Vector3(0f, 1.29f, -5.65f)
            };

            string[] dirLabels = { "Left_Erle", "Center_Tyler", "Right_Orville", "Rear_Sandy" };

            for (int i = 0; i < 4; i++)
            {
                var slot = _wallSlots[i];
                if (slot.wallSpriteRenderer == null)
                {
                    var wallObj = new GameObject($"Wall_2DSprite_{dirLabels[i]}");
                    wallObj.transform.SetParent(layerRoot, false);
                    wallObj.transform.localPosition = wall2DPositions[i];
                    wallObj.transform.localRotation = viewRotations[i];
                    slot.wallSpriteRenderer = wallObj.AddComponent<SpriteRenderer>();
                }
                slot.wallSpriteRenderer.sortingOrder = 10;

                if (slot.blindspotAnimatronicRenderer == null)
                {
                    var animObj = new GameObject($"Blindspot_Animatronic_2DSprite_{dirLabels[i]}");
                    animObj.transform.SetParent(layerRoot, false);
                    animObj.transform.localPosition = animatronic2DPositions[i];
                    animObj.transform.localRotation = viewRotations[i];
                    slot.blindspotAnimatronicRenderer = animObj.AddComponent<SpriteRenderer>();
                }
                slot.blindspotAnimatronicRenderer.sortingOrder = 4;

                if (slot.corridorSpriteRenderer == null)
                {
                    var corrObj = new GameObject($"Corridor_Backdrop_2DSprite_{dirLabels[i]}");
                    corrObj.transform.SetParent(layerRoot, false);
                    corrObj.transform.localPosition = corridor2DPositions[i];
                    corrObj.transform.localRotation = viewRotations[i];
                    slot.corridorSpriteRenderer = corrObj.AddComponent<SpriteRenderer>();
                }
                slot.corridorSpriteRenderer.sortingOrder = 0;
            }

            // Ensure the 4 physical in-world door and light button panels exist to the right of each doorway
            DoorWallButtonPanel.EnsureAllFourButtonPanelsExist(layerRoot);

            // Ensure the tabletop prop child renderers exist under SecurityDesk_2DSprite
            EnsureDeskTabletopRenderersExist();

            // Hide any stray legacy 3D MeshRenderers in the office environment
            if (root != null)
            {
                var allMeshes = root.GetComponentsInChildren<MeshRenderer>(true);
                foreach (var mr in allMeshes)
                {
                    mr.enabled = false;
                }
            }
        }

        /// <summary>
        /// Continuously syncs the 2D Animatronic Sprite standing in each doorway (0..3)
        /// with AnimatronicManager so that whenever an animatronic leaves its camera room,
        /// it is immediately visible standing in that doorway!
        /// </summary>
        private void SyncDoorwayAnimatronicSprites()
        {
            if (!AnimatronicManager.HasInstance || _wallSlots == null) return;

            for (int i = 0; i < 4 && i < _wallSlots.Length; i++)
            {
                var slot = _wallSlots[i];
                if (slot == null || slot.blindspotAnimatronicRenderer == null) continue;

                var attacker = AnimatronicManager.Instance.GetAnimatronicAtDoorBlindspot(i);
                if (attacker != null && attacker.Config != null)
                {
                    Sprite targetSprite = attacker.Config.GetBlindspotSpriteForDamage(attacker.DamageLevel);
                    if (targetSprite == null)
                    {
                        targetSprite = Procedural2DSpriteFactory.GetOrCreateAnimatronic2DSprite(
                            attacker.AnimatronicId,
                            attacker.Config.themeColor,
                            attacker.DamageLevel,
                            1.0f
                        );
                    }

                    if (slot.blindspotAnimatronicRenderer.sprite != targetSprite)
                    {
                        slot.blindspotAnimatronicSprite = targetSprite;
                        slot.blindspotAnimatronicRenderer.sprite = targetSprite;
                        if (targetSprite != null)
                        {
                            FitSpriteRendererToSize(slot.blindspotAnimatronicRenderer, slot.animatronicTargetSize);
                        }
                    }
                    slot.blindspotAnimatronicRenderer.enabled = true;
                }
                else
                {
                    slot.blindspotAnimatronicSprite = null;
                    slot.blindspotAnimatronicRenderer.sprite = null;
                }
            }
        }

        /// <summary>
        /// Applies 2D Sprites to all 4 walls, 4 corridors, 4 doorway blindspots, desk, floor, and ceiling.
        /// Uses artist's custom 2D sprites if assigned, or generated 2D sprites otherwise.
        /// </summary>
        [ContextMenu("Apply All 2D Sprites Now")]
        public void ApplyAllArt()
        {
            LoadDuvarlarSprites();

            Color[] wallTints =
            {
                new Color(0.36f, 0.33f, 0.24f, 1f), // 0: Left (Erle - Sheriff Gold-Gray)
                new Color(0.38f, 0.30f, 0.28f, 1f), // 1: Center (Tyler - Stage Crimson-Gray)
                new Color(0.24f, 0.34f, 0.42f, 1f), // 2: Right (Orville - Medical Blue-Gray)
                new Color(0.26f, 0.36f, 0.28f, 1f)  // 3: Rear (Sandy - Maintenance Green-Gray)
            };

            if (_wallSlots != null)
            {
                for (int i = 0; i < _wallSlots.Length; i++)
                {
                    var slot = _wallSlots[i];
                    if (slot == null) continue;

                    Color dirTint = wallTints[Mathf.Clamp(i, 0, 3)];

                    // 1. Office Wall 2D Sprite (with transparent doorway cutout)
                    if (slot.wallSpriteRenderer != null)
                    {
                        Sprite activeWall = slot.wallSprite != null
                            ? slot.wallSprite
                            : Procedural2DSpriteFactory.GetOrCreateWallSprite(i, dirTint);

                        slot.wallSpriteRenderer.sprite = activeWall;
                        slot.wallSpriteRenderer.color = _officeInteriorTint;
                        if (activeWall != null)
                        {
                            FitSpriteRendererToSize(slot.wallSpriteRenderer, slot.wallTargetSize);
                        }
                    }

                    // Always hide 3D wall cubes — everything is 100% 2D Sprites
                    if (slot.placeholder3DWallObjects != null)
                    {
                        foreach (var obj in slot.placeholder3DWallObjects)
                        {
                            if (obj != null)
                            {
                                var mr = obj.GetComponent<MeshRenderer>();
                                if (mr != null) mr.enabled = false;
                            }
                        }
                    }

                    // 2. Corridor Backdrop 2D Sprite
                    if (slot.corridorSpriteRenderer != null)
                    {
                        Vector3 corrPos = slot.corridorSpriteRenderer.transform.localPosition;
                        if (Mathf.Abs(corrPos.y - 1.29f) > 0.05f)
                        {
                            corrPos.y = 1.29f;
                            slot.corridorSpriteRenderer.transform.localPosition = corrPos;
                        }
                        if (slot.corridorTargetSize.x > 2.8f || slot.corridorTargetSize.y > 3.6f || slot.corridorTargetSize.y < 3.2f)
                        {
                            slot.corridorTargetSize = new Vector2(2.54f, 3.30f);
                        }

                        Sprite activeCorridor = slot.corridorBackdropSprite != null
                            ? slot.corridorBackdropSprite
                            : Procedural2DSpriteFactory.GetOrCreateCorridorSprite(i, dirTint);

                        slot.corridorSpriteRenderer.sprite = activeCorridor;
                        if (activeCorridor != null)
                        {
                            FitSpriteRendererToSize(slot.corridorSpriteRenderer, slot.corridorTargetSize);
                        }
                    }

                    // Always hide 3D corridor tunnel meshes — everything is 100% 2D Sprites
                    if (slot.placeholder3DCorridorObject != null)
                    {
                        var renderers = slot.placeholder3DCorridorObject.GetComponentsInChildren<MeshRenderer>(true);
                        foreach (var mr in renderers)
                        {
                            mr.enabled = false;
                        }
                    }

                    // 3. Blindspot Animatronic 2D Sprite
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

            // 4. Desk 2D Sprite + Tabletop Props (Lamp, Clock, Plushie, CRT Monitor)
            if (_deskSpriteRenderer != null)
            {
                if (Mathf.Abs(_deskSpriteRenderer.transform.localPosition.x) < 0.5f)
                {
                    _deskSpriteRenderer.transform.localPosition = new Vector3(1.35f, 0.50f, 2.30f);
                }

                Sprite activeDesk = _deskSprite != null
                    ? _deskSprite
                    : Procedural2DSpriteFactory.GetOrCreateDeskSprite();

                _deskSpriteRenderer.sprite = activeDesk;
                _deskSpriteRenderer.color = _officeInteriorTint;
                _deskSpriteRenderer.sortingOrder = 20;

                if (_deskSprite != null)
                {
                    Vector2 deskBounds = _deskSprite.bounds.size;
                    float targetWidth = _deskTargetSize.x > 0.5f ? _deskTargetSize.x : 2.30f;
                    float uniformScale = deskBounds.x > 0.001f ? (targetWidth / deskBounds.x) : 0.3898f;
                    _deskSpriteRenderer.transform.localScale = new Vector3(uniformScale, uniformScale, 1f);
                }
                else if (activeDesk != null)
                {
                    FitSpriteRendererToSize(_deskSpriteRenderer, _deskTargetSize);
                }

                ApplyDeskTabletopProps();
            }
            if (_placeholder3DDesk != null)
            {
                var renderers = _placeholder3DDesk.GetComponentsInChildren<MeshRenderer>(true);
                foreach (var mr in renderers) mr.enabled = false;
            }

            // 5. Floor 2D Sprite
            if (_floorSpriteRenderer != null)
            {
                Sprite activeFloor = _floorSprite != null
                    ? _floorSprite
                    : Procedural2DSpriteFactory.GetOrCreateCheckerFloorSprite(new Color(0.18f, 0.18f, 0.22f), new Color(0.11f, 0.11f, 0.14f));

                _floorSpriteRenderer.sprite = activeFloor;
                _floorSpriteRenderer.color = _officeInteriorTint;
                if (activeFloor != null)
                {
                    FitSpriteRendererToSize(_floorSpriteRenderer, _floorTargetSize);
                }
            }
            if (_placeholder3DFloor != null)
            {
                var mr = _placeholder3DFloor.GetComponent<MeshRenderer>();
                if (mr != null) mr.enabled = false;
            }

            // 6. Ceiling 2D Sprite
            if (_ceilingSpriteRenderer != null)
            {
                Sprite activeCeiling = _ceilingSprite != null
                    ? _ceilingSprite
                    : Procedural2DSpriteFactory.GetOrCreateCheckerFloorSprite(new Color(0.14f, 0.14f, 0.17f), new Color(0.10f, 0.10f, 0.12f));

                _ceilingSpriteRenderer.sprite = activeCeiling;
                _ceilingSpriteRenderer.color = _officeInteriorTint;
                _ceilingSpriteRenderer.sortingOrder = 11;
                if (activeCeiling != null)
                {
                    FitSpriteRendererToSize(_ceilingSpriteRenderer, _ceilingTargetSize);
                }
            }
            if (_placeholder3DCeiling != null)
            {
                var mr = _placeholder3DCeiling.GetComponent<MeshRenderer>();
                if (mr != null) mr.enabled = false;
            }
        }

        private const string DuvarlarFolder = "Assets/Sprites/Duvarlar/";
        private const string RestOfOfficeFolder = "Assets/Sprites/the rest of the office/";
        private const string NewCorridorFolder = "Assets/Sprites/office.corridor.dark/";
        private const string DeskSheetPath = "Assets/Sprites/office.table.1.png";

        /// <summary>
        /// Auto-assigns the artist sprites from Assets/Sprites (walls, floor, ceiling,
        /// corridors, and tabletop props) whenever serialized slots are empty (editor-side loader).
        /// </summary>
        private void LoadDuvarlarSprites()
        {
#if UNITY_EDITOR
            string[] wallFiles = { "spr_wall_left", "spr_wall_center", "spr_wall_right", "spr_wall_rear" };
            Sprite darkCorridor = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>(NewCorridorFolder + "office.corridor.dark.png")
                ?? UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>(RestOfOfficeFolder + "spr_corridor_dark.png");
            Sprite litCorridor = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>(NewCorridorFolder + "office.corridor.lit.png")
                ?? UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>(RestOfOfficeFolder + "spr_corridor_lit.png");

            if (_wallSlots != null)
            {
                for (int i = 0; i < _wallSlots.Length && i < wallFiles.Length; i++)
                {
                    if (_wallSlots[i] == null) continue;
                    if (_wallSlots[i].wallSprite == null)
                        _wallSlots[i].wallSprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>(DuvarlarFolder + wallFiles[i] + ".png");
                    if (darkCorridor != null && (_wallSlots[i].corridorBackdropSprite == null || _wallSlots[i].corridorBackdropSprite.name == "spr_corridor_dark"))
                        _wallSlots[i].corridorBackdropSprite = darkCorridor;
                    if (litCorridor != null && (_wallSlots[i].corridorLitSprite == null || _wallSlots[i].corridorLitSprite.name == "spr_corridor_lit"))
                        _wallSlots[i].corridorLitSprite = litCorridor;
                }
            }
            if (_floorSprite == null)
                _floorSprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>(DuvarlarFolder + "spr_floor.png");
            if (_ceilingSprite == null)
                _ceilingSprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>(DuvarlarFolder + "spr_ceiling.png");

            if (_deskLampSprite == null) _deskLampSprite = LoadSubSprite(DeskSheetPath, "office.table.1_0");
            if (_deskClockSprite == null) _deskClockSprite = LoadSubSprite(DeskSheetPath, "office.table.1_1");
            if (_deskMonitorSprite == null) _deskMonitorSprite = LoadSubSprite(DeskSheetPath, "office.table.1_2");
            if (_deskPlushieSprite == null) _deskPlushieSprite = LoadSubSprite(DeskSheetPath, "office.table.1_3");
            if (_deskSprite == null) _deskSprite = LoadSubSprite(DeskSheetPath, "office.table.1_4");
#endif
        }

#if UNITY_EDITOR
        private static Sprite LoadSubSprite(string assetPath, string spriteName)
        {
            UnityEngine.Object[] assets = UnityEditor.AssetDatabase.LoadAllAssetsAtPath(assetPath);
            if (assets == null) return null;
            for (int i = 0; i < assets.Length; i++)
            {
                if (assets[i] is Sprite sp && sp.name == spriteName)
                {
                    return sp;
                }
            }
            return null;
        }
#endif

        public void EnsureDeskTabletopRenderersExist()
        {
            if (_deskSpriteRenderer == null) return;
            Transform deskTf = _deskSpriteRenderer.transform;

            if (_deskLampRenderer == null) _deskLampRenderer = FindOrCreatePropRenderer(deskTf, "DeskLamp_2DSprite", 21);
            if (_deskClockRenderer == null) _deskClockRenderer = FindOrCreatePropRenderer(deskTf, "DigitalClock_2DSprite", 22);
            if (_deskPlushieRenderer == null) _deskPlushieRenderer = FindOrCreatePropRenderer(deskTf, "TylerPlushie_2DSprite", 22);
            if (_deskMonitorRenderer == null) _deskMonitorRenderer = FindOrCreatePropRenderer(deskTf, "ComputerMonitor_2DSprite", 21);
        }

        private static SpriteRenderer FindOrCreatePropRenderer(Transform parent, string childName, int sortingOrder)
        {
            Transform existing = parent.Find(childName);
            if (existing == null)
            {
                var go = new GameObject(childName);
                go.transform.SetParent(parent, false);
                existing = go.transform;
            }
            var sr = existing.GetComponent<SpriteRenderer>();
            if (sr == null) sr = existing.gameObject.AddComponent<SpriteRenderer>();
            sr.sortingOrder = sortingOrder;
            return sr;
        }

        private void ApplyDeskTabletopProps()
        {
            if (_deskSpriteRenderer == null) return;
            Transform deskTf = _deskSpriteRenderer.transform;

            if (_deskLampRenderer == null) _deskLampRenderer = FindExistingPropRenderer(deskTf, "DeskLamp_2DSprite");
            if (_deskClockRenderer == null) _deskClockRenderer = FindExistingPropRenderer(deskTf, "DigitalClock_2DSprite");
            if (_deskPlushieRenderer == null) _deskPlushieRenderer = FindExistingPropRenderer(deskTf, "TylerPlushie_2DSprite");
            if (_deskMonitorRenderer == null) _deskMonitorRenderer = FindExistingPropRenderer(deskTf, "ComputerMonitor_2DSprite");

            float deskPpu = (_deskSprite != null && _deskSprite.pixelsPerUnit > 0f)
                ? _deskSprite.pixelsPerUnit
                : 100f;

            // Exact pixel offsets from mockup relative to table center, converted via actual sprite PPU
            ApplyProp(_deskLampRenderer, _deskLampSprite, deskPpu, new Vector2(-178.5f, 129.0f), -0.01f, 21);
            ApplyProp(_deskClockRenderer, _deskClockSprite, deskPpu, new Vector2(-145.5f, 52.5f), -0.02f, 22);
            ApplyProp(_deskPlushieRenderer, _deskPlushieSprite, deskPpu, new Vector2(15.0f, 71.0f), -0.02f, 22);
            ApplyProp(_deskMonitorRenderer, _deskMonitorSprite, deskPpu, new Vector2(140.5f, 127.0f), -0.01f, 21);
        }

        private static SpriteRenderer FindExistingPropRenderer(Transform parent, string childName)
        {
            Transform existing = parent.Find(childName);
            return existing != null ? existing.GetComponent<SpriteRenderer>() : null;
        }

        private void ApplyProp(SpriteRenderer sr, Sprite sprite, float deskPpu, Vector2 pixelOffset, float localZ, int sortingOrder)
        {
            if (sr == null) return;
            sr.transform.localPosition = new Vector3(pixelOffset.x / deskPpu, pixelOffset.y / deskPpu, localZ);
            sr.transform.localRotation = Quaternion.identity;

            float propScale = 1f;
            if (sprite != null && sprite.pixelsPerUnit > 0f)
            {
                propScale = deskPpu / sprite.pixelsPerUnit;
            }
            sr.transform.localScale = new Vector3(propScale, propScale, 1f);
            sr.sortingOrder = sortingOrder;
            if (sprite != null)
            {
                sr.sprite = sprite;
                sr.color = _officeInteriorTint;
            }
        }

        /// <summary>
        /// Updates the brightness/tint of the 2D corridor backdrops and blindspot animatronic sprites.
        /// Even when the 'F' Hallway Light is OFF, an animatronic at the doorway is visible as a menacing
        /// silhouette so they are never "missing" when you look at the door! Pressing 'F' illuminates them brightly.
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

                bool hasSeparateCorridorArt = slot.corridorBackdropSprite != null && slot.corridorLitSprite != null;
                Color darkCorridorColor = hasSeparateCorridorArt ? new Color(0.85f, 0.86f, 0.92f, 1.0f) : _corridorDarkTint;
                Color corridorColor = darkCorridorColor;
                Color animatronicColor = _animatronicSilhouetteTint;

                if (isLightOn)
                {
                    float flicker = 1f;
                    if (_enableFluorescentFlicker && Application.isPlaying)
                    {
                        flicker = (Mathf.PerlinNoise(Time.time * 24f, i * 10f) > 0.12f) ? 1f : 0.45f;
                    }
                    corridorColor = Color.Lerp(hasSeparateCorridorArt ? new Color(0.55f, 0.55f, 0.60f, 1f) : _corridorDarkTint, _corridorLitTint, flicker);
                    animatronicColor = Color.Lerp(_animatronicSilhouetteTint, _corridorLitTint, flicker);
                }

                if (slot.corridorSpriteRenderer != null)
                {
                    Sprite targetCorridor = slot.corridorSpriteRenderer.sprite;
                    if (isLightOn && slot.corridorLitSprite != null)
                    {
                        targetCorridor = slot.corridorLitSprite;
                    }
                    else if (slot.corridorBackdropSprite != null)
                    {
                        targetCorridor = slot.corridorBackdropSprite;
                    }

                    if (targetCorridor != null && slot.corridorSpriteRenderer.sprite != targetCorridor)
                    {
                        slot.corridorSpriteRenderer.sprite = targetCorridor;
                        FitSpriteRendererToSize(slot.corridorSpriteRenderer, slot.corridorTargetSize);
                    }
                    slot.corridorSpriteRenderer.color = corridorColor;
                }

                if (slot.blindspotAnimatronicRenderer != null && slot.blindspotAnimatronicRenderer.sprite != null)
                {
                    // Always visible in the doorway when present (silhouette in the dark, full color when 'F' light is on!)
                    slot.blindspotAnimatronicRenderer.color = animatronicColor;
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
