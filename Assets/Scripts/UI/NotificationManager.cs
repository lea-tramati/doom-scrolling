using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using TMPro;

// iOS-style notification card that drops from the top of the screen.
// Layout:  [AppIcon]  AppName          now
//                     Bold message text
public class NotificationManager : MonoBehaviour
{
    public static NotificationManager Instance { get; private set; }

    [Header("Panel root (assign the notification panel RectTransform)")]
    [SerializeField] RectTransform    panelRoot;
    [SerializeField] TextMeshProUGUI  appLabel;
    [SerializeField] TextMeshProUGUI  timeLabel;
    [SerializeField] TextMeshProUGUI  notifLabel;

    [Header("Timing")]
    [SerializeField] float slideSpeed  = 1400f;
    [SerializeField] float displayTime = 3.5f;
    [SerializeField] float minInterval = 15f;
    [SerializeField] float maxInterval = 40f;

    // Panel geometry (canvas units)
    const float PanelW  = 420f;
    const float PanelH  = 80f;
    const float ShownY  = -14f;         // visible: 14 units below canvas top
    const float HiddenY = PanelH + 14f; // hidden:  fully above canvas top

    // ── iOS palette ───────────────────────────────────────────────
    static readonly Color BgColor      = new Color(0.97f, 0.97f, 0.98f, 0.96f); // white frosted
    static readonly Color ShadowColor  = new Color(0f,    0f,    0f,    0.14f); // subtle drop shadow
    static readonly Color AppNameColor = new Color(0.40f, 0.40f, 0.45f, 1f);   // medium gray
    static readonly Color TimeColor    = new Color(0.58f, 0.58f, 0.62f, 1f);   // lighter gray
    static readonly Color MsgColor     = new Color(0.08f, 0.08f, 0.10f, 1f);   // near-black

    // Per-app icon color + single letter displayed inside the icon
    static readonly Dictionary<string, (Color col, string letter)> AppStyles = new()
    {
        { "INSTAGRAM",   (new Color(0.86f, 0.15f, 0.44f), "I") },
        { "TIKTOK",      (new Color(0.05f, 0.05f, 0.05f), "T") },
        { "ANALYTICS",   (new Color(0.20f, 0.40f, 0.90f), "A") },
        { "SCREENTIME",  (new Color(0.20f, 0.75f, 0.30f), "S") },
        { "TWITTER",     (new Color(0.11f, 0.63f, 0.95f), "X") },
        { "SYSTEM",      (new Color(0.95f, 0.30f, 0.20f), "!") },
        { "VIRAL",       (new Color(0.60f, 0.20f, 0.90f), "V") },
        { "DOOM·SCROLL", (new Color(1.00f, 0.30f, 0.56f), "D") },
        { "ALERTS",      (new Color(0.95f, 0.60f, 0.10f), "A") },
    };

    static readonly Dictionary<string, string> IconKeyToApp = new()
    {
        { "malus",  "SYSTEM" },
        { "clone",  "VIRAL"  },
        { "auto",   ""       },
        { "notif",  "ALERTS" },
        { "follow", "INSTAGRAM" },
        { "like",   "INSTAGRAM" },
    };

    readonly Queue<(string text, string app)> _queue = new();
    bool _showing;

    Image           _iconBg;
    TextMeshProUGUI _iconLetter;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    void Start()
    {
        BuildPanelLayout();
        StartCoroutine(AutoTriggerLoop());
    }

    // ── Public API ────────────────────────────────────────────────

    public void TriggerNotification(string text, string iconKey)
    {
        string app = IconKeyToApp.TryGetValue(iconKey, out var a) ? a : "ALERTS";
        Enqueue(text, app);
    }

    // ── Queue & slide ─────────────────────────────────────────────

    void Enqueue(string text, string app)
    {
        _queue.Enqueue((text, app));
        if (!_showing) StartCoroutine(ProcessQueue());
    }

    IEnumerator ProcessQueue()
    {
        while (_queue.Count > 0)
        {
            _showing = true;
            var (text, app) = _queue.Dequeue();
            string displayApp = string.IsNullOrEmpty(app) ? "DOOM·SCROLL" : app;

            if (notifLabel) notifLabel.text = text;
            if (appLabel)   appLabel.text   = displayApp;
            if (timeLabel)  timeLabel.text  = "now";

            if (AppStyles.TryGetValue(displayApp, out var style))
            {
                if (_iconBg)     _iconBg.color     = style.col;
                if (_iconLetter) _iconLetter.text  = style.letter;
            }

            AudioManager.Instance?.PlaySFX("notif_slide");

            yield return StartCoroutine(SlideTo(ShownY));
            yield return new WaitForSeconds(displayTime);
            yield return StartCoroutine(SlideTo(HiddenY));
            yield return new WaitForSeconds(0.1f);
        }
        _showing = false;
    }

