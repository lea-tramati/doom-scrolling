// GameplayVisualSetup.cs
// Run: Tools > Doom Scrolling > Setup Gameplay Visuals
// - Fixes player sprite PPU (256 px/unit → 1 tile = 1 world unit)
// - Creates AnimatorController with 4-direction blend tree + Death/Malus states
// - Applies to player prefab and sets scale to 0.7
// - Adds CameraFit to Main Camera and centers it on the maze
#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine.U2D;
using System.IO;
using System.Linq;

public static class GameplayVisualSetup
{
    const string SHEET_PATH  = "Assets/_Sprites/Character/Player_Spritesheet.png";
    const string CTRL_PATH   = "Assets/_Anim/Player.controller";
    const string CLIP_DIR    = "Assets/_Anim/Clips";

    // ── Row order in User-walk-v2.png ──────────────────────────────
    // Row 0 = walk Down, Row 1 = walk Left, Row 2 = walk Right, Row 3 = walk Up
    // Row 4 = death / malus (plays once then freezes)
    const int FRAMES        = 5;
    const int ROW_DOWN      = 0;
    const int ROW_LEFT      = 1;
    const int ROW_RIGHT     = 2;
    const int ROW_UP        = 3;
    const int ROW_DEATH     = 4;
    const float ANIM_FPS    = 8f;

    [MenuItem("Tools/Doom Scrolling/Setup Gameplay Visuals", priority = 2)]
    public static void Run()
    {
        Debug.Log("[Visuals] Starting gameplay visual setup...");

        if (!Directory.Exists(CLIP_DIR))  Directory.CreateDirectory(CLIP_DIR);
        if (!Directory.Exists("Assets/_Anim")) Directory.CreateDirectory("Assets/_Anim");

        FixPlayerSpriteImport();
        AssetDatabase.Refresh();

        var sprites = LoadSprites();
        if (sprites == null || sprites.Length < 25)
        {
            Debug.LogError($"[Visuals] Expected 25 sprites from player sheet, got {sprites?.Length}. Aborting.");
            return;
        }

        var controller = BuildAnimatorController(sprites);
        ApplyToPlayerPrefab(controller);
        FixCameraInScene();

        AssetDatabase.SaveAssets();
        Debug.Log("[Visuals] Done — reopen GameScene to see changes.");
    }

    // ── 1. Re-import player spritesheet at PPU=256, grid-slice 5×5 ──
    static void FixPlayerSpriteImport()
    {
        var imp = (TextureImporter)AssetImporter.GetAtPath(SHEET_PATH);
        if (imp == null) { Debug.LogError("[Visuals] Player spritesheet not found at " + SHEET_PATH); return; }

        imp.textureType         = TextureImporterType.Sprite;
        imp.spriteImportMode    = SpriteImportMode.Multiple;
        imp.filterMode          = FilterMode.Point;
        imp.textureCompression  = TextureImporterCompression.Uncompressed;
        imp.spritePixelsPerUnit = 256;  // 256×256 frame → 1×1 world unit
        imp.mipmapEnabled       = false;
        imp.alphaIsTransparency = true;

        // Grid-slice 5 columns × 5 rows
        var settings = new TextureImporterSettings();
        imp.ReadTextureSettings(settings);
        settings.spriteGenerateFallbackPhysicsShape = false;
        imp.SetTextureSettings(settings);

        // Build sprite rects
        // Unity stores rect.y from bottom of texture
        // Frame 0 (row 0, col 0) is at top of image → in Unity coords: y = height - frameH
        // We'll use SpriteMetaData for explicit control
        const int COLS = 5, ROWS = 5;
        // We need texture size — read from metadata
        int texW = 1280, texH = 1280;
        int fW = texW / COLS;
        int fH = texH / ROWS;

        var metas = new SpriteMetaData[COLS * ROWS];
        for (int row = 0; row < ROWS; row++)
        {
            for (int col = 0; col < COLS; col++)
            {
                int idx = row * COLS + col;
                // In Unity, rect.y=0 is at BOTTOM of texture
                // Image row 0 (top) → Unity y = texH - fH = 1024
                float ux = col * fW;
                float uy = texH - (row + 1) * fH;
                metas[idx] = new SpriteMetaData
                {
                    name       = $"Player_{idx:D2}",
                    rect       = new Rect(ux, uy, fW, fH),
                    alignment  = (int)SpriteAlignment.Center,
                    pivot      = new Vector2(0.5f, 0.5f),
                    border     = Vector4.zero
                };
            }
        }
        imp.spritesheet = metas;
        imp.SaveAndReimport();

        Debug.Log("[Visuals] Player spritesheet reimported at 256 PPU, 25 sprites.");
    }

