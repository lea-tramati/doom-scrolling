using UnityEngine;
using System.Collections;

// Attach to: all collectible prefabs (dot, app icons, bonus, smartphone)
// Required: SpriteRenderer, Collider2D (trigger)
// Dependencies: GameManager, SpeedSystem, AudioManager, NotificationManager
public enum CollectibleType
{
    NotificationDot,
    AppIcon_Snapchat,
    AppIcon_Instagram,
    AppIcon_TikTok,
    AppIcon_Twitter,
    BonusPoints,
    Smartphone
}

public class CollectibleItem : MonoBehaviour
{
    [SerializeField] CollectibleType type;
    [SerializeField] Sprite[] idleFrames;   // 2-frame idle animation
    [SerializeField] float    frameRate = 4f;

    int   _frame;
    float _frameTimer;

    SpriteRenderer _sr;

    void Awake() => _sr = GetComponent<SpriteRenderer>();

    void Update()
    {
        if (idleFrames == null || idleFrames.Length < 2) return;
        _frameTimer += Time.deltaTime;
        if (_frameTimer >= 1f / frameRate)
        {
            _frameTimer = 0f;
            _frame = (_frame + 1) % idleFrames.Length;
            _sr.sprite = idleFrames[_frame];
        }
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;
        OnCollect();
    }

    void OnCollect()
    {
        int pts = 0;
        float speedDelta = 0f;
        string sfx = "notif_ping";

        switch (type)
        {
            case CollectibleType.NotificationDot:
                pts = 10; speedDelta = 0.01f; sfx = "notif_ping";
                GameManager.Instance?.OnDotCollected();
                break;

            case CollectibleType.AppIcon_Snapchat:
            case CollectibleType.AppIcon_Instagram:
            case CollectibleType.AppIcon_TikTok:
            case CollectibleType.AppIcon_Twitter:
                pts = 50; speedDelta = 0.05f; sfx = "app_collect";
                GameManager.Instance?.OnAppIconCollected();
                int appCount = GameManager.Instance?.AppIconsThisLevel ?? 0;
                NotificationManager.Instance?.TriggerNotification(
                    $"NEW FOLLOWERS: {appCount * 10}", "follow");
                break;

            case CollectibleType.BonusPoints:
                pts = 150; speedDelta = 0f; sfx = "bonus_points";
                NotificationManager.Instance?.TriggerNotification(
                    $"{GameManager.Instance?.Score / 50 ?? 0} LIKES", "like");
                break;

            case CollectibleType.Smartphone:
                pts = 200; speedDelta = 0f; sfx = "smartphone_trigger";
                TriggerClonePhase();
                break;
        }

        GameManager.Instance?.AddScore(pts);
        if (speedDelta > 0f) SpeedSystem.Instance?.AddSpeed(speedDelta);
        AudioManager.Instance?.PlaySFX(sfx);

        // Floating score popup
        if (pts > 0)
        {
            Color popupColor = pts >= 150
                ? new Color(0f, 0.96f, 1f)           // hyper blue for big pickups
                : new Color(1f, 0.30f, 0.56f);        // notification pink for dots
            ScorePopup.Spawn(transform.position, $"+{pts}", popupColor);
        }

        Destroy(gameObject);
    }

    void TriggerClonePhase()
    {
        foreach (var enemy in Object.FindObjectsByType<LikeEnemy>(FindObjectsSortMode.None))
            enemy.EnterClone();

        AudioManager.Instance?.PlayClonePhaseMusic();
        HUDController.Instance?.ShowOverlay("YOU ARE THE CONTENT", 8f);
        NotificationManager.Instance?.TriggerNotification("CONTENT SHARED", "clone");
    }
}
