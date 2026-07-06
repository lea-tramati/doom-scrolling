using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

// Attach to: root Canvas in CreditsScreen scene
// Reachable from TitleScreen (CREDITS button) and from EndScreen's "put down the phone" path.
public class CreditsScreenController : MonoBehaviour
{
    [SerializeField] RectTransform scrollingText; // child of a masked viewport
    [SerializeField] Button        backButton;
    [SerializeField] Button        quitButton;

    [SerializeField] float scrollSpeed = 40f; // px/sec, in canvas reference units
    [SerializeField] float restartDelay = 3f; // pause once fully scrolled before looping

    float _startY;
    float _travel;
    float _timer;
    bool  _looping;

    void Start()
    {
        if (backButton) backButton.onClick.AddListener(OnBack);

#if UNITY_WEBGL && !UNITY_EDITOR
        // Application.Quit() is a no-op in browsers; there's nothing useful for this button to do.
        if (quitButton) quitButton.gameObject.SetActive(false);
#else
        if (quitButton) quitButton.onClick.AddListener(OnQuit);
#endif

        if (scrollingText != null)
        {
            _startY = scrollingText.anchoredPosition.y;
            var viewport = scrollingText.parent as RectTransform;
            float viewportHeight = viewport != null ? viewport.rect.height : 900f;
            _travel = viewportHeight + scrollingText.rect.height;
        }
    }

    void Update()
    {
        if (scrollingText == null) return;

        if (_looping)
        {
            _timer -= Time.deltaTime;
            if (_timer <= 0f)
            {
                _looping = false;
                scrollingText.anchoredPosition = new Vector2(scrollingText.anchoredPosition.x, _startY);
            }
            return;
        }

        var pos = scrollingText.anchoredPosition;
        pos.y += scrollSpeed * Time.deltaTime;
        scrollingText.anchoredPosition = pos;

        if (pos.y >= _startY + _travel)
        {
            _looping = true;
            _timer   = restartDelay;
        }
    }

    void OnBack()
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
