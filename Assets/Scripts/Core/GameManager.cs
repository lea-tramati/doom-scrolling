using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

// Attach to: persistent GameObject "GameManager"
// Required components: none
// Dependencies: SpeedSystem, MazeLoader, HazardManager, HUDController
public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    // ── Scene names ──────────────────────────────────────────────
    const string SCENE_TITLE    = "TitleScreen";
    const string SCENE_GAME     = "GameScene";
    const string SCENE_GAMEOVER = "GameOverScreen";
    const string SCENE_END      = "EndScreen";

    // ── Public state ──────────────────────────────────────────────
    public int   Score         { get; private set; }
    public int   Lives         { get; private set; } = 3;
    public int   Level         { get; private set; } = 1;
    public float SessionTimer  { get; private set; }     // never resets
    public bool  ApparentWin   { get; private set; }
    public bool  IsPlaying     { get; private set; }

    [Header("Config")]
    [SerializeField] int   startLives     = 3;
    [SerializeField] int   dotsPerLevel   = 180;

    int   _dotsCollectedThisLevel;
    int   _appIconsThisLevel;

    // ── Events ────────────────────────────────────────────────────
    public System.Action<int>   OnScoreChanged;
    public System.Action<int>   OnLivesChanged;
    public System.Action<int>   OnLevelChanged;
    public System.Action<bool>  OnGameOver;      // true = apparent win
    public System.Action        OnLevelComplete;
    public System.Action        OnClonePhaseEnd;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    void Start()
    {
        // When starting directly in GameScene (editor testing / direct load), auto-start
        if (SceneManager.GetActiveScene().name == SCENE_GAME && !IsPlaying)
        {
            Lives     = startLives;
            Level     = 1;
            IsPlaying = true;
        }
    }

    void Update()
    {
        if (IsPlaying) SessionTimer += Time.deltaTime;
    }

    // ── Public API ────────────────────────────────────────────────

    public void StartGame()
    {
        Score = 0;
        Lives = startLives;
        Level = 1;
        _dotsCollectedThisLevel = 0;
        _appIconsThisLevel = 0;
        ApparentWin = false;
        IsPlaying = true;
        SpeedSystem.Instance?.ResetSpeed();
        SceneManager.LoadScene(SCENE_GAME);
    }

    public void AddScore(int pts)
    {
        Score += pts;
        OnScoreChanged?.Invoke(Score);
    }

    public void OnDotCollected()
    {
        _dotsCollectedThisLevel++;
        if (_dotsCollectedThisLevel >= dotsPerLevel) StartCoroutine(LevelComplete());
    }

    public void OnAppIconCollected()
    {
        _appIconsThisLevel++;
    }

    public int AppIconsThisLevel => _appIconsThisLevel;

    public void PlayerDied()
    {
        Lives--;
        OnLivesChanged?.Invoke(Lives);
        if (Lives <= 0) StartCoroutine(GameOver(false));
        else            StartCoroutine(RespawnDelay());
    }

    public void TriggerApparentWin()
    {
        ApparentWin = true;
        StartCoroutine(GameOver(true));
    }

    // ── Internals ────────────────────────────────────────────────

    IEnumerator LevelComplete()
    {
        IsPlaying = false;
        OnLevelComplete?.Invoke();
        yield return new WaitForSeconds(2f);

        Level++;
        _dotsCollectedThisLevel = 0;
        _appIconsThisLevel = 0;

        if (Level > 5) { TriggerApparentWin(); yield break; }

        OnLevelChanged?.Invoke(Level);
        SceneManager.LoadScene(SCENE_GAME);
        IsPlaying = true;
    }

    IEnumerator RespawnDelay()
    {
        IsPlaying = false;
        yield return new WaitForSeconds(1.5f);
        IsPlaying = true;
        // MazeLoader re-places player at spawn — signal via event
        FindObjectOfType<MazeLoader>()?.RespawnPlayer();
    }

    IEnumerator GameOver(bool win)
    {
        IsPlaying = false;
        yield return new WaitForSeconds(1.5f);
        ApparentWin = win;
        OnGameOver?.Invoke(win);

        if (win) SceneManager.LoadScene(SCENE_END);
        else     SceneManager.LoadScene(SCENE_GAMEOVER);
    }

    // Formatted session time for end screen
    public string FormattedTime()
    {
        int min = (int)(SessionTimer / 60);
        int sec = (int)(SessionTimer % 60);
        return $"{min:D2}:{sec:D2}";
    }

    // Layout index cycling (0-4)
    public int MazeLayoutIndex => (Level - 1) % 5;
}
