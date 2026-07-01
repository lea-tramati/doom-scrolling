// PressStart2PFontSetup.cs
// Generates a TextMeshPro SDF Font Asset from Assets/_Fonts/PressStart2P-Regular.ttf
// (SIL OFL licensed, see "PressStart2P - OFL.txt" next to it) and assigns it to the
// Game Over screen's retro-styled labels.
//
// Runs automatically once on Editor load. If the TMP internal API differs on this
// Unity/TMP version and the auto-run fails, use Tools > Doom Scrolling > Generate
// Press Start 2P Font Asset to retry after checking the Console for the error.
#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEngine;
using TMPro;

public static class PressStart2PFontSetup
{
    const string TTF_PATH  = "Assets/_Fonts/PressStart2P-Regular.ttf";
    const string SDF_PATH  = "Assets/_Fonts/PressStart2P SDF.asset";

    static PressStart2PFontSetup()
    {
        EditorApplication.delayCall += () => Run(silentIfDone: true);
    }

    [MenuItem("Tools/Doom Scrolling/Generate Press Start 2P Font Asset")]
    public static void RunFromMenu() => Run(silentIfDone: false);

    static void Run(bool silentIfDone)
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) return;

        try
        {
            var fontAsset = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(SDF_PATH);
            if (fontAsset == null)
                fontAsset = GenerateFontAsset();

            if (fontAsset == null)
            {
                Debug.LogWarning("[PressStart2PFontSetup] Could not generate the font asset — " +
                    "TMP's internal API may differ on this version. Import it manually via " +
                    "Window > TextMeshPro > Font Asset Creator using " + TTF_PATH);
                return;
            }

            int assigned = AssignToGameOverScreen(fontAsset);
            if (!silentIfDone || assigned > 0)
                Debug.Log($"[PressStart2PFontSetup] Font asset ready at {SDF_PATH} " +
                    $"({assigned} label(s) assigned in GameOverScreen).");
        }
        catch (System.Exception e)
        {
            Debug.LogWarning("[PressStart2PFontSetup] Failed — generate it manually via " +
                "Window > TextMeshPro > Font Asset Creator using " + TTF_PATH + "\n" + e);
        }
    }

    static TMP_FontAsset GenerateFontAsset()
    {
        var sourceFont = AssetDatabase.LoadAssetAtPath<Font>(TTF_PATH);
        if (sourceFont == null)
        {
            Debug.LogWarning("[PressStart2PFontSetup] Font file not found at " + TTF_PATH);
            return null;
        }

        var fontAsset = TMP_FontAsset.CreateFontAsset(sourceFont);
        if (fontAsset == null) return null;

        AssetDatabase.CreateAsset(fontAsset, SDF_PATH);
        if (fontAsset.material != null)
            AssetDatabase.AddObjectToAsset(fontAsset.material, fontAsset);
        if (fontAsset.atlasTextures != null)
            foreach (var tex in fontAsset.atlasTextures)
                if (tex != null) AssetDatabase.AddObjectToAsset(tex, fontAsset);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        return AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(SDF_PATH);
    }

    // Wires the new font onto the retro Game Over screen labels added earlier
    // (sessionEndedLabel / totalScoreLabel), leaving the rest of the UI (HUD, etc.)
    // on their existing fonts.
    static int AssignToGameOverScreen(TMP_FontAsset fontAsset)
    {
        const string scenePath = "Assets/_Scenes/GameOverScreen.unity";
        if (!File.Exists(scenePath)) return 0;

        var scene = UnityEditor.SceneManagement.EditorSceneManager.OpenScene(
            scenePath, UnityEditor.SceneManagement.OpenSceneMode.Additive);

        int count = 0;
        foreach (var root in scene.GetRootGameObjects())
        {
            foreach (var label in root.GetComponentsInChildren<TextMeshProUGUI>(true))
            {
                if (label.name == "SessionEndedLabel" || label.name == "TotalScoreLabel")
                {
                    if (label.font != fontAsset)
                    {
                        label.font = fontAsset;
                        count++;
                    }
                }
            }
        }

        if (count > 0)
        {
            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(scene);
            UnityEditor.SceneManagement.EditorSceneManager.SaveScene(scene);
        }
        UnityEditor.SceneManagement.EditorSceneManager.CloseScene(scene, true);
        return count;
    }
}
#endif
