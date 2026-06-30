// DoomScrollingFullSetup.cs
// Run: Tools > Doom Scrolling > Full Auto Setup
// Prerequisites: run GenerateSprites.ps1 first so PNGs exist in Assets/_Sprites/
#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.Tilemaps;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using System.IO;
using System.Linq;

public static class DoomScrollingFullSetup
{
    // ── Colors ────────────────────────────────────────────────────
    static Color CPink   = HexCol("FF4D90");
    static Color CPurple = HexCol("8115FF");
    static Color CViolet = HexCol("786CF6");
    static Color CBlack  = HexCol("120F1E");
    static Color CGhost  = HexCol("F7D8FF");
    static Color CBlue   = HexCol("00F5FF");
    static Color CRed    = HexCol("FF3A5E");

    static Color HexCol(string hex) {
        ColorUtility.TryParseHtmlString("#" + hex, out Color c); return c;
    }

    // ── Tile name order matching tilesheet row 0→13 (top→bottom) ──
    static readonly string[] TileNames = {
        "WALL_H","WALL_V","WALL_CORNER_TL","WALL_CORNER_TR",
        "WALL_CORNER_BL","WALL_CORNER_BR",
        "WALL_T_TOP","WALL_T_BOT","WALL_T_LEFT","WALL_T_RIGHT","WALL_CROSS",
        "FLOOR_PLAIN","FLOOR_FEED","MALUS"
    };

    // ── Scene paths ───────────────────────────────────────────────
    const string SCENE_TITLE    = "Assets/_Scenes/TitleScreen.unity";
    const string SCENE_GAME     = "Assets/_Scenes/GameScene.unity";
    const string SCENE_GAMEOVER = "Assets/_Scenes/GameOverScreen.unity";
    const string SCENE_END      = "Assets/_Scenes/EndScreen.unity";

    // ── Sprite paths ──────────────────────────────────────────────
    const string TILESHEET   = "Assets/_Sprites/Tilesheet/DoomScrolling_Tiles.png";
    const string PLAYER_SS   = "Assets/_Sprites/Character/Player_Spritesheet.png";
    const string ENEMY_SS    = "Assets/_Sprites/Enemies/LikeCreature_Spritesheet.png";
    const string NOTIF_DOT   = "Assets/_Sprites/Collectibles/NotifDot.png";
    const string FULL_PHONE  = "Assets/_Sprites/Collectibles/FullPhone.png";

    // ─────────────────────────────────────────────────────────────
    [MenuItem("Tools/Doom Scrolling/Full Auto Setup", priority = 0)]
    public static void RunFullSetup()
    {
        Debug.Log("[DoomScrolling] Starting full setup...");

        EnsureFolders();
        SetupTagsAndLayers();
        ConfigureSprites();
        AssetDatabase.Refresh();
        CreateTileAssets();
        CreateMazeDataAssets();
        CreateBasicPrefabs();
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        BuildScene_Title();
        BuildScene_Game();
        BuildScene_GameOver();
        BuildScene_End();
        SetupBuildSettings();

        AssetDatabase.SaveAssets();
        Debug.Log("[DoomScrolling] Full setup complete! Open Assets/_Scenes/GameScene.unity to start.");
    }

    // ─────────────────────────────────────────────────────────────
    // FOLDERS
    // ─────────────────────────────────────────────────────────────
    static void EnsureFolders()
    {
        string[] dirs = {
            "Assets/_Scenes","Assets/_Tilemaps","Assets/_Tilemaps/Tiles",
            "Assets/_MazeLayouts","Assets/_Prefabs","Assets/_Prefabs/Player",
            "Assets/_Prefabs/Enemies","Assets/_Prefabs/Collectibles",
            "Assets/_Prefabs/Hazards","Assets/_Prefabs/UI"
        };
        foreach (var d in dirs)
            if (!Directory.Exists(d)) Directory.CreateDirectory(d);
    }

    // ─────────────────────────────────────────────────────────────
    // TAGS & LAYERS
    // ─────────────────────────────────────────────────────────────
    static void SetupTagsAndLayers()
    {
        var tm = new SerializedObject(
            AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]);

        // Tags
        var tags = tm.FindProperty("tags");
        foreach (var t in new[]{"Player","Enemy","Wall","Collectible"}) AddTag(tags, t);

        // Layers 8–11
        var layers = tm.FindProperty("layers");
        SetLayer(layers,  8, "Wall");
        SetLayer(layers,  9, "Player");
        SetLayer(layers, 10, "Enemy");
        SetLayer(layers, 11, "Collectible");

