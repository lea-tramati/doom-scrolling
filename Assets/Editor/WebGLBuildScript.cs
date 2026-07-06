// WebGLBuildScript.cs
// Builds the game for the browser (WebGL) with settings tuned for this project:
// portrait phone aspect, uncompressed output so it runs on any static host
// without special server config (GitHub Pages, itch.io, etc.), no compression
// surprises with Content-Encoding headers.
//
// Run via Tools > Doom Scrolling > Build For Web (WebGL), or headless:
//   Unity.exe -batchmode -quit -projectPath <path> -executeMethod WebGLBuildScript.BuildFromCommandLine
using UnityEditor;
using UnityEngine;

public static class WebGLBuildScript
{
    const string OUTPUT_DIR = "Builds/WebGL";

    [MenuItem("Tools/Doom Scrolling/Build For Web (WebGL)")]
    public static void Build()
    {
        ConfigurePlayerSettings();

        var scenes = System.Array.ConvertAll(
            EditorBuildSettings.scenes, s => s.path);

        var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
        {
            scenes = scenes,
            locationPathName = OUTPUT_DIR,
            target = BuildTarget.WebGL,
            options = BuildOptions.None
        });

        var summary = report.summary;
        if (summary.result == UnityEditor.Build.Reporting.BuildResult.Succeeded)
            Debug.Log($"[WebGLBuildScript] Build succeeded: {summary.totalSize} bytes at {OUTPUT_DIR}");
        else
            Debug.LogError($"[WebGLBuildScript] Build {summary.result}: see log above for errors.");
    }

    public static void BuildFromCommandLine() => Build();

    static void ConfigurePlayerSettings()
    {
        PlayerSettings.SetScriptingBackend(
            UnityEditor.Build.NamedBuildTarget.WebGL, ScriptingImplementation.IL2CPP);

        // Uncompressed: guarantees the build runs on any static file host without
        // the server needing to send Content-Encoding: gzip/br headers.
        PlayerSettings.WebGL.compressionFormat = WebGLCompressionFormat.Disabled;
        PlayerSettings.WebGL.decompressionFallback = true;

        // Portrait phone aspect to match the UI (see PhoneGameViewSetup.cs: 1125x2436, ~19.5:9).
        PlayerSettings.defaultWebScreenWidth = 450;
        PlayerSettings.defaultWebScreenHeight = 975;

        PlayerSettings.WebGL.template = "APPLICATION:Default";
    }
}
