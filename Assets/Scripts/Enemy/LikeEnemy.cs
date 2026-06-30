using UnityEngine;
using System.Collections;

// Attach to: Enemy prefab
// Required: SpriteRenderer, Animator, CircleCollider2D (trigger)
// Dependencies: EnemyPathfinder (same GO), SpeedSystem, GameManager, AudioManager
[RequireComponent(typeof(SpriteRenderer))]
[RequireComponent(typeof(Animator))]
[RequireComponent(typeof(EnemyPathfinder))]
public class LikeEnemy : MonoBehaviour
{
    // ── Config ─────────────────────────────────────────────────────
    [SerializeField] float baseSpeed       = 4f;
    [SerializeField] float frightenedSpeed = 0.5f;
    [SerializeField] Vector2Int scatterTarget = new Vector2Int(0, 0);

    [Header("Tier sprites")]
    [SerializeField] Sprite spriteTier1;
    [SerializeField] Sprite spriteTier2;
    [SerializeField] Sprite spriteTier3;
    [SerializeField] Sprite spriteFrightened;
    [SerializeField] Sprite spriteClone;

    // ── Difficulty params (set by MazeLoader via Init) ─────────────
    float _diffSpeedMult  = 1f;   // from DifficultyConfig.EnemySpeed
    float _scatterSeconds = 5f;   // from DifficultyConfig.ScatterDuration
    bool  _anticipate     = false; // from DifficultyConfig.Anticipate

    // ── State machine ──────────────────────────────────────────────
    enum EnemyState { Chase, Scatter, Frightened, Clone, Respawning }
    EnemyState _state = EnemyState.Scatter;

    // ── Runtime ───────────────────────────────────────────────────
    bool[,]    _walkable;
    Vector2Int _gridPos;
    Vector2Int _spawnCell;
    bool       _isMoving;
    float      _trendingBurst;
    float      _trendingTimer;

    EnemyPathfinder _pathfinder;
    SpriteRenderer  _sr;
    Animator        _anim;

    static readonly int AnimSpeed      = Animator.StringToHash("Speed");
    static readonly int AnimFrightened = Animator.StringToHash("Frightened");
    static readonly int AnimRespawn    = Animator.StringToHash("Respawn");

    // ── Public API ─────────────────────────────────────────────────
    public bool IsFrightened => _state == EnemyState.Frightened || _state == EnemyState.Clone;

    public void Init(bool[,] walkabilityGrid, Vector2Int spawnCell,
                     float diffSpeedMult = 1f, float scatterDuration = 5f, bool anticipate = false)
    {
        _walkable       = walkabilityGrid;
        _spawnCell      = spawnCell;
        _gridPos        = spawnCell;
        _diffSpeedMult  = diffSpeedMult;
        _scatterSeconds = scatterDuration;
        _anticipate     = anticipate;

        _pathfinder = GetComponent<EnemyPathfinder>();
        _pathfinder.Init(_walkable);
        _sr   = GetComponent<SpriteRenderer>();
        _anim = GetComponent<Animator>();

        if (SpeedSystem.Instance != null)
            SpeedSystem.Instance.OnSpeedChanged += OnSpeedChanged;

        StartCoroutine(ScatterThenChase());
    }

    void OnDestroy()
    {
        if (SpeedSystem.Instance != null)
            SpeedSystem.Instance.OnSpeedChanged -= OnSpeedChanged;
    }

    void OnSpeedChanged(float m) => UpdateVisualTier(m);

    // ── Movement loop ──────────────────────────────────────────────

    void Update()
    {
        if (_trendingTimer > 0f)
        {
            _trendingTimer -= Time.deltaTime;
            if (_trendingTimer <= 0f) _trendingBurst = 0f;
        }

        if (!_isMoving && GameManager.Instance != null && GameManager.Instance.IsPlaying)
            StartCoroutine(MoveStep());
    }

    IEnumerator MoveStep()
    {
        _isMoving = true;

        var player = Object.FindAnyObjectByType<PlayerController>();
        Vector2Int target = GetTarget(player);

        Vector2Int playerDir = player != null
            ? new Vector2Int((int)player.MoveDir.x, -(int)player.MoveDir.y)
            : Vector2Int.zero;

        Vector2Int next = _pathfinder.GetNextCell(_gridPos, target, _anticipate, playerDir);

        Vector3 startPos = GridToWorld(_gridPos);
        Vector3 endPos   = GridToWorld(next);

        float speed = ComputeSpeed();
        float dur   = 1f / speed;
        float t     = 0f;

        while (t < dur)
        {
            t += Time.deltaTime;
            transform.position = Vector3.Lerp(startPos, endPos, t / dur);
            yield return null;
        }

        transform.position = endPos;
        _gridPos  = next;
        _isMoving = false;
    }