    IEnumerator SlideTo(float targetY)
    {
        if (panelRoot == null) yield break;
        float startY = panelRoot.anchoredPosition.y;
        float dist   = Mathf.Abs(targetY - startY);
        if (dist < 0.5f) yield break;

        float elapsed  = 0f;
        float duration = dist / slideSpeed;
        bool  slidingIn = targetY < startY;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t     = Mathf.Clamp01(elapsed / duration);
            float eased = slidingIn ? EaseOutCubic(t) : EaseInCubic(t);
            panelRoot.anchoredPosition = new Vector2(
                panelRoot.anchoredPosition.x,
                Mathf.Lerp(startY, targetY, eased));
            yield return null;
        }
        panelRoot.anchoredPosition = new Vector2(panelRoot.anchoredPosition.x, targetY);
    }

    // ── Auto notifications ────────────────────────────────────────

    IEnumerator AutoTriggerLoop()
    {
        while (true)
        {
            yield return new WaitForSeconds(Random.Range(minInterval, maxInterval));
            if (GameManager.Instance == null || !GameManager.Instance.IsPlaying) continue;

            int lvl   = GameManager.Instance.Level;
            int score = GameManager.Instance.Score;
            int min   = (int)(GameManager.Instance.SessionTimer / 60);

            int pick = Random.Range(0, 5);
            string[] apps = { "INSTAGRAM", "TIKTOK", "ANALYTICS", "SCREENTIME", "TWITTER" };
            string text = pick switch
            {
                0 => $"{score / 50} Likes",
                1 => $"{Random.Range(lvl * 100, lvl * 500)} Profile Views",
                2 => $"Engagement {Random.Range(60, 99)}%",
                3 => $"{min} min scrolled",
                _ => $"New Followers: +{GameManager.Instance.AppIconsThisLevel * 10}"
            };
            Enqueue(text, apps[pick]);
        }
    }

    // ── Layout builder ────────────────────────────────────────────

    void BuildPanelLayout()
    {
        if (panelRoot == null) return;

        // Anchor to top-center, drop down
        panelRoot.anchorMin = panelRoot.anchorMax = new Vector2(0.5f, 1f);
        panelRoot.pivot     = new Vector2(0.5f, 1f);
        panelRoot.sizeDelta = new Vector2(PanelW, PanelH);
        panelRoot.anchoredPosition = new Vector2(0f, HiddenY);

        // 1. Drop shadow (first child = behind everything)
        if (panelRoot.Find("Shadow") == null)
        {
            var sh    = new GameObject("Shadow");
            sh.transform.SetParent(panelRoot, false);
            sh.transform.SetAsFirstSibling();
            var shRt  = sh.AddComponent<RectTransform>();
            shRt.anchorMin = Vector2.zero;
            shRt.anchorMax = Vector2.one;
            shRt.offsetMin = new Vector2(-2f, -6f);
            shRt.offsetMax = new Vector2(2f,  2f);
            var shImg                    = sh.AddComponent<Image>();
            shImg.color                   = ShadowColor;
            shImg.sprite                  = RoundedRectSprite(18);
            shImg.type                    = Image.Type.Sliced;
            shImg.fillCenter              = true;
            shImg.pixelsPerUnitMultiplier = 1f;
        }

        // 2. White rounded background
        var bg    = panelRoot.GetComponent<Image>() ?? panelRoot.gameObject.AddComponent<Image>();
        bg.color                   = BgColor;
        bg.sprite                  = RoundedRectSprite(16);
        bg.type                    = Image.Type.Sliced;
        bg.fillCenter              = true;
        bg.pixelsPerUnitMultiplier = 1f;

        // 3. App icon  (rounded square, 36×36, left side, vertically centered)
        if (panelRoot.Find("AppIcon") == null)
        {
            var icon   = new GameObject("AppIcon");
            icon.transform.SetParent(panelRoot, false);
            var iconRt = icon.AddComponent<RectTransform>();
            iconRt.anchorMin        = new Vector2(0f, 0.5f);
            iconRt.anchorMax        = new Vector2(0f, 0.5f);
            iconRt.pivot            = new Vector2(0f, 0.5f);
            iconRt.sizeDelta        = new Vector2(36f, 36f);
            iconRt.anchoredPosition = new Vector2(14f, 0f);

            _iconBg        = icon.AddComponent<Image>();
            _iconBg.sprite = AppIconSprite();
            _iconBg.color  = new Color(1f, 0.30f, 0.56f);

            var ltr   = new GameObject("Letter");
            ltr.transform.SetParent(icon.transform, false);
            var ltrRt = ltr.AddComponent<RectTransform>();
            ltrRt.anchorMin = Vector2.zero;
            ltrRt.anchorMax = Vector2.one;
            ltrRt.offsetMin = Vector2.zero;
            ltrRt.offsetMax = Vector2.zero;
            _iconLetter           = ltr.AddComponent<TextMeshProUGUI>();
            _iconLetter.text      = "D";
            _iconLetter.fontSize  = 18f;
            _iconLetter.color     = Color.white;
            _iconLetter.fontStyle = FontStyles.Bold;
            _iconLetter.alignment = TextAlignmentOptions.Center;
        }
        else
        {
            var icon    = panelRoot.Find("AppIcon");
            _iconBg     = icon.GetComponent<Image>();
            _iconLetter = icon.GetComponentInChildren<TextMeshProUGUI>();
        }

        // 4. Thin separator line between top and bottom rows
        if (panelRoot.Find("Separator") == null)
        {
            var sep   = new GameObject("Separator");
            sep.transform.SetParent(panelRoot, false);
            var sepRt = sep.AddComponent<RectTransform>();
            sepRt.anchorMin = new Vector2(0f, 0.5f);
            sepRt.anchorMax = new Vector2(1f, 0.5f);
            sepRt.sizeDelta = new Vector2(0f, 0.5f);
            sepRt.offsetMin = new Vector2(14f, 0f);
            sepRt.offsetMax = new Vector2(-14f, 0f);
            var sepImg      = sep.AddComponent<Image>();
            sepImg.color    = new Color(0f, 0f, 0f, 0.07f);
        }

        // 5. App name (top row, left — starts after icon)
        if (appLabel == null)
            appLabel = MakeLabel("AppLabel", panelRoot,
                new Vector2(0f, 0.5f), Vector2.one,
                new Vector2(60f, 1f), new Vector2(-82f, -4f),
                8.5f, AppNameColor, FontStyles.Bold, TextAlignmentOptions.BottomLeft);

        // 6. Time label (top row, right)
        if (timeLabel == null)
            timeLabel = MakeLabel("TimeLabel", panelRoot,
                new Vector2(0.6f, 0.5f), Vector2.one,
                Vector2.zero, new Vector2(-14f, -4f),
                8.5f, TimeColor, FontStyles.Normal, TextAlignmentOptions.BottomRight);

        // 7. Message text (bottom row, bold, dark)
        if (notifLabel == null)
        {
            notifLabel = MakeLabel("NotifLabel", panelRoot,
                Vector2.zero, new Vector2(1f, 0.5f),
                new Vector2(60f, 6f), new Vector2(-14f, 0f),
                13f, MsgColor, FontStyles.Bold, TextAlignmentOptions.Left);
        }
        else
        {
            var rt     = notifLabel.rectTransform;
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = new Vector2(1f, 0.5f);
            rt.offsetMin = new Vector2(60f, 6f);
            rt.offsetMax = new Vector2(-14f, 0f);
            notifLabel.fontSize  = 13f;
            notifLabel.color     = MsgColor;
            notifLabel.fontStyle = FontStyles.Bold;
            notifLabel.alignment = TextAlignmentOptions.Left;
        }
    }

    // ── Texture / sprite helpers ──────────────────────────────────

    // 9-slice rounded rect — 128×128 source, radius en pixels sprite
    // ppu=100 (défaut Unity) → border en canvas units = radius/100*100 = radius units
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
        // ppu=100 est le défaut Unity — les coins feront exactement R canvas units
        return Sprite.Create(tex, new Rect(0, 0, S, S),
            new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect,
            new Vector4(R, R, R, R));
    }

    // Rounded square pour l'icône d'app
    static Sprite AppIconSprite()
    {
        const int S = 128, R = 28; // R = 28/100*100 = 28 canvas units de rayon
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

    static TextMeshProUGUI MakeLabel(string name, RectTransform parent,
        Vector2 anchorMin, Vector2 anchorMax, Vector2 offsetMin, Vector2 offsetMax,
        float size, Color color, FontStyles style, TextAlignmentOptions align)
    {
        var go  = new GameObject(name);
        go.transform.SetParent(parent, false);
        var rt  = go.AddComponent<RectTransform>();
        rt.anchorMin = anchorMin;
        rt.anchorMax = anchorMax;
        rt.offsetMin = offsetMin;
        rt.offsetMax = offsetMax;
        var tmp = go.AddComponent<TextMeshProUGUI>();
        tmp.fontSize  = size;
        tmp.color     = color;
        tmp.fontStyle = style;
        tmp.alignment = align;
        return tmp;
    }

    static float EaseOutCubic(float t) => 1f - Mathf.Pow(1f - t, 3f);
    static float EaseInCubic(float t)  => t * t * t;
}
