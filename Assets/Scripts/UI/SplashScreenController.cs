using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

// Attach to: root Canvas in SplashScreen scene
// First scene loaded on launch. Shows the game name, then hands off to TitleScreen.
public class SplashScreenController : MonoBehaviour
{
    [SerializeField] CanvasGroup fadeOverlay; // full-screen black, starts opaque
    [SerializeField] CanvasGroup contentGroup; // logo + tagline

    const float FADE_IN  = 0.6f;
    const float HOLD     = 1.4f;
    const float FADE_OUT = 0.6f;

    void Start()
    {
        if (fadeOverlay)  fadeOverlay.alpha  = 1f;
        if (contentGroup) contentGroup.alpha = 0f;
        StartCoroutine(RunSequence());
    }

    IEnumerator RunSequence()
    {
        yield return Fade(fadeOverlay, 1f, 0f, FADE_IN);
        yield return Fade(contentGroup, 0f, 1f, FADE_IN);

        yield return new WaitForSeconds(HOLD);

        yield return Fade(fadeOverlay, 0f, 1f, FADE_OUT);
        SceneManager.LoadScene("TitleScreen");
    }

    IEnumerator Fade(CanvasGroup group, float from, float to, float duration)
    {
        if (group == null) yield break;
        float t = 0f;
        while (t < duration)
        {
            t += Time.deltaTime;
            group.alpha = Mathf.Lerp(from, to, t / duration);
            yield return null;
        }
        group.alpha = to;
    }
}
