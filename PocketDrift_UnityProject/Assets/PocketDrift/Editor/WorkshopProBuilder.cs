#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using UnityEngine.Rendering.Universal;

public static class WorkshopProBuilder
{
    private const string Art = "Assets/PocketDrift/ArtPro/";
    private const string ScenePath = "Assets/Scenes/WorkshopPro.unity";
    [Serializable] private class CircuitData { public Point[] points; }
    [Serializable] private class Point { public float x; public float z; }

    [MenuItem("Tools/Pocket Drift/Build Workshop Pro")]
    public static void Build()
    {
        if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
        AssetDatabase.Refresh();
        var pipeline = AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>(Art + "WorkshopPipeline.asset");
        if (pipeline == null)
        {
            var renderer = ScriptableObject.CreateInstance<UniversalRendererData>();
            AssetDatabase.CreateAsset(renderer, Art + "WorkshopRenderer.asset");
            pipeline = UniversalRenderPipelineAsset.Create(renderer);
            pipeline.msaaSampleCount = 4;
            pipeline.shadowDistance = 90;
            AssetDatabase.CreateAsset(pipeline, Art + "WorkshopPipeline.asset");
        }
        GraphicsSettings.defaultRenderPipeline = pipeline;
        // The current editor/GPU combination corrupts material colors with SRP batching.
        pipeline.useSRPBatcher = false;
        EditorUtility.SetDirty(pipeline);
        QualitySettings.renderPipeline = pipeline;
        string[] names = { "Pro_WorkshopCircuit", "Pro_Scrapper", "Pro_VoltDozer", "Pro_NeonViper" };
        foreach (string name in names)
        {
            var importer = AssetImporter.GetAtPath(Art + name + ".fbx") as ModelImporter;
            if (importer == null) throw new InvalidOperationException("Generate Blender assets first: " + name);
            importer.materialImportMode = ModelImporterMaterialImportMode.ImportStandard;
            importer.importCameras = false;
            importer.importLights = false;
            importer.SaveAndReimport();
        }

        var scene = EditorSceneManager.OpenScene("Assets/Scenes/WorkbenchArena.unity", OpenSceneMode.Single);
        // Work on a separate scene; the original arena remains the rebuild source.
        EditorSceneManager.SaveScene(scene, ScenePath);
        foreach (var go in scene.GetRootGameObjects())
        {
            if (go.name == "Workbench_Table" || go.name.StartsWith("Prop_") || go.name == "RC_Recovery_ClawDrone")
                UnityEngine.Object.DestroyImmediate(go);
        }
        var environment = Instantiate("Pro_WorkshopCircuit");
        environment.transform.localScale = new Vector3(-1,1,1);
        ApplyMaterials(environment);
        foreach (var t in environment.GetComponentsInChildren<Transform>())
            if (t.name == "Table slab") t.localPosition += Vector3.down * .025f;
        var ground = new GameObject("Driveable tabletop");
        ground.transform.SetParent(environment.transform);
        var collider = ground.AddComponent<BoxCollider>();
        collider.center = new Vector3(0, -.55f, 0);
        collider.size = new Vector3(56, 1.1f, 40);
        foreach (var mesh in environment.GetComponentsInChildren<MeshFilter>())
        {
            if (mesh.name.StartsWith("Safety block") || mesh.name == "Toolbox" || mesh.name.StartsWith("Gantry pillar") ||
                mesh.name.StartsWith("Circuit safety rail") || mesh.name.StartsWith("Pit garage"))
            {
                var obstacle = mesh.gameObject.AddComponent<MeshCollider>();
                obstacle.sharedMesh = mesh.sharedMesh;
            }
        }

        var data = JsonUtility.FromJson<CircuitData>(File.ReadAllText(Art + "circuit.json"));
        if (data.points == null || data.points.Length < 3) throw new InvalidDataException("Circuit requires ordered points.");
        var circuit = UnityEngine.Object.FindAnyObjectByType<TrackWaypointCircuit>();
        if (circuit == null) circuit = new GameObject("TrackWaypoints_Container").AddComponent<TrackWaypointCircuit>();
        for (int i = circuit.transform.childCount - 1; i >= 0; i--)
            UnityEngine.Object.DestroyImmediate(circuit.transform.GetChild(i).gameObject);
        circuit.waypoints.Clear();
        foreach (var p in data.points)
        {
            var wp = new GameObject("Checkpoint " + circuit.waypoints.Count.ToString("D2"));
            wp.transform.SetParent(circuit.transform);
            wp.transform.position = new Vector3(p.x, .25f, p.z);
            circuit.waypoints.Add(wp.transform);
        }
        circuit.waypointReachRadius = 3.7f;
        var cars = UnityEngine.Object.FindObjectsByType<ArcadeDriftController>(FindObjectsSortMode.None)
            .OrderBy(c => c.GetComponent<CrossPlatformInputManager>() == null ? 1 : 0).ToArray();
        Vector3 start = circuit.GetWaypoint(0).position;
        Vector3 direction = (circuit.GetWaypoint(1).position - start).normalized;
        Vector3 right = Vector3.Cross(Vector3.up, direction);
        for (int i = 0; i < cars.Length; i++)
        {
            var car = cars[i];
            string model = car.transform.parent.name.Contains("VoltDozer") ? "Pro_VoltDozer" :
                car.transform.parent.name.Contains("NeonViper") ? "Pro_NeonViper" : "Pro_Scrapper";
            // Keep gameplay components and weapon sockets; replace only model geometry.
            foreach (Transform child in car.transform.Cast<Transform>().ToArray())
                if (child.name.StartsWith("RC_Car_") || child.name == "ChassisMesh") UnityEngine.Object.DestroyImmediate(child.gameObject);
            var body = Instantiate(model);
            body.transform.SetParent(car.transform, false);
            body.transform.localScale = new Vector3(-1,1,1);
            ApplyMaterials(body);
            car.visualRoot = body.transform;
            car.frontLeftWheel = Find(body, "Wheel_FL");
            car.frontRightWheel = Find(body, "Wheel_FR");
            var inventory = car.GetComponent<WeaponInventoryManager>();
            if (inventory != null)
            {
                inventory.roofMountPoint = Find(body, "Socket_Roof_Weapon");
                inventory.rearMountPoint = Find(body, "Socket_Rear_Utility");
            }
            Vector3 gridCenter = GridPosition(circuit, 2f + (i / 2) * 3.4f, out Vector3 gridDirection);
            Vector3 gridRight = Vector3.Cross(Vector3.up, gridDirection);
            Vector3 spawn = gridCenter + gridRight * (i % 2 == 0 ? -1.05f : 1.05f);
            spawn.y = .1f;
            car.transform.SetPositionAndRotation(spawn, Quaternion.LookRotation(gridDirection));
            car.sphereRb.transform.position = spawn + Vector3.up * .4f;
            car.sphereRb.interpolation = RigidbodyInterpolation.Interpolate;
            car.sphereRb.gameObject.layer = 2; // Exclude car spheres from ground raycasts.
            car.groundLayer = 1;
            var ai = car.GetComponent<AIRivalDriftBrain>();
            if (ai != null) { ai.trackCircuit = circuit; ai.groundLayer = 1; ai.waypointAdvanceDistance = 3.5f; }
            car.rearSkidTrails = new TrailRenderer[2];
            for (int side = 0; side < 2; side++)
            {
                var skid = new GameObject("Rear tire trail");
                skid.transform.SetParent(body.transform, false);
                skid.transform.localPosition = new Vector3(side == 0 ? -.72f : .72f, -.025f, -.77f);
                var trail = skid.AddComponent<TrailRenderer>();
                trail.sharedMaterial = MaterialFor("Rubber");
                trail.startWidth = .14f; trail.endWidth = .11f; trail.time = 2.5f;
                trail.minVertexDistance = .1f; trail.emitting = false;
                car.rearSkidTrails[side] = trail;
            }
        }
        var race = UnityEngine.Object.FindAnyObjectByType<RaceManager>();
        if (race == null) race = new GameObject("Race Manager").AddComponent<RaceManager>();
        race.circuit = circuit; race.racers = cars.ToList();

        foreach (var light in UnityEngine.Object.FindObjectsByType<Light>(FindObjectsSortMode.None))
            UnityEngine.Object.DestroyImmediate(light.gameObject);
        var key = new GameObject("Warm workshop key").AddComponent<Light>();
        key.type = LightType.Directional; key.color = new Color(1, .89f, .73f); key.intensity = 1.65f;
        key.shadows = LightShadows.Soft; key.transform.rotation = Quaternion.Euler(48,-28,0);
        var fill = new GameObject("Cool skylight fill").AddComponent<Light>();
        fill.type = LightType.Directional; fill.color = new Color(.55f,.75f,1); fill.intensity = .45f;
        fill.transform.rotation = Quaternion.Euler(35,145,0);
        RenderSettings.ambientMode = AmbientMode.Trilight;
        RenderSettings.ambientSkyColor = new Color(.42f,.49f,.55f);
        RenderSettings.ambientEquatorColor = new Color(.28f,.32f,.35f);
        RenderSettings.ambientGroundColor = new Color(.19f,.16f,.13f);
        RenderSettings.fog = false;
        var camera = Camera.main;
        if (camera == null)
        {
            camera = new GameObject("Main Camera").AddComponent<Camera>();
            camera.tag = "MainCamera";
            camera.gameObject.AddComponent<AudioListener>();
        }
        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.backgroundColor = new Color(.075f,.105f,.13f);
        camera.farClipPlane = 200;
        var follow = camera.GetComponent<FoldableCameraAdapter>();
        if (follow == null) follow = camera.gameObject.AddComponent<FoldableCameraAdapter>();
        follow.targetCarVisual = cars[0].transform;
        follow.targetDriftController = cars[0];
        follow.baseCameraOffset = new Vector3(0,18,-15);
        camera.transform.position = cars[0].transform.position + follow.baseCameraOffset;
        camera.transform.LookAt(cars[0].transform.position);
        camera.fieldOfView = 48;
        EditorSceneManager.SaveScene(scene, ScenePath);

        var menuScene = EditorSceneManager.OpenScene("Assets/Scenes/MainMenu.unity", OpenSceneMode.Single);
        var menu = UnityEngine.Object.FindAnyObjectByType<MainMenuController>();
        if (menu != null) { menu.gameSceneName = "WorkshopPro"; EditorSceneManager.SaveScene(menuScene); }
        EditorBuildSettings.scenes = new[] {
            new EditorBuildSettingsScene("Assets/Scenes/MainMenu.unity", true),
            new EditorBuildSettingsScene(ScenePath, true),
            new EditorBuildSettingsScene("Assets/Scenes/WorkbenchArena.unity", false)
        };
        EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        AssetDatabase.SaveAssets();
        Debug.Log("WORKSHOP_PRO_BUILD_OK: " + cars.Length + " cars; " + data.points.Length + " checkpoints.");
    }

