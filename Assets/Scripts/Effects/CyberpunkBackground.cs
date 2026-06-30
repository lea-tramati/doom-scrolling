using UnityEngine;

// Attach to: the CyberpunkBackground GameObject (SpriteRenderer)
// Adds subtle atmospheric effects: color cycling tint + star twinkle via shader property
[RequireComponent(typeof(SpriteRenderer))]
public class CyberpunkBackground : MonoBehaviour
{
    [SerializeField] float tintCycleSpeed = 0.08f;
    [SerializeField] float brightnessMin  = 0.92f;
    [SerializeField] float brightnessMax  = 1.02f;
    [SerializeField] float flickerSpeed   = 1.4f;

    SpriteRenderer _sr;
    float          _t;

    static readonly Color TintA = new Color(0.85f, 0.80f, 1.00f);  // violet-cool
    static readonly Color TintB = new Color(1.00f, 0.85f, 0.92f);  // slight pink warm

    void Awake() => _sr = GetComponent<SpriteRenderer>();

    void Update()
    {
        _t += Time.deltaTime;

        // Gentle pulsing brightness (simulates neon sign flicker ambience)
        float bright = Mathf.Lerp(brightnessMin, brightnessMax,
            (Mathf.Sin(_t * flickerSpeed) + 1f) * 0.5f);

        // Slow color temperature cycle
        Color tint = Color.Lerp(TintA, TintB,
            (Mathf.Sin(_t * tintCycleSpeed) + 1f) * 0.5f);

        _sr.color = tint * bright;
    }
}
