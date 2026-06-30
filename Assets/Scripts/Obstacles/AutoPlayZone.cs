using UnityEngine;
using System.Collections;

// Attach to: AutoPlay prefab
// Required: Collider2D (trigger)
// Dependencies: PlayerController, AudioManager
public class AutoPlayZone : MonoBehaviour
{
    [SerializeField] float lifetime = 20f;

    bool _triggered;

    void Start()
    {
        StartCoroutine(SelfDestruct());
    }

    IEnumerator SelfDestruct()
    {
        yield return new WaitForSeconds(lifetime);
        Destroy(gameObject);
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (_triggered || !other.CompareTag("Player")) return;
        _triggered = true;

        other.GetComponent<PlayerController>()?.ApplyAutoPlay();
        AudioManager.Instance?.PlaySFX("autoplay");
        Destroy(gameObject);
    }
}
