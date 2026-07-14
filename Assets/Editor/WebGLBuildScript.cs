// WebGLBuildScript.cs
// Builds the game for the browser (WebGL) with settings tuned for this project:
// the "Responsive" template (Assets/WebGLTemplates/Responsive) fills the browser
// viewport on phone, tablet and desktop alike — ResponsiveDisplay.cs then
// letterboxes the phone-aspect UI/gameplay inside whatever shape that turns out
// to be. Output is uncompressed so it runs on any static host without special
// server config (GitHub Pages, itch.io, etc.), no compression surprises with
// Content-Encoding headers.
//
// Outputs straight to /docs so GitHub Pages can serve it with zero extra steps —
// just enable Pages on this repo for "Deploy from a branch: main /docs" and
// commit + push the result of every rebuild.
//
// Run via Tools > Doom Scrolling > Build For Web (WebGL), or headless:
//   Unity.exe -batchmode -quit -projectPath <path> -executeMethod WebGLBuildScript.BuildFromCommandLine
using UnityEditor;
using UnityEngine;

public static class WebGLBuildScript
{
    const string OUTPUT_DIR = "docs";

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

        // Initial canvas buffer size before the page's CSS (100dvw/100dvh) and Unity's
        // matchWebGLToCanvasSize take over on the first frame — doesn't need to match any
        // particular device, just a sane starting point.
        PlayerSettings.defaultWebScreenWidth = 960;
        PlayerSettings.defaultWebScreenHeight = 600;

        PlayerSettings.WebGL.template = "PROJECT:Responsive";
    }
}
