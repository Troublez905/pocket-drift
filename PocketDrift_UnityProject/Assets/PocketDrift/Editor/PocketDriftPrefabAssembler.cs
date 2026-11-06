#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.Events;
using UnityEngine;
using UnityEngine.UI;

public static class PocketDriftPrefabAssembler
{
    [MenuItem("Tools/Pocket Drift/Auto-Assemble Workbench Scene & Cars")]
    public static void AssembleFullWorkbenchScene()
    {
        // 1. Create Workbench Table Ground
        GameObject table = GameObject.Find("Workbench_Table");
        if (table == null)
        {
            GameObject tableModel = LoadModelAsset("Prop_Workbench_Table");
            if (tableModel != null)
            {
                table = (GameObject)PrefabUtility.InstantiatePrefab(tableModel);
                table.name = "Workbench_Table";
                table.transform.position = Vector3.zero;
                AddMeshCollidersRecursive(table);
            }
            else
            {
                table = GameObject.CreatePrimitive(PrimitiveType.Cube);
                table.name = "Workbench_Table";
                table.transform.position = new Vector3(0f, -0.5f, 0f);
                table.transform.localScale = new Vector3(50f, 1f, 35f);
            }
        }

        // 2. Create TableEdgeFallTrigger Plane Below
        GameObject fallTriggerObj = GameObject.Find("TableEdgeFallTrigger");
        if (fallTriggerObj == null)
        {
            fallTriggerObj = new GameObject("TableEdgeFallTrigger");
            fallTriggerObj.transform.position = new Vector3(0f, -6f, 0f);
            BoxCollider box = fallTriggerObj.AddComponent<BoxCollider>();
            box.isTrigger = true;
            box.size = new Vector3(140f, 2f, 140f);
            fallTriggerObj.AddComponent<TableEdgeFallTrigger>();
        }

        // 3. Create Track Waypoint Circuit
        GameObject circuitObj = GameObject.Find("TrackWaypoints_Container");
        TrackWaypointCircuit circuit;
        if (circuitObj == null)
        {
            circuitObj = new GameObject("TrackWaypoints_Container");
            circuit = circuitObj.AddComponent<TrackWaypointCircuit>();

            Vector3[] wpPositions = new Vector3[]
            {
                new Vector3(-18f, 0.2f, -10f),
                new Vector3(-18f, 0.2f, 10f),
                new Vector3(-6f, 0.2f, 12f),
                new Vector3(6f, 0.2f, 12f),
                new Vector3(18f, 0.2f, 8f),
                new Vector3(18f, 0.2f, -10f),
                new Vector3(6f, 0.2f, -12f),
                new Vector3(-6f, 0.2f, -10f)
            };

            for (int i = 0; i < wpPositions.Length; i++)
            {
                GameObject wp = new GameObject($"WP_{i + 1:D2}");
                wp.transform.parent = circuitObj.transform;
                wp.transform.position = wpPositions[i];
            }
            circuit.RefreshWaypointsFromChildren();
        }
        else
        {
            circuit = circuitObj.GetComponent<TrackWaypointCircuit>();
        }

        // 4. Assemble Player Car (The Scrapper)
        GameObject playerCar = GameObject.Find("PlayerCar_Container");
        if (playerCar == null)
        {
            playerCar = CreateCarHierarchy("PlayerCar_Container", "RC_Car_Scrapper", new Vector3(-14f, 0.5f, -10f), true, circuit, AIRivalDriftBrain.AIPersonality.PrecisionDrifter);
        }

        // 5. Assemble 3 AI Rival Cars
        string[] aiCarContainers = new string[] { "AICar_Scrapper", "AICar_VoltDozer", "AICar_NeonViper" };
        string[] aiModelNames = new string[] { "RC_Car_Scrapper", "RC_Car_VoltDozer", "RC_Car_NeonViper" };
        AIRivalDriftBrain.AIPersonality[] personalities = new AIRivalDriftBrain.AIPersonality[]
        {
            AIRivalDriftBrain.AIPersonality.PrecisionDrifter,
            AIRivalDriftBrain.AIPersonality.WorkbenchBrawler,
            AIRivalDriftBrain.AIPersonality.ChaosTrickster
        };
        Vector3[] spawnOffsets = new Vector3[]
        {
            new Vector3(-16f, 0.5f, -10f),
            new Vector3(-12f, 0.5f, -10f),
            new Vector3(-10f, 0.5f, -10f)
        };

        for (int i = 0; i < aiCarContainers.Length; i++)
        {
            if (GameObject.Find(aiCarContainers[i]) == null)
            {
                CreateCarHierarchy(aiCarContainers[i], aiModelNames[i], spawnOffsets[i], false, circuit, personalities[i]);
            }
        }

        // 6. Spawn Track Environment Props
        SpawnTrackProp("Prop_Soldering_Iron", new Vector3(0f, 0f, 13f), Quaternion.Euler(0f, 90f, 0f));
        SpawnTrackProp("Prop_Crushed_SodaCan", new Vector3(12f, 0f, -2f), Quaternion.Euler(0f, 45f, 0f));
        SpawnTrackProp("Prop_Hardware_Pack", new Vector3(-8f, 0f, 11f), Quaternion.identity);

        // 7. Setup Foldable Camera Adapter on Main Camera
        Camera mainCam = Camera.main;
        if (mainCam != null)
        {
            FoldableCameraAdapter camAdapter = mainCam.GetComponent<FoldableCameraAdapter>();
            if (camAdapter == null) camAdapter = mainCam.gameObject.AddComponent<FoldableCameraAdapter>();

            Transform playerVisual = playerCar.transform.Find("VisualCarRoot");
            Rigidbody playerRb = playerCar.transform.Find("PhysicsSphere")?.GetComponent<Rigidbody>();

            camAdapter.targetCarVisual = playerVisual;
            camAdapter.targetDriftController = playerVisual != null ? playerVisual.GetComponent<ArcadeDriftController>() : null;
        }

        // 8. Spawn Recovery Claw Drone above track
        if (GameObject.Find("RC_Recovery_ClawDrone") == null)
        {
            GameObject droneModel = LoadModelAsset("RC_Recovery_ClawDrone");
            if (droneModel != null)
            {
                GameObject drone = (GameObject)PrefabUtility.InstantiatePrefab(droneModel);
                drone.name = "RC_Recovery_ClawDrone";
                drone.transform.position = new Vector3(-14f, 6f, -10f);
            }
        }

        // Save Scene to Assets/Scenes/WorkbenchArena.unity
        if (!AssetDatabase.IsValidFolder("Assets/Scenes"))
        {
            AssetDatabase.CreateFolder("Assets", "Scenes");
        }
        UnityEngine.SceneManagement.Scene activeScene = EditorSceneManager.GetActiveScene();
        EditorSceneManager.MarkSceneDirty(activeScene);
        EditorSceneManager.SaveScene(activeScene, "Assets/Scenes/WorkbenchArena.unity");

        // Sync EditorBuildSettings
        UpdateBuildSettingsWithBothScenes();

        Debug.Log("[Pocket Drift] Workbench Scene, 3D Cars, Obstacles, & Foldable Camera successfully assembled and saved to Assets/Scenes/WorkbenchArena.unity!");
        Selection.activeGameObject = playerCar;
    }

