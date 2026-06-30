// AestheticOverhaul.cs
// Run: Tools > Doom Scrolling > Apply Aesthetic Overhaul
// Prerequisites: run GenerateSprites.ps1 first (produces CyberpunkCity.png, MazeFrame.png, HeartIcon.png)
#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.UI;
using TMPro;
using System.IO;
using System.Linq;

public static class AestheticOverhaul
{
    // Palette
    static Color CPink   = Hex("FF4D90");
    static Color CPurple = Hex("8115FF");
    static Color CViolet = Hex("786CF6");
    static Color CBlack  = Hex("120F1E");
    static Color CDeepBg = Hex("0D0A1C");
    static Color CGhost  = Hex("F7D8FF");
    static Color CBlue   = Hex("00F5FF");
    static Color CRed    = Hex("FF3A5E");
    static Color CNeonM  = Hex("008899");  // mid cyan

    static Color Hex(string h) { ColorUtility.TryParseHtmlString("#" + h, out Color c); return c; }

    [MenuItem("Tools/Doom Scrolling/Apply Aesthetic Overhaul", priority = 1)]
    public static void Run()
    {
        Debug.Log("[Aesthetic] Starting overhaul...");

        ConfigureNewSprites();
        AssetDatabase.Refresh();

        ApplyPostProcessing();
        AssetDatabase.Refresh();

        OverhaulGameScene();

        AssetDatabase.SaveAssets();
        Debug.Log("[Aesthetic] Done! Re-open GameScene to see changes.");
    }

    // ─────────────────────────────────────────────────────────────
    // SPRITE IMPORT — background, frame, heart (all new PNGs)
    // ─────────────────────────────────────────────────────────────
    static void ConfigureNewSprites()
    {
        var singles = new[] {
            "Assets/_Sprites/Background/CyberpunkCity.png",
            "Assets/_Sprites/UI/MazeFrame.png",
            "Assets/_Sprites/UI/HeartIcon.png",
        };
        foreach (var path in singles)
        {
            if (!File.Exists(path)) continue;
            var imp = (TextureImporter)AssetImporter.GetAtPath(path);
            if (imp == null) continue;
            imp.textureType         = TextureImporterType.Sprite;
            imp.spriteImportMode    = SpriteImportMode.Single;
            imp.filterMode          = FilterMode.Point;
            imp.textureCompression  = TextureImporterCompression.Uncompressed;
            imp.spritePixelsPerUnit = 16;  // 16 PPU matches Unity tilemap (1 tile = 1 world unit)
            imp.mipmapEnabled       = false;
            imp.alphaIsTransparency = true;
            imp.SaveAndReimport();
        }

        // MazeFrame: configure as 9-slice (border=8px each side)
        var frameImp = (TextureImporter)AssetImporter.GetAtPath("Assets/_Sprites/UI/MazeFrame.png");
        if (frameImp != null)
        {
            var settings = new TextureImporterSettings();
            frameImp.ReadTextureSettings(settings);
            settings.spriteBorder = new Vector4(8, 8, 8, 8); // L B R T
            frameImp.SetTextureSettings(settings);
            frameImp.spritePixelsPerUnit = 16;
            frameImp.SaveAndReimport();
        }

        Debug.Log("[Aesthetic] New sprite importers configured.");
    }

