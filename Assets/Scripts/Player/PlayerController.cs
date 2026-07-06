using UnityEngine;
using System.Collections;

// Attach to: Player prefab
// Required: SpriteRenderer, Animator, CircleCollider2D (trigger)
// Dependencies: SpeedSystem, GameManager, PlayerStateManager, AudioManager, NotificationManager
[RequireComponent(typeof(SpriteRenderer))]
[RequireComponent(typeof(Animator))]
public class PlayerController : MonoBehaviour
{
    // ── Config ────────────────────────────────────────────────────
    [SerializeField] float baseSpeed    = 5f;   // tiles per second
    [SerializeField] float malusSpeed   = 2.5f;
    [SerializeField] float malusDuration = 2f;
    [SerializeField] LayerMask wallLayer;

    // ── State ─────────────────────────────────────────────────────
    bool[,]  _walkable;
    Vector2Int _gridPos;
    Vector2    _moveDir;
    Vector2    _queuedDir;

    bool _isMoving;
    bool _malusActive;
    bool _autoPlayActive;
    bool _isDead;

    // True for the whole Clone-phase window (Smartphone pickup) — lets the
    // player eat any enemy on contact even if that specific enemy already
    // reverted to Chase on its own (e.g. it was eaten-and-respawned earlier
    // in the same window), instead of relying on each enemy's own state.
    public bool PowerModeActive { get; private set; }

    float _currentSpeed;

    // ── References ────────────────────────────────────────────────
    PlayerStateManager _stateManager;
    Animator           _anim;
    bool               _hasAnimController;

    static readonly int AnimDirX   = Animator.StringToHash("DirX");
    static readonly int AnimDirY   = Animator.StringToHash("DirY");
    static readonly int AnimMoving = Animator.StringToHash("Moving");
    static readonly int AnimDeath  = Animator.StringToHash("Death");
    static readonly int AnimMalus  = Animator.StringToHash("Malus");

    // ── Lifecycle ─────────────────────────────────────────────────

    Vector3 _baseScale;

    void Awake()
    {
        _anim              = GetComponent<Animator>();
        _hasAnimController = _anim != null && _anim.runtimeAnimatorController != null;
        _stateManager      = GetComponent<PlayerStateManager>();
        _baseScale         = transform.localScale;
    }

    public void Init(bool[,] walkabilityGrid)
    {
        _walkable = walkabilityGrid;
        ResetState();
    }

    public void ResetState()
    {
        _isDead      = false;
        _isMoving    = false;
        _malusActive = false;
        _moveDir     = Vector2.zero;
        _queuedDir   = Vector2.zero;
        _currentSpeed = baseSpeed;

        GetComponent<DissolveEffect>()?.ResetVisual();

        // The Death state has no exit transition of its own (it's meant to freeze
        // on the last dissolve frame) — force the Animator back to Idle directly,
        // otherwise the sprite stays stuck on the death pose forever.
        if (_hasAnimController) { _anim.SetBool(AnimMoving, false); _anim.Play("Idle", 0, 0f); }

        SnapToGrid();
        _stateManager?.SetState(PlayerState.Normal);

        // Brief invincibility after respawn so player can't be hit immediately
        GetComponent<DamageFlash>()?.StartRespawnInvincibility();
    }

    void SnapToGrid()
    {
        // Convert world pos → grid cell
        // Grid origin: column 0 = x=0.5, row 0 = y = (Height-0.5) in world
        // MazeLoader places tiles so world.x ≈ cell.x + 0.5
        int gx = Mathf.RoundToInt(transform.position.x - 0.5f);
        int gy = MazeData.Height - 1 - Mathf.RoundToInt(transform.position.y - 0.5f);
        _gridPos = new Vector2Int(gx, gy);
    }

    // ── Input & movement ─────────────────────────────────────────

    void Update()
    {
        if (_isDead || !GameManager.Instance.IsPlaying) return;

        ReadInput();
        if (!_isMoving) TryMove();
    }

    // Swipe state — touch on device, mouse-drag fallback so it's testable in-editor/desktop.
    Vector2 _swipeStartPos;
    bool    _swipeActive;
    const float SWIPE_THRESHOLD = 40f; // pixels before a drag counts as a directional swipe

    void ReadInput()
    {
        if (Input.GetKey(KeyCode.RightArrow) || Input.GetKey(KeyCode.D)) _queuedDir = Vector2.right;
        else if (Input.GetKey(KeyCode.LeftArrow)  || Input.GetKey(KeyCode.A)) _queuedDir = Vector2.left;
        else if (Input.GetKey(KeyCode.UpArrow)    || Input.GetKey(KeyCode.W)) _queuedDir = Vector2.up;
        else if (Input.GetKey(KeyCode.DownArrow)  || Input.GetKey(KeyCode.S)) _queuedDir = Vector2.down;

        ReadSwipeInput();
    }

