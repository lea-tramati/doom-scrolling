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
    [SerializeField] CanvasGroup     fadeGroup;

    void Start()
    {
        if (sessionEndedLabel)
        {
            float hours = (GameManager.Instance?.SessionTimer ?? 0f) / 3600f;
            sessionEndedLabel.text =
                $"You played for {hours:0.0} hours. Maybe it's time to unplug" +
                "\n\n<size=150%>game over</size>";
        }

        if (totalScoreLabel)
        {
            int score = GameManager.Instance != null ? GameManager.Instance.Score : 0;
            totalScoreLabel.text = $"TOTAL ENGAGEMENT\n{score:D6}";
        }

        if (playAgainBtn)
            playAgainBtn.onClick.AddListener(OnPlayAgain);

        if (titleBtn)
            titleBtn.onClick.AddListener(OnBackToTitle);

        AudioManager.Instance?.PlayEndScreenMusic();
        StartCoroutine(FadeIn());
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
}
