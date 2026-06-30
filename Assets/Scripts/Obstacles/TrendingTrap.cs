using UnityEngine;
using System.Collections;

// Attach to: TrendingTrap prefab
// Required: Collider2D (trigger)
// Dependencies: LikeEnemy (all), AudioManager
public class TrendingTrap : MonoBehaviour
{
    [SerializeField] float burstAmount  = 1f;  // tiles/sec added
    [SerializeField] float burstDuration = 5f;
    [SerializeField] float lifetime      = 25f;

    bool _triggered;

    void Start() => StartCoroutine(SelfDestruct());

    IEnumerator SelfDestruct()
    {
        yield return new WaitForSeconds(lifetime);
        Destroy(gameObject);
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (_triggered || !other.CompareTag("Player")) return;
        _triggered = true;

        foreach (var enemy in FindObjectsOfType<LikeEnemy>())
            enemy.ApplyTrendingBurst(burstAmount, burstDuration);

        AudioManager.Instance?.PlaySFX("trending");
        HUDController.Instance?.ShowOverlay("TRENDING NOW", burstDuration);
        Destroy(gameObject);
    }
}
