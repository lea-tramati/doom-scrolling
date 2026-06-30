using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

public class HUDController : MonoBehaviour
{
    public static HUDController Instance { get; private set; }

    [Header("Score / Level / Lives")]
    [SerializeField] TextMeshProUGUI scoreLabel;
    [SerializeField] TextMeshProUGUI levelLabel;
    [SerializeField] TextMeshProUGUI livesLabel;
    [SerializeField] TextMeshProUGUI nextThresholdLabel;

    [Header("Level progress bar")]
    [SerializeField] Image levelProgressFill;
    [SerializeField] Image engagementFill;
    [SerializeField] TextMeshProUGUI engagementLabel;

    [Header("Overlay messages")]
    [SerializeField] GameObject      overlayPanel;
    [SerializeField] TextMeshProUGUI overlayText;

    [Header("Colors")]
    [SerializeField] Color colorNormal   = new Color(1f,   0.30f, 0.56f); // #FF4D90
    [SerializeField] Color colorDanger   = new Color(1f,   0.23f, 0.37f); // #FF3A5E
    [SerializeField] Color colorProgress = new Color(0.51f, 0.37f, 1f);   // #815FFF
    [SerializeField] Color colorOverlay  = new Color(1f,   0.30f, 0.56f); // #FF4D90

    Coroutine _overlayCoroutine;
    Coroutine _scorePunchCoroutine;
    Coroutine _blinkCoroutine;
    bool _engagementPulse;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    void OnEnable()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnScoreChanged         += RefreshScore;
            GameManager.Instance.OnLivesChanged         += RefreshLives;
            GameManager.Instance.OnLevelChanged         += RefreshLevel;
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
        if (overlayPanel)
        {
            // Constrain overlay to a compact centered banner (not full-screen)
            var rt = overlayPanel.GetComponent<RectTransform>();
            if (rt)
            {
                rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
                rt.pivot     = new Vector2(0.5f, 0.5f);
                rt.sizeDelta = new Vector2(580f, 90f);
                rt.anchoredPosition = Vector2.zero;
            }
            overlayPanel.SetActive(false);
        }
        RefreshAll();
    }

    void Update()
    {
        // Pulse engagement bar color when in danger zone
        if (_engagementPulse && engagementFill)
        {
            float pulse = (Mathf.Sin(Time.time * 10f) + 1f) * 0.5f;
            engagementFill.color = Color.Lerp(colorDanger, Color.white, pulse * 0.45f);
        }
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
        if (!scoreLabel) return;
        scoreLabel.text = $"{s:D6}";
        if (_scorePunchCoroutine != null) StopCoroutine(_scorePunchCoroutine);
        _scorePunchCoroutine = StartCoroutine(PunchScale(scoreLabel.transform, 1.2f, 0.14f));
    }

    void RefreshLives(int l)
    {
        if (!livesLabel) return;
        livesLabel.text = string.Concat(
            System.Linq.Enumerable.Repeat("♥ ", Mathf.Max(0, l))).TrimEnd();

        if (_blinkCoroutine != null) StopCoroutine(_blinkCoroutine);
        if (l == 1)
            _blinkCoroutine = StartCoroutine(BlinkLabel(livesLabel, colorDanger));
        else
            livesLabel.color = colorNormal;
    }

    void RefreshLevel(int lvl)
    {
        if (levelLabel)
            levelLabel.text = lvl >= GameManager.MaxLevel ? $"0{GameManager.MaxLevel}" : $"{lvl:D2}";

        if (nextThresholdLabel)
        {
            int next = GameManager.Instance?.NextLevelThreshold() ?? -1;
            nextThresholdLabel.text = next > 0 ? $"NEXT {next:D6}" : "MAX LEVEL";
        }
    }

    void RefreshLevelProgress(float t)
    {
        if (!levelProgressFill) return;
        levelProgressFill.fillAmount = t;
        levelProgressFill.color = Color.Lerp(colorProgress, Color.white, t * t);
    }

    void RefreshEngagement(float m)
    {
        float norm = SpeedSystem.Instance?.NormalizedSpeed ?? 0f;
        _engagementPulse = norm >= 0.67f;
        if (engagementFill)
        {
            engagementFill.fillAmount = norm;
            if (!_engagementPulse)
                engagementFill.color = colorNormal;
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
        if (overlayText) overlayText.text = message;

        bool isClone = message.Contains("CONTENT");
        const float fadeDur = 0.15f;

        yield return StartCoroutine(FadeOverlay(0f, 1f, fadeDur));

        if (isClone)
        {
            float elapsed = 0f;
            float glitchDur = Mathf.Max(0f, duration - fadeDur);
            while (elapsed < glitchDur)
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
            overlayPanel.SetActive(false);
        }
        else
        {
            float holdDur = Mathf.Max(0f, duration - fadeDur * 2f);
            yield return new WaitForSeconds(holdDur);
            yield return StartCoroutine(FadeOverlay(1f, 0f, fadeDur));
            overlayPanel.SetActive(false);
        }
    }

    public void UpdateAdCountdown(int secs)
    {
        if (overlayText && overlayPanel != null && overlayPanel.activeSelf)
            if (overlayText.text.StartsWith("AD"))
                overlayText.text = $"AD — SKIP IN {secs}";
    }

    // ── Animation helpers ─────────────────────────────────────────

    IEnumerator PunchScale(Transform t, float peak, float duration)
    {
        float half = duration * 0.5f;
        float elapsed = 0f;
        while (elapsed < half)
        {
            elapsed += Time.deltaTime;
            t.localScale = Vector3.one * Mathf.Lerp(1f, peak, elapsed / half);
            yield return null;
        }
        elapsed = 0f;
        while (elapsed < half)
        {
            elapsed += Time.deltaTime;
            t.localScale = Vector3.one * Mathf.Lerp(peak, 1f, elapsed / half);
            yield return null;
        }
        t.localScale = Vector3.one;
    }

    IEnumerator BlinkLabel(TextMeshProUGUI label, Color color)
    {
        while (true)
        {
            label.color = color;
            yield return new WaitForSeconds(0.28f);
            label.color = Color.clear;
            yield return new WaitForSeconds(0.22f);
        }
    }

    IEnumerator FadeOverlay(float from, float to, float duration)
    {
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float a = Mathf.Lerp(from, to, elapsed / duration);
            if (overlayText)
                overlayText.color = new Color(colorOverlay.r, colorOverlay.g, colorOverlay.b, a);
            yield return null;
        }
        if (overlayText)
            overlayText.color = new Color(colorOverlay.r, colorOverlay.g, colorOverlay.b, to);
    }
}
