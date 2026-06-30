using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

// Attach to: root GameObject in GameOverScreen scene
public class GameOverScreenController : MonoBehaviour
{
    [SerializeField] TextMeshProUGUI sessionEndedLabel;
    [SerializeField] TextMeshProUGUI totalScoreLabel;
    [SerializeField] Button playAgainBtn;

    void Start()
    {
        if (sessionEndedLabel) sessionEndedLabel.text = "SESSION ENDED";
        if (totalScoreLabel && GameManager.Instance != null)
            totalScoreLabel.text = $"TOTAL ENGAGEMENT: {GameManager.Instance.Score}";

        if (playAgainBtn)
            playAgainBtn.onClick.AddListener(() => GameManager.Instance?.StartGame());

        AudioManager.Instance?.PlayEndScreenMusic();
    }
}