    [MenuItem("Tools/Pocket Drift/Auto-Assemble Main Menu Scene")]
    public static void AssembleMainMenuScene()
    {
        // 1. Create a clean scene
        UnityEngine.SceneManagement.Scene menuScene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        // 2. Setup Camera
        GameObject camObj = new GameObject("Main Camera");
        Camera cam = camObj.AddComponent<Camera>();
        camObj.tag = "MainCamera";
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = new Color(0.05f, 0.07f, 0.10f, 1f);
        cam.fieldOfView = 50f;
        camObj.transform.position = new Vector3(0.3f, 1.35f, -3.2f);
        camObj.transform.rotation = Quaternion.Euler(16f, -6f, 0f);
        camObj.AddComponent<AudioListener>();

        // 3. Setup Directional Lighting
        GameObject keyLightObj = new GameObject("Key_Light");
        Light keyLight = keyLightObj.AddComponent<Light>();
        keyLight.type = LightType.Directional;
        keyLight.color = new Color(1f, 0.96f, 0.88f);
        keyLight.intensity = 1.3f;
        keyLightObj.transform.rotation = Quaternion.Euler(45f, -35f, 0f);

        GameObject rimLightObj = new GameObject("Rim_Fill_Light");
        Light rimLight = rimLightObj.AddComponent<Light>();
        rimLight.type = LightType.Directional;
        rimLight.color = new Color(0f, 0.85f, 1f); // Neon Cyan rim
        rimLight.intensity = 0.85f;
        rimLightObj.transform.rotation = Quaternion.Euler(-25f, 140f, 0f);

        // 4. Create 3D Turntable Pedestal with RC Car Showcase (Standby)
        GameObject turntableObj = new GameObject("Vehicle_Turntable_Pedestal");
        turntableObj.transform.position = new Vector3(0.6f, -10f, 0f); // Placed off-screen since 2D hero art is active

        // 5. Setup EventSystem for Gamepad Navigation
        GameObject eventSystemObj = new GameObject("EventSystem");
        eventSystemObj.AddComponent<UnityEngine.EventSystems.EventSystem>();
        eventSystemObj.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();

        // 6. Setup Canvas Hierarchy
        GameObject canvasObj = new GameObject("MainMenu_Canvas");
        Canvas canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;
        canvasObj.AddComponent<GraphicRaycaster>();

        MainMenuController menuController = canvasObj.AddComponent<MainMenuController>();
        menuController.vehicleTurntable = turntableObj.transform;
        menuController.turntableRotationSpeed = 20f;

        Font defaultFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        if (defaultFont == null) defaultFont = Resources.GetBuiltinResource<Font>("Arial.ttf");

        // --- Configure & Load Splash Screen Sprite ---
        string texturePath = "Assets/PocketDrift/Textures/MainMenu_SplashScreen.jpg";
        TextureImporter importer = AssetImporter.GetAtPath(texturePath) as TextureImporter;
        if (importer != null)
        {
            if (importer.textureType != TextureImporterType.Sprite)
            {
                importer.textureType = TextureImporterType.Sprite;
                importer.spriteImportMode = SpriteImportMode.Single;
                importer.SaveAndReimport();
            }
        }
        Sprite splashSprite = AssetDatabase.LoadAssetAtPath<Sprite>(texturePath);

        // --- Background Key Art Slot (Full Screen) ---
        GameObject bgObj = CreateUIObject("Panel_BackgroundKeyArt", canvasObj.transform);
        StretchFull(bgObj);
        Image bgImg = bgObj.AddComponent<Image>();
        if (splashSprite != null)
        {
            bgImg.sprite = splashSprite;
            bgImg.color = Color.white;
        }
        else
        {
            bgImg.color = new Color(0.05f, 0.15f, 0.30f, 1f);
        }
        bgImg.raycastTarget = false;

        // --- Cyber Black Screen UI Container ---
        // Exactly aligned with the black blank spot in the "Pocket Drifters" splash art:
        // X: 53.5% to 91.0% | Y: 22.5% to 65.5%
        Vector2 tabletMin = new Vector2(0.535f, 0.225f);
        Vector2 tabletMax = new Vector2(0.910f, 0.655f);

        // --- Main Buttons Panel (Fitted inside Black Screen) ---
        GameObject mainPanel = CreateUIObject("Panel_MainButtons", canvasObj.transform);
        SetRect(mainPanel, tabletMin, tabletMax, Vector2.zero, Vector2.zero);
        menuController.mainButtonsPanel = mainPanel;

        // Badge Header inside Black Screen
        GameObject badgeObj = CreateUIObject("Text_BadgeHeader", mainPanel.transform);
        Text badgeText = badgeObj.AddComponent<Text>();
        badgeText.font = defaultFont;
        badgeText.text = "⚡  DRIFT OVERDRIVE SERIES  ⚡";
        badgeText.fontSize = 20;
        badgeText.fontStyle = FontStyle.Bold;
        badgeText.alignment = TextAnchor.MiddleCenter;
        badgeText.color = new Color(1f, 0.08f, 0.58f); // Neon Magenta
        SetRect(badgeObj, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 155f), new Vector2(500f, 35f));

