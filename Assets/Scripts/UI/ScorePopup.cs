using UnityEngine;
using TMPro;
using System.Collections;

public class ScorePopup : MonoBehaviour
{
    [SerializeField] float floatSpeed = 2.5f;
    [SerializeField] float lifetime   = 0.9f;

    TextMeshPro _tmp;

    public static void Spawn(Vector3 worldPos, string text, Color color)
    {
        var go = new GameObject("ScorePopup");
        go.transform.position   = worldPos + Vector3.up * 0.4f;
        go.transform.localScale = Vector3.zero; // punch animation scales it in

        var tmp          = go.AddComponent<TextMeshPro>();
        tmp.text         = text;
        tmp.fontSize     = 48f;
        tmp.color        = new Color(color.r, color.g, color.b, 0f);
        tmp.fontStyle    = FontStyles.Bold;
        tmp.alignment    = TextAlignmentOptions.Center;
        tmp.sortingOrder = 50;
        tmp.outlineWidth = 0.25f;
        tmp.outlineColor = new Color32(18, 15, 30, 255); // #120F1E

        var sp = go.AddComponent<ScorePopup>();
        sp._tmp = tmp;
    }

    void Start() => StartCoroutine(Animate());

    IEnumerator Animate()
    {
        if (_tmp == null) _tmp = GetComponent<TextMeshPro>();
        Color startColor  = _tmp.color;
        Color targetColor = new Color(startColor.r, startColor.g, startColor.b, 1f);
        Vector3 baseScale = Vector3.one * 0.04f;
        float t = 0f;

        // Phase 1: ease-out-back scale punch
        float punchDur = lifetime * 0.22f;
        while (t < punchDur)
        {
            t += Time.deltaTime;
            float progress = Mathf.Clamp01(t / punchDur);
            transform.localScale = baseScale * EaseOutBack(progress);
            _tmp.color = new Color(targetColor.r, targetColor.g, targetColor.b, progress);
            yield return null;
        }
        transform.localScale = baseScale;

        // Phase 2: float up + fade out
        float remaining = lifetime - punchDur;
        float elapsed = 0f;
        while (elapsed < remaining)
        {
            elapsed += Time.deltaTime;
            transform.position += Vector3.up * floatSpeed * Time.deltaTime;
            float alpha = Mathf.Lerp(1f, 0f, elapsed / remaining);
            _tmp.color = new Color(targetColor.r, targetColor.g, targetColor.b, alpha);
            yield return null;
        }
        Destroy(gameObject);
    }

    // Overshoots slightly before settling — gives a satisfying "pop" feel
    static float EaseOutBack(float t)
    {
        const float c1 = 1.70158f;
        const float c3 = c1 + 1f;
        return 1f + c3 * Mathf.Pow(t - 1f, 3f) + c1 * Mathf.Pow(t - 1f, 2f);
    }
}
