// TitleScreenFeedSetup.cs
// Removes the old bouncing "99+" notif badge and adds a full-screen scrolling
// grid of dimmed app icons behind the title content (ScrollingFeedBackground)
// — an "endless feed" ambiance instead of the static badge.
// Idempotent — safe to re-run from Tools > Doom Scrolling > Setup Title Feed Background.
#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

public static class TitleScreenFeedSetup
{
    const string SCENE_PATH = "Assets/_Scenes/TitleScreen.unity";
    static readonly string[] IconPaths =
    {
        "Assets/_Sprites/AppDots/FacebookDot.png",
        "Assets/_Sprites/AppDots/GmailDot.png",
        "Assets/_Sprites/AppDots/InstagramDot.png",
        "Assets/_Sprites/AppDots/NetflixDot.png",
        "Assets/_Sprites/AppDots/PinterestDot.png",
        "Assets/_Sprites/AppDots/RedditDot.png",
        "Assets/_Sprites/AppDots/SnapchatDot.png",
        "Assets/_Sprites/AppDots/SpotifyDot.png",
        "Assets/_Sprites/AppDots/TwitchDot.png",
        "Assets/_Sprites/AppDots/WhatsAppDot.png",
        "Assets/_Sprites/AppDots/YouTubeDot.png",
    };

    static TitleScreenFeedSetup()
    {
        EditorApplication.delayCall += () => Run(silentIfDone: true);
    }

    [MenuItem("Tools/Doom Scrolling/Setup Title Feed Background")]
    public static void RunFromMenu() => Run(silentIfDone: false);

    static void Run(bool silentIfDone)
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) return;
        if (!File.Exists(SCENE_PATH)) return;

        var alreadyOpen = EditorSceneManager.GetSceneByPath(SCENE_PATH);
        bool wasLoaded = alreadyOpen.IsValid() && alreadyOpen.isLoaded;
        var scene = wasLoaded ? alreadyOpen : EditorSceneManager.OpenScene(SCENE_PATH, OpenSceneMode.Additive);

        bool dirty = false;
        Canvas canvas = null;
        foreach (var root in scene.GetRootGameObjects())
        {
            canvas = root.GetComponentInChildren<Canvas>(true);
            if (canvas != null) break;
        }

        if (canvas == null)
        {
            Debug.LogWarning("[TitleScreenFeedSetup] No Canvas found in TitleScreen.");
            if (!wasLoaded) EditorSceneManager.CloseScene(scene, true);
            return;
        }

        // Remove any leftover bouncing-badge GameObject (old field was named "notifBadge"
        // / object typically named "NotifBadge" or similar in the hierarchy).
        var badge = FindByNameContains(canvas.transform, "notifbadge") ?? FindByNameContains(canvas.transform, "99");
        if (badge != null)
        {
            Debug.Log($"[TitleScreenFeedSetup] Removing leftover badge '{badge.name}'.");
            Object.DestroyImmediate(badge.gameObject);
            dirty = true;
        }

        if (canvas.transform.Find("ScrollingFeedBackground") == null)
        {
            var bgGO = new GameObject("ScrollingFeedBackground", typeof(RectTransform));
            bgGO.transform.SetParent(canvas.transform, false);
            bgGO.transform.SetAsFirstSibling(); // render behind everything else

            var rt = bgGO.GetComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;

            var feed = bgGO.AddComponent<ScrollingFeedBackground>();
            var sprites = new System.Collections.Generic.List<Sprite>();
            foreach (var p in IconPaths)
            {
                var s = AssetDatabase.LoadAssetAtPath<Sprite>(p);
                if (s != null) sprites.Add(s);
            }

            var so = new SerializedObject(feed);
            var iconsProp = so.FindProperty("icons");
            iconsProp.arraySize = sprites.Count;
            for (int i = 0; i < sprites.Count; i++)
                iconsProp.GetArrayElementAtIndex(i).objectReferenceValue = sprites[i];
            so.ApplyModifiedProperties();

            Debug.Log($"[TitleScreenFeedSetup] Created ScrollingFeedBackground with {sprites.Count} icon sprites.");
            dirty = true;
        }

        if (dirty)
        {
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
        }
        else if (!silentIfDone)
        {
            Debug.Log("[TitleScreenFeedSetup] Nothing to do — already set up.");
        }

        if (!wasLoaded) EditorSceneManager.CloseScene(scene, true);
    }

    static Transform FindByNameContains(Transform root, string needleLower)
    {
        foreach (Transform t in root.GetComponentsInChildren<Transform>(true))
            if (t.name.ToLowerInvariant().Contains(needleLower))
                return t;
        return null;
    }
}
#endif