        Button playBtn = CreateStyledButton(mainPanel, "Button_Play", "⚡  RACE NOW", new Vector2(0f, 95f), new Vector2(480f, 60f), defaultFont, 25, true);
        Button garageBtn = CreateStyledButton(mainPanel, "Button_Garage", "🔧  GARAGE / CARS", new Vector2(0f, 25f), new Vector2(480f, 54f), defaultFont, 21, false);
        Button settingsBtn = CreateStyledButton(mainPanel, "Button_Settings", "⚙  CONTROLS & SETTINGS", new Vector2(0f, -45f), new Vector2(480f, 54f), defaultFont, 21, false);
        Button quitBtn = CreateStyledButton(mainPanel, "Button_Quit", "🚪  QUIT GAME", new Vector2(0f, -115f), new Vector2(480f, 52f), defaultFont, 20, false);

        ConfigureVerticalNavigation(playBtn, garageBtn, settingsBtn, quitBtn);
        UnityEventTools.AddPersistentListener(playBtn.onClick, menuController.PlayGame);
        UnityEventTools.AddPersistentListener(garageBtn.onClick, menuController.OpenGarage);
        UnityEventTools.AddPersistentListener(settingsBtn.onClick, menuController.OpenSettings);
        UnityEventTools.AddPersistentListener(quitBtn.onClick, menuController.OpenQuitConfirmation);

