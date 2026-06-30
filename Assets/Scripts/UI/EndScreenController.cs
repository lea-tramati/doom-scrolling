using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;
using System.Collections;

// Attach to: root GameObject in EndScreen scene
// Required: Canvas with child panels (IntroPanel, SequencePanel, ButtonPanel)
// Dependencies: GameManager (reads ApparentWin + FormattedTime)
public class EndScreenController : MonoBehaviour
{
    [Header("Intro")]
    [SerializeField] TextMeshProUGUI introLine1;
    [SerializeField] TextMeshProUGUI introLine2;
    [SerializeField] GameObject confettiParent;   // app icon rain

    [Header("Sequence")]
    [SerializeField] TextMeshProUGUI sequenceText;

    [Header("Buttons")]
    [SerializeField] Button playAgainBtn;
    [SerializeField] Button putDownBtn;

    [Header("Outro")]
    [SerializeField] CanvasGroup fadeOverlay;    // full-screen black
    [SerializeField] TextMeshProUGUI wontText;   // "YOU WON'T."
    [SerializeField] TextMeshProUGUI titleGlitch; // DOOM SCROLLING reappear

    const float TYPE_SPEED  = 0.04f;   // seconds per character
    const float LINE_PAUSE  = 2.5f;    // seconds between lines

    bool _apparentWin;
    string _timeStr;
    float  _sessionSeconds;

    void Start()
    {
        _apparentWin    = GameManager.Instance?.ApparentWin ?? false;
        _timeStr        = GameManager.Instance?.FormattedTime() ?? "00:00";
        _sessionSeconds = GameManager.Instance?.SessionTimer ?? 0f;

        if (playAgainBtn) playAgainBtn.onClick.AddListener(OnPlayAgain);
        if (putDownBtn)   putDownBtn.onClick.AddListener(OnPutDown);

        if (confettiParent) confettiParent.SetActive(false);
        if (wontText)       wontText.gameObject.SetActive(false);
        if (titleGlitch)    titleGlitch.gameObject.SetActive(false);
        if (fadeOverlay)    fadeOverlay.alpha = 0f;

        AudioManager.Instance?.PlayEndScreenMusic();
        StartCoroutine(RunSequence());
    }

    IEnumerator RunSequence()
    {
        // ── Path A / B intro ─────────────────────────────────────
        if (_apparentWin)
        {
            yield return TypeLine(introLine1, "YOU FINISHED THE FEED.");
            yield return new WaitForSeconds(1f);
            yield return TypeLine(introLine2, "There is always more.");
            if (confettiParent) { confettiParent.SetActive(true); }
            yield return new WaitForSeconds(1.5f);
        }
        else
        {
            yield return TypeLine(introLine1, "SESSION ENDED");
            yield return new WaitForSeconds(1f);
            yield return TypeLine(introLine2, "You lost.");
        }

        yield return new WaitForSeconds(1f);

        // ── Shared typewriter sequence ────────────────────────────
        string contextLine = GetContextLine(_sessionSeconds);

        string[] lines =
        {
            $"YOU PLAYED FOR {_timeStr}.",
            $"INSTEAD OF {contextLine}",
            "THE ALGORITHM KEPT YOU HERE.",
            "IT ALWAYS DOES."
        };

        foreach (var line in lines)
        {
            if (sequenceText) sequenceText.text = "";
            yield return TypeLine(sequenceText, line);
            yield return new WaitForSeconds(LINE_PAUSE);
        }

        // ── 3s black silence ──────────────────────────────────────
        yield return FadeTo(1f, 1f);
        yield return new WaitForSeconds(3f);

        // ── Title glitch reappear ────────────────────────────────
        if (titleGlitch) titleGlitch.gameObject.SetActive(true);
        yield return StartCoroutine(GlitchTitle());

        // Show buttons
        if (playAgainBtn) playAgainBtn.gameObject.SetActive(true);
        if (putDownBtn)   putDownBtn.gameObject.SetActive(true);
    }

    string GetContextLine(float secs)
    {
        if (secs < 300f)   return "TYING YOUR SHOES.";
        if (secs < 900f)   return "MAKING A COFFEE.";
        if (secs < 1800f)  return "CALLING A FRIEND.";
        if (secs < 3600f)  return "GOING FOR A WALK.";
        return "DOING SOMETHING THAT MATTERED.";
    }

    IEnumerator TypeLine(TextMeshProUGUI label, string text)
    {
        if (label == null) yield break;
        label.text = "";
        foreach (char c in text)
        {
            label.text += c;
            yield return new WaitForSeconds(TYPE_SPEED);
        }
    }

    IEnumerator GlitchTitle()
    {
        if (titleGlitch == null) yield break;
        titleGlitch.text = "DOOM SCROLLING";

        for (int i = 0; i < 6; i++)
        {
            titleGlitch.rectTransform.anchoredPosition =
                new Vector2(i % 2 == 0 ? 3f : -3f, 0f);
            yield return new WaitForSeconds(0.1f);
        }
        titleGlitch.rectTransform.anchoredPosition = Vector2.zero;
    }

    IEnumerator FadeTo(float target, float duration)
    {
        if (fadeOverlay == null) yield break;
        float start = fadeOverlay.alpha;
        float t = 0f;
        while (t < duration)
        {
            t += Time.deltaTime;
            fadeOverlay.alpha = Mathf.Lerp(start, target, t / duration);
            yield return null;
        }
        fadeOverlay.alpha = target;
    }

    void OnPlayAgain()
    {
        GameManager.Instance?.StartGame();
    }

    void OnPutDown()
    {
        StartCoroutine(PutDownSequence());
    }

    IEnumerator PutDownSequence()
    {
        if (putDownBtn) putDownBtn.interactable   = false;
        if (playAgainBtn) playAgainBtn.interactable = false;

        yield return FadeTo(1f, 0.5f);

        if (wontText)
        {
            wontText.gameObject.SetActive(true);
            wontText.text = "YOU WON'T.";
        }

        yield return new WaitForSeconds(2f);
        Application.Quit();

#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#endif
    }
}
