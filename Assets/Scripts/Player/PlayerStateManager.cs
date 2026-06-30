using UnityEngine;

// Attach to: Player prefab (same GameObject as PlayerController)
// Required: SpriteRenderer, Animator, optional ParticleSystem child
// Dependencies: SpeedSystem
public enum PlayerState { Normal, MoreContent, LosingControl, MalusSlowed, Dead }

[RequireComponent(typeof(SpriteRenderer))]
public class PlayerStateManager : MonoBehaviour
{
    [Header("State sprites")]
    [SerializeField] Sprite spriteNormal;
    [SerializeField] Sprite spriteMoreContent;
    [SerializeField] Sprite spriteLosingControl;

    [Header("Glitch VFX")]
    [SerializeField] ParticleSystem glitchParticles;

    PlayerState  _current;
    SpriteRenderer _sr;
    Animator       _anim;

    static readonly int AnimState = Animator.StringToHash("State");

    void Awake()
    {
        _sr   = GetComponent<SpriteRenderer>();
        _anim = GetComponent<Animator>();
    }

    void OnEnable()
    {
        if (SpeedSystem.Instance != null)
            SpeedSystem.Instance.OnSpeedChanged += OnSpeedChanged;
    }

    void OnDisable()
    {
        if (SpeedSystem.Instance != null)
            SpeedSystem.Instance.OnSpeedChanged -= OnSpeedChanged;
    }

    void OnSpeedChanged(float multiplier) => RefreshStateFromSpeed();

    public void RefreshStateFromSpeed()
    {
        if (SpeedSystem.Instance == null) return;
        float m = SpeedSystem.Instance.CurrentMultiplier;

        if (m >= 2.2f)      SetState(PlayerState.LosingControl);
        else if (m >= 1.5f) SetState(PlayerState.MoreContent);
        else                SetState(PlayerState.Normal);
    }

    public void SetState(PlayerState state)
    {
        if (_current == state && state != PlayerState.MalusSlowed) return;
        _current = state;
        ApplyVisuals();
    }

    void ApplyVisuals()
    {
        if (glitchParticles != null)
        {
            if (_current == PlayerState.LosingControl && !glitchParticles.isPlaying)
                glitchParticles.Play();
            else if (_current != PlayerState.LosingControl && glitchParticles.isPlaying)
                glitchParticles.Stop();
        }

        if (_anim != null) _anim.SetInteger(AnimState, (int)_current);

        // Sprite override when no animator handles it
        if (_sr != null)
        {
            switch (_current)
            {
                case PlayerState.Normal:
                    if (spriteNormal) _sr.sprite = spriteNormal;
                    _sr.color = Color.white;
                    break;
                case PlayerState.MoreContent:
                    if (spriteMoreContent) _sr.sprite = spriteMoreContent;
                    break;
                case PlayerState.LosingControl:
                    if (spriteLosingControl) _sr.sprite = spriteLosingControl;
                    break;
                case PlayerState.MalusSlowed:
                    _sr.color = new Color(0.47f, 0.42f, 0.96f); // Scroll Violet tint
                    break;
                case PlayerState.Dead:
                    break;
            }
        }
    }

    public PlayerState CurrentState => _current;
}