        menuController.playButton = playBtn;

        // --- Garage Panel (Fitted inside Black Screen) ---
        GameObject garagePanel = CreateUIObject("Panel_Garage", canvasObj.transform);
        SetRect(garagePanel, tabletMin, tabletMax, Vector2.zero, Vector2.zero);
        CreatePanelBackdrop(garagePanel);
        menuController.garagePanel = garagePanel;

        CreateHeaderText(garagePanel, "Text_GarageTitle", "VEHICLE ROSTER & TUNING", new Vector2(0f, 150f), defaultFont);
        CreateContentText(garagePanel, "Text_GarageInfo",
            "• RC Scrapper: Balanced Street Chassis\n  (Top Speed: 24 m/s | Grip: 0.92)\n\n" +
            "• RC Volt-Dozer: Heavyweight Brawler\n  (Mass: 2.2 kg | Kinetic Ramming)\n\n" +
            "• RC Neon Viper: Ultralight Pro-Drifter\n  (Grip: 0.35 | Instant Boost Charge)",
            new Vector2(0f, 25f), new Vector2(520f, 190f), defaultFont);

        Button garageBackBtn = CreateStyledButton(garagePanel, "Button_GarageBack", "◀ BACK TO MENU", new Vector2(0f, -145f), new Vector2(320f, 50f), defaultFont, 20, false);
        UnityEventTools.AddPersistentListener(garageBackBtn.onClick, menuController.CloseSubPanel);
        menuController.garageFirstButton = garageBackBtn;

        // --- Settings Panel (Fitted inside Black Screen) ---
        GameObject settingsPanel = CreateUIObject("Panel_Settings", canvasObj.transform);
        SetRect(settingsPanel, tabletMin, tabletMax, Vector2.zero, Vector2.zero);
        CreatePanelBackdrop(settingsPanel);
        menuController.settingsPanel = settingsPanel;

