using UnityEngine;
using UnityEditor;
using System.IO;
using Tylerland.Core;
using Tylerland.Config;
using Tylerland.Animatronics;

namespace Tylerland.Editor
{
    /// <summary>
    /// Editor automation tool that sets up the project configs and 100% 2D Sprite scene hierarchy with 1 click.
    /// Accessible via top menu bar: 'Tylerland'
    /// </summary>
    public static class TylerlandSetupMenu
    {
        private const string CONFIG_FOLDER = "Assets/ScriptableObjects";

        [MenuItem("Tylerland/1. Generate Default Config Assets (Nights 1-6, Economy & Animatronics)", false, 1)]
        public static void GenerateDefaultConfigs()
        {
            if (!AssetDatabase.IsValidFolder(CONFIG_FOLDER))
            {
                AssetDatabase.CreateFolder("Assets", "ScriptableObjects");
            }
            if (!AssetDatabase.IsValidFolder(CONFIG_FOLDER + "/Nights"))
            {
                AssetDatabase.CreateFolder(CONFIG_FOLDER, "Nights");
            }
            if (!AssetDatabase.IsValidFolder(CONFIG_FOLDER + "/Animatronics"))
            {
                AssetDatabase.CreateFolder(CONFIG_FOLDER, "Animatronics");
            }

            // 1. Economy Config
            string ecoPath = $"{CONFIG_FOLDER}/EconomyConfig.asset";
            var ecoConfig = AssetDatabase.LoadAssetAtPath<EconomyConfig>(ecoPath);
            if (ecoConfig == null)
            {
                ecoConfig = ScriptableObject.CreateInstance<EconomyConfig>();
                AssetDatabase.CreateAsset(ecoConfig, ecoPath);
                Debug.Log($"[Tylerland Editor] Created EconomyConfig at {ecoPath}");
            }

            // 2. Night Configs (Matching GDD Section 4 Activation Schedule:
            // Night 1 = Sandy active + Tyler passive observer;
            // Night 2 = Orville joins;
            // Night 3 = Erle joins;
            // Night 4+ = Tyler attacks!)
            int[] durations =  { 60, 75, 90, 90, 90, 90 };
            int[] sandyAI =    {  3,  4,  5,  7,  8, 10 }; // Active from Night 1 (Rear Door)
            int[] orvilleAI =  {  0,  3,  5,  7,  9, 10 }; // Active from Night 2 (Right Door)
            int[] erleAI =     {  0,  0,  3,  6,  8, 10 }; // Active from Night 3 (Left Door)
            int[] tylerAI =    {  0,  0,  0,  5,  8, 10 }; // Passive Nights 1-3, Attacks from Night 4 (Center Door)
            bool[] boss =      { false, false, false, false, false, true };

            for (int i = 0; i < 6; i++)
            {
                int nightNum = i + 1;
                string nightPath = $"{CONFIG_FOLDER}/Nights/Night_{nightNum}.asset";
                var nightConfig = AssetDatabase.LoadAssetAtPath<NightConfig>(nightPath);
                bool isNewNight = (nightConfig == null);
                if (isNewNight)
                {
                    nightConfig = ScriptableObject.CreateInstance<NightConfig>();
                }

                nightConfig.nightNumber = nightNum;
                nightConfig.realSecondsPerGameHour = durations[i];
                nightConfig.tylerAILevel = tylerAI[i];
                nightConfig.sharkAILevel = orvilleAI[i]; // Orville the Orca (Right Door)
                nightConfig.eagleAILevel = erleAI[i];    // Erle the Eagle (Left Door)
                nightConfig.snakeAILevel = sandyAI[i];   // Sandy the Snake (Rear Door)
                nightConfig.bossActive = boss[i];
                nightConfig.muteAllowed = nightNum >= 2;

                if (isNewNight)
                {
                    AssetDatabase.CreateAsset(nightConfig, nightPath);
                    Debug.Log($"[Tylerland Editor] Created NightConfig at {nightPath}");
                }
                else
                {
                    EditorUtility.SetDirty(nightConfig);
                }
            }

            // 3. Animatronic Configs (GDD Section 4: 0=Erle Left, 1=Tyler Center, 2=Orville Right, 3=Sandy Rear)
            CreateOrUpdateGddAnimatronicAssets();

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            EditorUtility.DisplayDialog("Tylerland Setup", "Default GDD Section 4 Config assets (Nights 1-6, Economy & Tyler, Orville, Sandy, Erle + Boss) generated in Assets/ScriptableObjects/!", "OK");
        }

