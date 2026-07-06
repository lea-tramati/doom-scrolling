using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

// Attach to: root Canvas in TitleScreen scene
public class TitleScreenController : MonoBehaviour
{
    [SerializeField] TextMeshProUGUI titleLabel;
    [SerializeField] TextMeshProUGUI subtitleLabel;
    [SerializeField] Button          openAppButton;
    [SerializeField] GameObject      howToPlayPanel;
    [SerializeField] Button          creditsButton;

    const string REDUCE_SHAKE_KEY = "ReduceScreenShake"; // must match CameraFollow's key

    float _glitchTimer;
    bool  _glitchOn;
    int   _glitchCount;
    const int MAX_GLITCHES = 3; // stop after a few reps — a screenshot or an idle player shouldn't see it loop forever

    GameObject _settingsPanel;

    void Start()
    {
        if (titleLabel)    titleLabel.text    = "DOOM SCROLLING";
        if (subtitleLabel) subtitleLabel.text = "YOU ARE ALREADY INSIDE.";

        if (openAppButton)
        {
            openAppButton.onClick.AddListener(OnOpenApp);
            // Gentle breathing pulse so the primary CTA visually outranks the
            // static credits button instead of both competing at equal weight.
            StartCoroutine(BreatheCTA(openAppButton.transform));
        }

        // How-to-play is now taught in-level (Level 1 contextual hints via
        // GameManager.ShowHintOnce) instead of a title-screen panel.
        if (howToPlayPanel) howToPlayPanel.SetActive(false);
        if (creditsButton)  creditsButton.onClick.AddListener(OnCredits);

        BuildSettingsUI();
        BuildVersionLabel();

        AudioManager.Instance?.PlayAmbientMusic();
    }

    IEnumerator BreatheCTA(Transform t)
    {
        const float period = 1.6f;
        const float amount  = 0.06f;
        while (true)
        {
            float phase = (Mathf.Sin(Time.time * (Mathf.PI * 2f / period)) + 1f) * 0.5f;
            t.localScale = Vector3.one * (1f + phase * amount);
            yield return null;
        }
    }

    void OnCredits()
    {
        SceneTransitionManager.Load("CreditsScreen");
    }

    void Update()
    {
        // Title glitch every 3s, capped so it doesn't loop forever on an idle title screen
        if (_glitchCount >= MAX_GLITCHES) return;
        _glitchTimer += Time.deltaTime;
        if (!_glitchOn && _glitchTimer >= 3f)
        {
            _glitchOn    = true;
            _glitchTimer = 0f;
            _glitchCount++;
            StartCoroutine(GlitchFlash());
        }
    }

    IEnumerator GlitchFlash()
    {
        if (titleLabel == null) { _glitchOn = false; yield break; }
        for (int i = 0; i < 4; i++)
        {
            titleLabel.rectTransform.anchoredPosition =
                new Vector2(i % 2 == 0 ? 4f : -4f, 0f);
            yield return new WaitForSeconds(0.07f);
        }
        titleLabel.rectTransform.anchoredPosition = Vector2.zero;
        _glitchOn = false;
    }

    void OnOpenApp()
    {
        if (GameManager.Instance != null)
            GameManager.Instance.StartGame();
        else
            SceneTransitionManager.Load("GameScene");  // fallback if no GameManager
    }

    // ── Settings panel (built at runtime — no scene wiring needed) ────

    void BuildSettingsUI()
    {
        var canvas = GetComponentInParent<Canvas>();
        if (canvas == null) return;

        var btnGO = CreateSimpleButton(canvas.transform, "SettingsButton", "SETTINGS",
            new Vector2(0f, 0f), new Vector2(140f, 44f), new Vector2(86f, 40f));
        btnGO.GetComponent<Button>().onClick.AddListener(() => _settingsPanel.SetActive(true));

        _settingsPanel = new GameObject("SettingsPanel", typeof(RectTransform));
        _settingsPanel.transform.SetParent(canvas.transform, false);
        var panelRT = _settingsPanel.GetComponent<RectTransform>();
        panelRT.anchorMin = Vector2.zero;
        panelRT.anchorMax = Vector2.one;
        panelRT.offsetMin = Vector2.zero;
        panelRT.offsetMax = Vector2.zero;
        _settingsPanel.SetActive(false);

        var backdrop = new GameObject("Backdrop", typeof(RectTransform), typeof(Image));
        backdrop.transform.SetParent(_settingsPanel.transform, false);
        var bdRT = backdrop.GetComponent<RectTransform>();
        bdRT.anchorMin = Vector2.zero;
        bdRT.anchorMax = Vector2.one;
        bdRT.offsetMin = Vector2.zero;
        bdRT.offsetMax = Vector2.zero;
        backdrop.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0.75f);