        CreateHeaderText(settingsPanel, "Text_SettingsTitle", "RAZER WOLVERINE TE CONTROLS", new Vector2(0f, 155f), defaultFont);
        CreateContentText(settingsPanel, "Text_ControlsGuide",
            "• Left Stick / D-Pad: Analog Steering (12% deadzone)\n" +
            "• Right Trigger (RT): Throttle (Hair-Trigger Ready)\n" +
            "• Left Trigger (LT): Brake & Reverse\n" +
            "• Right Bumper (RB) / Space: Drift & Turbo Boost\n" +
            "• Button X / Button B: Fire Modular Weapon\n" +
            "• View / Back: Quick Table Reset & Respawn",
            new Vector2(0f, 30f), new Vector2(540f, 200f), defaultFont);

        Button settingsBackBtn = CreateStyledButton(settingsPanel, "Button_SettingsBack", "◀ BACK TO MENU", new Vector2(0f, -145f), new Vector2(320f, 50f), defaultFont, 20, false);
        UnityEventTools.AddPersistentListener(settingsBackBtn.onClick, menuController.CloseSubPanel);
        menuController.settingsFirstButton = settingsBackBtn;

        // --- Quit Confirmation Panel (Fitted inside Black Screen) ---
        GameObject quitPanel = CreateUIObject("Panel_QuitConfirm", canvasObj.transform);
        SetRect(quitPanel, tabletMin, tabletMax, Vector2.zero, Vector2.zero);
        CreatePanelBackdrop(quitPanel);
        menuController.quitConfirmPanel = quitPanel;

        CreateHeaderText(quitPanel, "Text_QuitTitle", "EXIT TO DESKTOP?", new Vector2(0f, 50f), defaultFont);
        Button quitYesBtn = CreateStyledButton(quitPanel, "Button_QuitYes", "YES, EXIT", new Vector2(-110f, -40f), new Vector2(180f, 52f), defaultFont, 19, false);
        Button quitNoBtn = CreateStyledButton(quitPanel, "Button_QuitNo", "CANCEL", new Vector2(110f, -40f), new Vector2(180f, 52f), defaultFont, 19, false);

        UnityEventTools.AddPersistentListener(quitYesBtn.onClick, menuController.ConfirmQuit);
        UnityEventTools.AddPersistentListener(quitNoBtn.onClick, menuController.CloseSubPanel);
        menuController.quitConfirmYesButton = quitYesBtn;

        // --- Footer Controller Status HUD ---
        GameObject footerObj = CreateUIObject("Panel_FooterHUD", canvasObj.transform);
        SetRect(footerObj, new Vector2(0.12f, 0.012f), new Vector2(0.72f, 0.065f), Vector2.zero, Vector2.zero);

        GameObject statusTextObj = CreateUIObject("Text_ControllerStatus", footerObj.transform);
        Text statusText = statusTextObj.AddComponent<Text>();
        statusText.font = defaultFont;
        statusText.fontSize = 18;
        statusText.fontStyle = FontStyle.Bold;
        statusText.alignment = TextAnchor.MiddleLeft;
        statusText.color = new Color(0f, 1f, 0.85f);
        statusText.text = "<color=#00FFAA>[CONNECTED]</color> Razer Wolverine TE Active";
        SetRect(statusTextObj, new Vector2(0f, 0f), new Vector2(0.48f, 1f), Vector2.zero, Vector2.zero);
        menuController.controllerStatusText = statusText;

        GameObject promptTextObj = CreateUIObject("Text_InputPrompts", footerObj.transform);
        Text promptText = promptTextObj.AddComponent<Text>();
        promptText.font = defaultFont;
        promptText.fontSize = 17;
        promptText.alignment = TextAnchor.MiddleRight;
        promptText.color = new Color(0.95f, 0.85f, 1f);
        promptText.text = "[A] Select   [B] Back   [D-Pad / L-Stick] Navigate   [RT] Gas";
        SetRect(promptTextObj, new Vector2(0.5f, 0f), new Vector2(1f, 1f), Vector2.zero, Vector2.zero);
        menuController.inputPromptText = promptText;

