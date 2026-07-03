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
    [SerializeField] float malusDuration   = 5f;   // longer than player (2s)
    [SerializeField] float malusSpeedMult  = 0.3f; // 30% of normal speed
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

    // ── Personality (set by MazeLoader via Init, one per spawn slot) ─
    // Chaser: targets the player directly (Blinky-style)
    // Ambusher: targets 4 tiles ahead of the player's facing direction (Pinky-style)
    // Flanker: targets the point mirroring the player through this enemy's scatter corner (Inky-style)
    // Skittish: chases only within 6 tiles, otherwise retreats to its scatter corner (Clyde-style)
    enum Personality { Chaser, Ambusher, Flanker, Skittish }
    Personality _personality = Personality.Chaser;

    // Pac-Man-style ghost identity colors: Chaser=Blinky red, Ambusher=Pinky pink,
    // Flanker=Inky cyan, Skittish=Clyde orange — lets players read each enemy's role at a glance.
    static readonly Color[] PersonalityColors =
    {
        new Color(1f,    0.23f, 0.19f), // Chaser  — red
        new Color(1f,    0.40f, 0.77f), // Ambusher — pink
        new Color(0f,    0.90f, 1f),    // Flanker  — cyan
        new Color(1f,    0.58f, 0f),    // Skittish — orange
    };
    Color _baseColor = Color.white;

    // ── State machine ──────────────────────────────────────────────
    enum EnemyState { Chase, Scatter, Frightened, Clone, Respawning }
    EnemyState _state = EnemyState.Scatter;

    // ── Runtime ───────────────────────────────────────────────────
    bool[,]    _walkable;
    Vector2Int _gridPos;
    Vector2Int _spawnCell;
    bool       _isMoving;
    bool       _malusActive;
    float      _trendingBurst;
    float      _trendingTimer;
    Vector3    _originalScale;
    int        _forceHeartFrames; // force sprite cœur N frames après fin du clone

    // Référence au SpriteRenderer du joueur — utilisée pendant la clone phase
    SpriteRenderer _playerSpriteRef;

    EnemyPathfinder _pathfinder;
    SpriteRenderer  _sr;
    Animator        _anim;

    static readonly int AnimSpeed      = Animator.StringToHash("Speed");
    static readonly int AnimFrightened = Animator.StringToHash("Frightened");
    static readonly int AnimRespawn    = Animator.StringToHash("Respawn");

    // ── Public API ─────────────────────────────────────────────────
    public bool IsFrightened  => _state == EnemyState.Frightened || _state == EnemyState.Clone;
    public bool IsRespawning  => _state == EnemyState.Respawning;

    public void Init(bool[,] walkabilityGrid, Vector2Int spawnCell,
                     float diffSpeedMult = 1f, float scatterDuration = 5f, bool anticipate = false,
                     int personalityIndex = 0)
    {
        _walkable       = walkabilityGrid;
        _spawnCell      = spawnCell;
        _gridPos        = spawnCell;
        _diffSpeedMult  = diffSpeedMult;
        _scatterSeconds = scatterDuration;
        _anticipate     = anticipate;
        _personality    = (Personality)(personalityIndex % 4);
        _baseColor      = PersonalityColors[(int)_personality];

        _pathfinder    = GetComponent<EnemyPathfinder>();
        _pathfinder.Init(_walkable);
        _sr            = GetComponent<SpriteRenderer>();
        _anim          = GetComponent<Animator>();
        _originalScale = transform.localScale;

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

    // LateUpdate s'exécute après l'Animator — on gère le sprite manuellement
    void LateUpdate()
    {
        if (_state == EnemyState.Clone && _playerSpriteRef != null && _sr != null)
        {
            // Copier sprite + flip du joueur pour un mirroring parfait
            _sr.sprite  = _playerSpriteRef.sprite;
            _sr.flipX   = _playerSpriteRef.flipX;
        }
        else if (_forceHeartFrames > 0 && _sr != null)
        {
            // L'Animator peut mettre 1-2 frames à se réinitialiser après clone :
            // on force le sprite de cœur pour éviter un flash du sprite joueur.
            float m = SpeedSystem.Instance ? SpeedSystem.Instance.CurrentMultiplier : 1f;
            if      (m >= 2.2f && spriteTier3) _sr.sprite = spriteTier3;
            else if (m >= 1.5f && spriteTier2) _sr.sprite = spriteTier2;
            else if (spriteTier1)              _sr.sprite = spriteTier1;
            _sr.flipX = false;
            _forceHeartFrames--;
        }
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
                if (player == null) return scatterTarget;
                if (IsPlayerHidden(player)) return scatterTarget; // lost track — player ducked into a niche
                return GetPersonalityTarget(player);
            case EnemyState.Clone:
                return player != null ? GetPersonalityTarget(player) : scatterTarget;
            case EnemyState.Scatter:
                return scatterTarget;
            case EnemyState.Respawning:
                return _spawnCell;
            default:
                return scatterTarget;
        }
    }

    // True while the player stands in a dead-end niche (MazeLoader.HideCells) and this
    // enemy isn't already right next to them — they've broken line of sight.
    bool IsPlayerHidden(PlayerController player)
    {
        if (MazeLoader.HideCells == null || MazeLoader.HideCells.Count == 0) return false;
        if (!MazeLoader.HideCells.Contains(player.GridPos)) return false;
        int dist = Mathf.Abs(_gridPos.x - player.GridPos.x) + Mathf.Abs(_gridPos.y - player.GridPos.y);
        return dist > 1;
    }

    // Blinky/Pinky/Inky/Clyde-style targeting so the 4 enemies don't all behave identically.
    Vector2Int GetPersonalityTarget(PlayerController player)
    {
        Vector2Int playerCell = player.GridPos;

        switch (_personality)
        {
            case Personality.Ambusher:
            {
                Vector2Int dir = new Vector2Int((int)player.MoveDir.x, -(int)player.MoveDir.y);
                return new Vector2Int(
                    Mathf.Clamp(playerCell.x + dir.x * 4, 0, MazeData.Width - 1),
                    Mathf.Clamp(playerCell.y + dir.y * 4, 0, MazeData.Height - 1));
            }
            case Personality.Flanker:
                // Mirrors the player through this enemy's scatter corner — approaches from the opposite side.
                return new Vector2Int(
                    Mathf.Clamp(2 * playerCell.x - scatterTarget.x, 0, MazeData.Width - 1),
                    Mathf.Clamp(2 * playerCell.y - scatterTarget.y, 0, MazeData.Height - 1));
            case Personality.Skittish:
                // Only chases at close range, otherwise retreats — keeps some breathing room open.
                return Vector2Int.Distance(_gridPos, playerCell) > 6f ? scatterTarget : playerCell;
            default: // Chaser
                return playerCell;
        }
    }

    float ComputeSpeed()
    {
        float sysMult = SpeedSystem.Instance != null ? SpeedSystem.Instance.CurrentMultiplier : 1f;

        if (_state == EnemyState.Respawning)  return baseSpeed * 2f;
        if (_state == EnemyState.Frightened || _state == EnemyState.Clone)
            return Mathf.Max(baseSpeed * frightenedSpeed, 0.5f);

        float s = baseSpeed * _diffSpeedMult * sysMult + _trendingBurst;
        if (_malusActive) s *= malusSpeedMult;
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
        _isMoving = false;
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

        // Mémoriser le SpriteRenderer du joueur pour le copier en LateUpdate
        var player = Object.FindAnyObjectByType<PlayerController>();
        _playerSpriteRef = player != null ? player.GetComponent<SpriteRenderer>() : null;

        StopAllCoroutines();
        _isMoving = false;
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
        _isMoving = false;
        StartCoroutine(RespawnSequence());
    }

    IEnumerator RespawnSequence()
    {
        SetState(EnemyState.Respawning);
        AudioManager.Instance?.PlaySFX("enemy_return");

        var dissolve = GetComponent<DissolveEffect>();
        if (dissolve == null) dissolve = gameObject.AddComponent<DissolveEffect>();
        yield return StartCoroutine(dissolve.Dissolve(0.45f, new Color(0f, 0.96f, 1f, 1f)));
        _sr.enabled = false;

        yield return new WaitForSeconds(0.15f);

        // Téléportation directe au spawn — bloquer l'Update pendant ce temps
        _isMoving = true;
        _gridPos  = _spawnCell;
        transform.position = GridToWorld(_spawnCell);
        dissolve.ResetVisual();

        // Clignotement d'apparition (4 flashs)
        if (_anim && _anim.runtimeAnimatorController) _anim.SetTrigger(AnimRespawn);
        for (int i = 0; i < 4; i++)
        {
            _sr.enabled = true;
            yield return new WaitForSeconds(0.08f);
            _sr.enabled = false;
            yield return new WaitForSeconds(0.08f);
        }
        _sr.enabled = true;

        yield return new WaitForSeconds(0.2f);

        // Libérer le verrou AVANT SetState pour que l'Update redémarre immédiatement
        _isMoving = false;
        SetState(EnemyState.Chase);
    }

    // Appelé par GameManager après respawn du joueur
    public void ScatterBriefly(float duration)
    {
        if (_state == EnemyState.Respawning) return;
        StopAllCoroutines();
        _isMoving = false;
        StartCoroutine(BriefScatterSequence(duration));
    }

    IEnumerator BriefScatterSequence(float duration)
    {
        SetState(EnemyState.Scatter);
        yield return new WaitForSeconds(duration);
        if (_state == EnemyState.Scatter)
            SetState(EnemyState.Chase);
    }

    public void ApplyMalus()
    {
        if (_malusActive || _state == EnemyState.Respawning) return;
        StartCoroutine(MalusCoroutine());
    }

    IEnumerator MalusCoroutine()
    {
        _malusActive = true;
        if (_sr) _sr.color = new Color(0f, 0.96f, 1f); // cyan flash #00F5FF
        yield return new WaitForSeconds(malusDuration);
        _malusActive = false;
        if (_sr) _sr.color = _baseColor;
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
        _sr.color = _baseColor;
    }

    void UpdateVisualTier(float m)
    {
        if (_state == EnemyState.Frightened || _state == EnemyState.Clone ||
            _state == EnemyState.Respawning) return;

        if (_anim && _anim.runtimeAnimatorController) _anim.SetFloat(AnimSpeed, m);

        if (_sr)
        {
            if (m >= 2.2f && spriteTier3) _sr.sprite = spriteTier3;
            else if (m >= 1.5f && spriteTier2) _sr.sprite = spriteTier2;
            else if (spriteTier1) _sr.sprite = spriteTier1;
        }
    }

    void SetState(EnemyState s)
    {
        bool wasClone = _state == EnemyState.Clone;
        _state = s;
        if (_anim && _anim.runtimeAnimatorController)
            _anim.SetBool(AnimFrightened, s == EnemyState.Frightened || s == EnemyState.Clone);

        if (s != EnemyState.Clone)
        {
            _playerSpriteRef = null; // fin de la clone phase

            // Forcer la restauration du cœur pendant quelques frames pour
            // contrer le délai de transition de l'Animator
            if (wasClone)
                _forceHeartFrames = 6;
        }

        if (_sr)
        {
            switch (s)
            {
                case EnemyState.Frightened:
                    if (spriteFrightened) _sr.sprite = spriteFrightened;
                    _sr.color = Color.white;
                    transform.localScale = _originalScale;
                    break;

                case EnemyState.Clone:
                    _sr.color = Color.white;
                    // Prendre exactement la taille du joueur pour un rendu identique
                    if (_playerSpriteRef != null)
                        transform.localScale = _playerSpriteRef.transform.localScale;
                    else
                        transform.localScale = _originalScale * 3f;
                    break;

                default:
                    float m = SpeedSystem.Instance ? SpeedSystem.Instance.CurrentMultiplier : 1f;
                    UpdateVisualTier(m);
                    _sr.color = _baseColor;
                    _sr.flipX = false;
                    transform.localScale = _originalScale;
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
