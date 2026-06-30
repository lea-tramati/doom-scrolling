using UnityEngine;

// Attach to: MalusMarker prefab — gives a subtle breathing pulse to hazard icons
public class PulseMarker : MonoBehaviour
{
    [SerializeField] float speed    = 1.8f;
    [SerializeField] float minScale = 0.88f;
    [SerializeField] float maxScale = 1.00f;

    Vector3 _base;

    void Start() => _base = transform.localScale;

    void Update()
    {
        float t = (Mathf.Sin(Time.time * speed) + 1f) * 0.5f;
        transform.localScale = _base * Mathf.Lerp(minScale, maxScale, t);
    }
}
