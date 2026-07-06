using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

// Attach to: root Canvas in GameScene (built by PauseMenuSetup)
// Escape toggles pause. Uses Time.timeScale rather than GameManager.IsPlaying so it can't
// collide with the level-transition/respawn state machine, which already repurposes IsPlaying.
public class PauseMenuController : MonoBehaviour
{
    [SerializeField] GameObject panel;
    [SerializeField] Button     resumeBtn;
    [SerializeField] Button     restartBtn;
    [SerializeField] Button     titleBtn;
    [SerializeField] Button     quitBtn;
    [SerializeField] Slider     musicSlider;
    [SerializeField] Slider     sfxSlider;

    bool _isPaused;

    void Start()
    {
        if (panel) panel.SetActive(false);

        if (resumeBtn)  resumeBtn.onClick.AddListener(Resume);
        if (restartBtn) restartBtn.onClick.AddListener(Restart);
        if (titleBtn)   titleBtn.onClick.AddListener(BackToTitle);

#if UNITY_WEBGL && !UNITY_EDITOR
        // Application.Quit() is a no-op in browsers; there's nothing useful for this button to do.
        if (quitBtn) quitBtn.gameObject.SetActive(false);
#else
        if (quitBtn) quitBtn.onClick.AddListener(Quit);
#endif

        if (musicSlider)
        {
            musicSlider.value = AudioManager.Instance != null ? AudioManager.Instance.MusicVolume : 1f;
            musicSlider.onValueChanged.AddListener(v => AudioManager.Instance?.SetMusicVolume(v));
        }
        if (sfxSlider)
        {
            sfxSlider.value = AudioManager.Instance != null ? AudioManager.Instance.SFXVolume : 1f;
            sfxSlider.onValueChanged.AddListener(v => AudioManager.Instance?.SetSFXVolume(v));
        }
    }

    void Update()
    {
        if (!Input.GetKeyDown(KeyCode.Escape)) return;

        if (_isPaused) Resume();
        else if (GameManager.Instance != null && GameManager.Instance.IsPlaying) Pause();
    }

    void Pause()
    {
        _isPaused = true;
        Time.timeScale = 0f;
        if (panel) panel.SetActive(true);
    }

    void Resume()
    {
        _isPaused = false;
        Time.timeScale = 1f;
        if (panel) panel.SetActive(false);
    }

    void Restart()
    {
        Time.timeScale = 1f;
        _isPaused = false;
        if (GameManager.Instance != null) GameManager.Instance.StartGame();
        else SceneManager.LoadScene("GameScene");
    }

    void BackToTitle()
    {
        Time.timeScale = 1f;
        _isPaused = false;
        SceneManager.LoadScene("TitleScreen");
    }

    void Quit()
    {
        Application.Quit();
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#endif
    }

    void OnDestroy()
    {
        // Time.timeScale is global and outlives this object across a scene unload —
        // never leave the next scene frozen because we quit while paused.
        if (_isPaused) Time.timeScale = 1f;
    }
}
