// Run: Tools > Doom Scrolling > Fix UI Scenes (Add EventSystem)
// Without an EventSystem in each scene, Unity never processes button clicks —
// GraphicRaycaster casts rays but nobody reads them.
#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public static class FixUIScenes
{
    static readonly string[] TARGET_SCENES =
    {
        "Assets/_Scenes/TitleScreen.unity",
        "Assets/_Scenes/GameOverScreen.unity",
        "Assets/_Scenes/EndScreen.unity",
        "Assets/_Scenes/GameScene.unity",
    };

    [MenuItem("Tools/Doom Scrolling/Fix UI Scenes (Add EventSystem)", priority = 6)]
    public static void Run()
    {
        // Save the currently open scene first so nothing is lost
        EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo();

        int fixed_count = 0;
        foreach (var path in TARGET_SCENES)
        {
            var scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Single);

            bool hasEventSystem = false;
            foreach (var go in scene.GetRootGameObjects())
            {
                if (go.GetComponentInChildren<EventSystem>(true) != null)
                { hasEventSystem = true; break; }
            }

            if (!hasEventSystem)
            {
                var esGO = new GameObject("EventSystem");
                SceneManager.MoveGameObjectToScene(esGO, scene);
                esGO.AddComponent<EventSystem>();
                esGO.AddComponent<StandaloneInputModule>();
                EditorSceneManager.MarkSceneDirty(scene);
                fixed_count++;
                Debug.Log($"[FixUIScenes] Added EventSystem to {path}");
            }
            else
            {
                Debug.Log($"[FixUIScenes] {path} already has EventSystem — skipped.");
            }

            EditorSceneManager.SaveScene(scene);
        }

        Debug.Log($"[FixUIScenes] Done — {fixed_count} scene(s) patched. Buttons will now work.");
    }
}
#endif
