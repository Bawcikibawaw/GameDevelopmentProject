using UnityEngine;
using UnityEditor;
using System.IO;
using Tylerland.Core;
using Tylerland.Config;
using Tylerland.Animatronics;

namespace Tylerland.Editor
{
    /// <summary>
    /// Editor automation tool that sets up the project configs and scene hierarchy with 1 click.
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

            // 2. Night Configs (From Roadmap Section 8.1 table)
            int[] durations = { 60, 75, 90, 90, 90, 90 };
            int[] tylerAI =   {  2,  4,  5,  7,  9, 10 };
            int[] sharkAI =   {  0,  3,  5,  7,  9, 10 };
            int[] eagleAI =   {  0,  0,  3,  6,  8, 10 };
            int[] snakeAI =   {  0,  0,  0,  3,  7,  9 };
            bool[] boss =     { false, false, false, false, false, true };

            for (int i = 0; i < 6; i++)
            {
                int nightNum = i + 1;
                string nightPath = $"{CONFIG_FOLDER}/Nights/Night_{nightNum}.asset";
                var nightConfig = AssetDatabase.LoadAssetAtPath<NightConfig>(nightPath);
                if (nightConfig == null)
                {
                    nightConfig = ScriptableObject.CreateInstance<NightConfig>();
                    nightConfig.nightNumber = nightNum;
                    nightConfig.realSecondsPerGameHour = durations[i];
                    nightConfig.tylerAILevel = tylerAI[i];
                    nightConfig.sharkAILevel = sharkAI[i];
                    nightConfig.eagleAILevel = eagleAI[i];
                    nightConfig.snakeAILevel = snakeAI[i];
                    nightConfig.bossActive = boss[i];
                    nightConfig.muteAllowed = nightNum >= 2;

                    AssetDatabase.CreateAsset(nightConfig, nightPath);
                    Debug.Log($"[Tylerland Editor] Created NightConfig at {nightPath}");
                }
            }

