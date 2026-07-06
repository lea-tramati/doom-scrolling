using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;
using System.Collections;

// Attach to: root Canvas in GameOverScreen scene
public class GameOverScreenController : MonoBehaviour
{
    [SerializeField] TextMeshProUGUI sessionEndedLabel;
    [SerializeField] TextMeshProUGUI totalScoreLabel;
    [SerializeField] Button          playAgainBtn;
    [SerializeField] Button          titleBtn;           // optional back-to-title button
    [SerializeField] Button          quitBtn;
    [SerializeField] CanvasGroup     fadeGroup;

    const float TYPE_SPEED = 0.03f;
    const string PLAY_COUNT_KEY = "GameOverCount";

    // Rotates so the guilt-trip line doesn't read as a canned reprimand on every replay —
    // by the 4th+ visit it drops the lecture and just gives you the number back.
    static readonly string[] GuiltLines =
    {
        "\n\nYou played for {0:0.0} hours. Maybe it's time to unplug",
        "\n\n{0:0.0} hours, gone. The feed doesn't miss you back",
        "\n\nThat's {0:0.0} hours you're not getting back",
        "\n\nSession length: {0:0.0}h. No judgment. (Some judgment.)",
    };

    int _finalScore;

    void Start()
    {
        _finalScore = GameManager.Instance != null ? GameManager.Instance.Score : 0;

        if (playAgainBtn) { playAgainBtn.onClick.AddListener(OnPlayAgain); playAgainBtn.gameObject.SetActive(false); }
        if (titleBtn)     { titleBtn.onClick.AddListener(OnBackToTitle);   titleBtn.gameObject.SetActive(false); }

#if UNITY_WEBGL && !UNITY_EDITOR
        // Application.Quit() is a no-op in browsers; there's nothing useful for this button to do.
        if (quitBtn) quitBtn.gameObject.SetActive(false);
#else
        if (quitBtn) { quitBtn.onClick.AddListener(OnQuit); quitBtn.gameObject.SetActive(false); }
#endif
        if (totalScoreLabel) totalScoreLabel.text = "";
        if (sessionEndedLabel) sessionEndedLabel.text = "";

        AudioManager.Instance?.PlayEndScreenMusic();
        StartCoroutine(RunSequence());
    }

    IEnumerator RunSequence()
    {
        yield return FadeIn();

        if (sessionEndedLabel)
        {
            yield return TypeLine(sessionEndedLabel, "GAME OVER");
            yield return GlitchJitter(sessionEndedLabel.rectTransform);
        }

        yield return new WaitForSeconds(0.5f);

        float hours = (GameManager.Instance?.SessionTimer ?? 0f) / 3600f;
        int visitCount = PlayerPrefs.GetInt(PLAY_COUNT_KEY, 0);
        PlayerPrefs.SetInt(PLAY_COUNT_KEY, visitCount + 1);
        if (sessionEndedLabel)
            sessionEndedLabel.text += string.Format(GuiltLines[visitCount % GuiltLines.Length], hours);

        yield return new WaitForSeconds(0.3f);

        if (totalScoreLabel) yield return CountUpScore();

        yield return new WaitForSeconds(0.3f);

        if (playAgainBtn) playAgainBtn.gameObject.SetActive(true);
        if (titleBtn)     titleBtn.gameObject.SetActive(true);
#if !(UNITY_WEBGL && !UNITY_EDITOR)
        if (quitBtn)      quitBtn.gameObject.SetActive(true);
#endif
    }

    IEnumerator FadeIn()
    {
        if (fadeGroup == null) yield break;
        fadeGroup.alpha = 0f;
        float t = 0f;
        while (t < 0.6f)
        {
            t += Time.deltaTime;
            fadeGroup.alpha = t / 0.6f;
            yield return null;
        }
        fadeGroup.alpha = 1f;
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

    IEnumerator GlitchJitter(RectTransform rt)
    {
        if (rt == null) yield break;
        for (int i = 0; i < 6; i++)
        {
            rt.anchoredPosition = new Vector2(i % 2 == 0 ? 4f : -4f, 0f);
            yield return new WaitForSeconds(0.1f);
        }
        rt.anchoredPosition = Vector2.zero;
    }

    IEnumerator CountUpScore()
    {
        totalScoreLabel.text = $"TOTAL ENGAGEMENT\n000000";
        float t = 0f;
        const float dur = 0.8f;
        while (t < dur)
        {
            t += Time.deltaTime;
            int shown = Mathf.RoundToInt(Mathf.Lerp(0, _finalScore, t / dur));
            totalScoreLabel.text = $"TOTAL ENGAGEMENT\n{shown:D6}";
            yield return null;
        }
        totalScoreLabel.text = $"TOTAL ENGAGEMENT\n{_finalScore:D6}";
    }

    void OnPlayAgain()
    {
        if (GameManager.Instance != null)
            GameManager.Instance.StartGame();
        else
            SceneManager.LoadScene("TitleScreen");
    }

    void OnBackToTitle()
    {
        SceneManager.LoadScene("TitleScreen");
    }

    void OnQuit()
    {
        Application.Quit();
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#endif
    }
}