    public static void ValidateAndRender()
    {
        EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        SessionState.SetBool("WorkshopProCapture", true);
        EditorApplication.isPlaying = true;
    }

    [InitializeOnLoadMethod]
    private static void RegisterPlayModeCapture()
    {
        EditorApplication.playModeStateChanged += state =>
        {
            if (state != PlayModeStateChange.EnteredPlayMode || !SessionState.GetBool("WorkshopProCapture", false)) return;
            SessionState.SetBool("WorkshopProCapture", false);
            Time.timeScale = 0;
            RunValidation();
        };
    }

    private static void RunValidation()
    {
        ShaderUtil.allowAsyncCompilation = false;
        int frames = 0;
        EditorApplication.CallbackFunction capture = null;
        capture = () =>
        {
            if (++frames < 40) return;
            EditorApplication.update -= capture;
            try
            {
                foreach (var renderer in UnityEngine.Object.FindObjectsByType<Renderer>(FindObjectsSortMode.None))
                    foreach (var material in renderer.sharedMaterials)
                        if (material == null || material.shader == null || !material.shader.isSupported)
                            throw new InvalidOperationException("Invalid material on " + renderer.name);
                var circuit = UnityEngine.Object.FindAnyObjectByType<TrackWaypointCircuit>();
                var toolbox = GameObject.Find("Pro_WorkshopCircuit").GetComponentsInChildren<Renderer>().First(r => r.name == "Toolbox");
                if (Mathf.Abs(toolbox.bounds.center.x - 6) > .1f) throw new InvalidOperationException("FBX axis mismatch");
                if (circuit.waypoints.Count != 22) throw new InvalidOperationException("Checkpoint count mismatch");
                RenderPreview();
                Debug.Log("WORKSHOP_PRO_VALIDATION_OK");
                EditorApplication.Exit(0);
            }
            catch (Exception error)
            {
                Debug.LogException(error);
                EditorApplication.Exit(1);
            }
        };
        EditorApplication.update += capture;
    }

