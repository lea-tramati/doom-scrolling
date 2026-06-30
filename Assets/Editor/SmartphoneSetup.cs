using UnityEngine;
using UnityEditor;
using System.IO;

// Menu: Tools > Doom Scrolling > Setup Smartphone Sprite
// Importe le pixel-art iPhone, supprime le fond blanc, assigne au prefab Smartphone.
public static class SmartphoneSetup
{
    const string SRC_PATH    = @"c:\Users\Utilisateur\Downloads\z2uz8go191h51.jpg";
    const string SPRITE_PATH = "Assets/_Sprites/Collectibles/Smartphone.png";
    const string PREFAB_PATH = "Assets/_Prefabs/Collectibles/Smartphone.prefab";

    const int  PPU        = 256;   // pixels per unit
    const float PREFAB_SCALE = 0.25f; // world-space size ≈ 0.5 tile wide

    [MenuItem("Tools/Doom Scrolling/Setup Smartphone Sprite")]
    static void Run()
    {
        if (!File.Exists(SRC_PATH))
        {
            Debug.LogError($"[SmartphoneSetup] Source not found: {SRC_PATH}");
            return;
        }

        // ── 1. Load source JPG ────────────────────────────────────────
        byte[] raw = File.ReadAllBytes(SRC_PATH);
        var src = new Texture2D(2, 2, TextureFormat.RGB24, false);
        src.LoadImage(raw);

        int fullW = src.width;   // 2560
        int fullH = src.height;  // 2560

        // ── 2. Crop left half (front phone view) ──────────────────────
        int cropW = fullW / 2;
        int cropH = fullH;

        var dst = new Texture2D(cropW, cropH, TextureFormat.RGBA32, false);
        for (int y = 0; y < cropH; y++)
        {
            for (int x = 0; x < cropW; x++)
            {
                Color c = src.GetPixel(x, y);
                // Remove near-white background
                bool bg = c.r > 0.92f && c.g > 0.92f && c.b > 0.92f;
                dst.SetPixel(x, y, bg ? Color.clear : new Color(c.r, c.g, c.b, 1f));
            }
        }
        dst.Apply();

        // ── 3. Save as PNG ────────────────────────────────────────────
        string absPath = Path.GetFullPath(SPRITE_PATH);
        Directory.CreateDirectory(Path.GetDirectoryName(absPath));
        File.WriteAllBytes(absPath, dst.EncodeToPNG());
        AssetDatabase.Refresh();

        // ── 4. Configure texture importer ────────────────────────────
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

        // ── 5. Assign sprite + scale to prefab ───────────────────────
        var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(SPRITE_PATH);
        if (sprite == null)
        {
            Debug.LogError("[SmartphoneSetup] Could not load sprite after import.");
            return;
        }

        var contents = PrefabUtility.LoadPrefabContents(PREFAB_PATH);
        if (contents == null)
        {
            Debug.LogError($"[SmartphoneSetup] Prefab not found: {PREFAB_PATH}");
            return;
        }

        var sr = contents.GetComponent<SpriteRenderer>();
        if (sr != null) sr.sprite = sprite;

        contents.transform.localScale = new Vector3(PREFAB_SCALE, PREFAB_SCALE, 1f);

        PrefabUtility.SaveAsPrefabAsset(contents, PREFAB_PATH);
        PrefabUtility.UnloadPrefabContents(contents);

        Debug.Log("[SmartphoneSetup] Smartphone sprite importé et assigné avec succès !");
    }
}
