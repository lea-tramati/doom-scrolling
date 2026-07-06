// AddCreditsTagline.cs
// Replaces the old "A GAME ABOUT<br>NEVER LOGGING OFF" tagline in the
// CreditsScreen's scrolling text with the new mission-statement line. The
// text is TMP rich text using <br> tags (not real newlines), so this matches
// on the raw string via regex rather than splitting on '\n'.
#if UNITY_EDITOR
using System.IO;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using TMPro;

public static class AddCreditsTagline
{
    const string SCENE_PATH = "Assets/_Scenes/CreditsScreen.unity";
    const string TAGLINE = "A GAME THAT CRITIQUES<br>OUR DIGITAL CONSUMPTION";

    // Matches "A GAME ABOUT/TO <br>? (NEVER)? LOG(GING) OFF" case-insensitively,
    // tolerant of <br> vs space between words and ABOUT/TO/ON phrasing.
    static readonly Regex OldTaglinePattern = new Regex(
        @"A\s+GAME\s+(ABOUT|TO|ON)\s*(<br\s*/?>)?\s*(NEVER\s*(<br\s*/?>)?\s*)?LOGG?ING?\s+OFF",
        RegexOptions.IgnoreCase);

    static AddCreditsTagline()
    {
        EditorApplication.delayCall += () => Run(silentIfDone: true);
    }

    [MenuItem("Tools/Doom Scrolling/Add Credits Tagline")]
    public static void RunFromMenu() => Run(silentIfDone: false);

    static void Run(bool silentIfDone)
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) return;
        if (!File.Exists(SCENE_PATH)) return;

        var alreadyOpen = EditorSceneManager.GetSceneByPath(SCENE_PATH);
        bool wasLoaded = alreadyOpen.IsValid() && alreadyOpen.isLoaded;
        var scene = wasLoaded ? alreadyOpen : EditorSceneManager.OpenScene(SCENE_PATH, OpenSceneMode.Additive);

        bool dirty = false;
        foreach (var root in scene.GetRootGameObjects())
        {
            var controller = root.GetComponentInChildren<CreditsScreenController>(true);
            if (controller == null) continue;

            var so = new SerializedObject(controller);
            var prop = so.FindProperty("scrollingText");
            var scrollingRt = prop != null ? prop.objectReferenceValue as RectTransform : null;
            if (scrollingRt == null) continue;

            var label = scrollingRt.GetComponent<TextMeshProUGUI>() ?? scrollingRt.GetComponentInChildren<TextMeshProUGUI>(true);
            if (label == null) continue;

            string updated = ReplaceTagline(label.text);
            if (updated != label.text)
            {
                label.text = updated;
                EditorUtility.SetDirty(label);
                dirty = true;
                Debug.Log("[AddCreditsTagline] Replaced old tagline with the new mission statement.");
            }
        }

        if (dirty)
        {
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
        }
        else if (!silentIfDone)
        {
            Debug.Log("[AddCreditsTagline] Nothing to do — tagline already present.");
        }

        if (!wasLoaded) EditorSceneManager.CloseScene(scene, true);
    }

    static string ReplaceTagline(string text)
    {
        // Clean up a stray copy from an earlier (buggy) prepend pass first.
        string cleaned = text.Replace($"{TAGLINE}<br><br>", "").Replace(TAGLINE, "");

        if (OldTaglinePattern.IsMatch(cleaned))
            return OldTaglinePattern.Replace(cleaned, TAGLINE);

        // Old phrasing not found (already replaced, or worded differently) —
        // leave the text alone rather than guessing where to insert it.
        return text;
    }
}
#endif
