using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using System.IO;
using System.Collections.Generic;

// Menu: Tools > Doom Scrolling > Setup App Dot Sprites
// Importe les 8 logos pixel-art, supprime le fond, crée des prefabs de points
// et câble dotPrefabs sur le MazeLoader actif.
public static class AppDotSetup
{
    enum BgType { White, Black, Blue }

    // NOTE (2026-07): Instagram, Discord and Twitter were removed from this list.
    // Discord/Twitter source images were near-monochrome brand colors that the
    // flood-fill background remover misidentified as background, producing
    // near-blank sprites — they were replaced by Facebook/Reddit (processed
    // externally). Instagram was swapped for a cleaner externally-processed logo.
    // WhatsApp/Netflix/Gmail were also added externally. Re-running this tool will
    // NOT touch any of those 6 dots — only the ones still listed below.
    static readonly (string src, string name, BgType bg)[] Sources =
    {
        (@"c:\Users\Utilisateur\Downloads\Snapchat icons.png",                   "Snapchat",  BgType.White),
        (@"c:\Users\Utilisateur\Downloads\youtube-logo.png",                     "YouTube",   BgType.White),
        (@"c:\Users\Utilisateur\Downloads\5241dea711b7a20.png",                  "Spotify",   BgType.Black),
        (@"c:\Users\Utilisateur\Downloads\d7208097157399cec0aa4d072f3fb947.jpg", "Pinterest", BgType.White),
        (@"c:\Users\Utilisateur\Downloads\twitch-logo.png",                      "Twitch",    BgType.White),
    };

    const string SPRITE_DIR  = "Assets/_Sprites/AppDots";
    const string PREFAB_DIR  = "Assets/_Prefabs/AppDots";
    const int    TARGET_SIZE = 64;   // taille finale du sprite en pixels
    const int    PPU         = 256;  // pixels per unit → dot = 0.25 world units

