using UnityEngine;

namespace Tylerland.Office
{
    /// <summary>
    /// Section 4.3: Physical Industrial Door Control Panel.
    /// Mounted to the wall directly to the right of each of the 4 security doorways:
    /// - Door 0: Left Wall (Erle the Eagle)
    /// - Door 1: Center Wall (Tyler the Lion)
    /// - Door 2: Right Wall (Orville the Orca)
    /// - Door 3: Rear Wall (Sandy the Snake)
    /// Contains:
    /// 1. Industrial Backplate (spr_door_button_backplate)
    /// 2. Top Button: WHITE for Hallway Blindspot LIGHT
    /// 3. Bottom Button: RED for Security DOOR Shutter
    /// </summary>
    public class DoorWallButtonPanel : MonoBehaviour
    {
        [Header("Door Target")]
        [Tooltip("Door index: 0 = Left (Erle), 1 = Center (Tyler), 2 = Right (Orville), 3 = Rear (Sandy)")]
        [Range(0, 3)]
        [SerializeField] private int _doorIndex = 1;

        [Header("Child References")]
        [SerializeField] private SpriteRenderer _backplateRenderer;
        [SerializeField] private DoorPhysicalButton _lightButton;
        [SerializeField] private DoorPhysicalButton _doorButton;

        public int DoorIndex => _doorIndex;
        public DoorPhysicalButton LightButton => _lightButton;
        public DoorPhysicalButton DoorButton => _doorButton;

        public static DoorWallButtonPanel Create(Transform parent, int doorIndex, Vector3 worldPos, Quaternion worldRot)
        {
            string[] names = { "Left_Erle", "Center_Tyler", "Right_Orville", "Rear_Sandy" };
            var panelObj = new GameObject($"DoorWallButtonPanel_{names[doorIndex]}");
            if (parent != null) panelObj.transform.SetParent(parent, false);
            panelObj.transform.position = worldPos;
            panelObj.transform.rotation = worldRot;

            var panel = panelObj.AddComponent<DoorWallButtonPanel>();
            panel._doorIndex = doorIndex;

            // 1. Backplate
            var plateObj = new GameObject("Backplate");
            plateObj.transform.SetParent(panelObj.transform, false);
            plateObj.transform.localPosition = Vector3.zero;
            plateObj.transform.localRotation = Quaternion.identity;
            var plateSR = plateObj.AddComponent<SpriteRenderer>();
            plateSR.sprite = Procedural2DSpriteFactory.GetOrCreateButtonPlateSprite();
            plateSR.sortingOrder = 12; // Wall is 10, backplate is 12
            panel._backplateRenderer = plateSR;

            // 2. White Light Button (Top)
            var lightObj = new GameObject("Button_Light_White");
            lightObj.transform.SetParent(panelObj.transform, false);
            lightObj.transform.localPosition = new Vector3(0f, 0.17f, -0.02f); // in front of backplate
            lightObj.transform.localRotation = Quaternion.identity;
            var lightSR = lightObj.AddComponent<SpriteRenderer>();
            lightSR.sprite = Procedural2DSpriteFactory.GetOrCreateLightButtonSprite(false);
            lightSR.sortingOrder = 14;

            var lightBtn = lightObj.AddComponent<DoorPhysicalButton>();
            lightBtn.Setup(doorIndex, true, lightSR);
            panel._lightButton = lightBtn;

            // 3. Red Door Button (Bottom)
            var doorObj = new GameObject("Button_Door_Red");
            doorObj.transform.SetParent(panelObj.transform, false);
            doorObj.transform.localPosition = new Vector3(0f, -0.17f, -0.02f); // in front of backplate
            doorObj.transform.localRotation = Quaternion.identity;
            var doorSR = doorObj.AddComponent<SpriteRenderer>();
            doorSR.sprite = Procedural2DSpriteFactory.GetOrCreateDoorButtonSprite(false);
            doorSR.sortingOrder = 14;

            var doorBtn = doorObj.AddComponent<DoorPhysicalButton>();
            doorBtn.Setup(doorIndex, false, doorSR);
            panel._doorButton = doorBtn;

            return panel;
        }

        /// <summary>
        /// Ensures all 4 physical wall button panels exist beside the 4 doors.
        /// </summary>
        public static void EnsureAllFourButtonPanelsExist(Transform parent = null)
        {
            var existingPanels = Object.FindObjectsByType<DoorWallButtonPanel>(FindObjectsSortMode.None);
            bool[] hasPanel = new bool[4];
            for (int i = 0; i < existingPanels.Length; i++)
            {
                if (existingPanels[i].DoorIndex >= 0 && existingPanels[i].DoorIndex < 4)
                {
                    hasPanel[existingPanels[i].DoorIndex] = true;
                }
            }

            // Wall 2D positions & rotations (matching Build2DOfficeSpriteLayers)
            Vector3[] wall2DPositions =
            {
                new Vector3(-4.82f, 1.75f, 0f),  // 0: Left (Erle)
                new Vector3(0f, 1.75f, 4.82f),   // 1: Center (Tyler)
                new Vector3(4.82f, 1.75f, 0f),   // 2: Right (Orville)
                new Vector3(0f, 1.75f, -4.82f)   // 3: Rear (Sandy)
            };

            Quaternion[] viewRotations =
            {
                Quaternion.Euler(0, -90f, 0), // 0: Left
                Quaternion.Euler(0, 0f, 0),   // 1: Center
                Quaternion.Euler(0, 90f, 0),  // 2: Right
                Quaternion.Euler(0, 180f, 0)  // 3: Rear
            };

            // Mounted to the right of the door (+1.35m in local X), eye level (-0.25m from wall center), in front of wall (-0.08m local Z)
            Vector3 panelOffset = new Vector3(1.35f, -0.25f, -0.08f);

            for (int i = 0; i < 4; i++)
            {
                if (!hasPanel[i])
                {
                    Vector3 worldPos = wall2DPositions[i] + viewRotations[i] * panelOffset;
                    Quaternion worldRot = viewRotations[i];
                    Create(parent, i, worldPos, worldRot);
                    Debug.Log($"[Office] Generated physical door button panel for door {i} to the right of the doorway.");
                }
            }
        }
    }
}