        private static void CreateOrUpdateGddAnimatronicAssets(
            out AnimatronicConfig tylerCfg,
            out AnimatronicConfig orvilleCfg,
            out AnimatronicConfig erleCfg,
            out AnimatronicConfig sandyCfg,
            out AnimatronicConfig bossCfg)
        {
            // Tyler the Lion (Center Door 1 / CAM 02): 4 stages, 10s window, Shotgun, passive Nights 1-3, active Night 4
            tylerCfg = CreateOrLoadAnimatronicAsset(
                fileBase: "Tyler",
                id: "tyler",
                displayName: "Tyler the Lion",
                formerRole: "Lead Singer / Central Mascot",
                color: new Color(0.85f, 0.35f, 0.25f),
                doorIdx: 1,
                activeFromNight: 4,
                passiveBeforeActive: true,
                normalStages: 4,
                nightmareStages: 4,
                retreatStage: 0, // Resets to Stage 1 (0-indexed 0)
                tickInterval: 4.9f,
                lateRamp: 2,
                normalWindow: 10.0f,
                nightmareWindow: 10.0f,
                energyDrainNormal: 0f,
                energyDrainNightmare: 12f,
                camBehavior: CameraStallBehavior.TylerDestroysCameraInNightmare,
                weaponId: "Shotgun"
            );

            // Orville the Orca (Right Door 2 / CAM 03): 6 stages, 20s window, Harpoon, active Night 2, retreats to Stage 2 (index 1)
            orvilleCfg = CreateOrLoadAnimatronicAsset(
                fileBase: "Orville",
                id: "orville",
                displayName: "Orville the Orca",
                formerRole: "Medical Attendant / The Corrupted Healer",
                color: new Color(0.20f, 0.65f, 0.90f),
                doorIdx: 2,
                activeFromNight: 2,
                passiveBeforeActive: false,
                normalStages: 6,
                nightmareStages: 6,
                retreatStage: 1, // Retreats to Stage 2 (0-indexed 1) per GDD §4.3.2
                tickInterval: 4.4f,
                lateRamp: 2,
                normalWindow: 20.0f,
                nightmareWindow: 20.0f,
                energyDrainNormal: 12f,
                energyDrainNightmare: 18f,
                camBehavior: CameraStallBehavior.OrvilleProbabilisticAndReversal,
                weaponId: "Harpoon"
            );

            // Erle the Eagle (Left Door 0 / CAM 01): 5 stages, 15s Normal / 12s Nightmare window, Electric Gun, active Night 3
            erleCfg = CreateOrLoadAnimatronicAsset(
                fileBase: "Erle",
                id: "erle",
                displayName: "Erle the Eagle",
                formerRole: "Sheriff / Security Mascot",
                color: new Color(0.95f, 0.75f, 0.20f),
                doorIdx: 0,
                activeFromNight: 3,
                passiveBeforeActive: false,
                normalStages: 5,
                nightmareStages: 5,
                retreatStage: 0, // Resets to Stage 1 (0-indexed 0)
                tickInterval: 4.6f,
                lateRamp: 2,
                normalWindow: 15.0f,
                nightmareWindow: 12.0f,
                energyDrainNormal: 0f,
                energyDrainNightmare: 10f,
                camBehavior: CameraStallBehavior.ErleScannerAndGoodBadCop,
                weaponId: "Electric Gun"
            );

            // Sandy the Snake (Rear Door 3 / CAM 04): 4 stages Normal / 5 stages Nightmare, 20s Normal / 8s Nightmare window, Spiked Mace, active Night 1
            sandyCfg = CreateOrLoadAnimatronicAsset(
                fileBase: "Sandy",
                id: "sandy",
                displayName: "Sandy the Snake",
                formerRole: "Maintenance / Repair Mascot",
                color: new Color(0.35f, 0.85f, 0.40f),
                doorIdx: 3,
                activeFromNight: 1,
                passiveBeforeActive: false,
                normalStages: 4,
                nightmareStages: 5,
                retreatStage: 0, // Resets to Stage 1 (0-indexed 0)
                tickInterval: 5.0f,
                lateRamp: 2,
                normalWindow: 20.0f,
                nightmareWindow: 8.0f,
                energyDrainNormal: 0f,
                energyDrainNightmare: 22f,
                camBehavior: CameraStallBehavior.SandyNightmareDecoys,
                weaponId: "Spiked Mace"
            );

            // Boss (Night 6 / True Final Boss)
            bossCfg = CreateOrLoadAnimatronicAsset(
                fileBase: "Boss",
                id: "boss",
                displayName: "The Amalgam (True Final Boss)",
                formerRole: "True Final Boss (Requires All 4 Nightmares)",
                color: new Color(0.80f, 0.15f, 0.85f),
                doorIdx: 1,
                activeFromNight: 6,
                passiveBeforeActive: false,
                normalStages: 5,
                nightmareStages: 5,
                retreatStage: 0,
                tickInterval: 3.5f,
                lateRamp: 3,
                normalWindow: 8.0f,
                nightmareWindow: 8.0f,
                energyDrainNormal: 15f,
                energyDrainNightmare: 20f,
                camBehavior: CameraStallBehavior.SandyNightmareDecoys,
                weaponId: "Shotgun"
            );
        }