    Vector2Int GetTarget(PlayerController player)
    {
        switch (_state)
        {
            case EnemyState.Frightened:
                if (player != null)
                    return new Vector2Int(MazeData.Width  - 1 - player.GridPos.x,
                                         MazeData.Height - 1 - player.GridPos.y);
                return scatterTarget;
            case EnemyState.Chase:
            case EnemyState.Clone:
                return player != null ? player.GridPos : scatterTarget;
            case EnemyState.Scatter:
                return scatterTarget;
            case EnemyState.Respawning:
                return _spawnCell;
            default:
                return scatterTarget;
        }
    }

    float ComputeSpeed()
    {
        float sysMult = SpeedSystem.Instance != null ? SpeedSystem.Instance.CurrentMultiplier : 1f;
        float s = baseSpeed * _diffSpeedMult * sysMult + _trendingBurst;
        if (_state == EnemyState.Frightened || _state == EnemyState.Clone)
            s = baseSpeed * frightenedSpeed;
        if (_state == EnemyState.Respawning)
            s = baseSpeed * 2f;
        return Mathf.Max(s, 0.5f);
    }

    // ── State transitions ──────────────────────────────────────────

    IEnumerator ScatterThenChase()
    {
        SetState(EnemyState.Scatter);
        yield return new WaitForSeconds(_scatterSeconds);
        SetState(EnemyState.Chase);
    }

    public void EnterFrightened()
    {
        if (_state == EnemyState.Respawning) return;
        StopAllCoroutines();
        StartCoroutine(FrightenedSequence());
    }

    IEnumerator FrightenedSequence()
    {
        float cloneDuration = 8f;
        SetState(EnemyState.Frightened);
        yield return new WaitForSeconds(cloneDuration * 0.75f);
        for (int i = 0; i < 6; i++)
        {
            _sr.enabled = !_sr.enabled;
            yield return new WaitForSeconds(0.2f);
        }
        _sr.enabled = true;
        if (_state == EnemyState.Frightened)
            SetState(EnemyState.Chase);
    }

    public void EnterClone()
    {
        if (_state == EnemyState.Respawning) return;
        StopAllCoroutines();
        StartCoroutine(CloneSequence());
    }

    IEnumerator CloneSequence()
    {
        SetState(EnemyState.Clone);
        yield return new WaitForSeconds(8f);
        SetState(EnemyState.Chase);
    }

    public void GetEaten()
    {
        StopAllCoroutines();
        StartCoroutine(RespawnSequence());
    }

    IEnumerator RespawnSequence()
    {
        SetState(EnemyState.Respawning);
        _sr.enabled = false;
        AudioManager.Instance?.PlaySFX("enemy_return");

        while (_gridPos != _spawnCell)
            yield return StartCoroutine(MoveStep());

        _anim.SetTrigger(AnimRespawn);
        _sr.enabled = true;
        yield return new WaitForSeconds(0.8f);
        SetState(EnemyState.Chase);
    }

    public void ApplyTrendingBurst(float burstAmount, float duration)
    {
        _trendingBurst = burstAmount;
        _trendingTimer = duration;
        StartCoroutine(OrangeFlash());
    }

    IEnumerator OrangeFlash()
    {
        _sr.color = new Color(1f, 0.55f, 0.1f);
        yield return new WaitForSeconds(0.3f);
        _sr.color = Color.white;
    }

    void UpdateVisualTier(float m)
    {
        if (_state == EnemyState.Frightened || _state == EnemyState.Clone ||
            _state == EnemyState.Respawning) return;

        if (_anim) _anim.SetFloat(AnimSpeed, m);

        if (_sr)
        {
            if (m >= 2.2f && spriteTier3) _sr.sprite = spriteTier3;
            else if (m >= 1.5f && spriteTier2) _sr.sprite = spriteTier2;
            else if (spriteTier1) _sr.sprite = spriteTier1;
        }
    }

    void SetState(EnemyState s)
    {
        _state = s;
        if (_anim) _anim.SetBool(AnimFrightened, s == EnemyState.Frightened || s == EnemyState.Clone);

        if (_sr)
        {
            switch (s)
            {
                case EnemyState.Frightened:
                    if (spriteFrightened) _sr.sprite = spriteFrightened;
                    _sr.color = Color.white;
                    break;
                case EnemyState.Clone:
                    if (spriteClone) _sr.sprite = spriteClone;
                    break;
                default:
                    float m = SpeedSystem.Instance ? SpeedSystem.Instance.CurrentMultiplier : 1f;
                    UpdateVisualTier(m);
                    _sr.color = Color.white;
                    break;
            }
        }
    }

    Vector3 GridToWorld(Vector2Int cell) =>
        new Vector3(cell.x + 0.5f, MazeData.Height - 1 - cell.y + 0.5f, 0f);

    void OnTriggerEnter2D(Collider2D other)
    {
        // Collision handled from PlayerController side
    }
}