    // ─────────────────────────────────────────────────────────────
    // POST-PROCESSING — URP Global Volume with Bloom + Vignette
    // ─────────────────────────────────────────────────────────────
    static void ApplyPostProcessing()
    {
        const string PROFILE_PATH = "Assets/_PostProcessing/DoomScrollingProfile.asset";

        if (!Directory.Exists("Assets/_PostProcessing"))
            Directory.CreateDirectory("Assets/_PostProcessing");

        // Create or update Volume Profile
        var profile = AssetDatabase.LoadAssetAtPath<VolumeProfile>(PROFILE_PATH);
        if (profile == null)
        {
            profile = ScriptableObject.CreateInstance<VolumeProfile>();
            AssetDatabase.CreateAsset(profile, PROFILE_PATH);
        }

        // Bloom
        if (!profile.TryGet<Bloom>(out var bloom))
            bloom = profile.Add<Bloom>(true);
        bloom.active       = true;
        bloom.intensity.Override(1.2f);
        bloom.threshold.Override(0.7f);
        bloom.scatter.Override(0.6f);
        bloom.tint.Override(new Color(0.88f, 0.80f, 1.0f)); // slight violet tint

        // Vignette
        if (!profile.TryGet<Vignette>(out var vignette))
            vignette = profile.Add<Vignette>(true);
        vignette.active       = true;
        vignette.intensity.Override(0.42f);
        vignette.smoothness.Override(0.6f);
        vignette.color.Override(new Color(0.05f, 0.02f, 0.12f)); // deep purple vignette

        // Color Adjustments (push violet, boost saturation)
        if (!profile.TryGet<ColorAdjustments>(out var colorAdj))
            colorAdj = profile.Add<ColorAdjustments>(true);
        colorAdj.active             = true;
        colorAdj.saturation.Override(25f);
        colorAdj.contrast.Override(12f);

        EditorUtility.SetDirty(profile);
        AssetDatabase.SaveAssets();
        Debug.Log("[Aesthetic] Post-processing profile created: " + PROFILE_PATH);
    }

    // ─────────────────────────────────────────────────────────────
    // GAME SCENE OVERHAUL
    // ─────────────────────────────────────────────────────────────
    static void OverhaulGameScene()
    {
        var scene = EditorSceneManager.OpenScene("Assets/_Scenes/GameScene.unity",
            OpenSceneMode.Single);

        UpdateCamera();
        AddGlobalVolume();
        AddCyberpunkBackground();
        RebuildHUD();
        AddMazeFrame();

        EditorSceneManager.SaveScene(scene);
        Debug.Log("[Aesthetic] GameScene overhaul saved.");
    }

    // ─────────────────────────────────────────────────────────────
    // CAMERA
    // ─────────────────────────────────────────────────────────────
    static void UpdateCamera()
    {
        var camGO = GameObject.FindWithTag("MainCamera");
        if (camGO == null) return;
        var cam = camGO.GetComponent<Camera>();
        if (cam == null) return;

        cam.backgroundColor  = CDeepBg;
        cam.orthographicSize = 11f;  // slightly tighter framing

        // Enable post-processing on camera via URP camera data
        var urpData = camGO.GetComponent<UniversalAdditionalCameraData>();
        if (urpData == null) urpData = camGO.AddComponent<UniversalAdditionalCameraData>();
        urpData.renderPostProcessing = true;
        urpData.antialiasing         = AntialiasingMode.FastApproximateAntialiasing;
    }

    // ─────────────────────────────────────────────────────────────
    // GLOBAL VOLUME
    // ─────────────────────────────────────────────────────────────
    static void AddGlobalVolume()
    {
        var existing = Object.FindAnyObjectByType<Volume>();
        if (existing != null) Object.DestroyImmediate(existing.gameObject);

        var volGO   = new GameObject("PostProcessVolume");
        var vol     = volGO.AddComponent<Volume>();
        vol.isGlobal = true;

        var profile = AssetDatabase.LoadAssetAtPath<VolumeProfile>(
            "Assets/_PostProcessing/DoomScrollingProfile.asset");
        if (profile != null) vol.sharedProfile = profile;
    }

    // ─────────────────────────────────────────────────────────────
    // CYBERPUNK BACKGROUND LAYER
    // ─────────────────────────────────────────────────────────────
    static void AddCyberpunkBackground()
    {
        // Remove any existing bg
        var old = GameObject.Find("CyberpunkBackground");
        if (old != null) Object.DestroyImmediate(old);

        var bgSprite = AssetDatabase.LoadAssetAtPath<Sprite>(
            "Assets/_Sprites/Background/CyberpunkCity.png");
        if (bgSprite == null)
        {
            Debug.LogWarning("[Aesthetic] CyberpunkCity.png not found — run GenerateSprites.ps1 first.");
            return;
        }

        var bgGO = new GameObject("CyberpunkBackground");
        var sr   = bgGO.AddComponent<SpriteRenderer>();
        sr.sprite       = bgSprite;
        sr.sortingOrder = -10;

        // At 16 PPU, 304×336 px → 19×21 world units — exactly fits the maze grid
        bgGO.transform.position = new Vector3(9.5f, 10.5f, 1f);  // z=1 = behind tilemap

        bgGO.AddComponent<CyberpunkBackground>();
    }