        private static void CreateOrUpdateGddAnimatronicAssets()
        {
            CreateOrUpdateGddAnimatronicAssets(out _, out _, out _, out _, out _);
        }

        private static AnimatronicConfig CreateOrLoadAnimatronicAsset(
            string fileBase, string id, string displayName, string formerRole, Color color,
            int doorIdx, int activeFromNight, bool passiveBeforeActive,
            int normalStages, int nightmareStages, int retreatStage,
            float tickInterval, int lateRamp, float normalWindow, float nightmareWindow,
            float energyDrainNormal, float energyDrainNightmare,
            CameraStallBehavior camBehavior, string weaponId)
        {
            string path = $"{CONFIG_FOLDER}/Animatronics/{fileBase}.asset";
            var cfg = AssetDatabase.LoadAssetAtPath<AnimatronicConfig>(path);
            bool isNew = (cfg == null);
            if (isNew)
            {
                cfg = ScriptableObject.CreateInstance<AnimatronicConfig>();
            }

            cfg.animatronicId = id;
            cfg.displayName = displayName;
            cfg.formerRole = formerRole;
            cfg.themeColor = color;
            cfg.targetDoorIndex = doorIdx;
            cfg.activeFromNight = activeFromNight;
            cfg.passiveObserverBeforeActiveNight = passiveBeforeActive;
            cfg.normalStageCount = normalStages;
            cfg.nightmareStageCount = nightmareStages;

            int maxLen = Mathf.Max(normalStages, nightmareStages);
            cfg.cameraRouteIndices = new int[maxLen];
            for (int i = 0; i < maxLen; i++) cfg.cameraRouteIndices[i] = doorIdx;

            cfg.retreatStageIndex = retreatStage;
            cfg.baseTickInterval = tickInterval;
            cfg.lateNightAIRamp = lateRamp;
            cfg.attackWindowSeconds = normalWindow;
            cfg.nightmareAttackWindowSeconds = nightmareWindow;
            cfg.energyDrainOnNormalBlock = energyDrainNormal;
            cfg.energyDrainOnNightmareBlock = energyDrainNightmare;
            cfg.cameraBehavior = camBehavior;
            cfg.counterWeaponId = weaponId;

            if (isNew)
            {
                AssetDatabase.CreateAsset(cfg, path);
                Debug.Log($"[Tylerland Editor] Created AnimatronicConfig at {path}");
            }
            else
            {
                EditorUtility.SetDirty(cfg);
            }
            return cfg;
        }

