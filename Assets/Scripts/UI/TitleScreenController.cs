using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
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

    float _glitchTimer;
    bool  _glitchOn;

    void Start()
    {
        if (titleLabel)    titleLabel.text    = "DOOM SCROLLING";
        if (subtitleLabel) subtitleLabel.text = "YOU ARE ALREADY INSIDE.";

        if (openAppButton)
            openAppButton.onClick.AddListener(OnOpenApp);

        // How-to-play is now taught in-level (Level 1 contextual hints via
        // GameManager.ShowHintOnce) instead of a title-screen panel.
        if (howToPlayPanel) howToPlayPanel.SetActive(false);
        if (creditsButton)  creditsButton.onClick.AddListener(OnCredits);

        AudioManager.Instance?.PlayAmbientMusic();
    }

    void OnCredits()
    {
        SceneManager.LoadScene("CreditsScreen");
    }

    void Update()
    {
        // Title glitch every 3s
        _glitchTimer += Time.deltaTime;
        if (!_glitchOn && _glitchTimer >= 3f)
        {
            _glitchOn    = true;
            _glitchTimer = 0f;
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
            SceneManager.LoadScene("GameScene");  // fallback if no GameManager
    }
}