    // ─────────────────────────────────────────────────────────────
    // MAZE FRAME OVERLAY
    // ─────────────────────────────────────────────────────────────
    static void AddMazeFrame()
    {
        var old = GameObject.Find("MazeFrame");
        if (old != null) Object.DestroyImmediate(old);

        var frameSprite = AssetDatabase.LoadAssetAtPath<Sprite>(
            "Assets/_Sprites/UI/MazeFrame.png");
        if (frameSprite == null) return;

        var frameGO = new GameObject("MazeFrame");
        var sr      = frameGO.AddComponent<SpriteRenderer>();
        sr.sprite       = frameSprite;
        sr.sortingOrder = 20;  // above everything
        sr.drawMode     = SpriteDrawMode.Sliced;
        sr.size         = new Vector2(19.8f, 21.8f);  // slightly larger than maze
        frameGO.transform.position = new Vector3(9.5f, 10.5f, -0.1f);
    }

    // ─────────────────────────────────────────────────────────────
    // HUD REBUILD — arcade Pac-Man style
    // ─────────────────────────────────────────────────────────────
    static void RebuildHUD()
    {
        // Find and destroy existing HUD Canvas
        var existingHUD = GameObject.Find("HUD Canvas");
        HUDController hudCtrl = null;
        if (existingHUD != null)
        {
            Object.DestroyImmediate(existingHUD);
        }

        // Also remove any old notification manager wiring (we'll rewire)
        var notifMgr = Object.FindAnyObjectByType<NotificationManager>();

        // Build new HUD Canvas
        var canvasGO = MakeCanvas("HUD Canvas", 1080, 1920);
        hudCtrl = canvasGO.AddComponent<HUDController>();

        // ── TOP BAR ────────────────────────────────────────────────
        // Dark semi-transparent strip at top
        var topBar = MakeImage(canvasGO.transform, "TopBar",
            new Color(CDeepBg.r, CDeepBg.g, CDeepBg.b, 0.92f),
            new Vector2(0, 0.926f), new Vector2(1, 1f));
        // Cyan neon border at bottom of top bar
        var topLine = MakeImage(canvasGO.transform, "TopBarLine", CBlue,
            new Vector2(0, 0.922f), new Vector2(1, 0.926f));

        // LIVES: ♥ ♥ ♥  (left side)
        var livesLbl = MakeTMP(canvasGO.transform, "LivesLabel", "♥ ♥ ♥",
            20, CPink, TextAlignmentOptions.Left,
            new Vector2(0.02f, 0.93f), new Vector2(0.25f, 0.995f));

        // LVL X  (centre-left)
        var lvlPre = MakeTMP(canvasGO.transform, "LvlPrefix", "LVL",
            10, CViolet, TextAlignmentOptions.Center,
            new Vector2(0.26f, 0.95f), new Vector2(0.42f, 0.99f));
        var levelLbl = MakeTMP(canvasGO.transform, "LevelLabel", "01",
            22, CGhost, TextAlignmentOptions.Center,
            new Vector2(0.26f, 0.93f), new Vector2(0.42f, 0.96f));

        // SCORE  (centre)
        var scPre = MakeTMP(canvasGO.transform, "ScorePrefix", "SCORE",
            10, CViolet, TextAlignmentOptions.Center,
            new Vector2(0.42f, 0.95f), new Vector2(0.72f, 0.99f));
        var scoreLbl = MakeTMP(canvasGO.transform, "ScoreLabel", "000000",
            22, CGhost, TextAlignmentOptions.Center,
            new Vector2(0.42f, 0.93f), new Vector2(0.72f, 0.96f));

        // TIME  (right)
        var timePre = MakeTMP(canvasGO.transform, "TimePrefix", "TIME",
            10, CViolet, TextAlignmentOptions.Right,
            new Vector2(0.72f, 0.95f), new Vector2(0.98f, 0.99f));
        var timerLbl = MakeTMP(canvasGO.transform, "TimerLabel", "00:00",
            22, CGhost, TextAlignmentOptions.Right,
            new Vector2(0.72f, 0.93f), new Vector2(0.98f, 0.96f));
        timerLbl.gameObject.AddComponent<TimerDisplay>();

        // ── BOTTOM BAR ─────────────────────────────────────────────
        var botBar = MakeImage(canvasGO.transform, "BottomBar",
            new Color(CDeepBg.r, CDeepBg.g, CDeepBg.b, 0.92f),
            new Vector2(0, 0f), new Vector2(1, 0.062f));
        var botLine = MakeImage(canvasGO.transform, "BottomBarLine", CBlue,
            new Vector2(0, 0.062f), new Vector2(1, 0.065f));

        // ENGAGEMENT label + bar
        var engageLbl = MakeTMP(canvasGO.transform, "EngagementLabel", "ENGAGEMENT 0%",
            11, CViolet, TextAlignmentOptions.Left,
            new Vector2(0.01f, 0.008f), new Vector2(0.32f, 0.058f));

        // Bar background
        var barBgGO = MakeImage(canvasGO.transform, "EngBarBg",
            new Color(0.08f, 0.05f, 0.16f), new Vector2(0.32f, 0.01f), new Vector2(0.98f, 0.055f));
        Bord2(barBgGO, CNeonM, 1);

        // Bar fill
        var barFillGO = MakeImage(canvasGO.transform, "EngBarFill", CPurple,
            new Vector2(0.32f, 0.012f), new Vector2(0.32f, 0.053f));
        var fillImg = barFillGO.GetComponent<Image>();
        fillImg.type       = Image.Type.Filled;
        fillImg.fillMethod = Image.FillMethod.Horizontal;
        fillImg.fillAmount = 0f;
        // Stretch fill to full bar width (anchor covers full range, fill controls visible width)
        var barFillRT = barFillGO.GetComponent<RectTransform>();
        barFillRT.anchorMin = new Vector2(0.32f, 0.012f);
        barFillRT.anchorMax = new Vector2(0.98f, 0.053f);
        barFillRT.offsetMin = barFillRT.offsetMax = Vector2.zero;

        // ── OVERLAY (clone phase, popup countdown) ─────────────────
        var overlayGO = MakeImage(canvasGO.transform, "OverlayPanel",
            new Color(0.05f, 0.02f, 0.12f, 0.82f),
            new Vector2(0.05f, 0.38f), new Vector2(0.95f, 0.62f));
        Bord2(overlayGO, CBlue, 2);
        overlayGO.SetActive(false);
        var overlayTxt = MakeTMP(overlayGO.transform, "OverlayText", "",
            26, CPink, TextAlignmentOptions.Center,
            Vector2.zero, Vector2.one);

        // ── SCROLLING TAGLINE ──────────────────────────────────────
        var tagGO = MakeImage(canvasGO.transform, "TaglineStrip",
            new Color(0, 0, 0, 0.5f),
            new Vector2(0, 0.065f), new Vector2(1, 0.088f));
        var tagTxt = MakeTMP(tagGO.transform, "TaglineText",
            "YOU ARE THE PRODUCT · MORE CONTENT MORE YOU · FEED THE SYSTEM · KEEP SCROLLING · ENGAGEMENT LVL ∞ ·",
            8, new Color(CViolet.r, CViolet.g, CViolet.b, 0.8f),
            TextAlignmentOptions.Left, new Vector2(0, 0), new Vector2(4, 1));
        tagGO.AddComponent<ScrollingTagline>();
        var tagSO = new SerializedObject(tagGO.GetComponent<ScrollingTagline>());
        tagSO.FindProperty("label").objectReferenceValue     = tagTxt;
        tagSO.FindProperty("container").objectReferenceValue = tagGO.GetComponent<RectTransform>();
        tagSO.ApplyModifiedProperties();

        // ── NOTIFICATION PANEL ─────────────────────────────────────
        var notifPanelGO = new GameObject("NotificationPanel");
        notifPanelGO.transform.SetParent(canvasGO.transform, false);
        var notifRT = notifPanelGO.AddComponent<RectTransform>();
        notifRT.anchorMin = new Vector2(0.55f, 0.5f);
        notifRT.anchorMax = new Vector2(1.0f,  0.63f);
        notifRT.offsetMin = notifRT.offsetMax = Vector2.zero;

        var notifBg = notifPanelGO.AddComponent<Image>();
        notifBg.color = new Color(0.05f, 0.03f, 0.12f, 0.95f);
        Bord2(notifPanelGO, CBlue, 1);

        var notifTxt = MakeTMP(notifPanelGO.transform, "NotifLabel", "",
            11, CGhost, TextAlignmentOptions.Left,
            new Vector2(0.04f, 0.1f), new Vector2(0.96f, 0.9f));

        // ── WIRE HUDCONTROLLER ──────────────────────────────────────
        var hudSO = new SerializedObject(hudCtrl);
        hudSO.FindProperty("scoreLabel").objectReferenceValue      = scoreLbl;
        hudSO.FindProperty("levelLabel").objectReferenceValue      = levelLbl;
        hudSO.FindProperty("livesLabel").objectReferenceValue      = livesLbl;
        hudSO.FindProperty("engagementFill").objectReferenceValue  = fillImg;
        hudSO.FindProperty("engagementLabel").objectReferenceValue = engageLbl;
        hudSO.FindProperty("overlayPanel").objectReferenceValue    = overlayGO;
        hudSO.FindProperty("overlayText").objectReferenceValue     = overlayTxt;
        hudSO.ApplyModifiedProperties();

        // ── WIRE NOTIFICATIONMANAGER ────────────────────────────────
        if (notifMgr != null)
        {
            var nmSO = new SerializedObject(notifMgr);
            nmSO.FindProperty("panelRoot").objectReferenceValue  = notifRT;
            nmSO.FindProperty("notifLabel").objectReferenceValue = notifTxt;
            nmSO.ApplyModifiedProperties();
        }
    }

