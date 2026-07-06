// HUDBottomBarSetup.cs
// 1. Makes sure the level-progress bar is visible again (a previous pass had
//    hidden it entirely when it was still purple — now HUDController always
//    renders it plain white, so it just needs to be active).
// 2. Creates the bottom "app dock" bar: 4 framed slots (Snapchat, Instagram,
//    TikTok, Twitter) that light up as each app type is collected this level,
//    wired into HUDController.appIconFrames.
// Idempotent — safe to re-run from Tools > Doom Scrolling > Setup HUD Bottom Bar.
#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

public static class HUDBottomBarSetup
{
    const string SCENE_PATH = "Assets/_Scenes/GameScene.unity";
    const string FRAME_SPRITE_PATH = "Assets/_Sprites/UI/RoundedPanel.png";
    static readonly string[] IconPaths =
    {
        "Assets/_Sprites/Collectibles/Snapchat_Icon.png",
        "Assets/_Sprites/Collectibles/Instagram_Icon.png",
        "Assets/_Sprites/Collectibles/TikTok_Icon.png",
        "Assets/_Sprites/Collectibles/Twitter_Icon.png",
    };
    const float FRAME_SIZE = 96f;
    const float ICON_SIZE  = 64f;
    const float SPACING    = 16f;
    const float BOTTOM_OFFSET = 36f;

    static HUDBottomBarSetup()
    {
        EditorApplication.delayCall += () => Run(silentIfDone: true);
    }

    [MenuItem("Tools/Doom Scrolling/Setup HUD Bottom Bar")]
    public static void RunFromMenu() => Run(silentIfDone: false);

    static void Run(bool silentIfDone)
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) return;
        if (!File.Exists(SCENE_PATH)) return;

        var alreadyOpen = EditorSceneManager.GetSceneByPath(SCENE_PATH);
        bool wasLoaded = alreadyOpen.IsValid() && alreadyOpen.isLoaded;
        var scene = wasLoaded ? alreadyOpen : EditorSceneManager.OpenScene(SCENE_PATH, OpenSceneMode.Additive);

        bool dirty = false;
        HUDController hud = null;
        foreach (var root in scene.GetRootGameObjects())
        {
            hud = root.GetComponentInChildren<HUDController>(true);
            if (hud != null) break;
        }

        if (hud == null)
        {
            Debug.LogWarning("[HUDBottomBarSetup] No HUDController found in GameScene.");
            if (!wasLoaded) EditorSceneManager.CloseScene(scene, true);
            return;
        }

        // 1. Restore the level progress bar's visibility.
        var so = new SerializedObject(hud);
        var fillProp = so.FindProperty("levelProgressFill");
        var fill = fillProp != null ? fillProp.objectReferenceValue as Image : null;
        if (fill != null)
        {
            var container = fill.transform.parent != null ? fill.transform.parent.gameObject : fill.gameObject;
            if (!container.activeSelf)
            {
                container.SetActive(true);
                dirty = true;
                Debug.Log($"[HUDBottomBarSetup] Re-activated level progress bar '{GetPath(container.transform)}'.");
            }
        }

        // 2. Create the bottom app dock, if it doesn't already exist.
        var appIconFramesProp = so.FindProperty("appIconFrames");
        bool alreadyWired = appIconFramesProp != null && appIconFramesProp.arraySize == 4 &&
            appIconFramesProp.GetArrayElementAtIndex(0).objectReferenceValue != null;

        if (!alreadyWired)
        {
            var canvas = hud.GetComponentInParent<Canvas>();
            if (canvas == null)
            {
                Debug.LogWarning("[HUDBottomBarSetup] No parent Canvas found for HUDController.");
            }
            else
            {
                var dock = BuildDock(canvas.transform);
                var frames = dock.GetComponentsInChildren<Image>(true);
                // BuildDock returns container + 4 frame Images (icons are separate child Images) —
                // collect exactly the 4 frame Images (direct children of dock), in order.
                var frameList = new System.Collections.Generic.List<Image>();
                foreach (Transform child in dock.transform)
                {
                    var img = child.GetComponent<Image>();
                    if (img != null) frameList.Add(img);
                }

                appIconFramesProp.arraySize = 4;
                for (int i = 0; i < 4 && i < frameList.Count; i++)
                    appIconFramesProp.GetArrayElementAtIndex(i).objectReferenceValue = frameList[i];

                dirty = true;
                Debug.Log("[HUDBottomBarSetup] Created bottom app dock and wired 4 frames into HUDController.");
            }
        }

        if (dirty)
        {
            so.ApplyModifiedProperties();
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
        }
        else if (!silentIfDone)
        {
            Debug.Log("[HUDBottomBarSetup] Nothing to do — already set up.");
        }

        if (!wasLoaded) EditorSceneManager.CloseScene(scene, true);
    }

    static GameObject BuildDock(Transform canvasTransform)
    {
        var frameSprite = AssetDatabase.LoadAssetAtPath<Sprite>(FRAME_SPRITE_PATH);

        var dockGO = new GameObject("AppDock", typeof(RectTransform));
        dockGO.transform.SetParent(canvasTransform, false);
        var dockRT = dockGO.GetComponent<RectTransform>();
        dockRT.anchorMin = new Vector2(0.5f, 0f);
        dockRT.anchorMax = new Vector2(0.5f, 0f);
        dockRT.pivot     = new Vector2(0.5f, 0f);
        float totalWidth = 4 * FRAME_SIZE + 3 * SPACING;
        dockRT.sizeDelta = new Vector2(totalWidth, FRAME_SIZE);
        dockRT.anchoredPosition = new Vector2(0f, BOTTOM_OFFSET);

        var layout = dockGO.AddComponent<HorizontalLayoutGroup>();
        layout.spacing = SPACING;
        layout.childAlignment = TextAnchor.MiddleCenter;
        layout.childForceExpandWidth = false;
        layout.childForceExpandHeight = false;
        layout.childControlWidth = false;
        layout.childControlHeight = false;

        for (int i = 0; i < IconPaths.Length; i++)
        {
            var frameGO = new GameObject($"Frame_{i}", typeof(RectTransform), typeof(Image));
            frameGO.transform.SetParent(dockGO.transform, false);
            var frameRT = frameGO.GetComponent<RectTransform>();
            frameRT.sizeDelta = new Vector2(FRAME_SIZE, FRAME_SIZE);
            var frameImg = frameGO.GetComponent<Image>();
            frameImg.sprite = frameSprite;
            frameImg.type = Image.Type.Sliced;
            frameImg.color = new Color(1f, 1f, 1f, 0.3f); // dim until collected

            var iconSprite = AssetDatabase.LoadAssetAtPath<Sprite>(IconPaths[i]);
            var iconGO = new GameObject("Icon", typeof(RectTransform), typeof(Image));
            iconGO.transform.SetParent(frameGO.transform, false);
            var iconRT = iconGO.GetComponent<RectTransform>();
            iconRT.anchorMin = iconRT.anchorMax = new Vector2(0.5f, 0.5f);
            iconRT.pivot = new Vector2(0.5f, 0.5f);
            iconRT.sizeDelta = new Vector2(ICON_SIZE, ICON_SIZE);
            iconRT.anchoredPosition = Vector2.zero;
            var iconImg = iconGO.GetComponent<Image>();
            iconImg.sprite = iconSprite;
            iconImg.raycastTarget = false;
        }

        return dockGO;
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
