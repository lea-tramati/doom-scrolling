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

    SpriteRenderer _sr;
    float          _t;

    // Thème smartphone AMOLED : fond très sombre avec légère variation bleutée
    static readonly Color TintA = new Color(0.05f, 0.05f, 0.08f);  // noir bleuté
    static readonly Color TintB = new Color(0.04f, 0.04f, 0.06f);  // noir légèrement plus chaud

    void Awake() => _sr = GetComponent<SpriteRenderer>();

    void Update()
    {
        _t += Time.deltaTime;

        // Quantize time into discrete ticks so the flicker jumps in visible steps
        // instead of gliding smoothly — reads as a digital signal, not an analog fade.
        float steppedT = Mathf.Floor(_t * stepsPerSecond) / stepsPerSecond;

        // Stepped pulsing brightness (simulates a glitchy digital flicker)
        float bright = Mathf.Lerp(brightnessMin, brightnessMax,
            (Mathf.Sin(steppedT * flickerSpeed) + 1f) * 0.5f);

        // Slow color temperature cycle, same stepped cadence
        Color tint = Color.Lerp(TintA, TintB,
            (Mathf.Sin(steppedT * tintCycleSpeed) + 1f) * 0.5f);

        _sr.color = tint * bright;
    }
}