        var modal = new GameObject("Modal", typeof(RectTransform), typeof(Image));
        modal.transform.SetParent(_settingsPanel.transform, false);
        var modalRT = modal.GetComponent<RectTransform>();
        modalRT.anchorMin = modalRT.anchorMax = new Vector2(0.5f, 0.5f);
        modalRT.pivot = new Vector2(0.5f, 0.5f);
        modalRT.sizeDelta = new Vector2(520f, 480f);
        var modalImg = modal.GetComponent<Image>();
        modalImg.sprite = RoundedRectSprite(24);
        modalImg.type = Image.Type.Sliced;
        modalImg.color = new Color(0.08f, 0.08f, 0.11f, 0.97f);

        var title = CreateLabel(modal.transform, "SETTINGS", 28f, TextAlignmentOptions.Center,
            new Vector2(0.5f, 1f), new Vector2(400f, 50f), new Vector2(0f, -30f));
        title.color = new Color(1f, 0.30f, 0.56f);

        var musicSlider = CreateSliderRow(modal.transform, "MUSIC", -110f);
        musicSlider.value = AudioManager.Instance != null ? AudioManager.Instance.MusicVolume : 1f;
        musicSlider.onValueChanged.AddListener(v => AudioManager.Instance?.SetMusicVolume(v));

        var sfxSlider = CreateSliderRow(modal.transform, "SFX", -170f);
        sfxSlider.value = AudioManager.Instance != null ? AudioManager.Instance.SFXVolume : 1f;
        sfxSlider.onValueChanged.AddListener(v => AudioManager.Instance?.SetSFXVolume(v));

        var shakeToggle = CreateToggleRow(modal.transform, "REDUCE SCREEN SHAKE", -230f);
        shakeToggle.isOn = PlayerPrefs.GetInt(REDUCE_SHAKE_KEY, 0) == 1;
        shakeToggle.onValueChanged.AddListener(v => PlayerPrefs.SetInt(REDUCE_SHAKE_KEY, v ? 1 : 0));

