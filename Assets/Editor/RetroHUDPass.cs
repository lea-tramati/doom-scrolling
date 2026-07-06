// RetroHUDPass.cs
// Finishes the "full retro pivot" started in the 8-bit overhaul commit:
//   1. Any TextMeshProUGUI still left on the Roboto font (the iOS-inspired
//      pass swapped the default TMP font project-wide, but a handful of HUD
//      labels were wired directly to the Roboto SDF asset and didn't follow)
//      gets switched to the PressStart2P pixel font, with auto-sizing turned
//      on so the blockier glyph metrics don't overflow the existing layout.
//   2. Any leftover iOS-chrome visuals (notch, status bar icons, dock icons,
//      glass/phone-bezel widgets) that are still active in a scene — the
//      "Remove iPhone chrome" step from that overhaul only got partially
//      applied — get deactivated so they stop showing over the pixel-art UI.
//
// Runs automatically once per Editor load. Re-run any time via
// Tools > Doom Scrolling > Retrofit HUD To Pixel/Retro Style.
#if UNITY_EDITOR
using System.IO;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public static class RetroHUDPass
{
    const string PIXEL_FONT_PATH = "Assets/TextMesh Pro/Resources/Fonts & Materials/PressStart2P SDF.asset";

    static readonly string[] ScenePaths =
    {
        "Assets/_Scenes/SplashScreen.unity",
        "Assets/_Scenes/TitleScreen.unity",
        "Assets/_Scenes/GameScene.unity",
        "Assets/_Scenes/GameOverScreen.unity",
        "Assets/_Scenes/EndScreen.unity",
        "Assets/_Scenes/CreditsScreen.unity",
    };

    // Sprite names left over from the iOS-inspired UI pass that don't belong
    // in the pixel-retro HUD.
    static readonly string[] ChromeSpriteNames =
    {
        "Notch", "StatusIcons", "StatusBattery", "StatusSignal",
        "Dock_Phone", "Dock_Messages", "Dock_Music", "Dock_Safari",
        "PhoneBezel", "Widget_Glass", "Widget_Photo",
    };

    static RetroHUDPass()
    {
        EditorApplication.delayCall += () => Run(silentIfNone: true);
    }

    [MenuItem("Tools/Doom Scrolling/Retrofit HUD To Pixel-Retro Style")]
    public static void RunFromMenu() => Run(silentIfNone: false);

    static void Run(bool silentIfNone)
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) return;

        var pixelFont = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(PIXEL_FONT_PATH);
        if (pixelFont == null)
        {
            Debug.LogWarning("[RetroHUDPass] Pixel font asset not found at " + PIXEL_FONT_PATH);
            return;
        }

        int fontsSwapped = 0;
        int chromeHidden = 0;

        foreach (var path in ScenePaths)
        {
            if (!File.Exists(path)) continue;

            var alreadyOpen = EditorSceneManager.GetSceneByPath(path);
            bool wasLoaded = alreadyOpen.IsValid() && alreadyOpen.isLoaded;
            var scene = wasLoaded ? alreadyOpen : EditorSceneManager.OpenScene(path, OpenSceneMode.Additive);

            bool dirty = false;

            foreach (var root in scene.GetRootGameObjects())
            {
                foreach (var label in root.GetComponentsInChildren<TextMeshProUGUI>(true))
                {
                    if (label.font != null && label.font.name.StartsWith("Roboto"))
                    {
                        float originalSize = label.fontSize;
                        label.font = pixelFont;
                        label.enableAutoSizing = true;
                        label.fontSizeMin = Mathf.Min(4f, originalSize);
                        label.fontSizeMax = originalSize;
                        fontsSwapped++;
                        dirty = true;
                    }
                }

                foreach (var img in root.GetComponentsInChildren<Image>(true))
                {
                    if (img.sprite == null || !img.gameObject.activeSelf) continue;
                    foreach (var chromeName in ChromeSpriteNames)
                    {
                        if (img.sprite.name.StartsWith(chromeName))
                        {
                            Debug.Log($"[RetroHUDPass] Hiding leftover iOS-chrome '{img.sprite.name}' " +
                                $"on {GetPath(img.transform)} in {Path.GetFileName(path)}");
                            img.gameObject.SetActive(false);
                            chromeHidden++;
                            dirty = true;
                            break;
                        }
                    }
                }
            }

            if (dirty)
            {
                EditorSceneManager.MarkSceneDirty(scene);
                EditorSceneManager.SaveScene(scene);
            }
            if (!wasLoaded) EditorSceneManager.CloseScene(scene, true);
        }

        if (fontsSwapped > 0 || chromeHidden > 0 || !silentIfNone)
            Debug.Log($"[RetroHUDPass] Done — {fontsSwapped} label(s) switched to PressStart2P, " +
                $"{chromeHidden} leftover chrome element(s) hidden.");
    }

    static string GetPath(Transform t)
    {
        var stack = new List<string>();
        while (t != null) { stack.Add(t.name); t = t.parent; }
        stack.Reverse();
        return string.Join("/", stack);
    }
}
#endif