    void ReadSwipeInput()
    {
        bool down, held;
        Vector2 pos;

        if (Input.touchCount > 0)
        {
            var t = Input.GetTouch(0);
            pos  = t.position;
            down = t.phase == TouchPhase.Began;
            held = t.phase == TouchPhase.Moved || t.phase == TouchPhase.Stationary;
        }
        else
        {
            pos  = Input.mousePosition;
            down = Input.GetMouseButtonDown(0);
            held = Input.GetMouseButton(0);
        }

        if (down)
        {
            _swipeStartPos = pos;
            _swipeActive   = true;
        }
        else if (held && _swipeActive)
        {
            Vector2 delta = pos - _swipeStartPos;
            if (delta.magnitude >= SWIPE_THRESHOLD)
            {
                _queuedDir = Mathf.Abs(delta.x) > Mathf.Abs(delta.y)
                    ? (delta.x > 0f ? Vector2.right : Vector2.left)
                    : (delta.y > 0f ? Vector2.up    : Vector2.down);
                _swipeStartPos = pos; // reset so a continued drag can queue further turns
            }
        }
        else if (!held)
        {
            _swipeActive = false;
        }
    }

    void TryMove()
    {
        // Try queued direction first, then current direction
        if (_queuedDir != Vector2.zero && CanMoveTo(_queuedDir))
        {
            _moveDir   = _queuedDir;
            _queuedDir = Vector2.zero;
        }
        if (_moveDir != Vector2.zero && CanMoveTo(_moveDir))
            StartCoroutine(MoveStep(_moveDir));
        else if (_moveDir != Vector2.zero && !CanMoveTo(_moveDir))
        {
            _moveDir = Vector2.zero; // hit wall, stop
            if (_hasAnimController) _anim.SetBool(AnimMoving, false);
        }
    }

    bool CanMoveTo(Vector2 dir)
    {
        int nx = _gridPos.x + (int)dir.x;
        int ny = _gridPos.y - (int)dir.y; // grid Y is inverted (0=top)

        // Allow tunnel exit: moving off left/right edge at tunnel row
        if (ny == MazeData.TunnelRow && (nx < 0 || nx >= MazeData.Width)) return true;

        if (_walkable == null) return false;
        if (nx < 0 || nx >= MazeData.Width || ny < 0 || ny >= MazeData.Height) return false;
        return _walkable[nx, ny];
    }

    IEnumerator MoveStep(Vector2 dir)
    {
        _isMoving = true;
        if (_hasAnimController) _anim.SetBool(AnimMoving, true);

        int nx = _gridPos.x + (int)dir.x;
        int ny = _gridPos.y - (int)dir.y;

        // ── Tunnel wrap ─────────────────────────────────────────────
        if (ny == MazeData.TunnelRow)
        {
            if (nx < 0)              nx = MazeData.Width - 1;
            else if (nx >= MazeData.Width) nx = 0;

            if (nx != _gridPos.x + (int)dir.x)   // wrap happened
            {
                // Slide off-screen in the move direction, then snap to other side
                Vector3 offscreen = transform.position + (Vector3)(dir * 1.5f);
                float   elapsed0  = 0f;
                float   dur0      = 1f / (_malusActive ? malusSpeed : baseSpeed);
                while (elapsed0 < dur0 * 0.4f)   // move toward edge for 40% of step
                {
                    elapsed0 += Time.deltaTime;
                    transform.position = Vector3.Lerp(transform.position, offscreen,
                        elapsed0 / (dur0 * 0.4f));
                    yield return null;
                }

                // Snap to opposite side and tell camera to jump instantly
                transform.position = new Vector3(nx + 0.5f, MazeData.Height - 1 - ny + 0.5f, 0f);
                CameraFollow.Instance?.SnapOnce();

                _gridPos  = new Vector2Int(nx, ny);
                _isMoving = false;
                CheckHideHint();
                yield break;
            }
        }

        // ── Normal movement ─────────────────────────────────────────
        Vector3 start = transform.position;
        Vector3 end   = new Vector3(nx + 0.5f, MazeData.Height - 1 - ny + 0.5f, 0f);

        float speed = _currentSpeed;
        if (_malusActive)    speed = malusSpeed;
        if (_autoPlayActive) speed = baseSpeed * 1.5f;

        float elapsed  = 0f;
        float duration = 1f / speed;

        if (_hasAnimController)
        {
            _anim.SetFloat(AnimDirX, dir.x);
            _anim.SetFloat(AnimDirY, dir.y);
        }

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            transform.position = Vector3.Lerp(start, end, t);

            // Squash & stretch: stretch tall mid-step, squash back down on arrival.
            float stretch = Mathf.Sin(t * Mathf.PI) * 0.12f;
            transform.localScale = new Vector3(
                _baseScale.x * (1f - stretch), _baseScale.y * (1f + stretch), _baseScale.z);

            yield return null;
        }

