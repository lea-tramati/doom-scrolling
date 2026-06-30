using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

// Attach to: HUD Canvas GameObject
// Required: Canvas, child TMP labels and Image sliders assigned below
// Dependencies: GameManager, SpeedSystem
public class HUDController : MonoBehaviour
{
    public static HUDController Instance { get; private set; }

    [Header("Score / Level / Lives")]
    [SerializeField] TextMeshProUGUI scoreLabel;
    [SerializeField] TextMeshProUGUI levelLabel;
    [SerializeField] TextMeshProUGUI livesLabel;

    [Header("Engagement bar")]
    [SerializeField] Image engagementFill;     // Image type = Filled
    [SerializeField] TextMeshProUGUI engagementLabel;

    [Header("Overlay messages")]
    [SerializeField] GameObject    overlayPanel;
    [SerializeField] TextMeshProUGUI overlayText;

    [Header("Colors")]
    [SerializeField] Color colorNormal   = new Color(0.51f, 0.08f, 1f);    // #8115FF
    [SerializeField] Color colorDanger   = new Color(1f,   0.23f, 0.37f);  // #FF3A5E
    [SerializeField] Color colorOverlay  = new Color(1f,   0.30f, 0.56f);  // #FF4D90

    Coroutine _overlayCoroutine;
    int       _adCountdown;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    void OnEnable()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnScoreChanged += RefreshScore;
            GameManager.Instance.OnLivesChanged += RefreshLives;
            GameManager.Instance.OnLevelChanged += RefreshLevel;
        }
        if (SpeedSystem.Instance != null)
            SpeedSystem.Instance.OnSpeedChanged += RefreshEngagement;
    }

    void OnDisable()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnScoreChanged -= RefreshScore;
            GameManager.Instance.OnLivesChanged -= RefreshLives;
            GameManager.Instance.OnLevelChanged -= RefreshLevel;
        }
        if (SpeedSystem.Instance != null)
            SpeedSystem.Instance.OnSpeedChanged -= RefreshEngagement;
    }

    void Start()
    {
        if (overlayPanel != null) overlayPanel.SetActive(false);
        RefreshAll();
    }

    // ── Refresh helpers ───────────────────────────────────────────

    void RefreshAll()
    {
        if (GameManager.Instance == null) return;
        RefreshScore(GameManager.Instance.Score);
        RefreshLives(GameManager.Instance.Lives);
        RefreshLevel(GameManager.Instance.Level);
        if (SpeedSystem.Instance != null)
            RefreshEngagement(SpeedSystem.Instance.CurrentMultiplier);
    }

    void RefreshScore(int s)
    {
        if (scoreLabel) scoreLabel.text = $"{s:D6}";
    }

    void RefreshLives(int l)
    {
        if (livesLabel) livesLabel.text = string.Concat(System.Linq.Enumerable.Repeat("♥ ", Mathf.Max(0, l))).TrimEnd();
    }

    void RefreshLevel(int lvl)
    {
        if (levelLabel) levelLabel.text = lvl > 5 ? "∞" : $"{lvl:D2}";
    }

    void RefreshEngagement(float m)
    {
        float norm = SpeedSystem.Instance?.NormalizedSpeed ?? 0f;
        if (engagementFill)
        {
            engagementFill.fillAmount = norm;
            engagementFill.color = norm >= 0.67f ? colorDanger : colorNormal;
        }
        if (engagementLabel)
            engagementLabel.text = $"ENGAGEMENT {Mathf.RoundToInt(norm * 100)}%";
    }

    // ── Overlay messages ──────────────────────────────────────────

    public void ShowOverlay(string message, float duration)
    {
        if (_overlayCoroutine != null) StopCoroutine(_overlayCoroutine);
        _overlayCoroutine = StartCoroutine(OverlaySequence(message, duration));
    }

    IEnumerator OverlaySequence(string message, float duration)
    {
        if (overlayPanel == null) yield break;
        overlayPanel.SetActive(true);
        if (overlayText)
        {
            overlayText.text  = message;
            overlayText.color = colorOverlay;
        }

        // Glitch flicker for clone phase
        bool isClone = message.Contains("CONTENT");
        if (isClone)
        {
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                // 3-frame text offset glitch
                if (overlayText)
                {
                    int frame = Mathf.FloorToInt(elapsed * 8f) % 3;
                    overlayText.rectTransform.anchoredPosition =
                        new Vector2(frame == 1 ? 2f : frame == 2 ? -2f : 0f, 0f);
                }
                yield return null;
            }
            if (overlayText) overlayText.rectTransform.anchoredPosition = Vector2.zero;
        }
        else
        {
            yield return new WaitForSeconds(duration);
        }

        overlayPanel.SetActive(false);
    }

    public void UpdateAdCountdown(int secs)
    {
        if (overlayText && overlayPanel != null && overlayPanel.activeSelf)
            if (overlayText.text.StartsWith("AD"))
                overlayText.text = $"AD — SKIP IN {secs}";
    }
}