        [MenuItem("Tylerland/2. Setup Core Systems in Current Scene", false, 2)]
        public static void SetupCoreSystemsInScene()
        {
            var existing = Object.FindFirstObjectByType<TylerlandBootstrap>();
            GameObject coreObj;

            if (existing != null)
            {
                coreObj = existing.gameObject;
            }
            else
            {
                coreObj = new GameObject("[GAME_CORE]");
                Undo.RegisterCreatedObjectUndo(coreObj, "Create [GAME_CORE]");
                coreObj.AddComponent<TylerlandBootstrap>();
            }

            ConnectConfigsToGameObject(coreObj);

            Selection.activeGameObject = coreObj;
            EditorUtility.DisplayDialog("Tylerland Setup", "Successfully wired all Phase 1 Core Systems and Night Configs on '[GAME_CORE]'!\n\nPress PLAY to test the shift clock and state machine.", "Let's Go!");
        }

        [MenuItem("Tylerland/3. Connect Configs to [GAME_CORE]", false, 3)]
        public static void ConnectConfigsMenu()
        {
            var bootstrap = Object.FindFirstObjectByType<TylerlandBootstrap>();
            if (bootstrap == null)
            {
                EditorUtility.DisplayDialog("Tylerland Setup", "No '[GAME_CORE]' found in active scene. Run 'Setup Core Systems in Current Scene' first.", "OK");
                return;
            }

            ConnectConfigsToGameObject(bootstrap.gameObject);
            EditorUtility.DisplayDialog("Tylerland Setup", "All Night Configs (1-6) & Economy Config connected successfully to '[GAME_CORE]'!", "OK");
        }

