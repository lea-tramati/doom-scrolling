using UnityEngine;

// Attach to: the CyberpunkBackground GameObject (SpriteRenderer)
// Adds subtle atmospheric effects: color cycling tint + star twinkle via shader property
[RequireComponent(typeof(SpriteRenderer))]
public class CyberpunkBackground : MonoBehaviour
{
    [SerializeField] float tintCycleSpeed = 0.06f;
    [SerializeField] float brightnessMin  = 0.92f;
    [SerializeField] float brightnessMax  = 1.00f;
    [SerializeField] float flickerSpeed   = 0.8f;
    [SerializeField] float stepsPerSecond = 8f; // quantizes the flicker into visible digital steps

    // Ties the flicker to SpeedSystem so the backdrop escalates alongside the HUD's danger
    // pulse and the music's tension tiers, instead of staying oblivious to how close the
    // player is to losing.
    [Header("Danger reactivity")]
    [SerializeField] float maxDangerFlickerBoost  = 3f;    // extra flicker-speed multiplier at max danger
    [SerializeField] float maxDangerBrightnessDip = 0.15f; // extra brightness dip at max danger

    SpriteRenderer _sr;
    float          _t;

    // Thème smartphone AMOLED : fond très sombre avec légère variation bleutée
    static readonly Color TintA = new Color(0.05f, 0.05f, 0.08f);  // noir bleuté
    static readonly Color TintB = new Color(0.04f, 0.04f, 0.06f);  // noir légèrement plus chaud

    void Awake() => _sr = GetComponent<SpriteRenderer>();

    void Update()
    {
        _t += Time.deltaTime;

        float danger = SpeedSystem.Instance != null ? SpeedSystem.Instance.NormalizedSpeed : 0f;
        float effectiveFlickerSpeed  = flickerSpeed * (1f + danger * maxDangerFlickerBoost);
        float effectiveBrightnessMin = brightnessMin - danger * maxDangerBrightnessDip;

        // Quantize time into discrete ticks so the flicker jumps in visible steps
        // instead of gliding smoothly — reads as a digital signal, not an analog fade.
        float steppedT = Mathf.Floor(_t * stepsPerSecond) / stepsPerSecond;

        // Stepped pulsing brightness (simulates a glitchy digital flicker) — speeds up and
        // dips darker as danger rises, instead of pulsing at the same lazy rate regardless.
        float bright = Mathf.Lerp(effectiveBrightnessMin, brightnessMax,
            (Mathf.Sin(steppedT * effectiveFlickerSpeed) + 1f) * 0.5f);

        // Slow color temperature cycle, same stepped cadence
        Color tint = Color.Lerp(TintA, TintB,
            (Mathf.Sin(steppedT * tintCycleSpeed) + 1f) * 0.5f);

        _sr.color = tint * bright;
    }
}