        transform.position   = end;
        transform.localScale = _baseScale;
        _gridPos  = new Vector2Int(nx, ny);
        _isMoving = false;
        CheckHideHint();
    }

    // First time the player ducks into a dead-end niche, explain the mechanic.
    void CheckHideHint()
    {
        if (MazeLoader.HideCells != null && MazeLoader.HideCells.Contains(_gridPos))
            GameManager.Instance?.ShowHintOnce("hide", "DUCK INTO ALCOVES TO LOSE ENEMIES");
    }

    // ── Hazard effects ────────────────────────────────────────────

    public void ApplyMalus()
    {
        if (_malusActive) return;
        StartCoroutine(MalusCoroutine());
        AudioManager.Instance?.PlaySFX("malus_hit");
        NotificationManager.Instance?.TriggerNotification("CONNECTION THROTTLED", "malus");
        _stateManager?.SetState(PlayerState.MalusSlowed);
    }

    IEnumerator MalusCoroutine()
    {
        _malusActive = true;
        if (_hasAnimController) _anim.SetTrigger(AnimMalus);
        yield return new WaitForSeconds(malusDuration);
        _malusActive = false;
        _stateManager?.RefreshStateFromSpeed();
    }

    public void ApplyAutoPlay()
    {
        StartCoroutine(AutoPlayCoroutine());
        AudioManager.Instance?.PlaySFX("autoplay");
    }

    IEnumerator AutoPlayCoroutine()
    {
        _autoPlayActive = true;
        yield return new WaitForSeconds(3f);
        _autoPlayActive = false;
    }

    // ── Death ─────────────────────────────────────────────────────

    public void Die()
    {
        if (_isDead) return;
        _isDead   = true;
        _isMoving = false;
        if (_hasAnimController) _anim.SetTrigger(AnimDeath);
        AudioManager.Instance?.PlaySFX("player_death");
        CameraFollow.Instance?.Shake(0.35f, 0.18f);
        CameraFollow.Instance?.HitStop(0.06f);
        StartCoroutine(DeathSequence());
    }

    IEnumerator DeathSequence()
    {
        // Brief flash beat before the sprite breaks apart, matching the hit-stop/shake.
        yield return new WaitForSeconds(0.2f);

        var dissolve = GetComponent<DissolveEffect>();
        if (dissolve == null) dissolve = gameObject.AddComponent<DissolveEffect>();
        yield return StartCoroutine(dissolve.Dissolve(0.7f, new Color(1f, 0.3f, 0.56f, 1f)));

        yield return new WaitForSeconds(0.3f);
        GameManager.Instance?.PlayerDied();
    }

    // ── Trigger collisions ────────────────────────────────────────

    void OnTriggerEnter2D(Collider2D other)
    {
        if (_isDead) return;

        // Ignore damage during post-respawn invincibility frames
        var flash = GetComponent<DamageFlash>();

        if (other.CompareTag("Enemy"))
        {
            var enemy = other.GetComponent<LikeEnemy>();
            if (enemy == null) return;

            // Les ennemis en cours de respawn (invisibles) ne tuent pas et ne se mangent pas
            if (enemy.IsRespawning) return;

            if (enemy.IsFrightened || PowerModeActive)
            {
                enemy.GetEaten();
                GameManager.Instance?.AddScore(200);
                ScorePopup.Spawn(transform.position, "+200",
                    new Color(0f, 0.96f, 1f));
                AudioManager.Instance?.PlaySFX("like_consume");
                NotificationManager.Instance?.TriggerNotification("CONTENT SHARED", "clone");
                CameraFollow.Instance?.Shake(0.15f, 0.06f);
            }
            else
            {
                if (flash != null && flash.IsInvincible) return;
                flash?.PlayHitFlash();
                Die();
            }
        }
    }

    // Called by CollectibleItem when the Smartphone pickup starts the Clone
    // phase — keeps the player safe to eat any enemy for the whole window.
    public void ActivatePowerMode(float duration)
    {
        StopCoroutine(nameof(PowerModeTimer));
        StartCoroutine(PowerModeTimer(duration));
    }

    IEnumerator PowerModeTimer(float duration)
    {
        PowerModeActive = true;
        yield return new WaitForSeconds(duration);
        PowerModeActive = false;
    }

    public Vector2Int GridPos   => _gridPos;
    public Vector2    MoveDir   => _moveDir;
    public bool       IsDead    => _isDead;
}