        var closeBtn = CreateSimpleButton(modal.transform, "CloseButton", "[ CLOSE ]",
            new Vector2(0.5f, 0f), new Vector2(180f, 50f), new Vector2(0f, 30f));
        closeBtn.GetComponent<Button>().onClick.AddListener(() => _settingsPanel.SetActive(false));
    }

    void BuildVersionLabel()
    {
        var canvas = GetComponentInParent<Canvas>();
        if (canvas == null) return;

        var label = CreateLabel(canvas.transform, $"v{Application.version}", 14f, TextAlignmentOptions.BottomRight,
            new Vector2(1f, 0f), new Vector2(160f, 30f), new Vector2(-12f, 12f));
        label.color = new Color(1f, 1f, 1f, 0.35f);
        label.raycastTarget = false;
    }

    // ── Runtime UI helpers (procedural — no sprite/prefab assets required) ──

    static GameObject CreateSimpleButton(Transform parent, string name, string label,
        Vector2 anchor, Vector2 sizeDelta, Vector2 anchoredPos)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
        go.transform.SetParent(parent, false);
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = rt.pivot = anchor;
        rt.sizeDelta = sizeDelta;
        rt.anchoredPosition = anchoredPos;

        var img = go.GetComponent<Image>();
        img.sprite = RoundedRectSprite(12);
        img.type = Image.Type.Sliced;
        img.color = new Color(1f, 1f, 1f, 0.12f);

        var lbl = CreateLabel(go.transform, label, 14f, TextAlignmentOptions.Center,
            new Vector2(0.5f, 0.5f), sizeDelta, Vector2.zero);
        lbl.raycastTarget = false;

        go.GetComponent<Button>().targetGraphic = img;
        return go;
    }

    static TextMeshProUGUI CreateLabel(Transform parent, string text, float size, TextAlignmentOptions align,
        Vector2 anchor, Vector2 sizeDelta, Vector2 anchoredPos)
    {
        var go = new GameObject("Label", typeof(RectTransform));
        go.transform.SetParent(parent, false);
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = rt.pivot = anchor;
        rt.sizeDelta = sizeDelta;
        rt.anchoredPosition = anchoredPos;

        var tmp = go.AddComponent<TextMeshProUGUI>();
        tmp.text = text;
        tmp.fontSize = size;
        tmp.alignment = align;
        tmp.color = Color.white;
        return tmp;
    }

    static Slider CreateSliderRow(Transform parent, string label, float yOffset)
    {
        CreateLabel(parent, label, 16f, TextAlignmentOptions.Center,
            new Vector2(0.5f, 1f), new Vector2(300f, 26f), new Vector2(0f, yOffset));

        var sliderGO = new GameObject($"{label}Slider", typeof(RectTransform), typeof(Slider));
        sliderGO.transform.SetParent(parent, false);
        var rt = sliderGO.GetComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 1f);
        rt.pivot = new Vector2(0.5f, 1f);
        rt.sizeDelta = new Vector2(320f, 20f);
        rt.anchoredPosition = new Vector2(0f, yOffset - 28f);

        var bg = new GameObject("Background", typeof(RectTransform), typeof(Image));
        bg.transform.SetParent(sliderGO.transform, false);
        StretchFull(bg.GetComponent<RectTransform>());
        bg.GetComponent<Image>().color = new Color(1f, 1f, 1f, 0.15f);

        var fillArea = new GameObject("Fill Area", typeof(RectTransform));
        fillArea.transform.SetParent(sliderGO.transform, false);
        StretchFull(fillArea.GetComponent<RectTransform>());

        var fill = new GameObject("Fill", typeof(RectTransform), typeof(Image));
        fill.transform.SetParent(fillArea.transform, false);
        var fillRT = fill.GetComponent<RectTransform>();
        fillRT.anchorMin = Vector2.zero;
        fillRT.anchorMax = new Vector2(0f, 1f);
        fillRT.offsetMin = Vector2.zero;
        fillRT.offsetMax = Vector2.zero;
        fill.GetComponent<Image>().color = new Color(1f, 0.30f, 0.56f);

        var handleArea = new GameObject("Handle Slide Area", typeof(RectTransform));
        handleArea.transform.SetParent(sliderGO.transform, false);
        StretchFull(handleArea.GetComponent<RectTransform>());

        var handle = new GameObject("Handle", typeof(RectTransform), typeof(Image));
        handle.transform.SetParent(handleArea.transform, false);
        handle.GetComponent<RectTransform>().sizeDelta = new Vector2(16f, 16f);
        handle.GetComponent<Image>().color = Color.white;

        var slider = sliderGO.GetComponent<Slider>();
        slider.fillRect = fillRT;
        slider.handleRect = handle.GetComponent<RectTransform>();
        slider.targetGraphic = handle.GetComponent<Image>();
        slider.minValue = 0f;
        slider.maxValue = 1f;
        slider.value = 1f;

        return slider;
    }

    static Toggle CreateToggleRow(Transform parent, string label, float yOffset)
    {
        var rowGO = new GameObject($"{label}Toggle", typeof(RectTransform), typeof(Toggle));
        rowGO.transform.SetParent(parent, false);
        var rt = rowGO.GetComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 1f);
        rt.pivot = new Vector2(0.5f, 1f);
        rt.sizeDelta = new Vector2(320f, 30f);
        rt.anchoredPosition = new Vector2(0f, yOffset);

        var bg = new GameObject("Background", typeof(RectTransform), typeof(Image));
        bg.transform.SetParent(rowGO.transform, false);
        var bgRT = bg.GetComponent<RectTransform>();
        bgRT.anchorMin = bgRT.anchorMax = new Vector2(0f, 0.5f);
        bgRT.pivot = new Vector2(0f, 0.5f);
        bgRT.sizeDelta = new Vector2(24f, 24f);
        bg.GetComponent<Image>().color = new Color(1f, 1f, 1f, 0.15f);

        var checkmark = new GameObject("Checkmark", typeof(RectTransform), typeof(Image));
        checkmark.transform.SetParent(bg.transform, false);
        var cmRT = checkmark.GetComponent<RectTransform>();
        cmRT.anchorMin = Vector2.zero;
        cmRT.anchorMax = Vector2.one;
        cmRT.offsetMin = new Vector2(4f, 4f);
        cmRT.offsetMax = new Vector2(-4f, -4f);
        checkmark.GetComponent<Image>().color = new Color(1f, 0.30f, 0.56f);

        CreateLabel(rowGO.transform, label, 15f, TextAlignmentOptions.Left,
            new Vector2(0f, 0.5f), new Vector2(260f, 26f), new Vector2(34f, 0f));

        var toggle = rowGO.GetComponent<Toggle>();
        toggle.targetGraphic = bg.GetComponent<Image>();
        toggle.graphic = checkmark.GetComponent<Image>();
        toggle.isOn = true;

        return toggle;
    }

    static void StretchFull(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
    }

    // Same procedural-sprite technique as NotificationManager — no sprite asset dependency.
    static Sprite RoundedRectSprite(int radius)
    {
        const int S = 128;
        int R = Mathf.Min(radius, S / 2 - 1);
        var tex = new Texture2D(S, S, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Bilinear;
        Color[] px = new Color[S * S];
        for (int y = 0; y < S; y++)
        for (int x = 0; x < S; x++)
        {
            float cx = Mathf.Clamp(x, R, S - R - 1);
            float cy = Mathf.Clamp(y, R, S - R - 1);
            float d  = Mathf.Sqrt((x - cx) * (x - cx) + (y - cy) * (y - cy));
            float a  = Mathf.Clamp01(R - d + 0.5f);
            px[y * S + x] = new Color(1f, 1f, 1f, a);
        }
        tex.SetPixels(px);
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, S, S),
            new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect,
            new Vector4(R, R, R, R));
    }
}
