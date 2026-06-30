using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using System.IO;
using System.Collections.Generic;

// Menu: Tools > Doom Scrolling > Setup TikTok Obstacle Sprite
// Importe le logo TikTok, supprime le fond gris par flood-fill, crée le prefab MalusMarker
// et le câble automatiquement sur le MazeLoader de la scène active.
public static class TikTokObstacleSetup
{
    const string SRC_PATH    = @"c:\Users\Utilisateur\Downloads\1421afb9c12fe2e48bf369a60f3b3eed.jpg";
    const string SPRITE_PATH = "Assets/_Sprites/Collectibles/TikTokObstacle.png";
    const string PREFAB_PATH = "Assets/Resources/MalusMarker.prefab"; // Resources = chargement auto sans Inspector
    const int    PPU         = 256;
    const float  SCALE       = 0.25f; // identique au Smartphone prefab

    [MenuItem("Tools/Doom Scrolling/Setup TikTok Obstacle Sprite")]
    static void Run()
    {
        if (!File.Exists(SRC_PATH))
        {
            Debug.LogError($"[TikTokObstacleSetup] Fichier introuvable : {SRC_PATH}");
            return;
        }

        // ── 1. Charger le JPG source ──────────────────────────────
        byte[] raw = File.ReadAllBytes(SRC_PATH);
        var src = new Texture2D(2, 2, TextureFormat.RGB24, false);
        src.LoadImage(raw);
        int w = src.width, h = src.height;

        // ── 2. Copier en RGBA ─────────────────────────────────────
        Color[] pixels = new Color[w * h];
        for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
            {
                Color c = src.GetPixel(x, y);
                pixels[y * w + x] = new Color(c.r, c.g, c.b, 1f);
            }

        // ── 3. Flood-fill depuis les coins pour supprimer le fond ─
        //       Le fond gris/blanc est connecté aux bords.
        //       Le noir du logo et le blanc interne ne le sont pas.
        bool[] isBg = new bool[w * h];
        var queue = new Queue<int>();

        void Seed(int sx, int sy)
        {
            int i = sy * w + sx;
            if (!isBg[i]) { isBg[i] = true; queue.Enqueue(i); }
        }
        Seed(0, 0); Seed(w - 1, 0); Seed(0, h - 1); Seed(w - 1, h - 1);

        int[] dx = { -1, 1, 0, 0 };
        int[] dy = {  0, 0,-1, 1 };

        while (queue.Count > 0)
        {
            int idx = queue.Dequeue();
            int cx = idx % w, cy = idx / w;
            for (int d = 0; d < 4; d++)
            {
                int nx = cx + dx[d], ny = cy + dy[d];
                if (nx < 0 || nx >= w || ny < 0 || ny >= h) continue;
                int ni = ny * w + nx;
                if (isBg[ni]) continue;
                Color nc = pixels[ni];
                // Propager seulement vers les pixels clairs (fond gris)
                if (nc.r > 0.68f && nc.g > 0.68f && nc.b > 0.68f)
                {
                    isBg[ni] = true;
                    queue.Enqueue(ni);
                }
            }
        }

        for (int i = 0; i < pixels.Length; i++)
            if (isBg[i]) pixels[i] = Color.clear;

        // ── 4. Sauvegarder en PNG ─────────────────────────────────
        var dst = new Texture2D(w, h, TextureFormat.RGBA32, false);
        dst.SetPixels(pixels);
        dst.Apply();

        string absPath = Path.GetFullPath(SPRITE_PATH);
        Directory.CreateDirectory(Path.GetDirectoryName(absPath));
        Directory.CreateDirectory(Path.GetFullPath("Assets/Resources")); // dossier Resources requis
        File.WriteAllBytes(absPath, dst.EncodeToPNG());
        AssetDatabase.Refresh();

        // ── 5. Configurer l'importer ──────────────────────────────
        var imp = AssetImporter.GetAtPath(SPRITE_PATH) as TextureImporter;
        if (imp != null)
        {
            imp.textureType         = TextureImporterType.Sprite;
            imp.spriteImportMode    = SpriteImportMode.Single;
            imp.filterMode          = FilterMode.Point;
            imp.textureCompression  = TextureImporterCompression.Uncompressed;
            imp.spritePixelsPerUnit = PPU;
            imp.mipmapEnabled       = false;
            imp.alphaIsTransparency = true;
            imp.SaveAndReimport();
        }

        var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(SPRITE_PATH);
        if (sprite == null) { Debug.LogError("[TikTokObstacleSetup] Sprite non chargé."); return; }

        // ── 6. Créer ou mettre à jour le prefab MalusMarker ───────
        if (File.Exists(PREFAB_PATH))
        {
            var c2 = PrefabUtility.LoadPrefabContents(PREFAB_PATH);
            var sr2 = c2.GetComponent<SpriteRenderer>();
            if (sr2) sr2.sprite = sprite;
            c2.transform.localScale = new Vector3(SCALE, SCALE, 1f);
            PrefabUtility.SaveAsPrefabAsset(c2, PREFAB_PATH);
            PrefabUtility.UnloadPrefabContents(c2);
        }
        else
        {
            var go = new GameObject("MalusMarker");
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite       = sprite;
            sr.sortingOrder = 3;
            go.transform.localScale = new Vector3(SCALE, SCALE, 1f);
            go.AddComponent<PulseMarker>();
            PrefabUtility.SaveAsPrefabAsset(go, PREFAB_PATH);
            Object.DestroyImmediate(go);
        }

        AssetDatabase.Refresh();

        // ── 7. Câbler automatiquement sur le MazeLoader actif ─────
        var mazeLoader = Object.FindAnyObjectByType<MazeLoader>();
        if (mazeLoader != null)
        {
            var prefabAsset = AssetDatabase.LoadAssetAtPath<GameObject>(PREFAB_PATH);
            var so   = new SerializedObject(mazeLoader);
            var prop = so.FindProperty("malusMarkerPrefab");
            if (prop != null)
            {
                prop.objectReferenceValue = prefabAsset;
                so.ApplyModifiedProperties();
                EditorUtility.SetDirty(mazeLoader.gameObject);
                EditorSceneManager.MarkSceneDirty(mazeLoader.gameObject.scene);
                Debug.Log("[TikTokObstacleSetup] MalusMarker câblé sur MazeLoader — sauvegarde la scène !");
            }
            else
            {
                Debug.LogWarning("[TikTokObstacleSetup] Champ 'malusMarkerPrefab' introuvable — assigne-le manuellement dans le MazeLoader Inspector.");
            }
        }

        Debug.Log("[TikTokObstacleSetup] Terminé !");
    }
}
