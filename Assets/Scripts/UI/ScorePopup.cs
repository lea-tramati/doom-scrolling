using UnityEngine;
using TMPro;
using System.Collections;

// Spawned programmatically when player collects items.
// Uses TextMeshPro in world space — no Canvas needed.
public class ScorePopup : MonoBehaviour
{
    [SerializeField] float floatSpeed = 2.5f;
    [SerializeField] float lifetime   = 0.9f;

    TextMeshPro _tmp;

    // ── Static factory ────────────────────────────────────────────
    public static void Spawn(Vector3 worldPos, string text, Color color)
    {
        var go = new GameObject("ScorePopup");
        go.transform.position   = worldPos + Vector3.up * 0.4f;
        go.transform.localScale = Vector3.one * 0.04f; // TMP world-space default is huge

        var tmp        = go.AddComponent<TextMeshPro>();
        tmp.text       = text;
        tmp.fontSize   = 48f;
        tmp.color      = color;
        tmp.fontStyle  = FontStyles.Bold;
        tmp.alignment  = TextAlignmentOptions.Center;
        tmp.sortingOrder = 50;

        var sp = go.AddComponent<ScorePopup>();
        sp._tmp = tmp;
    }

    void Start() => StartCoroutine(Animate());

    IEnumerator Animate()
    {
        if (_tmp == null) _tmp = GetComponent<TextMeshPro>();
        Color startColor = _tmp.color;
        float t = 0f;

        while (t < lifetime)
        {
            t += Time.deltaTime;
            transform.position += Vector3.up * floatSpeed * Time.deltaTime;
            _tmp.color = new Color(startColor.r, startColor.g, startColor.b,
                                   Mathf.Lerp(1f, 0f, t / lifetime));
            yield return null;
        }
        Destroy(gameObject);
    }
}