        tm.ApplyModifiedProperties();
        Debug.Log("[DoomScrolling] Tags and layers configured.");
    }

    static void AddTag(SerializedProperty tags, string name)
    {
        for (int i = 0; i < tags.arraySize; i++)
            if (tags.GetArrayElementAtIndex(i).stringValue == name) return;
        tags.InsertArrayElementAtIndex(tags.arraySize);
        tags.GetArrayElementAtIndex(tags.arraySize - 1).stringValue = name;
    }

    static void SetLayer(SerializedProperty layers, int idx, string name)
    {
        if (idx < layers.arraySize)
            layers.GetArrayElementAtIndex(idx).stringValue = name;
    }

    // ─────────────────────────────────────────────────────────────
    // SPRITE IMPORT SETTINGS
    // ─────────────────────────────────────────────────────────────
    static void ConfigureSprites()
    {
        // Tilesheet — slice into 14 tiles (single column, 16×224)
        ConfigureTilesheet(TILESHEET);

        // Player spritesheet (5×5 grid, 256×256 per frame)
        ConfigureSheet(PLAYER_SS, 256, 256);

        // Enemy spritesheet (2×5 grid, 16×16 per frame)
        ConfigureSheet(ENEMY_SS, 16, 16);

        // Single-sprite collectibles and hazards
        foreach (var path in new[]{
            NOTIF_DOT, FULL_PHONE,
            "Assets/_Sprites/Collectibles/Snapchat_Icon.png",
            "Assets/_Sprites/Collectibles/Instagram_Icon.png",
            "Assets/_Sprites/Collectibles/TikTok_Icon.png",
            "Assets/_Sprites/Collectibles/Twitter_Icon.png",
            "Assets/_Sprites/Collectibles/BonusPoints.png",
            "Assets/_Sprites/Hazards/PopupAd_Sprite.png",
            "Assets/_Sprites/Hazards/AutoPlay_Sprite.png",
            "Assets/_Sprites/Hazards/TrendingTrap_Sprite.png",
            "Assets/_Sprites/UI/NotificationPanel.png",
            "Assets/_Sprites/UI/HUD_Elements.png"
        })
        {
            if (!File.Exists(path)) continue;
            var imp = (TextureImporter)AssetImporter.GetAtPath(path);
            if (imp == null) continue;
            imp.textureType         = TextureImporterType.Sprite;
            imp.spriteImportMode    = SpriteImportMode.Single;
            imp.filterMode          = FilterMode.Point;
            imp.textureCompression  = TextureImporterCompression.Uncompressed;
            imp.spritePixelsPerUnit = 16;
            imp.mipmapEnabled       = false;
            imp.SaveAndReimport();
        }
        Debug.Log("[DoomScrolling] Sprite importers configured.");
    }

    static void ConfigureTilesheet(string path)
    {
        if (!File.Exists(path)) { Debug.LogWarning($"[DoomScrolling] Missing: {path}"); return; }
        var imp = (TextureImporter)AssetImporter.GetAtPath(path);
        if (imp == null) return;
        imp.textureType         = TextureImporterType.Sprite;
        imp.spriteImportMode    = SpriteImportMode.Multiple;
        imp.filterMode          = FilterMode.Point;
        imp.textureCompression  = TextureImporterCompression.Uncompressed;
        imp.spritePixelsPerUnit = 16;
        imp.mipmapEnabled       = false;

        // 14 tiles, single column 16 wide × 224 tall
        // Unity pixel coords: y=0 is BOTTOM; our tilesheet has tile 0 at top (y=208 in Unity coords)
        var metas = new SpriteMetaData[14];
        for (int i = 0; i < 14; i++)
        {
            metas[i] = new SpriteMetaData
            {
                name      = TileNames[i],
                rect      = new Rect(0, (13 - i) * 16, 16, 16),
                alignment = (int)SpriteAlignment.Center,
                pivot     = new Vector2(0.5f, 0.5f),
                border    = Vector4.zero
            };
        }
        imp.spritesheet = metas;
        imp.SaveAndReimport();
    }

    static void ConfigureSheet(string path, int fw, int fh)
    {
        if (!File.Exists(path)) return;
        var imp = (TextureImporter)AssetImporter.GetAtPath(path);
        if (imp == null) return;
        imp.textureType         = TextureImporterType.Sprite;
        imp.spriteImportMode    = SpriteImportMode.Multiple;
        imp.filterMode          = FilterMode.Point;
        imp.textureCompression  = TextureImporterCompression.Uncompressed;
        imp.spritePixelsPerUnit = 16;
        imp.mipmapEnabled       = false;

        // Auto-slice by cell size — Unity will handle the grid
        var settings = new TextureImporterSettings();
        imp.ReadTextureSettings(settings);

        // Build sprite metas
        var tex = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        if (tex == null) { imp.SaveAndReimport(); return; }
        int cols = tex.width  / fw;
        int rows = tex.height / fh;
        var metaList = new List<SpriteMetaData>();
        for (int row = 0; row < rows; row++)
        {
            for (int col = 0; col < cols; col++)
            {
                metaList.Add(new SpriteMetaData
                {
                    name      = $"frame_{row}_{col}",
                    rect      = new Rect(col * fw, (rows - 1 - row) * fh, fw, fh),
                    alignment = (int)SpriteAlignment.Center,
                    pivot     = new Vector2(0.5f, 0.5f)
                });
            }
        }
        imp.spritesheet = metaList.ToArray();
        imp.SaveAndReimport();
    }

    // ─────────────────────────────────────────────────────────────
    // TILE ASSETS
    // ─────────────────────────────────────────────────────────────
    static Tile[] _tiles;  // cached for scene setup

    static void CreateTileAssets()
    {
        if (!File.Exists(TILESHEET)) { Debug.LogWarning("[DoomScrolling] Tilesheet not found."); return; }

        var allSprites = AssetDatabase.LoadAllAssetsAtPath(TILESHEET)
            .OfType<Sprite>().ToArray();

        _tiles = new Tile[14];
        for (int i = 0; i < 14; i++)
        {
            string tilePath = $"Assets/_Tilemaps/Tiles/{TileNames[i]}.asset";
            var existing = AssetDatabase.LoadAssetAtPath<Tile>(tilePath);
            if (existing != null) { _tiles[i] = existing; continue; }

            var sprite = allSprites.FirstOrDefault(s => s.name == TileNames[i]);
            if (sprite == null)
            {
                Debug.LogWarning($"[DoomScrolling] Sprite '{TileNames[i]}' not found in tilesheet.");
                continue;
            }
            var tile = ScriptableObject.CreateInstance<Tile>();
            tile.sprite = sprite;
            tile.color  = Color.white;
            AssetDatabase.CreateAsset(tile, tilePath);
            _tiles[i] = tile;
        }
        Debug.Log("[DoomScrolling] Tile assets created.");
    }

    // ─────────────────────────────────────────────────────────────
    // MAZEDATA ASSETS
    // ─────────────────────────────────────────────────────────────
    static void CreateMazeDataAssets()
    {
        string[] names = {"MazeLayout_A","MazeLayout_B","MazeLayout_C","MazeLayout_D","MazeLayout_E"};
        foreach (var n in names)
        {
            string p = $"Assets/_MazeLayouts/{n}.asset";
            if (AssetDatabase.LoadAssetAtPath<MazeData>(p) != null) continue;
            var asset = ScriptableObject.CreateInstance<MazeData>();
            asset.PopulateDefaultLayoutA();
            AssetDatabase.CreateAsset(asset, p);
        }
        Debug.Log("[DoomScrolling] MazeData assets created.");
    }

    // ─────────────────────────────────────────────────────────────
    // BASIC PREFABS
    // ─────────────────────────────────────────────────────────────
    static void CreateBasicPrefabs()
    {
        CreatePlayerPrefab();
        CreateEnemyPrefab();
        CreateDotPrefab();
        CreateSmartphonePrefab();
        CreateHazardPrefabs();
    }

    static void CreatePlayerPrefab()
    {
        string path = "Assets/_Prefabs/Player/Player.prefab";
        if (File.Exists(path)) return;

        var go = new GameObject("Player");
        go.tag = "Player";
        go.layer = 9;
        go.transform.localScale = new Vector3(1.5f, 1.5f, 1f);
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sortingOrder = 5;
        var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(PLAYER_SS);
        if (sprite == null)
        {
            var all = AssetDatabase.LoadAllAssetsAtPath(PLAYER_SS).OfType<Sprite>().ToArray();
            if (all.Length > 0) sprite = all[0];
        }
        if (sprite != null) sr.sprite = sprite;

        var anim = go.AddComponent<Animator>();
        go.AddComponent<PlayerController>();
        go.AddComponent<PlayerStateManager>();
        go.AddComponent<GlitchRenderer>();
        var col = go.AddComponent<CircleCollider2D>();
        col.radius = 0.25f;
        col.isTrigger = true;

        PrefabUtility.SaveAsPrefabAsset(go, path);
        Object.DestroyImmediate(go);
    }

    static void CreateEnemyPrefab()
    {
        string path = "Assets/_Prefabs/Enemies/LikeEnemy.prefab";
        if (File.Exists(path)) return;

        var go = new GameObject("LikeEnemy");
        go.tag = "Enemy";
        go.layer = 10;
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sortingOrder = 4;
        var allSprites = AssetDatabase.LoadAllAssetsAtPath(ENEMY_SS).OfType<Sprite>().ToArray();
        if (allSprites.Length > 0) sr.sprite = allSprites[0];

        go.AddComponent<Animator>();
        go.AddComponent<EnemyPathfinder>();
        go.AddComponent<LikeEnemy>();
        var col = go.AddComponent<CircleCollider2D>();
        col.radius = 0.4f;
        col.isTrigger = true;

        PrefabUtility.SaveAsPrefabAsset(go, path);
        Object.DestroyImmediate(go);
    }

    static void CreateDotPrefab()
    {
        string path = "Assets/_Prefabs/Collectibles/NotifDot.prefab";
        if (File.Exists(path)) return;

        var go = new GameObject("NotifDot");
        go.tag = "Collectible";
        go.layer = 11;
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sortingOrder = 2;
        var s = AssetDatabase.LoadAssetAtPath<Sprite>(NOTIF_DOT);
        if (s != null) sr.sprite = s;

        var item = go.AddComponent<CollectibleItem>();
        var so = new SerializedObject(item);
        so.FindProperty("type").enumValueIndex = 0; // NotificationDot
        so.ApplyModifiedProperties();
        var col = go.AddComponent<CircleCollider2D>();
        col.radius = 0.3f;
        col.isTrigger = true;

        PrefabUtility.SaveAsPrefabAsset(go, path);
        Object.DestroyImmediate(go);
    }

    static void CreateSmartphonePrefab()
    {
        string path = "Assets/_Prefabs/Collectibles/Smartphone.prefab";
        if (File.Exists(path)) return;

        var go = new GameObject("Smartphone");
        go.tag = "Collectible";
        go.layer = 11;
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sortingOrder = 2;
        var s = AssetDatabase.LoadAssetAtPath<Sprite>(FULL_PHONE);
        if (s != null) sr.sprite = s;

        var item = go.AddComponent<CollectibleItem>();
        var so = new SerializedObject(item);
        so.FindProperty("type").enumValueIndex = 6; // Smartphone
        so.ApplyModifiedProperties();
        var col = go.AddComponent<CircleCollider2D>();
        col.radius = 0.4f;
        col.isTrigger = true;

        PrefabUtility.SaveAsPrefabAsset(go, path);
        Object.DestroyImmediate(go);
    }

    static void CreateHazardPrefabs()
    {
        CreateHazardPrefab("Assets/_Prefabs/Hazards/PopupAd.prefab",     "PopupAd",     typeof(PopupAd),
            "Assets/_Sprites/Hazards/PopupAd_Sprite.png");
        CreateHazardPrefab("Assets/_Prefabs/Hazards/AutoPlayZone.prefab", "AutoPlayZone", typeof(AutoPlayZone),
            "Assets/_Sprites/Hazards/AutoPlay_Sprite.png");
        CreateHazardPrefab("Assets/_Prefabs/Hazards/TrendingTrap.prefab", "TrendingTrap", typeof(TrendingTrap),
            "Assets/_Sprites/Hazards/TrendingTrap_Sprite.png");
    }

    static void CreateHazardPrefab(string prefabPath, string goName, System.Type script, string spritePath)
    {
        if (File.Exists(prefabPath)) return;
        var go = new GameObject(goName);
        var sr = go.AddComponent<SpriteRenderer>();
        var s = AssetDatabase.LoadAssetAtPath<Sprite>(spritePath);
        if (s != null) sr.sprite = s;
        go.AddComponent(script);
        var col = go.AddComponent<BoxCollider2D>();
        col.isTrigger = true;
        PrefabUtility.SaveAsPrefabAsset(go, prefabPath);
        Object.DestroyImmediate(go);
    }

    // ─────────────────────────────────────────────────────────────
    // SCENE: TITLE SCREEN
    // ─────────────────────────────────────────────────────────────
    static void BuildScene_Title()
    {
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        // Singletons (DontDestroyOnLoad managers)
        MakeSingleton<SpeedSystem>("SpeedSystem");
        MakeSingleton<GameManager>("GameManager");
        var audioMgr = MakeAudioManager();

        // Camera
        var cam = MakeCamera("Main Camera", 12f);

        // Canvas
        var canvas = MakeCanvas("Canvas", 1080, 1920);
        var ctrl = canvas.AddComponent<TitleScreenController>();

        // Background
        var bg = MakeImage(canvas.transform, "Background", CBlack,
            new Vector2(0,0), new Vector2(1,1), new Vector2(0,0), new Vector2(0,0));

        // Title label
        var titleLbl = MakeTMP(canvas.transform, "TitleLabel", "DOOM SCROLLING",
            64, CPink, TextAlignmentOptions.Center,
            new Vector2(0.05f,0.65f), new Vector2(0.95f,0.82f));

        // Subtitle
        var subLbl = MakeTMP(canvas.transform, "SubtitleLabel", "YOU ARE ALREADY INSIDE.",
            20, CGhost, TextAlignmentOptions.Center,
            new Vector2(0.1f,0.56f), new Vector2(0.9f,0.64f));

        // Open App button
        var btnGO = MakeButton(canvas.transform, "OpenAppButton", "[ OPEN APP ]",
            new Vector2(0.25f,0.35f), new Vector2(0.75f,0.46f), CPurple, CGhost);

        // Notification badge
        var badgeGO = new GameObject("NotifBadge");
        badgeGO.transform.SetParent(canvas.transform, false);
        var badgeTMP = badgeGO.AddComponent<TextMeshProUGUI>();
        badgeTMP.text = "99+"; badgeTMP.fontSize = 16; badgeTMP.color = CGhost;
        badgeTMP.alignment = TextAlignmentOptions.Center;
        var badgeRT = badgeGO.GetComponent<RectTransform>();
        badgeRT.anchorMin = new Vector2(0.7f, 0.82f); badgeRT.anchorMax = new Vector2(0.9f, 0.88f);
        badgeRT.offsetMin = badgeRT.offsetMax = Vector2.zero;

        // Wire TitleScreenController fields
        var so = new SerializedObject(ctrl);
        so.FindProperty("titleLabel").objectReferenceValue    = titleLbl;
        so.FindProperty("subtitleLabel").objectReferenceValue = subLbl;
        so.FindProperty("openAppButton").objectReferenceValue = btnGO.GetComponent<Button>();
        so.FindProperty("notifBadge").objectReferenceValue    = badgeGO.GetComponent<RectTransform>();
        so.ApplyModifiedProperties();

        EditorSceneManager.SaveScene(scene, SCENE_TITLE);
        Debug.Log("[DoomScrolling] TitleScreen saved.");
    }

    // ─────────────────────────────────────────────────────────────
    // SCENE: GAME SCENE
    // ─────────────────────────────────────────────────────────────
    static void BuildScene_Game()
    {
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        // Singletons
        MakeSingleton<SpeedSystem>("SpeedSystem");
        MakeSingleton<GameManager>("GameManager");
        MakeAudioManager();
        MakeSingleton<HazardManager>("HazardManager");
        MakeSingleton<NotificationManager>("NotificationManager");

        // Camera
        MakeCamera("Main Camera", 12f);

        // ── Grid + Tilemaps ──────────────────────────────────────
        var gridGO = new GameObject("Grid");
        gridGO.AddComponent<Grid>();

        // WallTilemap — Rigidbody2D must be added BEFORE CompositeCollider2D (Unity requires it)
        var wallGO = new GameObject("WallTilemap");
        wallGO.transform.SetParent(gridGO.transform, false);
        wallGO.layer = 8;
        wallGO.tag   = "Wall";
        wallGO.AddComponent<Tilemap>();
        wallGO.AddComponent<TilemapRenderer>();
        var wallRB = wallGO.AddComponent<Rigidbody2D>();
        wallRB.bodyType = RigidbodyType2D.Kinematic;
        var wallCol = wallGO.AddComponent<TilemapCollider2D>();
        wallCol.compositeOperation = Collider2D.CompositeOperation.Merge;
        wallGO.AddComponent<CompositeCollider2D>();

        // FloorTilemap
        var floorGO = new GameObject("FloorTilemap");
        floorGO.transform.SetParent(gridGO.transform, false);
        floorGO.AddComponent<Tilemap>();
        var floorR = floorGO.AddComponent<TilemapRenderer>();
        floorR.sortingOrder = -1;

        // MalusTilemap
        var malusGO = new GameObject("MalusTilemap");
        malusGO.transform.SetParent(gridGO.transform, false);
        malusGO.AddComponent<Tilemap>();
        var malusR = malusGO.AddComponent<TilemapRenderer>();
        malusR.sortingOrder = 1;
        var malusCol = malusGO.AddComponent<TilemapCollider2D>();
        malusCol.isTrigger = true;
        malusGO.AddComponent<MalusZone>();

        // ── MazeLoader ────────────────────────────────────────────
        var mlGO = new GameObject("MazeLoader");
        var ml   = mlGO.AddComponent<MazeLoader>();

        var mlSO = new SerializedObject(ml);
        mlSO.FindProperty("wallTilemap").objectReferenceValue  = wallGO.GetComponent<Tilemap>();
        mlSO.FindProperty("floorTilemap").objectReferenceValue = floorGO.GetComponent<Tilemap>();
        mlSO.FindProperty("malusTilemap").objectReferenceValue = malusGO.GetComponent<Tilemap>();

        // Assign tiles (11 wall tiles + 3 floor tiles)
        if (_tiles != null && _tiles.Length >= 14)
        {
            var wallTilesArr = mlSO.FindProperty("wallTiles");
            wallTilesArr.arraySize = 11;
            for (int i = 0; i < 11; i++)
                wallTilesArr.GetArrayElementAtIndex(i).objectReferenceValue = _tiles[i];
            mlSO.FindProperty("tileFloorPlain").objectReferenceValue = _tiles[11];
            mlSO.FindProperty("tileFloorFeed").objectReferenceValue  = _tiles[12];
            mlSO.FindProperty("tileMalus").objectReferenceValue      = _tiles[13];
        }

        // Assign MazeData layouts
        var mazeLayouts = mlSO.FindProperty("layouts");
        mazeLayouts.arraySize = 5;
        for (int i = 0; i < 5; i++)
        {
            string[] names2 = {"A","B","C","D","E"};
            var md = AssetDatabase.LoadAssetAtPath<MazeData>($"Assets/_MazeLayouts/MazeLayout_{names2[i]}.asset");
            mazeLayouts.GetArrayElementAtIndex(i).objectReferenceValue = md;
        }

        // Assign prefabs
        var playerPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Prefabs/Player/Player.prefab");
        mlSO.FindProperty("playerPrefab").objectReferenceValue = playerPrefab;

        var enemyPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Prefabs/Enemies/LikeEnemy.prefab");
        var enemiesArr  = mlSO.FindProperty("enemyPrefabs");
        enemiesArr.arraySize = 4;
        for (int i = 0; i < 4; i++) enemiesArr.GetArrayElementAtIndex(i).objectReferenceValue = enemyPrefab;

        var dotPrefab   = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Prefabs/Collectibles/NotifDot.prefab");
        var phonePrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Prefabs/Collectibles/Smartphone.prefab");
        mlSO.FindProperty("dotPrefab").objectReferenceValue        = dotPrefab;
        mlSO.FindProperty("smartphonePrefab").objectReferenceValue = phonePrefab;
        mlSO.ApplyModifiedProperties();

        // Wire HazardManager prefabs
        var hm   = Object.FindAnyObjectByType<HazardManager>();
        var hmSO = new SerializedObject(hm);
        hmSO.FindProperty("popupAdPrefab").objectReferenceValue    = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Prefabs/Hazards/PopupAd.prefab");
        hmSO.FindProperty("autoPlayPrefab").objectReferenceValue   = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Prefabs/Hazards/AutoPlayZone.prefab");
        hmSO.FindProperty("trendingTrapPrefab").objectReferenceValue = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Prefabs/Hazards/TrendingTrap.prefab");
        hmSO.ApplyModifiedProperties();

        // ── HUD Canvas ────────────────────────────────────────────
        var canvas = MakeCanvas("HUD Canvas", 1080, 1920);
        var hudCtrl = canvas.AddComponent<HUDController>();

        // Top bar background
        MakeImage(canvas.transform, "TopBarBg", new Color(CBlack.r,CBlack.g,CBlack.b,0.9f),
            new Vector2(0,0.9f), new Vector2(1,1), Vector2.zero, Vector2.zero);

        var scoreLbl = MakeTMP(canvas.transform, "ScoreLabel", "SCORE\n0", 16, CGhost,
            TextAlignmentOptions.Left, new Vector2(0,0.92f), new Vector2(0.3f,1));

        var timerLbl = MakeTMP(canvas.transform, "TimerLabel", "00:00", 18, CGhost,
            TextAlignmentOptions.Center, new Vector2(0.3f,0.92f), new Vector2(0.7f,1));
        timerLbl.gameObject.AddComponent<TimerDisplay>();

        // "TIME WASTED" prefix label
        var twLbl = MakeTMP(canvas.transform, "TimeWastedLabel", "TIME WASTED", 10, CRed,
            TextAlignmentOptions.Center, new Vector2(0.3f,0.96f), new Vector2(0.7f,1));

        var levelLbl = MakeTMP(canvas.transform, "LevelLabel", "LVL 1", 16, CGhost,
            TextAlignmentOptions.Right, new Vector2(0.7f,0.92f), new Vector2(1,1));

        // Engagement bar (bottom)
        MakeImage(canvas.transform, "EngagementBg", new Color(CBlack.r,CBlack.g,CBlack.b,0.9f),
            new Vector2(0,0), new Vector2(1,0.07f), Vector2.zero, Vector2.zero);
        var engageLbl = MakeTMP(canvas.transform, "EngagementLabel", "ENGAGEMENT 0%", 12, CGhost,
            TextAlignmentOptions.Left, new Vector2(0.01f,0.01f), new Vector2(0.35f,0.065f));
        var engageFillBg = MakeImage(canvas.transform, "EngagementFillBg", CViolet,
            new Vector2(0.35f,0.01f), new Vector2(0.99f,0.065f), Vector2.zero, Vector2.zero);
        var engageFillGO = MakeImage(canvas.transform, "EngagementFill", CPurple,
            new Vector2(0.35f,0.01f), new Vector2(0.35f,0.065f), Vector2.zero, Vector2.zero);
        var fillImg = engageFillGO.GetComponent<Image>();
        fillImg.type = Image.Type.Filled;
        fillImg.fillMethod = Image.FillMethod.Horizontal;
        fillImg.fillAmount = 0f;

        // Overlay panel (hidden by default)
        var overlayGO = MakeImage(canvas.transform, "OverlayPanel", new Color(0,0,0,0.6f),
            new Vector2(0,0.4f), new Vector2(1,0.65f), Vector2.zero, Vector2.zero);
        overlayGO.SetActive(false);
        var overlayTxt = MakeTMP(overlayGO.transform, "OverlayText", "", 28, CPink,
            TextAlignmentOptions.Center, new Vector2(0,0), new Vector2(1,1));

        // Scrolling tagline strip
        var taglineGO = MakeImage(canvas.transform, "TaglineStrip", new Color(0,0,0,0.3f),
            new Vector2(0,0.89f), new Vector2(1,0.915f), Vector2.zero, Vector2.zero);
        var taglineTxt = MakeTMP(taglineGO.transform, "TaglineText",
            "YOU ARE THE PRODUCT · MORE CONTENT MORE YOU · FEED THE SYSTEM · KEEP SCROLLING · ENGAGEMENT LVL ∞ ·",
            8, CViolet, TextAlignmentOptions.Left, new Vector2(0,0), new Vector2(4,1));
        taglineGO.AddComponent<ScrollingTagline>();
        var taglineSO = new SerializedObject(taglineGO.GetComponent<ScrollingTagline>());
        taglineSO.FindProperty("label").objectReferenceValue     = taglineTxt;
        taglineSO.FindProperty("container").objectReferenceValue = taglineGO.GetComponent<RectTransform>();
        taglineSO.ApplyModifiedProperties();

        // Notification panel
        var notifPanelGO = new GameObject("NotificationPanel");
        notifPanelGO.transform.SetParent(canvas.transform, false);
        var notifRT = notifPanelGO.AddComponent<RectTransform>();
        notifRT.anchorMin = new Vector2(0.6f,0.5f); notifRT.anchorMax = new Vector2(1f,0.65f);
        notifRT.offsetMin = notifRT.offsetMax = Vector2.zero;
        var notifImg = notifPanelGO.AddComponent<Image>();
        notifImg.color = new Color(0.11f,0.11f,0.12f,0.95f);
        var notifTxt = MakeTMP(notifPanelGO.transform, "NotifLabel", "", 12, CGhost,
            TextAlignmentOptions.Left, new Vector2(0.02f,0.1f), new Vector2(0.98f,0.9f));

        // Wire HUDController
        var hudSO = new SerializedObject(hudCtrl);
        hudSO.FindProperty("scoreLabel").objectReferenceValue     = scoreLbl;
        hudSO.FindProperty("levelLabel").objectReferenceValue     = levelLbl;
        hudSO.FindProperty("livesLabel").objectReferenceValue     = MakeTMP(canvas.transform,"LivesLabel","♥♥♥",14,CPink,TextAlignmentOptions.Right,new Vector2(0.7f,0.895f),new Vector2(1f,0.92f));
        hudSO.FindProperty("engagementFill").objectReferenceValue = fillImg;
        hudSO.FindProperty("engagementLabel").objectReferenceValue= engageLbl;
        hudSO.FindProperty("overlayPanel").objectReferenceValue   = overlayGO;
        hudSO.FindProperty("overlayText").objectReferenceValue    = overlayTxt;
        hudSO.ApplyModifiedProperties();

        // Wire NotificationManager
        var notifMgr = Object.FindAnyObjectByType<NotificationManager>();
        var nmSO = new SerializedObject(notifMgr);
        nmSO.FindProperty("panelRoot").objectReferenceValue  = notifPanelGO.GetComponent<RectTransform>();
        nmSO.FindProperty("notifLabel").objectReferenceValue = notifTxt;
        nmSO.ApplyModifiedProperties();

        EditorSceneManager.SaveScene(scene, SCENE_GAME);
        Debug.Log("[DoomScrolling] GameScene saved.");
    }

    // ─────────────────────────────────────────────────────────────
    // SCENE: GAME OVER
    // ─────────────────────────────────────────────────────────────
    static void BuildScene_GameOver()
    {
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        MakeCamera("Main Camera", 12f);
        MakeSingleton<GameManager>("GameManager");
        MakeSingleton<AudioManager>("AudioManager");

        var canvas = MakeCanvas("Canvas", 1080, 1920);
        MakeImage(canvas.transform, "Background", CBlack,
            new Vector2(0,0), new Vector2(1,1), Vector2.zero, Vector2.zero);

        var ctrl    = canvas.AddComponent<GameOverScreenController>();
        var seLbl   = MakeTMP(canvas.transform,"SessionEndedLabel","SESSION ENDED",48,CRed,
            TextAlignmentOptions.Center,new Vector2(0.05f,0.65f),new Vector2(0.95f,0.8f));
        var scoreLbl= MakeTMP(canvas.transform,"TotalScoreLabel","TOTAL ENGAGEMENT: 0",22,CGhost,
            TextAlignmentOptions.Center,new Vector2(0.1f,0.5f),new Vector2(0.9f,0.62f));
        var btn     = MakeButton(canvas.transform,"PlayAgainButton","[ PLAY AGAIN ]",
            new Vector2(0.25f,0.3f),new Vector2(0.75f,0.42f),CPurple,CGhost);

        var so = new SerializedObject(ctrl);
        so.FindProperty("sessionEndedLabel").objectReferenceValue = seLbl;
        so.FindProperty("totalScoreLabel").objectReferenceValue   = scoreLbl;
        so.FindProperty("playAgainBtn").objectReferenceValue      = btn.GetComponent<Button>();
        so.ApplyModifiedProperties();

        EditorSceneManager.SaveScene(scene, SCENE_GAMEOVER);
        Debug.Log("[DoomScrolling] GameOverScreen saved.");
    }

    // ─────────────────────────────────────────────────────────────
    // SCENE: END SCREEN
    // ─────────────────────────────────────────────────────────────
    static void BuildScene_End()
    {
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        MakeCamera("Main Camera", 12f);
        MakeSingleton<GameManager>("GameManager");
        MakeSingleton<AudioManager>("AudioManager");

        var canvas = MakeCanvas("Canvas", 1080, 1920);
        MakeImage(canvas.transform,"Background",CBlack,
            new Vector2(0,0),new Vector2(1,1),Vector2.zero,Vector2.zero);

        var ctrl = canvas.AddComponent<EndScreenController>();

        // Intro panel
        var introGO = new GameObject("IntroPanel");
        introGO.transform.SetParent(canvas.transform,false);
        var introRT = introGO.AddComponent<RectTransform>();
        introRT.anchorMin=new Vector2(0,0.7f); introRT.anchorMax=new Vector2(1,0.9f);
        introRT.offsetMin=introRT.offsetMax=Vector2.zero;

        var intro1 = MakeTMP(introGO.transform,"IntroLine1","",36,CRed,
            TextAlignmentOptions.Center,new Vector2(0,0.55f),new Vector2(1,1));
        var intro2 = MakeTMP(introGO.transform,"IntroLine2","",24,CGhost,
            TextAlignmentOptions.Center,new Vector2(0,0.05f),new Vector2(1,0.5f));

        var confettiGO = new GameObject("ConfettiParent");
        confettiGO.transform.SetParent(introGO.transform,false);

        // Sequence text
        var seqTxt = MakeTMP(canvas.transform,"SequenceText","",20,CGhost,
            TextAlignmentOptions.Center,new Vector2(0.05f,0.3f),new Vector2(0.95f,0.68f));

        // Buttons
        var playBtn = MakeButton(canvas.transform,"PlayAgainBtn","[ PLAY AGAIN ]",
            new Vector2(0.1f,0.12f),new Vector2(0.9f,0.22f),CPurple,CGhost);
        var putDownBtn = MakeButton(canvas.transform,"PutDownBtn","[ PUT DOWN THE PHONE ]",
            new Vector2(0.1f,0.02f),new Vector2(0.9f,0.11f),CRed,CGhost);

        // Fade overlay
        var fadeGO = MakeImage(canvas.transform,"FadeOverlay",Color.black,
            new Vector2(0,0),new Vector2(1,1),Vector2.zero,Vector2.zero);
        var fadeGroup = fadeGO.AddComponent<CanvasGroup>();
        fadeGroup.alpha = 0f;
        fadeGO.transform.SetAsLastSibling();

        // "YOU WON'T." text (hidden)
        var wontTxt = MakeTMP(canvas.transform,"WontText","YOU WON'T.",42,CGhost,
            TextAlignmentOptions.Center,new Vector2(0.05f,0.45f),new Vector2(0.95f,0.6f));
        wontTxt.gameObject.SetActive(false);

        // Title glitch
        var glitchTxt = MakeTMP(canvas.transform,"TitleGlitch","DOOM SCROLLING",52,CPink,
            TextAlignmentOptions.Center,new Vector2(0.05f,0.5f),new Vector2(0.95f,0.7f));
        glitchTxt.gameObject.SetActive(false);

        // Wire EndScreenController
        var so = new SerializedObject(ctrl);
        so.FindProperty("introLine1").objectReferenceValue    = intro1;
        so.FindProperty("introLine2").objectReferenceValue    = intro2;
        so.FindProperty("confettiParent").objectReferenceValue= confettiGO;
        so.FindProperty("sequenceText").objectReferenceValue  = seqTxt;
        so.FindProperty("playAgainBtn").objectReferenceValue  = playBtn.GetComponent<Button>();
        so.FindProperty("putDownBtn").objectReferenceValue    = putDownBtn.GetComponent<Button>();
        so.FindProperty("fadeOverlay").objectReferenceValue   = fadeGroup;
        so.FindProperty("wontText").objectReferenceValue      = wontTxt;
        so.FindProperty("titleGlitch").objectReferenceValue   = glitchTxt;
        so.ApplyModifiedProperties();

        EditorSceneManager.SaveScene(scene, SCENE_END);
        Debug.Log("[DoomScrolling] EndScreen saved.");
    }

    // ─────────────────────────────────────────────────────────────
    // BUILD SETTINGS
    // ─────────────────────────────────────────────────────────────
    static void SetupBuildSettings()
    {
        var scenes = new[]
        {
            new EditorBuildSettingsScene(SCENE_TITLE,    true),
            new EditorBuildSettingsScene(SCENE_GAME,     true),
            new EditorBuildSettingsScene(SCENE_GAMEOVER, true),
            new EditorBuildSettingsScene(SCENE_END,      true)
        };
        EditorBuildSettings.scenes = scenes;
        Debug.Log("[DoomScrolling] Build settings updated: 4 scenes registered.");
    }

    // ─────────────────────────────────────────────────────────────
    // HELPERS
    // ─────────────────────────────────────────────────────────────

    static GameObject MakeSingleton<T>(string name) where T : MonoBehaviour
    {
        var go = new GameObject(name);
        go.AddComponent<T>();
        return go;
    }

    static GameObject MakeAudioManager()
    {
        var go = new GameObject("AudioManager");
        go.AddComponent<AudioManager>();
        var musicSrc = new GameObject("MusicSource"); musicSrc.transform.SetParent(go.transform,false);
        var sfxSrc   = new GameObject("SFXSource");   sfxSrc.transform.SetParent(go.transform,false);
        var ms = musicSrc.AddComponent<AudioSource>(); ms.loop = true; ms.playOnAwake = false;
        var ss = sfxSrc.AddComponent<AudioSource>();   ss.playOnAwake = false;
        var so = new SerializedObject(go.GetComponent<AudioManager>());
        so.FindProperty("musicSource").objectReferenceValue = ms;
        so.FindProperty("sfxSource").objectReferenceValue   = ss;
        so.ApplyModifiedProperties();
        return go;
    }

    static Camera MakeCamera(string name, float orthoSize)
    {
        var camGO  = new GameObject(name);
        camGO.tag  = "MainCamera";
        var cam    = camGO.AddComponent<Camera>();
        cam.orthographic       = true;
        cam.orthographicSize   = orthoSize;
        cam.backgroundColor    = CBlack;
        cam.clearFlags         = CameraClearFlags.SolidColor;
        cam.nearClipPlane      = -100f;
        cam.farClipPlane       = 100f;
        camGO.AddComponent<AudioListener>();
        return cam;
    }

    static GameObject MakeCanvas(string name, float refW, float refH)
    {
        var go      = new GameObject(name);
        var canvas  = go.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 10;
        var scaler  = go.AddComponent<CanvasScaler>();
        scaler.uiScaleMode        = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(refW, refH);
        scaler.matchWidthOrHeight  = 0.5f;
        go.AddComponent<GraphicRaycaster>();
        return go;
    }

    static GameObject MakeImage(Transform parent, string name, Color color,
        Vector2 anchorMin, Vector2 anchorMax, Vector2 offsetMin, Vector2 offsetMax)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        var img = go.AddComponent<Image>();
        img.color = color;
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = anchorMin; rt.anchorMax = anchorMax;
        rt.offsetMin = offsetMin; rt.offsetMax = offsetMax;
        return go;
    }

    static TextMeshProUGUI MakeTMP(Transform parent, string name, string text,
        float fontSize, Color color, TextAlignmentOptions align,
        Vector2 anchorMin, Vector2 anchorMax)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        var tmp = go.AddComponent<TextMeshProUGUI>();
        tmp.text       = text;
        tmp.fontSize   = fontSize;
        tmp.color      = color;
        tmp.alignment  = align;
        tmp.textWrappingMode = TextWrappingModes.Normal;
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = anchorMin; rt.anchorMax = anchorMax;
        rt.offsetMin = rt.offsetMax = Vector2.zero;
        return tmp;
    }

    static GameObject MakeButton(Transform parent, string name, string label,
        Vector2 anchorMin, Vector2 anchorMax, Color bgColor, Color textColor)
    {
        var go = MakeImage(parent, name, bgColor, anchorMin, anchorMax, Vector2.zero, Vector2.zero);
        go.AddComponent<Button>();
        var lbl = MakeTMP(go.transform, "Label", label, 20, textColor,
            TextAlignmentOptions.Center, Vector2.zero, Vector2.one);
        return go;
    }
}
#endif