    [MenuItem("Tools/Doom Scrolling/Setup App Dot Sprites")]
    static void Run()
    {
        Directory.CreateDirectory(Path.GetFullPath(SPRITE_DIR));
        Directory.CreateDirectory(Path.GetFullPath(PREFAB_DIR));

        var createdPrefabs = new List<GameObject>();

        foreach (var (src, name, bg) in Sources)
        {
            if (!File.Exists(src))
            {
                Debug.LogWarning($"[AppDotSetup] Fichier manquant : {src}");
                continue;
            }

            // ── 1. Charger l'image source ──────────────────────────────
            byte[] raw = File.ReadAllBytes(src);
            var tex = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            tex.LoadImage(raw);
            int sw = tex.width, sh = tex.height;
            Color[] pixels = tex.GetPixels();
            Object.DestroyImmediate(tex);

            // ── 2. Supprimer le fond par flood-fill depuis les bords ───
            RemoveBackground(pixels, sw, sh, bg);

            // ── 3. Recadrer sur le contenu réel (supprime le padding inégal)
            var (cropped, cs) = CropToContent(pixels, sw, sh, padding: 3);

            // ── 4. Redimensionner à TARGET_SIZE×TARGET_SIZE ────────────
            Color[] small = BilinearResize(cropped, cs, cs, TARGET_SIZE, TARGET_SIZE);

            // ── 5. Sauvegarder en PNG ──────────────────────────────────
            string spritePath = $"{SPRITE_DIR}/{name}Dot.png";
            var dst = new Texture2D(TARGET_SIZE, TARGET_SIZE, TextureFormat.RGBA32, false);
            dst.SetPixels(small);
            dst.Apply();
            File.WriteAllBytes(Path.GetFullPath(spritePath), dst.EncodeToPNG());
            Object.DestroyImmediate(dst);
            AssetDatabase.Refresh();

            // ── 6. Configurer l'importer ───────────────────────────────
            var imp = AssetImporter.GetAtPath(spritePath) as TextureImporter;
            if (imp != null)
            {
                imp.textureType         = TextureImporterType.Sprite;
                imp.spriteImportMode    = SpriteImportMode.Single;
                imp.filterMode          = FilterMode.Point;   // look pixel-art
                imp.textureCompression  = TextureImporterCompression.Uncompressed;
                imp.spritePixelsPerUnit = PPU;
                imp.mipmapEnabled       = false;
                imp.alphaIsTransparency = true;
                imp.SaveAndReimport();
            }

            var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(spritePath);
            if (sprite == null)
            {
                Debug.LogError($"[AppDotSetup] Sprite non chargé : {spritePath}");
                continue;
            }

            // ── 7. Créer / mettre à jour le prefab ────────────────────
            string prefabPath = $"{PREFAB_DIR}/{name}Dot.prefab";

            if (File.Exists(prefabPath))
            {
                var existing = PrefabUtility.LoadPrefabContents(prefabPath);
                var sr2 = existing.GetComponent<SpriteRenderer>();
                if (sr2) sr2.sprite = sprite;
                PrefabUtility.SaveAsPrefabAsset(existing, prefabPath);
                PrefabUtility.UnloadPrefabContents(existing);
            }
            else
            {
                var go    = new GameObject($"{name}Dot");
                go.tag    = "Collectible";
                go.layer  = 11; // Collectible layer

                var sr         = go.AddComponent<SpriteRenderer>();
                sr.sprite      = sprite;
                sr.sortingOrder = 2;

                var col        = go.AddComponent<CircleCollider2D>();
                col.isTrigger  = true;
                col.radius     = 0.28f; // identique au NotifDot existant

                go.AddComponent<CollectibleItem>();
                // CollectibleItem.type = 0 → NotificationDot (10pts, vitesse +0.01)

                PrefabUtility.SaveAsPrefabAsset(go, prefabPath);
                Object.DestroyImmediate(go);
            }

            var prefabAsset = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            if (prefabAsset != null)
                createdPrefabs.Add(prefabAsset);

            Debug.Log($"[AppDotSetup] {name} ✓");
        }

        AssetDatabase.Refresh();

        // ── 8. Câbler dotPrefabs sur le MazeLoader actif ─────────────
        var mazeLoader = Object.FindAnyObjectByType<MazeLoader>();
        if (mazeLoader != null && createdPrefabs.Count > 0)
        {
            var so   = new SerializedObject(mazeLoader);
            var prop = so.FindProperty("dotPrefabs");
            if (prop != null)
            {
                prop.ClearArray();
                prop.arraySize = createdPrefabs.Count;
                for (int i = 0; i < createdPrefabs.Count; i++)
                    prop.GetArrayElementAtIndex(i).objectReferenceValue = createdPrefabs[i];
                so.ApplyModifiedProperties();
                EditorUtility.SetDirty(mazeLoader.gameObject);
                EditorSceneManager.MarkSceneDirty(mazeLoader.gameObject.scene);
                Debug.Log("[AppDotSetup] dotPrefabs câblés sur MazeLoader — sauvegarde la scène !");
            }
            else
            {
                Debug.LogWarning("[AppDotSetup] Champ 'dotPrefabs' introuvable sur MazeLoader.");
            }
        }

        Debug.Log($"[AppDotSetup] Terminé — {createdPrefabs.Count}/{Sources.Length} prefabs créés.");
    }

    // ── Recadrage sur le contenu (uniformise la taille visuelle) ─────────

