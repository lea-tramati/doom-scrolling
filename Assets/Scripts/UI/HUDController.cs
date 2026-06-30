using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

// Attach to: HUD Canvas GameObject
// Dependencies: GameManager, SpeedSystem
public class HUDController : MonoBehaviour
{
    public static HUDController Instance { get; private set; }

    [Header("Score / Level / Lives")]
    [SerializeField] TextMeshProUGUI scoreLabel;
    [SerializeField] TextMeshProUGUI levelLabel;
    [SerializeField] TextMeshProUGUI livesLabel;
    [SerializeField] TextMeshProUGUI nextThresholdLabel; // optional: shows "NEXT: 1600"

    [Header("Level progress bar")]
    [SerializeField] Image levelProgressFill;   // fills as score → next threshold
    [SerializeField] Image engagementFill;      // fills with speed (addiction meter)
    [SerializeField] TextMeshProUGUI engagementLabel;

    [Header("Overlay messages")]
    [SerializeField] GameObject      overlayPanel;
    [SerializeField] TextMeshProUGUI overlayText;

    [Header("Colors")]
    [SerializeField] Color colorNormal   = new Color(1f,   0.30f, 0.56f); // #FF4D90 pink
    [SerializeField] Color colorDanger   = new Color(1f,   0.23f, 0.37f); // #FF3A5E red
    [SerializeField] Color colorProgress = new Color(0.51f, 0.37f, 1f);   // #815FFF purple
    [SerializeField] Color colorOverlay  = new Color(1f,   0.30f, 0.56f); // #FF4D90

    Coroutine _overlayCoroutine;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    void OnEnable()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnScoreChanged        += RefreshScore;
            GameManager.Instance.OnLivesChanged        += RefreshLives;
            GameManager.Instance.OnLevelChanged        += RefreshLevel;
            GameManager.Instance.OnLevelProgressChanged += RefreshLevelProgress;
        }
        if (SpeedSystem.Instance != null)
            SpeedSystem.Instance.OnSpeedChanged += RefreshEngagement;
    }

    void OnDisable()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnScoreChanged         -= RefreshScore;
            GameManager.Instance.OnLivesChanged         -= RefreshLives;
            GameManager.Instance.OnLevelChanged         -= RefreshLevel;
            GameManager.Instance.OnLevelProgressChanged -= RefreshLevelProgress;
        }
        if (SpeedSystem.Instance != null)
            SpeedSystem.Instance.OnSpeedChanged -= RefreshEngagement;
    }

    void Start()
    {
        if (overlayPanel) overlayPanel.SetActive(false);
        RefreshAll();
    }

    // ── Refresh helpers ───────────────────────────────────────────

    void RefreshAll()
    {
        if (GameManager.Instance == null) return;
        RefreshScore(GameManager.Instance.Score);
        RefreshLives(GameManager.Instance.Lives);
        RefreshLevel(GameManager.Instance.Level);
        RefreshLevelProgress(GameManager.Instance.LevelProgress());
        if (SpeedSystem.Instance != null)
            RefreshEngagement(SpeedSystem.Instance.CurrentMultiplier);
    }

    void RefreshScore(int s)
    {
        if (scoreLabel) scoreLabel.text = $"{s:D6}";
    }

    void RefreshLives(int l)
    {
        if (livesLabel)
            livesLabel.text = string.Concat(
                System.Linq.Enumerable.Repeat("♥ ", Mathf.Max(0, l))).TrimEnd();
    }

    void RefreshLevel(int lvl)
    {
        if (levelLabel)
            levelLabel.text = lvl >= GameManager.MaxLevel ? $"0{GameManager.MaxLevel}" : $"{lvl:D2}";

        // Update next-threshold label
        if (nextThresholdLabel)
        {
            int next = GameManager.Instance?.NextLevelThreshold() ?? -1;
            nextThresholdLabel.text = next > 0 ? $"NEXT {next:D6}" : "MAX LEVEL";
        }
    }

    void RefreshLevelProgress(float t)
    {
        if (levelProgressFill)
        {
            levelProgressFill.fillAmount = t;
            // Pulse to white near 100%
            levelProgressFill.color = Color.Lerp(colorProgress, Color.white, t * t);
        }
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

        bool isClone = message.Contains("CONTENT");
        if (isClone)
        {
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
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
