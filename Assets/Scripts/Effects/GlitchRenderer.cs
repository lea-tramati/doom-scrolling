using UnityEngine;

// Attach to: Player or Enemy GameObjects to show chromatic aberration + glitch offsets.
// Requires: SpriteRenderer on same object
// Driven by PlayerStateManager (player) or LikeEnemy tier3 flag (enemy).
[RequireComponent(typeof(SpriteRenderer))]
public class GlitchRenderer : MonoBehaviour
{
    [Header("Glitch Config")]
    [SerializeField] float glitchIntensity = 2f;   // pixel offset magnitude
    [SerializeField] float glitchFrameRate = 8f;
    [SerializeField] bool  active;

    // Extra SpriteRenderers for chromatic aberration red/cyan layers
    [SerializeField] SpriteRenderer redLayer;
    [SerializeField] SpriteRenderer cyanLayer;

    SpriteRenderer _main;
    float          _frameTimer;
    int            _glitchFrame;

    void Awake()
    {
        _main = GetComponent<SpriteRenderer>();

        if (redLayer  != null) redLayer.color  = new Color(1f, 0.30f, 0.56f, 0.5f); // Pink at 50%
        if (cyanLayer != null) cyanLayer.color = new Color(0f, 0.96f, 1f,   0.5f); // Blue at 50%
    }

    void Update()
    {
        if (!active)
        {
            SetLayerOffset(Vector2.zero);
            return;
        }

        _frameTimer += Time.deltaTime;
        if (_frameTimer >= 1f / glitchFrameRate)
        {
            _frameTimer = 0f;
            _glitchFrame = (_glitchFrame + 1) % 3;
            ApplyGlitchFrame(_glitchFrame);
        }
    }

    void ApplyGlitchFrame(int frame)
    {
        // Frame 0: neutral | Frame 1: shift right | Frame 2: shift left
        float offset = frame == 0 ? 0f : frame == 1 ? glitchIntensity / 16f : -glitchIntensity / 16f;
        SetLayerOffset(new Vector2(offset, 0f));
    }

    void SetLayerOffset(Vector2 off)
    {
        if (redLayer  != null) redLayer.transform.localPosition  = new Vector3( off.x, 0f, 0f);
        if (cyanLayer != null) cyanLayer.transform.localPosition = new Vector3(-off.x, 0f, 0f);
    }

    public void SetActive(bool on)
    {
        active = on;
        if (!on) SetLayerOffset(Vector2.zero);
    }
}
