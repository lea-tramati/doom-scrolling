// CameraSetup.cs
// Run: Tools > Doom Scrolling > Setup Camera (Follow + Zoom)
// Replaces CameraFit with CameraFollow on the Main Camera in GameScene.
#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;

public static class CameraSetup
{
    [MenuItem("Tools/Doom Scrolling/Setup Camera (Follow + Zoom)", priority = 3)]
    public static void Run()
    {
        var scene = EditorSceneManager.OpenScene("Assets/_Scenes/GameScene.unity",
            OpenSceneMode.Single);

        var camGO = GameObject.FindWithTag("MainCamera");
        if (camGO == null)
        {
            Debug.LogError("[CameraSetup] No GameObject tagged MainCamera in GameScene.");
            return;
        }

        // Remove old CameraFit if present
        var oldFit = camGO.GetComponent<CameraFit>();
        if (oldFit != null) Object.DestroyImmediate(oldFit);

        // Add CameraFollow (zoomed-in tracking camera)
        var follow = camGO.GetComponent<CameraFollow>();
        if (follow == null) follow = camGO.AddComponent<CameraFollow>();

        // Set orthographic size via SerializedObject so it shows in Inspector
        var so = new SerializedObject(follow);
        so.FindProperty("orthographicSize").floatValue = 5.5f;  // ~11 tiles visible = tiles look big
        so.FindProperty("smoothSpeed").floatValue      = 10f;
        so.ApplyModifiedProperties();

        // Also set Camera.orthographicSize for immediate Editor preview
        var cam = camGO.GetComponent<Camera>();
        if (cam != null) cam.orthographicSize = 5.5f;

        // Centre camera at maze centre for editor preview
        camGO.transform.position = new Vector3(9.5f, 10.5f, -10f);

        EditorSceneManager.SaveScene(scene);
        Debug.Log("[CameraSetup] CameraFollow added (orthographicSize=5.5). Done.");
    }
}
#endif