    // ─────────────────────────────────────────────────────────────
    // HELPERS
    // ─────────────────────────────────────────────────────────────

    static GameObject MakeCanvas(string name, float rw, float rh)
    {
        var go     = new GameObject(name);
        var canvas = go.AddComponent<Canvas>();
        canvas.renderMode   = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 10;
        var scaler = go.AddComponent<CanvasScaler>();
        scaler.uiScaleMode         = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(rw, rh);
        scaler.matchWidthOrHeight  = 0.5f;
        go.AddComponent<GraphicRaycaster>();
        return go;
    }

    static GameObject MakeImage(Transform parent, string name, Color color,
        Vector2 anchorMin, Vector2 anchorMax)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.AddComponent<Image>().color = color;
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = anchorMin; rt.anchorMax = anchorMax;
        rt.offsetMin = rt.offsetMax = Vector2.zero;
        return go;
    }

    static TextMeshProUGUI MakeTMP(Transform parent, string name, string text,
        float fontSize, Color color, TextAlignmentOptions align,
        Vector2 anchorMin, Vector2 anchorMax)
    {
        var go  = new GameObject(name);
        go.transform.SetParent(parent, false);
        var tmp = go.AddComponent<TextMeshProUGUI>();
        tmp.text            = text;
        tmp.fontSize        = fontSize;
        tmp.color           = color;
        tmp.alignment       = align;
        tmp.textWrappingMode = TextWrappingModes.NoWrap;
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = anchorMin; rt.anchorMax = anchorMax;
        rt.offsetMin = rt.offsetMax = Vector2.zero;
        return tmp;
    }

    // Unity UI Outline effect — works on Image (adds coloured shadow at ±distance)
    static void Bord2(GameObject go, Color borderColor, int width)
    {
        var uiOutline = go.AddComponent<Outline>();
        uiOutline.effectColor    = new Color(borderColor.r, borderColor.g, borderColor.b, 0.8f);
        uiOutline.effectDistance = new Vector2(width, -width);
    }
}
#endif