        // --- Loading Overlay ---
        GameObject loadOverlay = CreateUIObject("Panel_LoadingOverlay", canvasObj.transform);
        StretchFull(loadOverlay);
        Image loadBg = loadOverlay.AddComponent<Image>();
        loadBg.color = new Color(0.02f, 0.03f, 0.05f, 0.95f);
        menuController.loadingOverlay = loadOverlay;

        GameObject loadTextObj = CreateUIObject("Text_LoadingTitle", loadOverlay.transform);
        Text loadText = loadTextObj.AddComponent<Text>();
        loadText.font = defaultFont;
        loadText.fontSize = 32;
        loadText.fontStyle = FontStyle.Bold;
        loadText.alignment = TextAnchor.MiddleCenter;
        loadText.color = new Color(0f, 0.95f, 1f);
        loadText.text = "LAUNCHING WORKBENCH ARENA...";
        SetRect(loadTextObj, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 40f), new Vector2(800f, 60f));

        loadOverlay.SetActive(false);
        garagePanel.SetActive(false);
        settingsPanel.SetActive(false);
        quitPanel.SetActive(false);

        // 7. Save Scene to Assets/Scenes/MainMenu.unity
        if (!AssetDatabase.IsValidFolder("Assets/Scenes"))
        {
            AssetDatabase.CreateFolder("Assets", "Scenes");
        }
        EditorSceneManager.MarkSceneDirty(menuScene);
        EditorSceneManager.SaveScene(menuScene, "Assets/Scenes/MainMenu.unity");

        // 8. Register both scenes in EditorBuildSettings
        UpdateBuildSettingsWithBothScenes();

        Debug.Log("[Pocket Drift] Main Menu Scene with Razer Wolverine TE navigation assembled & saved to Assets/Scenes/MainMenu.unity!");
    }

    private static void UpdateBuildSettingsWithBothScenes()
    {
        EditorBuildSettingsScene[] scenes = new EditorBuildSettingsScene[]
        {
            new EditorBuildSettingsScene("Assets/Scenes/MainMenu.unity", true),
            new EditorBuildSettingsScene("Assets/Scenes/WorkbenchArena.unity", true)
        };
        EditorBuildSettings.scenes = scenes;
    }

    private static GameObject CreateUIObject(string name, Transform parent)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.AddComponent<RectTransform>();
        return go;
    }

    private static void StretchFull(GameObject go)
    {
        RectTransform rt = go.GetComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
    }

    private static void SetRect(GameObject go, Vector2 anchorMin, Vector2 anchorMax, Vector2 anchoredPos, Vector2 sizeDelta)
    {
        RectTransform rt = go.GetComponent<RectTransform>();
        rt.anchorMin = anchorMin;
        rt.anchorMax = anchorMax;
        rt.anchoredPosition = anchoredPos;
        rt.sizeDelta = sizeDelta;
    }

    private static void CreatePanelBackdrop(GameObject panel)
    {
        Image img = panel.AddComponent<Image>();
        img.color = new Color(0.06f, 0.02f, 0.09f, 0.97f);
    }

    private static void CreateHeaderText(GameObject parent, string name, string text, Vector2 pos, Font font)
    {
        GameObject go = CreateUIObject(name, parent.transform);
        Text t = go.AddComponent<Text>();
        t.font = font;
        t.text = text;
        t.fontSize = 22;
        t.fontStyle = FontStyle.Bold;
        t.alignment = TextAnchor.MiddleCenter;
        t.color = new Color(1f, 0.08f, 0.58f); // Neon Magenta Header
        SetRect(go, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), pos, new Vector2(480f, 40f));
    }

    private static void CreateContentText(GameObject parent, string name, string text, Vector2 pos, Vector2 size, Font font)
    {
        GameObject go = CreateUIObject(name, parent.transform);
        Text t = go.AddComponent<Text>();
        t.font = font;
        t.text = text;
        t.fontSize = 16;
        t.lineSpacing = 1.25f;
        t.alignment = TextAnchor.MiddleLeft;
        t.color = new Color(0.95f, 0.90f, 1f);
        SetRect(go, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), pos, size);
    }

    private static Button CreateStyledButton(GameObject parent, string name, string label, Vector2 pos, Vector2 size, Font font, int fontSize, bool isHighlight)
    {
        GameObject btnObj = CreateUIObject(name, parent.transform);
        SetRect(btnObj, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), pos, size);

        Image btnImg = btnObj.AddComponent<Image>();
        btnImg.color = isHighlight ? new Color(0.20f, 0.04f, 0.18f, 0.95f) : new Color(0.08f, 0.03f, 0.10f, 0.90f);

        Button btn = btnObj.AddComponent<Button>();
        ColorBlock colors = btn.colors;
        colors.normalColor = isHighlight ? new Color(0.25f, 0.05f, 0.22f, 1f) : new Color(0.12f, 0.04f, 0.14f, 1f);
        colors.highlightedColor = new Color(1f, 0.08f, 0.58f, 1f); // Glowing Neon Magenta on hover / focus
        colors.selectedColor = new Color(1f, 0.08f, 0.58f, 1f);
        colors.pressedColor = new Color(0f, 0.95f, 1f, 1f);       // Electric Cyan flash
        btn.colors = colors;

        GameObject textObj = CreateUIObject("Text", btnObj.transform);
        StretchFull(textObj);
        Text txt = textObj.AddComponent<Text>();
        txt.font = font;
        txt.text = label;
        txt.fontSize = fontSize;
        txt.fontStyle = FontStyle.Bold;
        txt.alignment = TextAnchor.MiddleCenter;
        txt.color = Color.white;

        return btn;
    }

    private static void ConfigureVerticalNavigation(params Button[] buttons)
    {
        for (int i = 0; i < buttons.Length; i++)
        {
            Navigation nav = new Navigation();
            nav.mode = Navigation.Mode.Explicit;
            nav.selectOnUp = buttons[(i - 1 + buttons.Length) % buttons.Length];
            nav.selectOnDown = buttons[(i + 1) % buttons.Length];
            buttons[i].navigation = nav;
        }
    }

    private static GameObject CreateCarHierarchy(string carName, string modelName, Vector3 spawnPos, bool isPlayer, TrackWaypointCircuit circuit, AIRivalDriftBrain.AIPersonality personality)
    {
        GameObject container = new GameObject(carName);
        container.transform.position = spawnPos;

        // Physics Sphere
        GameObject sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        sphere.name = "PhysicsSphere";
        sphere.transform.parent = container.transform;
        sphere.transform.localPosition = new Vector3(0f, 0.4f, 0f);
        sphere.transform.localScale = Vector3.one * 0.8f;

        MeshRenderer sphereRend = sphere.GetComponent<MeshRenderer>();
        if (sphereRend != null) sphereRend.enabled = false;

        Rigidbody rb = sphere.GetComponent<Rigidbody>();
        if (rb == null) rb = sphere.AddComponent<Rigidbody>();
        rb.mass = modelName.Contains("VoltDozer") ? 2.2f : (modelName.Contains("NeonViper") ? 1.1f : 1.4f);
        rb.linearDamping = 3.5f;
        rb.angularDamping = 1f;
        rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;

        // Visual Car Root
        GameObject visualRoot = new GameObject("VisualCarRoot");
        visualRoot.transform.parent = container.transform;
        visualRoot.transform.localPosition = Vector3.zero;

        // Core Drift Controller
        ArcadeDriftController controller = visualRoot.AddComponent<ArcadeDriftController>();
        controller.sphereRb = rb;
        controller.visualRoot = visualRoot.transform;

        // Instantiate 3D Model if available
        GameObject modelPrefab = LoadModelAsset(modelName);
        Transform roofMount = null;
        Transform rearMount = null;

        if (modelPrefab != null)
        {
            GameObject modelInstance = (GameObject)PrefabUtility.InstantiatePrefab(modelPrefab);
            modelInstance.name = modelName;
            modelInstance.transform.parent = visualRoot.transform;
            modelInstance.transform.localPosition = Vector3.zero;
            modelInstance.transform.localRotation = Quaternion.identity;

            // Connect wheels to controller
            Transform fl = modelInstance.transform.Find("Wheel_FL");
            Transform fr = modelInstance.transform.Find("Wheel_FR");
            if (fl != null) controller.frontLeftWheel = fl;
            if (fr != null) controller.frontRightWheel = fr;

            // Connect weapon mounts
            roofMount = modelInstance.transform.Find("Socket_Roof_Weapon");
            rearMount = modelInstance.transform.Find("Socket_Rear_Utility");
        }
        else
        {
            // Fallback primitive chassis
            GameObject chassis = GameObject.CreatePrimitive(PrimitiveType.Cube);
            chassis.name = "ChassisMesh";
            chassis.transform.parent = visualRoot.transform;
            chassis.transform.localPosition = new Vector3(0f, 0.3f, 0f);
            chassis.transform.localScale = new Vector3(1.2f, 0.45f, 2.2f);
            Collider chassisCol = chassis.GetComponent<Collider>();
            if (chassisCol != null) Object.DestroyImmediate(chassisCol);
        }

        // Ensure mounts exist
        if (roofMount == null)
        {
            GameObject sRoof = new GameObject("Socket_Roof_Weapon");
            sRoof.transform.parent = visualRoot.transform;
            sRoof.transform.localPosition = new Vector3(0f, 0.55f, 0.1f);
            roofMount = sRoof.transform;
        }
        if (rearMount == null)
        {
            GameObject sRear = new GameObject("Socket_Rear_Utility");
            sRear.transform.parent = visualRoot.transform;
            sRear.transform.localPosition = new Vector3(0f, 0.3f, -1.1f);
            rearMount = sRear.transform;
        }

        // Weapon Inventory
        WeaponInventoryManager weaponInv = visualRoot.AddComponent<WeaponInventoryManager>();
        weaponInv.roofMountPoint = roofMount;
        weaponInv.rearMountPoint = rearMount;

        // Knockback Collision Handler
        KnockbackCollisionHandler knockback = sphere.AddComponent<KnockbackCollisionHandler>();
        knockback.myDriftController = controller;
        knockback.mySphereRb = rb;

        if (isPlayer)
        {
            CrossPlatformInputManager inputMgr = visualRoot.AddComponent<CrossPlatformInputManager>();
            inputMgr.targetCar = controller;
            inputMgr.weaponInventory = weaponInv;
        }
        else
        {
            AIRivalDriftBrain aiBrain = visualRoot.AddComponent<AIRivalDriftBrain>();
            aiBrain.trackCircuit = circuit;
            aiBrain.personality = personality;
        }

        return container;
    }

    private static GameObject LoadModelAsset(string modelName)
    {
        string[] guids = AssetDatabase.FindAssets($"{modelName} t:Model");
        if (guids != null && guids.Length > 0)
        {
            string path = AssetDatabase.GUIDToAssetPath(guids[0]);
            return AssetDatabase.LoadAssetAtPath<GameObject>(path);
        }
        return null;
    }

    private static void SpawnTrackProp(string propName, Vector3 pos, Quaternion rot)
    {
        if (GameObject.Find(propName) != null) return;

        GameObject model = LoadModelAsset(propName);
        if (model != null)
        {
            GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(model);
            instance.name = propName;
            instance.transform.position = pos;
            instance.transform.rotation = rot;
            AddMeshCollidersRecursive(instance);
        }
    }

    private static void AddMeshCollidersRecursive(GameObject root)
    {
        MeshFilter[] filters = root.GetComponentsInChildren<MeshFilter>();
        foreach (var mf in filters)
        {
            if (mf.gameObject.GetComponent<Collider>() == null && mf.sharedMesh != null)
            {
                MeshCollider col = mf.gameObject.AddComponent<MeshCollider>();
                col.convex = false;
            }
        }
    }
}
#endif
