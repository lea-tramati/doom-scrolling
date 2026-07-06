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
    int            _invincibilityRequests; // counter, not a bool — overlapping windows (e.g.
                                            // respawn grace + a hazard block) must not let one
                                            // ending early cancel protection the other still owes

    public bool IsInvincible => _invincibilityRequests > 0;

    void Awake() => _sr = GetComponent<SpriteRenderer>();

    // Called by PlayerController right after respawn (ResetState)
    public void StartRespawnInvincibility() => StartInvincibility(invincibleDuration);

    // Called by anything that needs to guarantee the player can't be hit for a window —
    // e.g. PopupAd, whose solid blocker leaves no way to dodge on its own.
    public void StartInvincibility(float duration) => StartCoroutine(InvincibilityRoutine(duration));

    // Called by PlayerController on first hit (before death) — brief red flash
    public void PlayHitFlash() => StartCoroutine(HitFlashRoutine());

    IEnumerator InvincibilityRoutine(float duration)
    {
        _invincibilityRequests++;
        float timer = 0f;
        while (timer < duration)
        {
            _sr.enabled = !_sr.enabled;
            yield return new WaitForSeconds(flashInterval);
            timer += flashInterval;
        }
        _invincibilityRequests--;
        if (_invincibilityRequests == 0) _sr.enabled = true;
    }

    IEnumerator HitFlashRoutine()
    {
        _sr.color = hitTint;
        yield return new WaitForSeconds(0.12f);
        _sr.color = Color.white;
    }
}
