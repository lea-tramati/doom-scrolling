// RemoveConflictingCameraFit.cs
// GameplayVisualSetup.Run() auto-adds a CameraFit component to Main Camera
// if one isn't already present. CameraFit computes a "zoom out to fit the
// whole maze + HUD margins" orthographic size, which fights with
// CameraFollow's own tuned "zoomed in, big tiles" size (5.5) — whichever
// Awake() runs last wins, and it made the maze render tiny. CameraFollow is
// the actual intended runtime zoom controller, so CameraFit doesn't belong
// on the same camera; this removes it.
#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class RemoveConflictingCameraFit
{
    const string SCENE_PATH = "Assets/_Scenes/GameScene.unity";

    static RemoveConflictingCameraFit()
    {
        EditorApplication.delayCall += () => Run(silentIfDone: true);
    }

    [MenuItem("Tools/Doom Scrolling/Remove Conflicting CameraFit")]
    public static void RunFromMenu() => Run(silentIfDone: false);

    static void Run(bool silentIfDone)
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) return;
        if (!File.Exists(SCENE_PATH)) return;

        var alreadyOpen = EditorSceneManager.GetSceneByPath(SCENE_PATH);
        bool wasLoaded = alreadyOpen.IsValid() && alreadyOpen.isLoaded;
        var scene = wasLoaded ? alreadyOpen : EditorSceneManager.OpenScene(SCENE_PATH, OpenSceneMode.Additive);

        bool dirty = false;
        var camGO = GameObject.FindWithTag("MainCamera");
        if (camGO == null)
        {
            foreach (var root in scene.GetRootGameObjects())
            {
                var cam = root.GetComponentInChildren<Camera>(true);
                if (cam != null) { camGO = cam.gameObject; break; }
            }
        }

        if (camGO != null)
        {
            var fit = camGO.GetComponent<CameraFit>();
            var follow = camGO.GetComponent<CameraFollow>();
            if (fit != null && follow != null)
            {
                Object.DestroyImmediate(fit);
                dirty = true;
                Debug.Log("[RemoveConflictingCameraFit] Removed CameraFit — CameraFollow already owns zoom.");
            }
        }

        if (dirty)
        {
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
        }
        else if (!silentIfDone)
        {
            Debug.Log("[RemoveConflictingCameraFit] Nothing to do.");
        }

        if (!wasLoaded) EditorSceneManager.CloseScene(scene, true);
    }
}
#endif