    // ── 2. Load all 25 sprites in order ─────────────────────────────
    static Sprite[] LoadSprites()
    {
        return AssetDatabase.LoadAllAssetsAtPath(SHEET_PATH)
            .OfType<Sprite>()
            .OrderBy(s => s.name)   // Player_00 … Player_24
            .ToArray();
    }

    // ── 3. Build AnimatorController ──────────────────────────────────
    static AnimatorController BuildAnimatorController(Sprite[] sprites)
    {
        // Delete existing to start fresh
        if (File.Exists(CTRL_PATH)) AssetDatabase.DeleteAsset(CTRL_PATH);
        var controller = AnimatorController.CreateAnimatorControllerAtPath(CTRL_PATH);

        // Parameters
        controller.AddParameter("DirX",  AnimatorControllerParameterType.Float);
        controller.AddParameter("DirY",  AnimatorControllerParameterType.Float);
        controller.AddParameter("Death", AnimatorControllerParameterType.Trigger);
        controller.AddParameter("Malus", AnimatorControllerParameterType.Trigger);

        var sm = controller.layers[0].stateMachine;

        // Animation clips
        var clipDown  = MakeClip("Walk_Down",  sprites, ROW_DOWN,  FRAMES, true);
        var clipLeft  = MakeClip("Walk_Left",  sprites, ROW_LEFT,  FRAMES, true);
        var clipRight = MakeClip("Walk_Right", sprites, ROW_RIGHT, FRAMES, true);
        var clipUp    = MakeClip("Walk_Up",    sprites, ROW_UP,    FRAMES, true);
        var clipDeath = MakeClip("Death",      sprites, ROW_DEATH, FRAMES, false);
        var clipMalus = MakeClip("Malus",      sprites, ROW_DOWN,  FRAMES, true);

        // Walk blend tree (2D simple directional)
        BlendTree blendTree;
        var walkState = controller.CreateBlendTreeInController("Walk", out blendTree, 0);
        blendTree.blendType      = BlendTreeType.SimpleDirectional2D;
        blendTree.blendParameter  = "DirX";
        blendTree.blendParameterY = "DirY";
        blendTree.AddChild(clipDown,  new Vector2( 0f, -1f));
        blendTree.AddChild(clipLeft,  new Vector2(-1f,  0f));
        blendTree.AddChild(clipRight, new Vector2( 1f,  0f));
        blendTree.AddChild(clipUp,    new Vector2( 0f,  1f));
        sm.defaultState = walkState;

        // Death state
        var deathState = sm.AddState("Death");
        deathState.motion = clipDeath;
        deathState.speed  = 0.8f;

        // Malus state (slow flash)
        var malusState = sm.AddState("Malus");
        malusState.motion = clipMalus;
        malusState.speed  = 0.5f;  // half-speed when throttled

        // Transitions: Walk → Death
        var t1 = walkState.AddTransition(deathState);
        t1.hasExitTime = false; t1.duration = 0;
        t1.AddCondition(AnimatorConditionMode.If, 0, "Death");

        // Transitions: Walk → Malus
        var t2 = walkState.AddTransition(malusState);
        t2.hasExitTime = false; t2.duration = 0;
        t2.AddCondition(AnimatorConditionMode.If, 0, "Malus");

        // Malus → Walk (after one cycle)
        var t3 = malusState.AddTransition(walkState);
        t3.hasExitTime = true; t3.exitTime = 1f; t3.duration = 0;

        AssetDatabase.SaveAssets();
        Debug.Log("[Visuals] AnimatorController created at " + CTRL_PATH);
        return controller;
    }

