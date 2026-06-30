// Run: Tools > Doom Scrolling > Fix Player Physics (Rigidbody2D)
// Without a Rigidbody2D on the Player, OnTriggerEnter2D NEVER fires —
// Unity treats colliderless objects as static and static↔static triggers
// are ignored by the physics engine. The Player moves via transform.position
// so we need a Kinematic body with gravity=0.
#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;

public static class FixPlayerPhysics
{
    [MenuItem("Tools/Doom Scrolling/Fix Player Physics (Rigidbody2D)", priority = 5)]
    public static void Run()
    {
        var guids = AssetDatabase.FindAssets("t:Prefab Player");
        foreach (var guid in guids)
        {
            var path = AssetDatabase.GUIDToAssetPath(guid);
            if (!path.Contains("Player")) continue;

            var go = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (go == null || go.GetComponent<PlayerController>() == null) continue;

            using var scope = new PrefabUtility.EditPrefabContentsScope(path);
            var root = scope.prefabContentsRoot;

            var rb = root.GetComponent<Rigidbody2D>();
            if (rb == null)
            {
                rb = root.AddComponent<Rigidbody2D>();
                Debug.Log($"[FixPlayerPhysics] Added Rigidbody2D to {path}");
            }

            rb.bodyType       = RigidbodyType2D.Kinematic;
            rb.gravityScale   = 0f;
            rb.simulated      = true;
            rb.interpolation  = RigidbodyInterpolation2D.None;
            rb.constraints    = RigidbodyConstraints2D.FreezeRotation;
            rb.useFullKinematicContacts = true;  // ensures trigger events fire with other triggers

            Debug.Log($"[FixPlayerPhysics] Player Rigidbody2D configured — triggers will now work.");
        }

        AssetDatabase.SaveAssets();
        Debug.Log("[FixPlayerPhysics] Done. Press Play and walk into a dot to test.");
    }
}
#endif
