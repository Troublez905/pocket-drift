#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

public static class PocketDriftBetaBuild
{
    private const string Version = "3.1-beta";
    private const string OutputDirectory = "../Releases/Pocket-Drift-Overdrive-v3.1-Beta-Windows";

    [MenuItem("Tools/Pocket Drift/Build Windows Beta v3.1")]
    public static void BuildWindowsBeta()
    {
        string outputDirectory = Path.GetFullPath(OutputDirectory);
        Directory.CreateDirectory(outputDirectory);

        PlayerSettings.companyName = "Branded Creations";
        PlayerSettings.productName = "Pocket Drift Overdrive";
        PlayerSettings.bundleVersion = Version;
        PlayerSettings.fullScreenMode = FullScreenMode.Windowed;
        PlayerSettings.resizableWindow = true;

        string[] scenes = EditorBuildSettings.scenes
            .Where(scene => scene.enabled)
            .Select(scene => scene.path)
            .ToArray();

        if (scenes.Length == 0 || scenes[0] != "Assets/Scenes/MainMenu.unity" ||
            !scenes.Contains("Assets/Scenes/WorkshopPro.unity"))
        {
            throw new InvalidOperationException("Beta build requires MainMenu first and WorkshopPro enabled.");
        }

        var options = new BuildPlayerOptions
        {
            scenes = scenes,
            locationPathName = Path.Combine(outputDirectory, "Pocket Drift Overdrive.exe"),
            target = BuildTarget.StandaloneWindows64,
            options = BuildOptions.CleanBuildCache | BuildOptions.CompressWithLz4HC
        };

        BuildReport report = BuildPipeline.BuildPlayer(options);
        if (report.summary.result != BuildResult.Succeeded)
        {
            throw new InvalidOperationException($"Windows beta build failed: {report.summary.result}");
        }

        File.WriteAllText(Path.Combine(outputDirectory, "INSTALL.txt"),
            "POCKET DRIFT: OVERDRIVE - v3.1 BETA (WINDOWS 64-BIT)\r\n" +
            "\r\n" +
            "1. Extract the entire ZIP file to a folder.\r\n" +
            "2. Keep the EXE, Pocket Drift Overdrive_Data folder, UnityPlayer.dll, and MonoBleedingEdge together.\r\n" +
            "3. Double-click Pocket Drift Overdrive.exe.\r\n" +
            "4. If Windows SmartScreen appears, verify the file came from the official project release before choosing More info > Run anyway.\r\n" +
            "\r\n" +
            "CONTROLS\r\n" +
            "WASD / Arrow Keys: steer and accelerate\r\n" +
            "Space / Left Shift: hold to drift, release to boost\r\n" +
            "E / Enter / Left Mouse: use weapon\r\n" +
            "R: respawn\r\n" +
            "Xbox controller: left stick, triggers/A, RB/LB, X/B\r\n");

        Debug.Log($"POCKET_DRIFT_BETA_BUILD_OK path={outputDirectory} bytes={report.summary.totalSize}");
    }
}
#endif
