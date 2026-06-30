// ColliderSetup.cs
// Run: Tools > Doom Scrolling > Setup Colliders & Tags
// Adds CircleCollider2D (trigger) + correct tags to Player, Enemy, and Collectible prefabs.
// Also adds DamageFlash to the Player prefab.
#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System.Linq;

public static class ColliderSetup
{
    [MenuItem("Tools/Doom Scrolling/Setup Colliders & Tags", priority = 4)]
    public static void Run()
    {
        int patched = 0;
        var allPaths = AssetDatabase.FindAssets("t:Prefab")
            .Select(AssetDatabase.GUIDToAssetPath)
            .ToArray();

        foreach (var path in allPaths)
        {
            using var scope = new PrefabUtility.EditPrefabContentsScope(path);
            var root = scope.prefabContentsRoot;
            bool modified = false;

            if (root.GetComponent<PlayerController>() != null)
            {
                modified |= EnsureTag(root, "Player");
                modified |= EnsureTrigger(root, 0.32f);
                if (root.GetComponent<DamageFlash>() == null)
                { root.AddComponent<DamageFlash>(); modified = true; }
            }
            else if (root.GetComponent<LikeEnemy>() != null)
            {
                modified |= EnsureTag(root, "Enemy");
                modified |= EnsureTrigger(root, 0.32f);
            }
            else if (root.GetComponent<CollectibleItem>() != null)
            {
                modified |= EnsureTag(root, "Collectible");
                modified |= EnsureTrigger(root, 0.28f);
            }

            if (modified)
            {
                patched++;
                Debug.Log($"[ColliderSetup] Patched: {path}");
            }
        }

        AssetDatabase.SaveAssets();
        Debug.Log($"[ColliderSetup] Done — {patched} prefab(s) patched.");
    }

    static bool EnsureTag(GameObject go, string tag)
    {
        if (go.tag == tag) return false;
        go.tag = tag;
        return true;
    }

    static bool EnsureTrigger(GameObject go, float radius)
    {
        var col = go.GetComponent<CircleCollider2D>();
        if (col == null)
        {
            col = go.AddComponent<CircleCollider2D>();
            col.isTrigger = true;
            col.radius    = radius;
            return true;
        }
        bool changed = false;
        if (!col.isTrigger)        { col.isTrigger = true;   changed = true; }
        if (col.radius != radius)  { col.radius    = radius; changed = true; }
        return changed;
    }
}
#endif
