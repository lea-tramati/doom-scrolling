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

    SpriteRenderer _sr;
    float          _t;

    // Thème smartphone AMOLED : fond très sombre avec légère variation bleutée
    static readonly Color TintA = new Color(0.05f, 0.05f, 0.08f);  // noir bleuté
    static readonly Color TintB = new Color(0.04f, 0.04f, 0.06f);  // noir légèrement plus chaud

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
