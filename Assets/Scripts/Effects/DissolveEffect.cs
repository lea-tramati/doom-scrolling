using UnityEngine;
using System.Collections;

// Attach to (or auto-added onto): Player and Enemy GameObjects.
// Requires: SpriteRenderer on the same object.
// Swaps in a dissolve material for the death/eaten moment so the sprite's
// pixels break apart and vanish instead of just fading or blinking out,
// then restores the original material.
[RequireComponent(typeof(SpriteRenderer))]
public class DissolveEffect : MonoBehaviour
{
    static Shader _shader;
    static readonly int DissolveAmountID = Shader.PropertyToID("_DissolveAmount");
    static readonly int ColorID          = Shader.PropertyToID("_Color");
    static readonly int EdgeColorID      = Shader.PropertyToID("_EdgeColor");

    SpriteRenderer _sr;
    Material _dissolveMat;
    Material _originalMat;

    void Awake()
    {
        _sr = GetComponent<SpriteRenderer>();
        _originalMat = _sr.sharedMaterial;
        if (_shader == null) _shader = Shader.Find("Custom/SpriteDissolve");
    }

    // Animates dissolve amount 0 -> 1 over `duration` seconds. Leaves the
    // sprite fully dissolved (invisible) at the end — call ResetVisual()
    // once the object is repositioned/reused (respawn) or before disabling.
    public IEnumerator Dissolve(float duration, Color edgeColor)
    {
        if (_shader == null || _sr == null) yield break;

        if (_dissolveMat == null)
            _dissolveMat = new Material(_shader);

        _dissolveMat.SetColor(ColorID, Color.white);
        _dissolveMat.SetColor(EdgeColorID, edgeColor);
        _dissolveMat.SetFloat(DissolveAmountID, 0f);
        _sr.material = _dissolveMat;

        float t = 0f;
        while (t < duration)
        {
            t += Time.deltaTime;
            _dissolveMat.SetFloat(DissolveAmountID, Mathf.Clamp01(t / duration));
            yield return null;
        }
        _dissolveMat.SetFloat(DissolveAmountID, 1f);
    }

    // Restores the original material and full visibility — call after a
    // respawn/reset so normal color tinting (frightened, malus, etc.) works
    // again through the default sprite shader.
    public void ResetVisual()
    {
        if (_sr == null) return;
        if (_originalMat != null) _sr.material = _originalMat;
        _sr.color = Color.white;
    }
}
