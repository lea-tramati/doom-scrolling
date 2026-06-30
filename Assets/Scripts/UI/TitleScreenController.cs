using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

// Attach to: root Canvas in TitleScreen scene
public class TitleScreenController : MonoBehaviour
{
    [SerializeField] TextMeshProUGUI titleLabel;
    [SerializeField] TextMeshProUGUI subtitleLabel;
    [SerializeField] Button          openAppButton;
    [SerializeField] RectTransform   notifBadge;   // bouncing "99+"

    float _glitchTimer;
    bool  _glitchOn;

    void Start()
    {
        if (titleLabel)    titleLabel.text    = "DOOM SCROLLING";
        if (subtitleLabel) subtitleLabel.text = "YOU ARE ALREADY INSIDE.";
        if (openAppButton) openAppButton.onClick.AddListener(OnOpenApp);

        AudioManager.Instance?.PlayAmbientMusic();
        StartCoroutine(BadgeBounce());
    }

    void Update()
    {
        // Title glitch every 3s
        _glitchTimer += Time.deltaTime;
        if (!_glitchOn && _glitchTimer >= 3f)
        {
            _glitchOn = true;
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

    IEnumerator BadgeBounce()
    {
        if (notifBadge == null) yield break;
        while (true)
        {
            float t = 0f;
            while (t < 1f)
            {
                t += Time.deltaTime * 2f;
                float y = Mathf.Sin(t * Mathf.PI) * 12f;
                notifBadge.anchoredPosition =
                    new Vector2(notifBadge.anchoredPosition.x, y);
                yield return null;
            }
        }
    }

    void OnOpenApp() => GameManager.Instance?.StartGame();
}