        private static void ConnectConfigsToGameObject(GameObject coreObj)
        {
            var bootstrap = coreObj.GetComponent<TylerlandBootstrap>();
            if (bootstrap != null)
            {
                bootstrap.AutoFindAndAssignConfigs();
            }

            var eco = AssetDatabase.LoadAssetAtPath<EconomyConfig>($"{CONFIG_FOLDER}/EconomyConfig.asset");
            if (eco != null)
            {
                var runMgr = coreObj.GetComponent<RunManager>();
                if (runMgr != null)
                {
                    var prop = typeof(RunManager).GetField("_economyConfig", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                    if (prop != null) prop.SetValue(runMgr, eco);
                    EditorUtility.SetDirty(runMgr);
                }
            }

            EditorUtility.SetDirty(coreObj);
        }

        [MenuItem("Tylerland/4. Build Complete 2D Sprite Office Environment in Scene", false, 4)]
        public static void Build3DOfficeEnvironment()
        {
            // 1. Root
            var existingOffice = GameObject.Find("[OFFICE_ENVIRONMENT]");
            if (existingOffice != null)
            {
                if (EditorUtility.DisplayDialog("Tylerland Setup", "An '[OFFICE_ENVIRONMENT]' already exists in this scene. Replace it?", "Replace", "Cancel"))
                {
                    Undo.DestroyObjectImmediate(existingOffice);
                }
                else return;
            }

            var officeRoot = new GameObject("[OFFICE_ENVIRONMENT]");
            Undo.RegisterCreatedObjectUndo(officeRoot, "Build Complete 2D Sprite Office");

            // 2. Ambient Office Point Light (for spatial atmosphere)
            var lightObj = new GameObject("OfficeCeilingLight");
            lightObj.transform.SetParent(officeRoot.transform, false);
            lightObj.transform.localPosition = new Vector3(0, 3.0f, 0);
            var light = lightObj.AddComponent<Light>();
            light.type = LightType.Point;
            light.range = 14f;
            light.color = new Color(1f, 0.95f, 0.8f);
            light.intensity = 1.3f;

            // 3. 100% 2D Sprite Desk, Floor & Ceiling Layers (Zero 3D Cubes!)
            var desk2DObj = new GameObject("SecurityDesk_2DSprite");
            desk2DObj.transform.SetParent(officeRoot.transform, false);
            desk2DObj.transform.localPosition = new Vector3(0, 0.65f, 1.15f);
            desk2DObj.transform.localRotation = Quaternion.identity;
            var deskSR = desk2DObj.AddComponent<SpriteRenderer>();
            deskSR.sortingOrder = 20;

            var floor2DObj = new GameObject("Floor_2DSprite");
            floor2DObj.transform.SetParent(officeRoot.transform, false);
            floor2DObj.transform.localPosition = new Vector3(0, 0.01f, 0);
            floor2DObj.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            var floorSR = floor2DObj.AddComponent<SpriteRenderer>();
            floorSR.sortingOrder = -10;

            var ceiling2DObj = new GameObject("Ceiling_2DSprite");
            ceiling2DObj.transform.SetParent(officeRoot.transform, false);
            ceiling2DObj.transform.localPosition = new Vector3(0, 3.39f, 0);
            ceiling2DObj.transform.localRotation = Quaternion.Euler(-90f, 0f, 0f);
            var ceilingSR = ceiling2DObj.AddComponent<SpriteRenderer>();
            ceilingSR.sortingOrder = -10;

            // 4. 2D Sprite Doors (GDD Section 4: Left=Erle, Center=Tyler, Right=Orville, Rear=Sandy)
            var doorControllers = new Tylerland.Office.DoorController[4];

            // Inward-facing door pivots:
            // Door 0 Left (Erle the Eagle): at X = -4.9, faces inward (+X) -> Y rotation = 90
            doorControllers[0] = Create2DDoor(officeRoot.transform, 0, "Left Door (Erle the Eagle)", new Vector3(-4.9f, 0, 0), Quaternion.Euler(0, 90f, 0));
            // Door 1 Center (Tyler the Lion): at Z = +4.9, faces inward (-Z) -> Y rotation = 180
            doorControllers[1] = Create2DDoor(officeRoot.transform, 1, "Center Door (Tyler the Lion)", new Vector3(0, 0, 4.9f), Quaternion.Euler(0, 180f, 0));
            // Door 2 Right (Orville the Orca): at X = +4.9, faces inward (-X) -> Y rotation = -90
            doorControllers[2] = Create2DDoor(officeRoot.transform, 2, "Right Door (Orville the Orca)", new Vector3(4.9f, 0, 0), Quaternion.Euler(0, -90f, 0));
            // Door 3 Rear (Sandy the Snake): at Z = -4.9, faces inward (+Z) -> Y rotation = 0
            doorControllers[3] = Create2DDoor(officeRoot.transform, 3, "Rear Door (Sandy the Snake)", new Vector3(0, 0, -4.9f), Quaternion.Euler(0, 0f, 0));

            // 5. Office & Animatronic Systems Managers
            var officeMgrObj = new GameObject("[OFFICE_MANAGERS]");
            officeMgrObj.transform.SetParent(officeRoot.transform, false);
            var doorSys = officeMgrObj.AddComponent<Tylerland.Office.DoorSystemManager>();
            var attackRes = officeMgrObj.AddComponent<Tylerland.Office.AttackResolver>();
            var camSys = officeMgrObj.AddComponent<Tylerland.Office.CameraMonitorSystem>();
            camSys.InitializeFourAnimatronicRooms();
            var phoneSys = officeMgrObj.AddComponent<Tylerland.Office.PhoneSystem>();
            var hud = officeMgrObj.AddComponent<Tylerland.Office.OfficeHUD>();
            var artMgr = officeMgrObj.AddComponent<Tylerland.Office.OfficeArtManager>();

            // Section 5: Animatronic Systems
            var animMgr = officeMgrObj.AddComponent<AnimatronicManager>();
            officeMgrObj.AddComponent<SnakeCameraManipulator>();
            officeMgrObj.AddComponent<SpecialWeaponSystem>();
            officeMgrObj.AddComponent<JumpscareManager>();

            // Ensure default GDD AnimatronicConfig assets exist and wire them into AnimatronicManager
            if (!AssetDatabase.IsValidFolder(CONFIG_FOLDER))
            {
                AssetDatabase.CreateFolder("Assets", "ScriptableObjects");
            }
            if (!AssetDatabase.IsValidFolder(CONFIG_FOLDER + "/Animatronics"))
            {
                AssetDatabase.CreateFolder(CONFIG_FOLDER, "Animatronics");
            }

            CreateOrUpdateGddAnimatronicAssets(out var tylerCfg, out var orvilleCfg, out var erleCfg, out var sandyCfg, out var bossCfg);

            var animFlags = System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance;
            typeof(AnimatronicManager).GetField("_tylerConfig", animFlags)?.SetValue(animMgr, tylerCfg);
            typeof(AnimatronicManager).GetField("_sharkConfig", animFlags)?.SetValue(animMgr, orvilleCfg);
            typeof(AnimatronicManager).GetField("_eagleConfig", animFlags)?.SetValue(animMgr, erleCfg);
            typeof(AnimatronicManager).GetField("_snakeConfig", animFlags)?.SetValue(animMgr, sandyCfg);
            typeof(AnimatronicManager).GetField("_bossConfig", animFlags)?.SetValue(animMgr, bossCfg);

            // 6. 100% 2D Sprite Walls, Corridors & Doorway Blindspots
            Build2DOfficeSpriteLayers(officeRoot.transform, artMgr, deskSR, floorSR, ceilingSR);
            artMgr.ApplyAllArt();

            // Wire doors into DoorSystemManager
            var doorsField = typeof(Tylerland.Office.DoorSystemManager).GetField("_doors", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (doorsField != null) doorsField.SetValue(doorSys, doorControllers);

            // 7. Camera setup (3D camera rotation in a 100% 2D Sprite room)
            var mainCam = Camera.main;
            if (mainCam == null)
            {
                var camObj = new GameObject("Main Camera");
                mainCam = camObj.AddComponent<Camera>();
                camObj.tag = "MainCamera";
                camObj.AddComponent<AudioListener>();
            }

            mainCam.transform.position = new Vector3(0, 1.6f, 0);
            mainCam.transform.rotation = Quaternion.identity;
            var viewCtrl = mainCam.GetComponent<Tylerland.Office.PlayerViewController>();
            if (viewCtrl == null)
            {
                viewCtrl = mainCam.gameObject.AddComponent<Tylerland.Office.PlayerViewController>();
            }

            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene());

            Selection.activeGameObject = officeMgrObj;
            EditorUtility.DisplayDialog("Tylerland Setup", "100% 2D Sprite Office & GDD Section 4 Animatronic Systems built successfully!\n\n- CAM 01 Left: Erle the Eagle (Electric Gun)\n- CAM 02 Center: Tyler the Lion (Shotgun)\n- CAM 03 Right: Orville the Orca (Harpoon)\n- CAM 04 Rear: Sandy the Snake (Spiked Mace)\n\nPress PLAY to test!", "Awesome!");
        }

        private static void Build2DOfficeSpriteLayers(
            Transform parent,
            Tylerland.Office.OfficeArtManager artMgr,
            SpriteRenderer deskSR,
            SpriteRenderer floorSR,
            SpriteRenderer ceilingSR)
        {
            var sprites2DRoot = new GameObject("Office_2DSprite_Layers");
            sprites2DRoot.transform.SetParent(parent, false);

            // Camera look rotations: Left = -90, Center = 0, Right = +90, Back = 180
            Quaternion[] viewRotations =
            {
                Quaternion.Euler(0, -90f, 0),
                Quaternion.Euler(0, 0f, 0),
                Quaternion.Euler(0, 90f, 0),
                Quaternion.Euler(0, 180f, 0)
            };

            // Wall 2D positions (4.82m from center, just inside the door shutter at 4.90m)
            Vector3[] wall2DPositions =
            {
                new Vector3(-4.82f, 1.75f, 0f),
                new Vector3(0f, 1.75f, 4.82f),
                new Vector3(4.82f, 1.75f, 0f),
                new Vector3(0f, 1.75f, -4.82f)
            };

            // Blindspot Animatronic 2D positions (5.35m from center, right outside the door shutter)
            Vector3[] animatronic2DPositions =
            {
                new Vector3(-5.35f, 1.45f, 0f),
                new Vector3(0f, 1.45f, 5.35f),
                new Vector3(5.35f, 1.45f, 0f),
                new Vector3(0f, 1.45f, -5.35f)
            };

            // Corridor Backdrop 2D positions (5.85m from center, behind the doorway animatronic)
            Vector3[] corridor2DPositions =
            {
                new Vector3(-5.85f, 1.75f, 0f),
                new Vector3(0f, 1.75f, 5.85f),
                new Vector3(5.85f, 1.75f, 0f),
                new Vector3(0f, 1.75f, -5.85f)
            };

            string[] dirLabels = { "Left_Erle", "Center_Tyler", "Right_Orville", "Rear_Sandy" };
            string[] slotTitles =
            {
                "0. Left Wall & Corridor (Erle the Eagle)",
                "1. Center Wall & Corridor (Tyler the Lion)",
                "2. Right Wall & Corridor (Orville the Orca)",
                "3. Rear Wall & Corridor (Sandy the Snake)"
            };

            var slots = new Tylerland.Office.DirectionalWallArtSlot[4];
            for (int i = 0; i < 4; i++)
            {
                var wall2DObj = new GameObject($"Wall_2DSprite_{dirLabels[i]}");
                wall2DObj.transform.SetParent(sprites2DRoot.transform, false);
                wall2DObj.transform.localPosition = wall2DPositions[i];
                wall2DObj.transform.localRotation = viewRotations[i];
                var wallSR = wall2DObj.AddComponent<SpriteRenderer>();
                wallSR.sortingOrder = 10;

                var anim2DObj = new GameObject($"Blindspot_Animatronic_2DSprite_{dirLabels[i]}");
                anim2DObj.transform.SetParent(sprites2DRoot.transform, false);
                anim2DObj.transform.localPosition = animatronic2DPositions[i];
                anim2DObj.transform.localRotation = viewRotations[i];
                var animSR = anim2DObj.AddComponent<SpriteRenderer>();
                animSR.sortingOrder = 2;

                var corr2DObj = new GameObject($"Corridor_Backdrop_2DSprite_{dirLabels[i]}");
                corr2DObj.transform.SetParent(sprites2DRoot.transform, false);
                corr2DObj.transform.localPosition = corridor2DPositions[i];
                corr2DObj.transform.localRotation = viewRotations[i];
                var corrSR = corr2DObj.AddComponent<SpriteRenderer>();
                corrSR.sortingOrder = 0;

                slots[i] = new Tylerland.Office.DirectionalWallArtSlot
                {
                    directionName = slotTitles[i],
                    wallSpriteRenderer = wallSR,
                    corridorSpriteRenderer = corrSR,
                    blindspotAnimatronicRenderer = animSR
                };
            }

            if (artMgr != null)
            {
                var flags = System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance;
                typeof(Tylerland.Office.OfficeArtManager).GetField("_wallSlots", flags)?.SetValue(artMgr, slots);
                typeof(Tylerland.Office.OfficeArtManager).GetField("_deskSpriteRenderer", flags)?.SetValue(artMgr, deskSR);
                typeof(Tylerland.Office.OfficeArtManager).GetField("_floorSpriteRenderer", flags)?.SetValue(artMgr, floorSR);
                typeof(Tylerland.Office.OfficeArtManager).GetField("_ceilingSpriteRenderer", flags)?.SetValue(artMgr, ceilingSR);
            }
        }

        private static Tylerland.Office.DoorController Create2DDoor(Transform parent, int index, string doorName, Vector3 position, Quaternion rotation)
        {
            var doorFrameObj = new GameObject(doorName);
            doorFrameObj.transform.SetParent(parent, false);
            doorFrameObj.transform.position = position;
            doorFrameObj.transform.rotation = rotation;

            // Moving Door Root Pivot (Scale 1,1,1 so child 2D Sprites are never distorted!)
            var doorMovingPivot = new GameObject("Door_MovingShutter_Pivot");
            doorMovingPivot.transform.SetParent(doorFrameObj.transform, false);
            doorMovingPivot.transform.localPosition = new Vector3(0, 4.5f, 0); // Open position high
            doorMovingPivot.transform.localRotation = Quaternion.identity;
            doorMovingPivot.transform.localScale = Vector3.one;

            // Dedicated 2D Sprite Child (rotated 180Y so it faces camera without horizontal mirroring)
            var door2DSpriteObj = new GameObject("Door_2DSprite_Art");
            door2DSpriteObj.transform.SetParent(doorMovingPivot.transform, false);
            door2DSpriteObj.transform.localPosition = Vector3.zero;
            door2DSpriteObj.transform.localRotation = Quaternion.Euler(0, 180f, 0);
            door2DSpriteObj.transform.localScale = Vector3.one;
            var spriteRenderer = door2DSpriteObj.AddComponent<SpriteRenderer>();
            spriteRenderer.sortingOrder = 5;
            spriteRenderer.sprite = Tylerland.Office.Procedural2DSpriteFactory.GetOrCreateDoorShutterSprite(index);
            Tylerland.Office.OfficeArtManager.FitSpriteRendererToSize(spriteRenderer, new Vector2(2.0f, 2.8f));

            // Controller component
            var controller = doorFrameObj.AddComponent<Tylerland.Office.DoorController>();

            var flags = System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance;
            typeof(Tylerland.Office.DoorController).GetField("_doorIndex", flags)?.SetValue(controller, index);
            typeof(Tylerland.Office.DoorController).GetField("_doorName", flags)?.SetValue(controller, doorName);
            typeof(Tylerland.Office.DoorController).GetField("_doorMeshTransform", flags)?.SetValue(controller, doorMovingPivot.transform);
            typeof(Tylerland.Office.DoorController).GetField("_doorSpriteRenderer", flags)?.SetValue(controller, spriteRenderer);
            typeof(Tylerland.Office.DoorController).GetField("_openLocalPosition", flags)?.SetValue(controller, new Vector3(0, 4.5f, 0));
            typeof(Tylerland.Office.DoorController).GetField("_closedLocalPosition", flags)?.SetValue(controller, new Vector3(0, 1.4f, 0));
            typeof(Tylerland.Office.DoorController).GetField("_rechargeDuringNight", flags)?.SetValue(controller, false);
            typeof(Tylerland.Office.DoorController).GetField("_energyRegenOpen", flags)?.SetValue(controller, 0.0f);

            // Hallway Blindspot Light outside door frame
            var lightObj = new GameObject("HallwayBlindspotLight");
            lightObj.transform.SetParent(doorFrameObj.transform, false);
            lightObj.transform.localPosition = new Vector3(0, 2.5f, 1.2f);
            lightObj.transform.localRotation = Quaternion.identity;
            var hallLight = lightObj.AddComponent<Light>();
            hallLight.type = LightType.Point;
            hallLight.range = 8f;
            hallLight.color = new Color(1f, 0.95f, 0.75f);
            hallLight.intensity = 2.5f;
            hallLight.enabled = false;

            typeof(Tylerland.Office.DoorController).GetField("_hallwayLight", flags)?.SetValue(controller, hallLight);

            return controller;
        }
    }
}
