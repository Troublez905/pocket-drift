#if UNITY_EDITOR
using UnityEditor;
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
            table = GameObject.CreatePrimitive(PrimitiveType.Cube);
            table.name = "Workbench_Table";
            table.transform.position = new Vector3(0f, -0.5f, 0f);
            table.transform.localScale = new Vector3(50f, 1f, 35f);
        }

        // 2. Create TableEdgeFallTrigger Plane Below
        GameObject fallTriggerObj = GameObject.Find("TableEdgeFallTrigger");
        if (fallTriggerObj == null)
        {
            fallTriggerObj = new GameObject("TableEdgeFallTrigger");
            fallTriggerObj.transform.position = new Vector3(0f, -6f, 0f);
            BoxCollider box = fallTriggerObj.AddComponent<BoxCollider>();
            box.isTrigger = true;
            box.size = new Vector3(120f, 2f, 120f);
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
            circuit.RefreshWaypoints();
        }
        else
        {
            circuit = circuitObj.GetComponent<TrackWaypointCircuit>();
        }

        // 4. Assemble Player Car
        GameObject playerCar = GameObject.Find("PlayerCar_Container");
        if (playerCar == null)
        {
            playerCar = CreateCarHierarchy("PlayerCar_Container", new Vector3(-14f, 0.5f, -10f), true, circuit, AIRivalDriftBrain.AIPersonality.PrecisionDrifter);
        }

        // 5. Assemble 3 AI Rival Cars
        string[] aiNames = new string[] { "AICar_Scrapper", "AICar_VoltDozer", "AICar_NeonViper" };
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

        for (int i = 0; i < aiNames.Length; i++)
        {
            if (GameObject.Find(aiNames[i]) == null)
            {
                CreateCarHierarchy(aiNames[i], spawnOffsets[i], false, circuit, personalities[i]);
            }
        }

        // 6. Setup Foldable Camera Adapter on Main Camera
        Camera mainCam = Camera.main;
        if (mainCam != null)
        {
            FoldableCameraAdapter camAdapter = mainCam.GetComponent<FoldableCameraAdapter>();
            if (camAdapter == null) camAdapter = mainCam.gameObject.AddComponent<FoldableCameraAdapter>();

            Transform playerVisual = playerCar.transform.Find("VisualCarRoot");
            Rigidbody playerRb = playerCar.transform.Find("PhysicsSphere")?.GetComponent<Rigidbody>();

            camAdapter.targetCar = playerVisual;
            camAdapter.targetSphereRb = playerRb;
        }

        Debug.Log("[Pocket Drift] Workbench Scene, Player Car, 3 AI Rivals, Circuit, & Foldable Camera successfully assembled!");
        Selection.activeGameObject = playerCar;
    }

    private static GameObject CreateCarHierarchy(string carName, Vector3 spawnPos, bool isPlayer, TrackWaypointCircuit circuit, AIRivalDriftBrain.AIPersonality personality)
    {
        GameObject container = new GameObject(carName);
        container.transform.position = spawnPos;

        // Physics Sphere
        GameObject sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        sphere.name = "PhysicsSphere";
        sphere.transform.parent = container.transform;
        sphere.transform.localPosition = new Vector3(0f, 0.4f, 0f);
        sphere.transform.localScale = Vector3.one * 0.8f;

        // Hide mesh renderer of physics sphere (invisible physics collider)
        MeshRenderer sphereRend = sphere.GetComponent<MeshRenderer>();
        if (sphereRend != null) sphereRend.enabled = false;

        Rigidbody rb = sphere.GetComponent<Rigidbody>();
        if (rb == null) rb = sphere.AddComponent<Rigidbody>();
        rb.mass = 1.4f;
        rb.linearDamping = 3.5f;
        rb.angularDamping = 1f;
        rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;

        // Visual Car Root
        GameObject visualRoot = new GameObject("VisualCarRoot");
        visualRoot.transform.parent = container.transform;
        visualRoot.transform.localPosition = Vector3.zero;

        // Chassis body primitive placeholder
        GameObject chassis = GameObject.CreatePrimitive(PrimitiveType.Cube);
        chassis.name = "ChassisMesh";
        chassis.transform.parent = visualRoot.transform;
        chassis.transform.localPosition = new Vector3(0f, 0.3f, 0f);
        chassis.transform.localScale = new Vector3(1.2f, 0.45f, 2.2f);
        Collider chassisCol = chassis.GetComponent<Collider>();
        if (chassisCol != null) Object.DestroyImmediate(chassisCol);

        // Weapon Mounts
        GameObject roofMount = new GameObject("Socket_Roof_Weapon");
        roofMount.transform.parent = visualRoot.transform;
        roofMount.transform.localPosition = new Vector3(0f, 0.55f, 0.1f);

        GameObject rearMount = new GameObject("Socket_Rear_Utility");
        rearMount.transform.parent = visualRoot.transform;
        rearMount.transform.localPosition = new Vector3(0f, 0.3f, -1.1f);

        // Core Drift Controller
        ArcadeDriftController controller = visualRoot.AddComponent<ArcadeDriftController>();
        controller.sphereRb = rb;
        controller.visualRoot = visualRoot.transform;

        // Weapon Inventory
        WeaponInventoryManager weaponInv = visualRoot.AddComponent<WeaponInventoryManager>();
        weaponInv.roofMountPoint = roofMount.transform;
        weaponInv.rearMountPoint = rearMount.transform;

        // Knockback Collision Handler
        KnockbackCollisionHandler knockback = sphere.AddComponent<KnockbackCollisionHandler>();
        knockback.ownerCarController = controller;

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
}
#endif
