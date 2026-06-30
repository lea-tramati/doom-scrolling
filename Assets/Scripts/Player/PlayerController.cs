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

    float _currentSpeed;

    // ── References ────────────────────────────────────────────────
    PlayerStateManager _stateManager;
    Animator           _anim;

    static readonly int AnimDirX  = Animator.StringToHash("DirX");
    static readonly int AnimDirY  = Animator.StringToHash("DirY");
    static readonly int AnimDeath = Animator.StringToHash("Death");
    static readonly int AnimMalus = Animator.StringToHash("Malus");

    // ── Lifecycle ─────────────────────────────────────────────────

    void Awake()
    {
        _anim         = GetComponent<Animator>();
        _stateManager = GetComponent<PlayerStateManager>();
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

    void ReadInput()
    {
        if (Input.GetKey(KeyCode.RightArrow) || Input.GetKey(KeyCode.D)) _queuedDir = Vector2.right;
        else if (Input.GetKey(KeyCode.LeftArrow)  || Input.GetKey(KeyCode.A)) _queuedDir = Vector2.left;
        else if (Input.GetKey(KeyCode.UpArrow)    || Input.GetKey(KeyCode.W)) _queuedDir = Vector2.up;
        else if (Input.GetKey(KeyCode.DownArrow)  || Input.GetKey(KeyCode.S)) _queuedDir = Vector2.down;
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
            _moveDir = Vector2.zero; // hit wall, stop
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

        _anim.SetFloat(AnimDirX, dir.x);
        _anim.SetFloat(AnimDirY, dir.y);

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            transform.position = Vector3.Lerp(start, end, elapsed / duration);
            yield return null;
        }

        transform.position = end;
        _gridPos  = new Vector2Int(nx, ny);
        _isMoving = false;
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
        _anim.SetTrigger(AnimMalus);
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
        _anim.SetTrigger(AnimDeath);
        AudioManager.Instance?.PlaySFX("player_death");
        StartCoroutine(DeathSequence());
    }

    IEnumerator DeathSequence()
    {
        yield return new WaitForSeconds(1.2f);
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

            if (enemy.IsFrightened)
            {
                enemy.GetEaten();
                GameManager.Instance?.AddScore(200);
                ScorePopup.Spawn(transform.position, "+200",
                    new Color(0f, 0.96f, 1f));
                AudioManager.Instance?.PlaySFX("like_consume");
                NotificationManager.Instance?.TriggerNotification("CONTENT SHARED", "clone");
            }
            else if (!enemy.IsFrightened && !enemy.IsRespawning)
            {
                // Les ennemis en cours de respawn (invisibles) ne tuent pas
                if (flash != null && flash.IsInvincible) return;
                flash?.PlayHitFlash();
                Die();
            }
        }
    }

    public Vector2Int GridPos   => _gridPos;
    public Vector2    MoveDir   => _moveDir;
    public bool       IsDead    => _isDead;
}
