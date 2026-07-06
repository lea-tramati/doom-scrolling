// DumpHUDImages.cs — one-off diagnostic: logs every active Image in GameScene's
// Canvas hierarchy (name, sprite, color, anchored Y) so leftover/misplaced HUD
// elements (e.g. "purple bar", "blue oval") can be identified precisely instead
// of guessed at by sprite-name blacklist.
#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using System.Text;

public static class DumpHUDImages
{
    const string SCENE_PATH = "Assets/_Scenes/GameScene.unity";

    static DumpHUDImages()
    {
        EditorApplication.delayCall += () => Run();
    }

    [MenuItem("Tools/Doom Scrolling/Dump HUD Images (diagnostic)")]
    public static void RunFromMenu() => Run();

    static void Run()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) return;
        if (!File.Exists(SCENE_PATH)) return;

        var alreadyOpen = EditorSceneManager.GetSceneByPath(SCENE_PATH);
        bool wasLoaded = alreadyOpen.IsValid() && alreadyOpen.isLoaded;
        var scene = wasLoaded ? alreadyOpen : EditorSceneManager.OpenScene(SCENE_PATH, OpenSceneMode.Additive);

        var sb = new StringBuilder();
        sb.AppendLine("[DumpHUDImages] ---- Active Image components in GameScene ----");
        foreach (var root in scene.GetRootGameObjects())
        {
            foreach (var img in root.GetComponentsInChildren<Image>(true))
            {
                var rt = img.GetComponent<RectTransform>();
                sb.AppendLine($"  path={GetPath(img.transform)} | active={img.gameObject.activeSelf} | " +
                    $"sprite={(img.sprite ? img.sprite.name : "null")} | color=#{ColorUtility.ToHtmlStringRGBA(img.color)} | " +
                    $"anchoredPos={rt.anchoredPosition} | sizeDelta={rt.sizeDelta} | anchorMin={rt.anchorMin} | anchorMax={rt.anchorMax}");
            }
        }
        Debug.Log(sb.ToString());

        if (!wasLoaded) EditorSceneManager.CloseScene(scene, true);
    }

    static string GetPath(Transform t)
    {
        var stack = new System.Collections.Generic.List<string>();
        while (t != null) { stack.Add(t.name); t = t.parent; }
        stack.Reverse();
        return string.Join("/", stack);
    }
}
#endif