    private static GameObject Instantiate(string name)
    {
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(Art + name + ".fbx");
        if (prefab == null) throw new InvalidOperationException("Missing generated model " + name);
        return (GameObject)PrefabUtility.InstantiatePrefab(prefab);
    }
    private static Vector3 GridPosition(TrackWaypointCircuit circuit, float distance, out Vector3 forward)
    {
        Vector3 end = circuit.GetWaypoint(0).position;
        for (int i = circuit.waypoints.Count - 1; i >= 0; i--)
        {
            Vector3 begin = circuit.GetWaypoint(i).position;
            float length = Vector3.Distance(begin, end);
            if (length > .001f && distance <= length)
            {
                forward = (end - begin).normalized;
                return end - forward * distance;
            }
            distance -= length;
            end = begin;
        }
        throw new InvalidOperationException("Starting grid exceeds circuit length.");
    }
    private static Transform Find(GameObject root, string name) => root.GetComponentsInChildren<Transform>().FirstOrDefault(t => t.name == name);
    private static void ApplyMaterials(GameObject root)
    {
        foreach (var renderer in root.GetComponentsInChildren<Renderer>())
            renderer.sharedMaterials = renderer.sharedMaterials.Select(m => MaterialFor(m.name)).ToArray();
    }
    private static Material MaterialFor(string name)
    {
        string path = Art + name + ".mat";
        var material = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (material != null) return material;
        string[] names = {"Graphite","Rubber","Alloy","Orange","Yellow","Teal","Ivory","Red","Glass","Wood","WoodLight","Road","Mat","Copper","Lamp"};
        string[] hex = {"202B32","10181D","9AAEB5","F47732","F3C64C","36B9AD","E9E3D1","D74C43","183D4D","846043","967050","35434A","305951","B97B4E","C0F1EF"};
        int index = Array.IndexOf(names,name);
        if (index < 0) throw new InvalidOperationException("Unknown art material: " + name);
        ColorUtility.TryParseHtmlString("#" + hex[index], out Color color);
        var shader = Shader.Find("Universal Render Pipeline/Lit");
        if (shader == null) throw new InvalidOperationException("URP Lit shader unavailable.");
        material = new Material(shader) { name = name };
        material.SetColor("_BaseColor", color);
        material.SetFloat("_Metallic", name == "Alloy" || name == "Copper" ? .75f : .1f);
        material.SetFloat("_Smoothness", name == "Glass" ? .8f : name == "Road" || name.StartsWith("Wood") ? .15f : .4f);
        // Thin road ribbons need to render from either side after FBX axis conversion.
        material.SetFloat("_Cull", 0);
        if (name == "Lamp") { material.EnableKeyword("_EMISSION"); material.SetColor("_EmissionColor",color * 1.2f); }
        AssetDatabase.CreateAsset(material,path);
        return material;
    }

