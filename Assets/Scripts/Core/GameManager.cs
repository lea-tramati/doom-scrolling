using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

// Attach to: persistent GameObject "GameManager"
// Level progression: score-based, thresholds double each level
//   Level 1→2: 800   Level 2→3: 1600   Level 3→4: 3200
//   Level 4→5: 6400  Level 5→6: 12800  Level 6→7: 25600  Level 7→8: 51200
public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    // ── Scene names ───────────────────────────────────────────────
    const string SCENE_TITLE    = "TitleScreen";
    const string SCENE_GAME     = "GameScene";
    const string SCENE_GAMEOVER = "GameOverScreen";
    const string SCENE_END      = "EndScreen";

    // ── Level progression (score-based) ──────────────────────────
    // ScoreThresholds[i] = cumulative score needed to advance from level (i+1) to (i+2)
    static readonly int[] ScoreThresholds = { 800, 1600, 3200, 6400, 12800, 25600, 51200 };
    public const int MaxLevel = 8;

    // ── Public state ──────────────────────────────────────────────
    public int   Score         { get; private set; }
    public int   Lives         { get; private set; } = 3;
    public int   Level         { get; private set; } = 1;
    public float SessionTimer  { get; private set; }
    public bool  ApparentWin   { get; private set; }
    public bool  IsPlaying     { get; private set; }

    [Header("Config")]
    [SerializeField] int startLives = 3;

    int  _dotsCollected;     // stat tracking only — no longer drives level
    int  _appIconsThisLevel;
    bool _levelingUp;        // guard against double-trigger

    // ── Events ────────────────────────────────────────────────────
    public System.Action<int>   OnScoreChanged;
    public System.Action<int>   OnLivesChanged;
    public System.Action<int>   OnLevelChanged;
    public System.Action<float> OnLevelProgressChanged; // 0–1, progress toward next threshold
    public System.Action<bool>  OnGameOver;
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
        Score             = 0;
        Lives             = startLives;
        Level             = 1;
        _dotsCollected    = 0;
        _appIconsThisLevel = 0;
        _levelingUp       = false;
        ApparentWin       = false;
        IsPlaying         = true;
        SpeedSystem.Instance?.ResetSpeed();
        SceneManager.LoadScene(SCENE_GAME);
    }

    public void AddScore(int pts)
    {
        Score += pts;
        OnScoreChanged?.Invoke(Score);
        BroadcastLevelProgress();
        CheckLevelUp();
    }

    // Dot collected — stat tracking only, no level trigger
    public void OnDotCollected()   => _dotsCollected++;
    public void OnAppIconCollected() => _appIconsThisLevel++;
    public int  AppIconsThisLevel  => _appIconsThisLevel;

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

    // Returns score needed to reach next level (or -1 at max level)
    public int NextLevelThreshold()
    {
        int idx = Level - 1;
        return idx < ScoreThresholds.Length ? ScoreThresholds[idx] : -1;
    }

    // 0–1 progress towards the next level threshold
    public float LevelProgress()
    {
        int idx = Level - 1;
        if (idx >= ScoreThresholds.Length) return 1f; // max level
        int prevThreshold = idx > 0 ? ScoreThresholds[idx - 1] : 0;
        int nextThreshold = ScoreThresholds[idx];
        return Mathf.Clamp01((float)(Score - prevThreshold) / (nextThreshold - prevThreshold));
    }

    // ── Internals ────────────────────────────────────────────────

    void CheckLevelUp()
    {
        if (_levelingUp || !IsPlaying) return;

        int idx = Level - 1;
        if (idx < ScoreThresholds.Length && Score >= ScoreThresholds[idx])
            StartCoroutine(LevelComplete());
    }

    void BroadcastLevelProgress()
    {
        OnLevelProgressChanged?.Invoke(LevelProgress());
    }

    IEnumerator LevelComplete()
    {
        _levelingUp = true;
        IsPlaying   = false;
        OnLevelComplete?.Invoke();

        // Show level-up overlay with new tier name
        var nextTier = DifficultyConfig.Get(Level + 1);
        HUDController.Instance?.ShowOverlay($"LEVEL {Level + 1}  —  {nextTier.Name}", 2.5f);

        yield return new WaitForSeconds(2.5f);

        Level++;
        _appIconsThisLevel = 0;

        if (Level > MaxLevel)
        {
            _levelingUp = false;
            TriggerApparentWin();
            yield break;
        }

        // Full life refill on every level-up
        Lives = startLives;
        OnLivesChanged?.Invoke(Lives);

        OnLevelChanged?.Invoke(Level);
        _levelingUp = false;
        IsPlaying   = true;

        // Reload maze with new layout
        SceneManager.LoadScene(SCENE_GAME);
    }

    IEnumerator RespawnDelay()
    {
        IsPlaying = false;
        yield return new WaitForSeconds(1.5f);
        IsPlaying = true;
        Object.FindAnyObjectByType<MazeLoader>()?.RespawnPlayer();

        // Grace period : les ennemis scattent 3s pour donner le temps au joueur de se repositionner
        foreach (var enemy in Object.FindObjectsByType<LikeEnemy>(FindObjectsSortMode.None))
            enemy.ScatterBriefly(3f);
    }

    IEnumerator GameOver(bool win)
    {
        IsPlaying = false;
        yield return new WaitForSeconds(1.5f);
        ApparentWin = win;
        OnGameOver?.Invoke(win);
        SceneManager.LoadScene(win ? SCENE_END : SCENE_GAMEOVER);
    }

    public string FormattedTime()
    {
        int min = (int)(SessionTimer / 60);
        int sec = (int)(SessionTimer % 60);
        return $"{min:D2}:{sec:D2}";
    }

    public int MazeLayoutIndex => (Level - 1) % 5;
}