    // Creates an AnimationClip from a row in the spritesheet
    static AnimationClip MakeClip(string clipName, Sprite[] sprites,
        int row, int frameCount, bool loop)
    {
        string path = $"{CLIP_DIR}/{clipName}.anim";
        if (File.Exists(path)) AssetDatabase.DeleteAsset(path);

        var clip = new AnimationClip();
        clip.frameRate = ANIM_FPS;
        clip.name      = clipName;

        var binding = EditorCurveBinding.PPtrCurve("", typeof(SpriteRenderer), "m_Sprite");
        var keyframes = new ObjectReferenceKeyframe[frameCount];
        for (int i = 0; i < frameCount; i++)
        {
            int idx = row * FRAMES + i;
            if (idx >= sprites.Length) idx = sprites.Length - 1;
            keyframes[i] = new ObjectReferenceKeyframe
            {
                time  = i / ANIM_FPS,
                value = sprites[idx]
            };
        }
        AnimationUtility.SetObjectReferenceCurve(clip, binding, keyframes);

        var settings = AnimationUtility.GetAnimationClipSettings(clip);
        settings.loopTime = loop;
        AnimationUtility.SetAnimationClipSettings(clip, settings);

        AssetDatabase.CreateAsset(clip, path);
        return clip;
    }

    // ── 4. Apply controller to player prefab + fix scale ─────────────
    static void ApplyToPlayerPrefab(AnimatorController controller)
    {
        // Find player prefab
        var guids = AssetDatabase.FindAssets("Player t:Prefab", new[] { "Assets/_Prefabs" });
        if (guids.Length == 0)
        {
            Debug.LogWarning("[Visuals] No Player prefab found in Assets/_Prefabs — applying to scene instance instead.");
            ApplyToScenePlayer(controller);
            return;
        }

        string prefabPath = AssetDatabase.GUIDToAssetPath(guids[0]);
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
        using var scope = new PrefabUtility.EditPrefabContentsScope(prefabPath);
        var root = scope.prefabContentsRoot;

        var anim = root.GetComponent<Animator>();
        if (anim == null) anim = root.AddComponent<Animator>();
        anim.runtimeAnimatorController = controller;

        // Scale: at 256 PPU, 1 sprite = 1 world unit. Scale 0.7 → fits in corridor.
        root.transform.localScale = new Vector3(0.7f, 0.7f, 1f);

        Debug.Log($"[Visuals] Applied controller + scale 0.7 to prefab: {prefabPath}");
    }

    static void ApplyToScenePlayer(AnimatorController controller)
    {
        var pc = Object.FindAnyObjectByType<PlayerController>();
        if (pc == null) { Debug.LogWarning("[Visuals] No PlayerController found in scene."); return; }

        var anim = pc.GetComponent<Animator>();
        if (anim == null) anim = pc.gameObject.AddComponent<Animator>();
        anim.runtimeAnimatorController = controller;

        pc.transform.localScale = new Vector3(0.7f, 0.7f, 1f);
        Debug.Log("[Visuals] Applied to scene player (prefab not found).");
    }

    // ── 5. Camera: center on maze + add CameraFit ───────────────────
    static void FixCameraInScene()
    {
        var scene = EditorSceneManager.OpenScene("Assets/_Scenes/GameScene.unity",
            OpenSceneMode.Single);

        var camGO = GameObject.FindWithTag("MainCamera");
        if (camGO == null) { Debug.LogWarning("[Visuals] MainCamera not found."); return; }

        // Center on maze (19×21 tiles, tiles start at world (0,0))
        camGO.transform.position = new Vector3(9.5f, 10.5f, -10f);

        // Add CameraFit if not present
        if (camGO.GetComponent<CameraFit>() == null)
            camGO.AddComponent<CameraFit>();

        // Set a good default orthographic size for Editor preview
        var cam = camGO.GetComponent<Camera>();
        if (cam != null) cam.orthographicSize = 11f;

        EditorSceneManager.SaveScene(scene);
        Debug.Log("[Visuals] Camera centered and CameraFit added.");
    }
}
#endif
