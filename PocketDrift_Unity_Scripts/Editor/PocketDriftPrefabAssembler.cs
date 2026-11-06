#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
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

        // Add to EditorBuildSettings
        EditorBuildSettingsScene[] scenes = new EditorBuildSettingsScene[]
        {
            new EditorBuildSettingsScene("Assets/Scenes/WorkbenchArena.unity", true)
        };
        EditorBuildSettings.scenes = scenes;

        Debug.Log("[Pocket Drift] Workbench Scene, 3D Cars, Obstacles, & Foldable Camera successfully assembled and saved to Assets/Scenes/WorkbenchArena.unity!");
        Selection.activeGameObject = playerCar;
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