    // Retourne un extrait CARRÉ centré sur les pixels non-transparents + padding
    static (Color[] pixels, int size) CropToContent(Color[] px, int w, int h, int padding = 4)
    {
        int x0 = w, y0 = h, x1 = -1, y1 = -1;
        for (int y = 0; y < h; y++)
        for (int x = 0; x < w; x++)
        {
            if (px[y * w + x].a > 0.05f)
            {
                if (x < x0) x0 = x;
                if (x > x1) x1 = x;
                if (y < y0) y0 = y;
                if (y > y1) y1 = y;
            }
        }

        if (x1 < 0) return (px, w); // aucun contenu → image complète

        // Ajouter une marge uniforme autour du logo
        x0 = Mathf.Max(0, x0 - padding);
        y0 = Mathf.Max(0, y0 - padding);
        x1 = Mathf.Min(w - 1, x1 + padding);
        y1 = Mathf.Min(h - 1, y1 + padding);

        // Forcer un carré (dimension max) centré sur le contenu
        int cw = x1 - x0 + 1, ch = y1 - y0 + 1;
        int size = Mathf.Max(cw, ch);
        int ox = x0 + (cw - size) / 2;
        int oy = y0 + (ch - size) / 2;

        Color[] result = new Color[size * size];
        for (int y = 0; y < size; y++)
        for (int x = 0; x < size; x++)
        {
            int sx = ox + x, sy = oy + y;
            result[y * size + x] = (sx >= 0 && sx < w && sy >= 0 && sy < h)
                ? px[sy * w + sx]
                : Color.clear;
        }
        return (result, size);
    }

    // ── Suppression du fond par flood-fill ────────────────────────────

    static void RemoveBackground(Color[] px, int w, int h, BgType bg)
    {
        bool[] isBg = new bool[w * h];
        var queue = new Queue<int>();

        void Seed(int x, int y)
        {
            int i = y * w + x;
            if (!isBg[i]) { isBg[i] = true; queue.Enqueue(i); }
        }

        // Seeder les 4 bords entiers (pas juste les coins)
        for (int x = 0; x < w; x++) { Seed(x, 0); Seed(x, h - 1); }
        for (int y = 1; y < h - 1; y++) { Seed(0, y); Seed(w - 1, y); }

        int[] dx = { -1, 1,  0, 0 };
        int[] dy = {  0, 0, -1, 1 };

        while (queue.Count > 0)
        {
            int cur = queue.Dequeue();
            int cx = cur % w, cy = cur / w;

            for (int d = 0; d < 4; d++)
            {
                int nx = cx + dx[d], ny = cy + dy[d];
                if (nx < 0 || nx >= w || ny < 0 || ny >= h) continue;
                int ni = ny * w + nx;
                if (isBg[ni]) continue;

                Color nc = px[ni];
                bool match = bg switch
                {
                    // Déjà transparent = fond quelle que soit la couleur
                    BgType.White => nc.a < 0.05f || (nc.r > 0.84f && nc.g > 0.84f && nc.b > 0.84f),
                    BgType.Black => nc.a < 0.05f || (nc.r < 0.16f && nc.g < 0.16f && nc.b < 0.16f),
                    BgType.Blue  => nc.a < 0.05f || (nc.b > 0.70f && nc.r < 0.55f && nc.g < 0.65f),
                    _            => false
                };

                if (match) { isBg[ni] = true; queue.Enqueue(ni); }
            }
        }

        for (int i = 0; i < px.Length; i++)
            if (isBg[i]) px[i] = Color.clear;
    }

    // ── Redimensionnement bilinéaire ──────────────────────────────────

    static Color[] BilinearResize(Color[] src, int sw, int sh, int dw, int dh)
    {
        Color[] dst = new Color[dw * dh];
        float xr = (float)sw / dw, yr = (float)sh / dh;

        for (int y = 0; y < dh; y++)
        for (int x = 0; x < dw; x++)
        {
            float gx = x * xr, gy = y * yr;
            int x0 = (int)gx, y0 = (int)gy;
            int x1 = Mathf.Min(x0 + 1, sw - 1);
            int y1 = Mathf.Min(y0 + 1, sh - 1);
            float fx = gx - x0, fy = gy - y0;

            Color c00 = src[y0 * sw + x0], c10 = src[y0 * sw + x1];
            Color c01 = src[y1 * sw + x0], c11 = src[y1 * sw + x1];

            dst[y * dw + x] = Color.Lerp(
                Color.Lerp(c00, c10, fx),
                Color.Lerp(c01, c11, fx), fy);
        }
        return dst;
    }
}
