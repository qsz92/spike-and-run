using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

public static class ItchBuildTools
{
    private const string ProductName = "School Project 2025";
    private const string BuildRoot = "Builds/Itch";

    [MenuItem("Build/Itch/Build Windows Release")]
    public static void BuildWindowsRelease()
    {
        Build(
            BuildTarget.StandaloneWindows64,
            Path.Combine(BuildRoot, "Windows", ProductName + ".exe"));
    }

    [MenuItem("Build/Itch/Build macOS Release")]
    public static void BuildMacRelease()
    {
        Build(
            BuildTarget.StandaloneOSX,
            Path.Combine(BuildRoot, "macOS", ProductName + ".app"));
    }

    [MenuItem("Build/Itch/Build Both Releases")]
    public static void BuildBothReleases()
    {
        BuildMacRelease();
        BuildWindowsRelease();
    }

    private static void Build(BuildTarget target, string outputPath)
    {
        string[] scenes = EditorBuildSettings.scenes
            .Where(scene => scene.enabled)
            .Select(scene => scene.path)
            .ToArray();

        if (scenes.Length == 0)
            throw new InvalidOperationException("No enabled scenes in Build Settings.");

        Directory.CreateDirectory(Path.GetDirectoryName(outputPath));

        BuildPlayerOptions options = new BuildPlayerOptions
        {
            scenes = scenes,
            locationPathName = outputPath,
            target = target,
            options = BuildOptions.None
        };

        BuildReport report = BuildPipeline.BuildPlayer(options);
        if (report.summary.result != BuildResult.Succeeded)
            throw new InvalidOperationException($"Build failed: {report.summary.result}");

        Debug.Log($"Itch release build ready: {outputPath}");
    }
}
