using UnityEngine;
using System.Collections;

// Attach to: Player prefab (alongside PlayerController)
// Provides post-respawn invincibility frames with a visual flash.
[RequireComponent(typeof(SpriteRenderer))]
public class DamageFlash : MonoBehaviour
{
    [SerializeField] float invincibleDuration = 2f;   // seconds after respawn
    [SerializeField] float flashInterval      = 0.1f;  // seconds between on/off
    [SerializeField] Color hitTint = new Color(1f, 0.3f, 0.3f, 1f);

    SpriteRenderer _sr;
    bool           _isInvincible;

    public bool IsInvincible => _isInvincible;

    void Awake() => _sr = GetComponent<SpriteRenderer>();

    // Called by PlayerController right after respawn (ResetState)
    public void StartRespawnInvincibility() => StartCoroutine(InvincibilityRoutine());

    // Called by PlayerController on first hit (before death) — brief red flash
    public void PlayHitFlash() => StartCoroutine(HitFlashRoutine());

    IEnumerator InvincibilityRoutine()
    {
        _isInvincible = true;
        float timer = 0f;
        while (timer < invincibleDuration)
        {
            _sr.enabled = !_sr.enabled;
            yield return new WaitForSeconds(flashInterval);
            timer += flashInterval;
        }
        _sr.enabled   = true;
        _isInvincible = false;
    }

    IEnumerator HitFlashRoutine()
    {
        _sr.color = hitTint;
        yield return new WaitForSeconds(0.12f);
        _sr.color = Color.white;
    }
}
