using UnityEngine;
using System.Collections;
using TMPro;

// Attach to: PopupAd prefab
// Required: SpriteRenderer, Collider2D (NOT trigger — blocks movement)
// Dependencies: AudioManager, PlayerController
public class PopupAd : MonoBehaviour
{
    [SerializeField] float   lifetime    = 3f;
    [SerializeField] TextMeshPro countdownText;
    [SerializeField] GameObject playerBlocker; // solid collider that blocks player

    float _timer;

    void Start()
    {
        AudioManager.Instance?.PlaySFX("popup_appear");
        HUDController.Instance?.ShowOverlay($"AD — SKIP IN 3", 3f);

        // The solid blocker leaves the player no way to dodge — grant invincibility for
        // the whole block so a badly-timed spawn near an enemy can't force an unavoidable hit.
        Object.FindAnyObjectByType<PlayerController>()?.GetComponent<DamageFlash>()?.StartInvincibility(lifetime);

        StartCoroutine(Countdown());
    }

    IEnumerator Countdown()
    {
        float remaining = lifetime;
        while (remaining > 0f)
        {
            remaining -= Time.deltaTime;
            int secs = Mathf.CeilToInt(remaining);
            if (countdownText != null) countdownText.text = $"SKIP IN {secs}";
            HUDController.Instance?.UpdateAdCountdown(secs);
            yield return null;
        }
        Destroy(gameObject);
    }
}
