using System;
using System.IO;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

public static class MMPublicBuild
{
    private const string MainScene = "Assets/Scenes/World/Enroth.unity";
    private const string DefaultOutput = "Builds/Windows/MMUnity.exe";

    [MenuItem("MM Unity/Build/Public Windows x64")]
    public static void BuildWindowsMenu()
    {
        BuildWindows(DefaultOutput);
    }

    public static void BuildWindowsBatch()
    {
        var output = Environment.GetEnvironmentVariable("MM_BUILD_PATH");
        BuildWindows(string.IsNullOrWhiteSpace(output) ? DefaultOutput : output);
    }

    private static void BuildWindows(string outputPath)
    {
        if (!File.Exists(MainScene))
            throw new FileNotFoundException("Main public scene is missing.", MainScene);

        var fullOutput = Path.GetFullPath(outputPath);
        var directory = Path.GetDirectoryName(fullOutput);
        if (!string.IsNullOrEmpty(directory))
            Directory.CreateDirectory(directory);

        var options = new BuildPlayerOptions
        {
            scenes = new[] { MainScene },
            locationPathName = fullOutput,
            target = BuildTarget.StandaloneWindows64,
            options = BuildOptions.None
        };

        Debug.Log($"MM Public Build: {MainScene} -> {fullOutput}");
        var report = BuildPipeline.BuildPlayer(options);

        if (report.summary.result != BuildResult.Succeeded)
            throw new Exception($"MM public build failed: {report.summary.result}");

        Debug.Log($"MM Public Build succeeded: {fullOutput} ({report.summary.totalSize} bytes)");
    }
}