            // 3. Animatronic Configs (§5.2 Cast Table)
            CreateOrLoadAnimatronicAsset("tyler", "Tyler", new Color(0.85f, 0.35f, 0.25f), 1, new[] { 0, 1, 7, 1 }, 1, 4.9f, 2, 4.5f, CameraStallBehavior.StalledWhenWatchedOnCamera, "Shears");
            CreateOrLoadAnimatronicAsset("shark", "Shark", new Color(0.25f, 0.65f, 0.90f), 0, new[] { 4, 4, 2, 3 }, 1, 4.2f, 2, 3.8f, CameraStallBehavior.StalledWhenWatchedOnCamera, "Harpoon");
            CreateOrLoadAnimatronicAsset("eagle", "Eagle", new Color(0.95f, 0.75f, 0.20f), 2, new[] { 0, 1, 8, 5, 6 }, 0, 4.6f, 2, 3.5f, CameraStallBehavior.None, "Net");
            CreateOrLoadAnimatronicAsset("snake", "Snake", new Color(0.35f, 0.85f, 0.40f), 3, new[] { 7, 8, 9, 9 }, 0, 5.1f, 3, 4.2f, CameraStallBehavior.ManipulatesCameraFeeds, "Prod");
            CreateOrLoadAnimatronicAsset("boss", "The Amalgam (Boss)", new Color(0.80f, 0.15f, 0.85f), 1, new[] { 0, 1, 2, 5, 9 }, 1, 3.4f, 4, 3.2f, CameraStallBehavior.ManipulatesCameraFeeds, "Shears");

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            EditorUtility.DisplayDialog("Tylerland Setup", "Default Config assets (Nights 1-6, Economy & 5 Animatronics) generated successfully in Assets/ScriptableObjects/!", "OK");
        }

        private static AnimatronicConfig CreateOrLoadAnimatronicAsset(
            string id, string displayName, Color color, int doorIdx, int[] route,
            int retreatStage, float tickInterval, int lateRamp, float attackWindow,
            CameraStallBehavior camBehavior, string weaponId)
        {
            string path = $"{CONFIG_FOLDER}/Animatronics/{displayName.Split(' ')[0]}.asset";
            var cfg = AssetDatabase.LoadAssetAtPath<AnimatronicConfig>(path);
            if (cfg == null)
            {
                cfg = ScriptableObject.CreateInstance<AnimatronicConfig>();
                cfg.animatronicId = id;
                cfg.displayName = displayName;
                cfg.themeColor = color;
                cfg.targetDoorIndex = doorIdx;
                cfg.cameraRouteIndices = route;
                cfg.retreatStageIndex = retreatStage;
                cfg.baseTickInterval = tickInterval;
                cfg.lateNightAIRamp = lateRamp;
                cfg.attackWindowSeconds = attackWindow;
                cfg.cameraBehavior = camBehavior;
                cfg.counterWeaponId = weaponId;
                AssetDatabase.CreateAsset(cfg, path);
                Debug.Log($"[Tylerland Editor] Created AnimatronicConfig at {path}");
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

            // Wire all configs
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

        [MenuItem("Tylerland/4. Build Complete 3D Office Environment in Scene", false, 4)]
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
            Undo.RegisterCreatedObjectUndo(officeRoot, "Build Complete 3D Office");

            // 2. Room Structure (Floor, Ceiling, Light)
            var floor = GameObject.CreatePrimitive(PrimitiveType.Cube);
            floor.name = "Floor";
            floor.transform.SetParent(officeRoot.transform, false);
            floor.transform.localPosition = new Vector3(0, -0.1f, 0);
            floor.transform.localRotation = Quaternion.identity;
            floor.transform.localScale = new Vector3(10f, 0.2f, 10f);

            var ceiling = GameObject.CreatePrimitive(PrimitiveType.Cube);
            ceiling.name = "Ceiling";
            ceiling.transform.SetParent(officeRoot.transform, false);
            ceiling.transform.localPosition = new Vector3(0, 3.5f, 0);
            ceiling.transform.localRotation = Quaternion.identity;
            ceiling.transform.localScale = new Vector3(10f, 0.2f, 10f);

            var lightObj = new GameObject("OfficeCeilingLight");
            lightObj.transform.SetParent(officeRoot.transform, false);
            lightObj.transform.localPosition = new Vector3(0, 3.0f, 0);
            var light = lightObj.AddComponent<Light>();
            light.type = LightType.Point;
            light.range = 14f;
            light.color = new Color(1f, 0.95f, 0.8f);
            light.intensity = 1.3f;

            // 3. Desk & Monitor (3D Placeholder + 2D Sprite Layer)
            var desk = GameObject.CreatePrimitive(PrimitiveType.Cube);
            desk.name = "SecurityDesk_3DPlaceholder";
            desk.transform.SetParent(officeRoot.transform, false);
            desk.transform.localPosition = new Vector3(0, 0.4f, 1.2f);
            desk.transform.localRotation = Quaternion.identity;
            desk.transform.localScale = new Vector3(2.6f, 0.8f, 1.0f);

            var monitor = GameObject.CreatePrimitive(PrimitiveType.Cube);
            monitor.name = "DeskSecurityMonitor";
            monitor.transform.SetParent(desk.transform, false);
            monitor.transform.localPosition = new Vector3(0, 0.7f, 0);
            monitor.transform.localRotation = Quaternion.identity;
            monitor.transform.localScale = new Vector3(0.5f, 0.4f, 0.1f);

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

            // 4. Doors (Left=Shark, Center=Tyler, Right=Eagle, Back=Snake)
            var doorControllers = new Tylerland.Office.DoorController[4];

            // Inward-facing door frames:
            // Left (Shark): at X = -4.9, faces inward (+X) -> Y rotation = 90
            doorControllers[0] = Create3DDoor(officeRoot.transform, 0, "Left Door (Shark)", new Vector3(-4.9f, 0, 0), Quaternion.Euler(0, 90f, 0));
            // Center (Tyler): at Z = +4.9, faces inward (-Z) -> Y rotation = 180
            doorControllers[1] = Create3DDoor(officeRoot.transform, 1, "Center Door (Tyler)", new Vector3(0, 0, 4.9f), Quaternion.Euler(0, 180f, 0));
            // Right (Eagle): at X = +4.9, faces inward (-X) -> Y rotation = -90
            doorControllers[2] = Create3DDoor(officeRoot.transform, 2, "Right Door (Eagle)", new Vector3(4.9f, 0, 0), Quaternion.Euler(0, -90f, 0));
            // Back (Snake): at Z = -4.9, faces inward (+Z) -> Y rotation = 0
            doorControllers[3] = Create3DDoor(officeRoot.transform, 3, "Back Door (Snake)", new Vector3(0, 0, -4.9f), Quaternion.Euler(0, 0f, 0));

            // 5. Office & Animatronic Systems Managers
            var officeMgrObj = new GameObject("[OFFICE_MANAGERS]");
            officeMgrObj.transform.SetParent(officeRoot.transform, false);
            var doorSys = officeMgrObj.AddComponent<Tylerland.Office.DoorSystemManager>();
            var attackRes = officeMgrObj.AddComponent<Tylerland.Office.AttackResolver>();
            var camSys = officeMgrObj.AddComponent<Tylerland.Office.CameraMonitorSystem>();
            var phoneSys = officeMgrObj.AddComponent<Tylerland.Office.PhoneSystem>();
            var hud = officeMgrObj.AddComponent<Tylerland.Office.OfficeHUD>();
            var artMgr = officeMgrObj.AddComponent<Tylerland.Office.OfficeArtManager>();

            // Section 5: Animatronic Systems
            var animMgr = officeMgrObj.AddComponent<AnimatronicManager>();
            officeMgrObj.AddComponent<SnakeCameraManipulator>();
            officeMgrObj.AddComponent<SpecialWeaponSystem>();
            officeMgrObj.AddComponent<JumpscareManager>();

            // Ensure default AnimatronicConfig assets exist and wire them into AnimatronicManager
            if (!AssetDatabase.IsValidFolder(CONFIG_FOLDER))
            {
                AssetDatabase.CreateFolder("Assets", "ScriptableObjects");
            }
            if (!AssetDatabase.IsValidFolder(CONFIG_FOLDER + "/Animatronics"))
            {
                AssetDatabase.CreateFolder(CONFIG_FOLDER, "Animatronics");
            }

            var tylerCfg = CreateOrLoadAnimatronicAsset("tyler", "Tyler", new Color(0.85f, 0.35f, 0.25f), 1, new[] { 0, 1, 7, 1 }, 1, 4.9f, 2, 4.5f, CameraStallBehavior.StalledWhenWatchedOnCamera, "Shears");
            var sharkCfg = CreateOrLoadAnimatronicAsset("shark", "Shark", new Color(0.25f, 0.65f, 0.90f), 0, new[] { 4, 4, 2, 3 }, 1, 4.2f, 2, 3.8f, CameraStallBehavior.StalledWhenWatchedOnCamera, "Harpoon");
            var eagleCfg = CreateOrLoadAnimatronicAsset("eagle", "Eagle", new Color(0.95f, 0.75f, 0.20f), 2, new[] { 0, 1, 8, 5, 6 }, 0, 4.6f, 2, 3.5f, CameraStallBehavior.None, "Net");
            var snakeCfg = CreateOrLoadAnimatronicAsset("snake", "Snake", new Color(0.35f, 0.85f, 0.40f), 3, new[] { 7, 8, 9, 9 }, 0, 5.1f, 3, 4.2f, CameraStallBehavior.ManipulatesCameraFeeds, "Prod");
            var bossCfg  = CreateOrLoadAnimatronicAsset("boss", "Boss", new Color(0.80f, 0.15f, 0.85f), 1, new[] { 0, 1, 2, 5, 9 }, 1, 3.4f, 4, 3.2f, CameraStallBehavior.ManipulatesCameraFeeds, "Shears");

            var animFlags = System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance;
            typeof(AnimatronicManager).GetField("_tylerConfig", animFlags)?.SetValue(animMgr, tylerCfg);
            typeof(AnimatronicManager).GetField("_sharkConfig", animFlags)?.SetValue(animMgr, sharkCfg);
            typeof(AnimatronicManager).GetField("_eagleConfig", animFlags)?.SetValue(animMgr, eagleCfg);
            typeof(AnimatronicManager).GetField("_snakeConfig", animFlags)?.SetValue(animMgr, snakeCfg);
            typeof(AnimatronicManager).GetField("_bossConfig", animFlags)?.SetValue(animMgr, bossCfg);

            // 6. Walls with Doorway Openings, Dark Corridor Tunnels & 2D Sprite Canvases
            BuildOfficeWallsAndCorridors(officeRoot.transform, artMgr, deskSR, desk, floorSR, floor, ceilingSR, ceiling);

            // Wire doors into DoorSystemManager
            var doorsField = typeof(Tylerland.Office.DoorSystemManager).GetField("_doors", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (doorsField != null) doorsField.SetValue(doorSys, doorControllers);

            // 7. Camera setup
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
            EditorUtility.DisplayDialog("Tylerland Setup", "3D + 2D Hybrid Office & Animatronic Systems built successfully!\n\n- 4 Inward-Facing Doors + 2D Sprite Layers\n- OfficeArtManager ready for 100% 2D Hand-Drawn Walls, Corridors & Desk\n- All 5 Animatronics (Tyler, Shark, Eagle, Snake, Boss) + Counter-Weapons + Jumpscare System wired!\n\nSelect '[OFFICE_MANAGERS]' to customize sprites or configs anytime!", "Awesome!");
        }

        private static void BuildOfficeWallsAndCorridors(
            Transform parent,
            Tylerland.Office.OfficeArtManager artMgr,
            SpriteRenderer deskSR, GameObject desk3D,
            SpriteRenderer floorSR, GameObject floor3D,
            SpriteRenderer ceilingSR, GameObject ceiling3D)
        {
            var wallsRoot = new GameObject("OfficeWallsAndCorridors");
            wallsRoot.transform.SetParent(parent, false);

            // 3D Placeholder Wall Sections
            // 0 = WEST WALL (X = -5.0, Left - Shark)
            var westWalls = new GameObject[]
            {
                CreateWallSection(wallsRoot.transform, "Wall_West_Left", new Vector3(-5.0f, 1.75f, -3.05f), new Vector3(0.2f, 3.5f, 3.9f)),
                CreateWallSection(wallsRoot.transform, "Wall_West_Right", new Vector3(-5.0f, 1.75f, 3.05f), new Vector3(0.2f, 3.5f, 3.9f)),
                CreateWallSection(wallsRoot.transform, "Wall_West_Top", new Vector3(-5.0f, 3.25f, 0f), new Vector3(0.2f, 0.5f, 2.2f))
            };

            // 1 = NORTH WALL (Z = +5.0, Center - Tyler)
            var northWalls = new GameObject[]
            {
                CreateWallSection(wallsRoot.transform, "Wall_North_Left", new Vector3(-3.05f, 1.75f, 5.0f), new Vector3(3.9f, 3.5f, 0.2f)),
                CreateWallSection(wallsRoot.transform, "Wall_North_Right", new Vector3(3.05f, 1.75f, 5.0f), new Vector3(3.9f, 3.5f, 0.2f)),
                CreateWallSection(wallsRoot.transform, "Wall_North_Top", new Vector3(0f, 3.25f, 5.0f), new Vector3(2.2f, 0.5f, 0.2f))
            };

            // 2 = EAST WALL (X = +5.0, Right - Eagle)
            var eastWalls = new GameObject[]
            {
                CreateWallSection(wallsRoot.transform, "Wall_East_Left", new Vector3(5.0f, 1.75f, -3.05f), new Vector3(0.2f, 3.5f, 3.9f)),
                CreateWallSection(wallsRoot.transform, "Wall_East_Right", new Vector3(5.0f, 1.75f, 3.05f), new Vector3(0.2f, 3.5f, 3.9f)),
                CreateWallSection(wallsRoot.transform, "Wall_East_Top", new Vector3(5.0f, 3.25f, 0f), new Vector3(0.2f, 0.5f, 2.2f))
            };

            // 3 = SOUTH WALL (Z = -5.0, Back - Snake)
            var southWalls = new GameObject[]
            {
                CreateWallSection(wallsRoot.transform, "Wall_South_Left", new Vector3(-3.05f, 1.75f, -5.0f), new Vector3(3.9f, 3.5f, 0.2f)),
                CreateWallSection(wallsRoot.transform, "Wall_South_Right", new Vector3(3.05f, 1.75f, -5.0f), new Vector3(3.9f, 3.5f, 0.2f)),
                CreateWallSection(wallsRoot.transform, "Wall_South_Top", new Vector3(0f, 3.25f, -5.0f), new Vector3(2.2f, 0.5f, 0.2f))
            };

            // 3D Placeholder Corridor Tunnels
            var westTunnel = CreateCorridorTunnel(wallsRoot.transform, "Corridor_West_Shark", new Vector3(-8.0f, 1.75f, 0f), new Vector3(6.0f, 3.5f, 2.4f), true);
            var northTunnel = CreateCorridorTunnel(wallsRoot.transform, "Corridor_North_Tyler", new Vector3(0f, 1.75f, 8.0f), new Vector3(2.4f, 3.5f, 6.0f), false);
            var eastTunnel = CreateCorridorTunnel(wallsRoot.transform, "Corridor_East_Eagle", new Vector3(8.0f, 1.75f, 0f), new Vector3(6.0f, 3.5f, 2.4f), true);
            var southTunnel = CreateCorridorTunnel(wallsRoot.transform, "Corridor_South_Snake", new Vector3(0f, 1.75f, -8.0f), new Vector3(2.4f, 3.5f, 6.0f), false);

            // 2D Hand-Drawn Sprite Layers (Properly rotated to match camera view angle so art is never mirrored!)
            var sprites2DRoot = new GameObject("Office_2DSprite_Layers");
            sprites2DRoot.transform.SetParent(wallsRoot.transform, false);

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

            string[] dirLabels = { "Left_Shark", "Center_Tyler", "Right_Eagle", "Back_Snake" };
            string[] slotTitles =
            {
                "0. Left Wall & West Corridor (Shark)",
                "1. Center Wall & North Corridor (Tyler)",
                "2. Right Wall & East Corridor (Eagle)",
                "3. Back Wall & South Vent Corridor (Snake)"
            };
            GameObject[][] wall3DGroups = { westWalls, northWalls, eastWalls, southWalls };
            GameObject[] corridor3DGroups = { westTunnel, northTunnel, eastTunnel, southTunnel };

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
                    blindspotAnimatronicRenderer = animSR,
                    placeholder3DWallObjects = wall3DGroups[i],
                    placeholder3DCorridorObject = corridor3DGroups[i]
                };
            }

            if (artMgr != null)
            {
                var flags = System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance;
                typeof(Tylerland.Office.OfficeArtManager).GetField("_wallSlots", flags)?.SetValue(artMgr, slots);
                typeof(Tylerland.Office.OfficeArtManager).GetField("_deskSpriteRenderer", flags)?.SetValue(artMgr, deskSR);
                typeof(Tylerland.Office.OfficeArtManager).GetField("_placeholder3DDesk", flags)?.SetValue(artMgr, desk3D);
                typeof(Tylerland.Office.OfficeArtManager).GetField("_floorSpriteRenderer", flags)?.SetValue(artMgr, floorSR);
                typeof(Tylerland.Office.OfficeArtManager).GetField("_placeholder3DFloor", flags)?.SetValue(artMgr, floor3D);
                typeof(Tylerland.Office.OfficeArtManager).GetField("_ceilingSpriteRenderer", flags)?.SetValue(artMgr, ceilingSR);
                typeof(Tylerland.Office.OfficeArtManager).GetField("_placeholder3DCeiling", flags)?.SetValue(artMgr, ceiling3D);
            }
        }

        private static GameObject CreateWallSection(Transform parent, string name, Vector3 pos, Vector3 scale)
        {
            var wall = GameObject.CreatePrimitive(PrimitiveType.Cube);
            wall.name = name;
            wall.transform.SetParent(parent, false);
            wall.transform.localPosition = pos;
            wall.transform.localRotation = Quaternion.identity;
            wall.transform.localScale = scale;
            return wall;
        }

        private static GameObject CreateCorridorTunnel(Transform parent, string name, Vector3 center, Vector3 size, bool runsEastWest)
        {
            var tunnel = new GameObject(name);
            tunnel.transform.SetParent(parent, false);
            tunnel.transform.localPosition = center;

            // Tunnel Floor
            var f = GameObject.CreatePrimitive(PrimitiveType.Cube);
            f.name = "Floor";
            f.transform.SetParent(tunnel.transform, false);
            f.transform.localPosition = new Vector3(0, -size.y / 2f, 0);
            f.transform.localScale = new Vector3(size.x, 0.2f, size.z);

            // Tunnel Ceiling
            var c = GameObject.CreatePrimitive(PrimitiveType.Cube);
            c.name = "Ceiling";
            c.transform.SetParent(tunnel.transform, false);
            c.transform.localPosition = new Vector3(0, size.y / 2f, 0);
            c.transform.localScale = new Vector3(size.x, 0.2f, size.z);

            if (runsEastWest)
            {
                // Left & Right walls along Z
                var w1 = GameObject.CreatePrimitive(PrimitiveType.Cube);
                w1.name = "Wall_North";
                w1.transform.SetParent(tunnel.transform, false);
                w1.transform.localPosition = new Vector3(0, 0, size.z / 2f);
                w1.transform.localScale = new Vector3(size.x, size.y, 0.2f);

                var w2 = GameObject.CreatePrimitive(PrimitiveType.Cube);
                w2.name = "Wall_South";
                w2.transform.SetParent(tunnel.transform, false);
                w2.transform.localPosition = new Vector3(0, 0, -size.z / 2f);
                w2.transform.localScale = new Vector3(size.x, size.y, 0.2f);
            }
            else
            {
                // Left & Right walls along X
                var w1 = GameObject.CreatePrimitive(PrimitiveType.Cube);
                w1.name = "Wall_West";
                w1.transform.SetParent(tunnel.transform, false);
                w1.transform.localPosition = new Vector3(-size.x / 2f, 0, 0);
                w1.transform.localScale = new Vector3(0.2f, size.y, size.z);

                var w2 = GameObject.CreatePrimitive(PrimitiveType.Cube);
                w2.name = "Wall_East";
                w2.transform.SetParent(tunnel.transform, false);
                w2.transform.localPosition = new Vector3(size.x / 2f, 0, 0);
                w2.transform.localScale = new Vector3(0.2f, size.y, size.z);
            }
            return tunnel;
        }

        private static Tylerland.Office.DoorController Create3DDoor(Transform parent, int index, string doorName, Vector3 position, Quaternion rotation)
        {
            var doorFrameObj = new GameObject(doorName);
            doorFrameObj.transform.SetParent(parent, false);
            doorFrameObj.transform.position = position;
            doorFrameObj.transform.rotation = rotation;

            // Frame pillars (Strictly local space, localRotation identity)
            var leftPillar = GameObject.CreatePrimitive(PrimitiveType.Cube);
            leftPillar.name = "Frame_Left";
            leftPillar.transform.SetParent(doorFrameObj.transform, false);
            leftPillar.transform.localPosition = new Vector3(-1.1f, 1.5f, 0);
            leftPillar.transform.localRotation = Quaternion.identity;
            leftPillar.transform.localScale = new Vector3(0.2f, 3.0f, 0.4f);

            var rightPillar = GameObject.CreatePrimitive(PrimitiveType.Cube);
            rightPillar.name = "Frame_Right";
            rightPillar.transform.SetParent(doorFrameObj.transform, false);
            rightPillar.transform.localPosition = new Vector3(1.1f, 1.5f, 0);
            rightPillar.transform.localRotation = Quaternion.identity;
            rightPillar.transform.localScale = new Vector3(0.2f, 3.0f, 0.4f);

            var topPillar = GameObject.CreatePrimitive(PrimitiveType.Cube);
            topPillar.name = "Frame_Top";
            topPillar.transform.SetParent(doorFrameObj.transform, false);
            topPillar.transform.localPosition = new Vector3(0, 3.1f, 0);
            topPillar.transform.localRotation = Quaternion.identity;
            topPillar.transform.localScale = new Vector3(2.4f, 0.2f, 0.4f);

            // Moving Door Root Pivot (Scale 1,1,1 so child 2D Sprites are never distorted!)
            var doorMovingPivot = new GameObject("Door_MovingShutter_Pivot");
            doorMovingPivot.transform.SetParent(doorFrameObj.transform, false);
            doorMovingPivot.transform.localPosition = new Vector3(0, 4.5f, 0); // Open position high
            doorMovingPivot.transform.localRotation = Quaternion.identity;
            doorMovingPivot.transform.localScale = Vector3.one;

            // 1. 3D Placeholder Box (Automatically hides when a 2D door sprite is assigned)
            var door3DBox = GameObject.CreatePrimitive(PrimitiveType.Cube);
            door3DBox.name = "Door_3DPlaceholderBox";
            door3DBox.transform.SetParent(doorMovingPivot.transform, false);
            door3DBox.transform.localPosition = Vector3.zero;
            door3DBox.transform.localRotation = Quaternion.identity;
            door3DBox.transform.localScale = new Vector3(2.0f, 2.8f, 0.05f);
            var placeholderMeshRenderer = door3DBox.GetComponent<MeshRenderer>();

            // 2. Dedicated 2D Sprite Child (No MeshRenderer conflict, rotated 180Y so it faces camera without horizontal mirroring)
            var door2DSpriteObj = new GameObject("Door_2DSprite_Art");
            door2DSpriteObj.transform.SetParent(doorMovingPivot.transform, false);
            door2DSpriteObj.transform.localPosition = Vector3.zero;
            door2DSpriteObj.transform.localRotation = Quaternion.Euler(0, 180f, 0);
            door2DSpriteObj.transform.localScale = Vector3.one;
            var spriteRenderer = door2DSpriteObj.AddComponent<SpriteRenderer>();
            spriteRenderer.sortingOrder = 5;

            // Controller component
            var controller = doorFrameObj.AddComponent<Tylerland.Office.DoorController>();

            var flags = System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance;
            typeof(Tylerland.Office.DoorController).GetField("_doorIndex", flags)?.SetValue(controller, index);
            typeof(Tylerland.Office.DoorController).GetField("_doorName", flags)?.SetValue(controller, doorName);
            typeof(Tylerland.Office.DoorController).GetField("_doorMeshTransform", flags)?.SetValue(controller, doorMovingPivot.transform);
            typeof(Tylerland.Office.DoorController).GetField("_doorSpriteRenderer", flags)?.SetValue(controller, spriteRenderer);
            typeof(Tylerland.Office.DoorController).GetField("_placeholder3DSlabRenderer", flags)?.SetValue(controller, placeholderMeshRenderer);
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
