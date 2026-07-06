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
    int   _glitchCount;
    const int MAX_GLITCHES = 3; // stop after a few reps — a screenshot or an idle player shouldn't see it loop forever

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
        SceneManager.LoadScene("CreditsScreen");
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
            SceneManager.LoadScene("GameScene");  // fallback if no GameManager
    }
}