    private static void RenderPreview()
    {
        UniversalRenderPipelineDebugDisplaySettings.Instance.Reset();
        var go = new GameObject("Temporary overview camera");
        var camera = go.AddComponent<Camera>();
        camera.transform.position = new Vector3(38,48,-48);
        camera.transform.LookAt(Vector3.zero);
        camera.orthographic = true; camera.orthographicSize = 30;
        camera.clearFlags = CameraClearFlags.SolidColor; camera.backgroundColor = new Color(.075f,.105f,.13f);
        var rt = new RenderTexture(1600,1100,24);
        camera.gameObject.AddComponent<UniversalAdditionalCameraData>();
        RenderPipeline.SubmitRenderRequest(camera, new UniversalRenderPipeline.SingleCameraRequest { destination = rt });
        var previous = RenderTexture.active;
        RenderTexture.active = rt;
        var image = new Texture2D(1600,1100,TextureFormat.RGB24,false);
        image.ReadPixels(new Rect(0,0,1600,1100),0,0); image.Apply();
        File.WriteAllBytes(Path.GetFullPath("../Blender/workshop-pro-unity.png"),image.EncodeToPNG());
        RenderTexture.active = previous; camera.targetTexture = null;
        UnityEngine.Object.DestroyImmediate(image); rt.Release(); UnityEngine.Object.DestroyImmediate(rt);
        UnityEngine.Object.DestroyImmediate(go);
    }
}
#endif
