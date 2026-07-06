using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using System.Collections;

// Self-bootstrapping — no scene wiring needed. Builds its own fullscreen fade canvas at
// runtime (same procedural-UI approach as NotificationManager) and persists across scenes,
// so every SceneManager.LoadScene(...) call site can route through SceneTransitionManager.Load(...)
// for an async load with a fade instead of a synchronous cut.
public class SceneTransitionManager : MonoBehaviour
{
    public static SceneTransitionManager Instance { get; private set; }

    CanvasGroup _fadeGroup;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void Bootstrap()
    {
        if (Instance != null) return;
        var go = new GameObject("SceneTransitionManager");
        go.AddComponent<SceneTransitionManager>();
    }

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
        BuildFadeCanvas();
    }

    void BuildFadeCanvas()
    {
        var canvasGO = new GameObject("SceneTransitionCanvas", typeof(Canvas), typeof(CanvasGroup), typeof(GraphicRaycaster));
        canvasGO.transform.SetParent(transform, false);
        var canvas = canvasGO.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 1000; // always above gameplay/HUD/menus

        _fadeGroup = canvasGO.GetComponent<CanvasGroup>();
        _fadeGroup.alpha = 0f;
        _fadeGroup.blocksRaycasts = false;

        var imgGO = new GameObject("Fade", typeof(RectTransform), typeof(Image));
        imgGO.transform.SetParent(canvasGO.transform, false);
        var rt = imgGO.GetComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
        imgGO.GetComponent<Image>().color = Color.black;
    }

    // Static convenience wrapper — falls back to a plain synchronous load if, for any
    // reason, the manager hasn't bootstrapped yet (shouldn't happen in practice).
    public static void Load(string sceneName, float fadeDuration = 0.35f)
    {
        if (Instance != null) Instance.LoadScene(sceneName, fadeDuration);
        else SceneManager.LoadScene(sceneName);
    }

    public void LoadScene(string sceneName, float fadeDuration = 0.35f)
    {
        StartCoroutine(LoadSceneRoutine(sceneName, fadeDuration));
    }

    IEnumerator LoadSceneRoutine(string sceneName, float fadeDuration)
    {
        _fadeGroup.blocksRaycasts = true;
        yield return Fade(0f, 1f, fadeDuration);

        var op = SceneManager.LoadSceneAsync(sceneName);
        while (op != null && !op.isDone) yield return null;

        yield return Fade(1f, 0f, fadeDuration);
        _fadeGroup.blocksRaycasts = false;
    }

    IEnumerator Fade(float from, float to, float duration)
    {
        float t = 0f;
        while (t < duration)
        {
            // Unscaled: a scene load can be triggered while Time.timeScale is 0 (pause menu).
            t += Time.unscaledDeltaTime;
            _fadeGroup.alpha = Mathf.Lerp(from, to, t / duration);
            yield return null;
        }
        _fadeGroup.alpha = to;
    }
}
