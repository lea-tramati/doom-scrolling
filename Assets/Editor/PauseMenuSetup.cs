// PauseMenuSetup.cs
// Builds an Escape-key pause menu in GameScene: dim backdrop + centered panel with
// Resume/Restart/Title/Quit (cloned from GameOverScreen's already-styled buttons, for
// visual consistency) plus Music/SFX volume sliders wired to AudioManager.
// Idempotent — safe to re-run from Tools > Doom Scrolling > Setup Pause Menu.
#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public static class PauseMenuSetup
{
    const string GAME_SCENE_PATH     = "Assets/_Scenes/GameScene.unity";
    const string GAMEOVER_SCENE_PATH = "Assets/_Scenes/GameOverScreen.unity";
    const string PIXEL_FONT_PATH     = "Assets/TextMesh Pro/Resources/Fonts & Materials/PressStart2P SDF.asset";
    const string PANEL_SPRITE_PATH   = "Assets/_Sprites/UI/RoundedPanel.png";

    static readonly Color PanelBg  = new Color(0.08f, 0.08f, 0.11f, 0.96f);
    static readonly Color AccentPink = new Color(1f, 0.30f, 0.56f);

    static PauseMenuSetup()
    {
        EditorApplication.delayCall += () => Run(silentIfDone: true);
    }

    [MenuItem("Tools/Doom Scrolling/Setup Pause Menu")]
    public static void RunFromMenu() => Run(silentIfDone: false);

    static void Run(bool silentIfDone)
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) return;
        if (!File.Exists(GAME_SCENE_PATH) || !File.Exists(GAMEOVER_SCENE_PATH)) return;

        var gsAlready = EditorSceneManager.GetSceneByPath(GAME_SCENE_PATH);
        bool gsWasLoaded = gsAlready.IsValid() && gsAlready.isLoaded;
        var gameScene = gsWasLoaded ? gsAlready : EditorSceneManager.OpenScene(GAME_SCENE_PATH, OpenSceneMode.Additive);

        HUDController hud = null;
        foreach (var root in gameScene.GetRootGameObjects())
        {
            hud = root.GetComponentInChildren<HUDController>(true);
            if (hud != null) break;
        }

        if (hud == null)
        {
            Debug.LogWarning("[PauseMenuSetup] No HUDController found in GameScene.");
            if (!gsWasLoaded) EditorSceneManager.CloseScene(gameScene, true);
            return;
        }

        var canvas = hud.GetComponentInParent<Canvas>();
        if (canvas == null)
        {
            Debug.LogWarning("[PauseMenuSetup] No parent Canvas found for HUDController.");
            if (!gsWasLoaded) EditorSceneManager.CloseScene(gameScene, true);
            return;
        }

        if (canvas.transform.Find("PauseMenuController") != null)
        {
            if (!silentIfDone) Debug.Log("[PauseMenuSetup] Nothing to do — already set up.");
            if (!gsWasLoaded) EditorSceneManager.CloseScene(gameScene, true);
            return;
        }

        // Pull reference buttons from GameOverScreen so the pause menu matches the
        // game's existing button style instead of introducing a new one.
        var goAlready = EditorSceneManager.GetSceneByPath(GAMEOVER_SCENE_PATH);
        bool goWasLoaded = goAlready.IsValid() && goAlready.isLoaded;
        var goScene = goWasLoaded ? goAlready : EditorSceneManager.OpenScene(GAMEOVER_SCENE_PATH, OpenSceneMode.Additive);

        Button refPlayAgain = null, refTitle = null, refQuit = null;
        foreach (var root in goScene.GetRootGameObjects())
        {
            var goController = root.GetComponentInChildren<GameOverScreenController>(true);
            if (goController == null) continue;
            var so = new SerializedObject(goController);
            refPlayAgain = so.FindProperty("playAgainBtn")?.objectReferenceValue as Button;
            refTitle     = so.FindProperty("titleBtn")?.objectReferenceValue as Button;
            refQuit      = so.FindProperty("quitBtn")?.objectReferenceValue as Button;
            break;
        }

        if (refPlayAgain == null)
        {
            Debug.LogWarning("[PauseMenuSetup] Couldn't find a reference button on GameOverScreenController to clone.");
            if (!goWasLoaded) EditorSceneManager.CloseScene(goScene, true);
            if (!gsWasLoaded) EditorSceneManager.CloseScene(gameScene, true);
            return;
        }

        // ── Controller root (stays active — needs to poll Escape even while the panel is hidden) ──
        var controllerGO = new GameObject("PauseMenuController", typeof(RectTransform));
        controllerGO.transform.SetParent(canvas.transform, false);
        var controllerRT = controllerGO.GetComponent<RectTransform>();
        controllerRT.anchorMin = Vector2.zero;
        controllerRT.anchorMax = Vector2.one;
        controllerRT.offsetMin = Vector2.zero;
        controllerRT.offsetMax = Vector2.zero;
        var controller = controllerGO.AddComponent<PauseMenuController>();

        // ── Panel root (toggled active/inactive by the controller) ──
        var panelGO = new GameObject("PauseMenu", typeof(RectTransform));
        panelGO.transform.SetParent(controllerGO.transform, false);
        var panelRootRT = panelGO.GetComponent<RectTransform>();
        panelRootRT.anchorMin = Vector2.zero;
        panelRootRT.anchorMax = Vector2.one;
        panelRootRT.offsetMin = Vector2.zero;
        panelRootRT.offsetMax = Vector2.zero;
        panelGO.SetActive(false);

        // Dim backdrop
        var backdropGO = new GameObject("Backdrop", typeof(RectTransform), typeof(Image));
        backdropGO.transform.SetParent(panelGO.transform, false);
        var backdropRT = backdropGO.GetComponent<RectTransform>();
        backdropRT.anchorMin = Vector2.zero;
        backdropRT.anchorMax = Vector2.one;
        backdropRT.offsetMin = Vector2.zero;
        backdropRT.offsetMax = Vector2.zero;
        backdropGO.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0.75f);

        // Centered modal
        var modalGO = new GameObject("Modal", typeof(RectTransform), typeof(Image));
        modalGO.transform.SetParent(panelGO.transform, false);
        var modalRT = modalGO.GetComponent<RectTransform>();
        modalRT.anchorMin = modalRT.anchorMax = new Vector2(0.5f, 0.5f);
        modalRT.pivot = new Vector2(0.5f, 0.5f);
        modalRT.sizeDelta = new Vector2(560f, 680f);
        modalRT.anchoredPosition = Vector2.zero;
        var modalImg = modalGO.GetComponent<Image>();
        modalImg.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(PANEL_SPRITE_PATH);
        modalImg.type = Image.Type.Sliced;
        modalImg.color = PanelBg;

        var pixelFont = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(PIXEL_FONT_PATH);

        // Title
        var titleGO = new GameObject("PausedLabel", typeof(RectTransform));
        titleGO.transform.SetParent(modalGO.transform, false);
        var titleRT = titleGO.GetComponent<RectTransform>();
        titleRT.anchorMin = titleRT.anchorMax = new Vector2(0.5f, 1f);
        titleRT.pivot = new Vector2(0.5f, 1f);
        titleRT.sizeDelta = new Vector2(480f, 60f);
        titleRT.anchoredPosition = new Vector2(0f, -36f);
        var titleTMP = titleGO.AddComponent<TextMeshProUGUI>();
        titleTMP.text = "PAUSED";
        titleTMP.alignment = TextAlignmentOptions.Center;
        titleTMP.fontSize = 32f;
        titleTMP.color = AccentPink;
        if (pixelFont != null) titleTMP.font = pixelFont;

        // Buttons — clone GameOverScreen's styled buttons and stack them below the title
        var anchorRt = refPlayAgain.GetComponent<RectTransform>();
        float stepY = anchorRt.sizeDelta.y + 20f;
        float firstY = -140f;

        var resumeBtn  = CloneButton(refPlayAgain, anchorRt, modalGO.transform, "ResumeButton",  "[ RESUME ]",  firstY);
        var restartBtn = CloneButton(refPlayAgain, anchorRt, modalGO.transform, "RestartButton", "[ RESTART ]", firstY - stepY);
        var titleBtn   = CloneButton(refTitle != null ? refTitle : refPlayAgain, anchorRt, modalGO.transform, "TitleButton", "[ TITLE ]", firstY - stepY * 2);
        var quitBtn    = CloneButton(refQuit  != null ? refQuit  : refPlayAgain, anchorRt, modalGO.transform, "QuitButton",  "[ QUIT ]",  firstY - stepY * 3);

        // Volume sliders below the buttons
        var musicSlider = CreateSliderRow(modalGO.transform, "MusicVolumeRow", "MUSIC", pixelFont, firstY - stepY * 4 - 40f);
        var sfxSlider   = CreateSliderRow(modalGO.transform, "SFXVolumeRow",   "SFX",   pixelFont, firstY - stepY * 4 - 100f);

        var so2 = new SerializedObject(controller);
        so2.FindProperty("panel").objectReferenceValue       = panelGO;
        so2.FindProperty("resumeBtn").objectReferenceValue   = resumeBtn;
        so2.FindProperty("restartBtn").objectReferenceValue  = restartBtn;
        so2.FindProperty("titleBtn").objectReferenceValue    = titleBtn;
        so2.FindProperty("quitBtn").objectReferenceValue     = quitBtn;
        so2.FindProperty("musicSlider").objectReferenceValue = musicSlider;
        so2.FindProperty("sfxSlider").objectReferenceValue   = sfxSlider;
        so2.ApplyModifiedProperties();

        EditorSceneManager.MarkSceneDirty(gameScene);
        EditorSceneManager.SaveScene(gameScene);
        Debug.Log("[PauseMenuSetup] Created pause menu (Resume/Restart/Title/Quit + volume sliders) in GameScene.");

        if (!goWasLoaded) EditorSceneManager.CloseScene(goScene, true);
        if (!gsWasLoaded) EditorSceneManager.CloseScene(gameScene, true);
    }

    static Button CloneButton(Button reference, RectTransform anchorRt, Transform parent, string name, string label, float yOffset)
    {
        var clone = Object.Instantiate(reference.gameObject, parent);
        clone.name = name;
        var rt = clone.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 1f);
        rt.anchorMax = new Vector2(0.5f, 1f);
        rt.pivot     = new Vector2(0.5f, 1f);
        rt.sizeDelta = anchorRt.sizeDelta;
        rt.anchoredPosition = new Vector2(0f, yOffset);

        var label_ = clone.GetComponentInChildren<TextMeshProUGUI>(true);
        if (label_ != null) label_.text = label;

        return clone.GetComponent<Button>();
    }

    static Slider CreateSliderRow(Transform parent, string name, string labelText, TMP_FontAsset font, float yOffset)
    {
        var rowGO = new GameObject(name, typeof(RectTransform));
        rowGO.transform.SetParent(parent, false);
        var rowRT = rowGO.GetComponent<RectTransform>();
        rowRT.anchorMin = rowRT.anchorMax = new Vector2(0.5f, 1f);
        rowRT.pivot = new Vector2(0.5f, 1f);
        rowRT.sizeDelta = new Vector2(440f, 40f);
        rowRT.anchoredPosition = new Vector2(0f, yOffset);

        var labelGO = new GameObject("Label", typeof(RectTransform));
        labelGO.transform.SetParent(rowGO.transform, false);
        var labelRT = labelGO.GetComponent<RectTransform>();
        labelRT.anchorMin = new Vector2(0f, 0.5f);
        labelRT.anchorMax = new Vector2(0f, 0.5f);
        labelRT.pivot = new Vector2(0f, 0.5f);
        labelRT.sizeDelta = new Vector2(100f, 30f);
        labelRT.anchoredPosition = Vector2.zero;
        var labelTMP = labelGO.AddComponent<TextMeshProUGUI>();
        labelTMP.text = labelText;
        labelTMP.fontSize = 16f;
        labelTMP.alignment = TextAlignmentOptions.Left;
        labelTMP.color = Color.white;
        if (font != null) labelTMP.font = font;

        var sliderGO = new GameObject("Slider", typeof(RectTransform), typeof(Slider));
        sliderGO.transform.SetParent(rowGO.transform, false);
        var sliderRT = sliderGO.GetComponent<RectTransform>();
        sliderRT.anchorMin = new Vector2(0f, 0.5f);
        sliderRT.anchorMax = new Vector2(1f, 0.5f);
        sliderRT.pivot = new Vector2(0f, 0.5f);
        sliderRT.offsetMin = new Vector2(110f, -10f);
        sliderRT.offsetMax = new Vector2(0f, 10f);

        var bgGO = new GameObject("Background", typeof(RectTransform), typeof(Image));
        bgGO.transform.SetParent(sliderGO.transform, false);
        var bgRT = bgGO.GetComponent<RectTransform>();
        bgRT.anchorMin = Vector2.zero; bgRT.anchorMax = Vector2.one;
        bgRT.offsetMin = Vector2.zero; bgRT.offsetMax = Vector2.zero;
        bgGO.GetComponent<Image>().color = new Color(1f, 1f, 1f, 0.15f);

        var fillAreaGO = new GameObject("Fill Area", typeof(RectTransform));
        fillAreaGO.transform.SetParent(sliderGO.transform, false);
        var fillAreaRT = fillAreaGO.GetComponent<RectTransform>();
        fillAreaRT.anchorMin = Vector2.zero; fillAreaRT.anchorMax = Vector2.one;
        fillAreaRT.offsetMin = Vector2.zero; fillAreaRT.offsetMax = Vector2.zero;

        var fillGO = new GameObject("Fill", typeof(RectTransform), typeof(Image));
        fillGO.transform.SetParent(fillAreaGO.transform, false);
        var fillRT = fillGO.GetComponent<RectTransform>();
        fillRT.anchorMin = Vector2.zero; fillRT.anchorMax = new Vector2(0f, 1f);
        fillRT.offsetMin = Vector2.zero; fillRT.offsetMax = Vector2.zero;
        fillGO.GetComponent<Image>().color = AccentPink;

        var handleAreaGO = new GameObject("Handle Slide Area", typeof(RectTransform));
        handleAreaGO.transform.SetParent(sliderGO.transform, false);
        var handleAreaRT = handleAreaGO.GetComponent<RectTransform>();
        handleAreaRT.anchorMin = Vector2.zero; handleAreaRT.anchorMax = Vector2.one;
        handleAreaRT.offsetMin = Vector2.zero; handleAreaRT.offsetMax = Vector2.zero;

        var handleGO = new GameObject("Handle", typeof(RectTransform), typeof(Image));
        handleGO.transform.SetParent(handleAreaGO.transform, false);
        var handleRT = handleGO.GetComponent<RectTransform>();
        handleRT.sizeDelta = new Vector2(20f, 20f);
        handleGO.GetComponent<Image>().color = Color.white;

        var slider = sliderGO.GetComponent<Slider>();
        slider.fillRect = fillRT;
        slider.handleRect = handleRT;
        slider.targetGraphic = handleGO.GetComponent<Image>();
        slider.direction = Slider.Direction.LeftToRight;
        slider.minValue = 0f;
        slider.maxValue = 1f;
        slider.value = 1f;

        return slider;
    }
}
#endif
