using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using TMPro;

// Attach to: "NotificationManager" GameObject in GameScene
// Required: NotificationPanel prefab with a TMP label
// Dependencies: GameManager
public class NotificationManager : MonoBehaviour
{
    public static NotificationManager Instance { get; private set; }

    [Header("Panel setup")]
    [SerializeField] RectTransform panelRoot;   // the sliding panel
    [SerializeField] TextMeshProUGUI notifLabel;
    [SerializeField] float slideDistance = 300f; // pixels
    [SerializeField] float slideSpeed    = 800f;
    [SerializeField] float displayTime   = 4f;

    [Header("Auto-trigger interval")]
    [SerializeField] float minInterval = 15f;
    [SerializeField] float maxInterval = 40f;

    readonly Queue<(string text, string icon)> _queue = new();
    bool _showing;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    void Start()
    {
        if (panelRoot != null)
            panelRoot.anchoredPosition = new Vector2(slideDistance, panelRoot.anchoredPosition.y);
        StartCoroutine(AutoTriggerLoop());
    }

    public void TriggerNotification(string text, string iconKey)
    {
        _queue.Enqueue((text, iconKey));
        if (!_showing) StartCoroutine(ProcessQueue());
    }

    IEnumerator ProcessQueue()
    {
        while (_queue.Count > 0)
        {
            _showing = true;
            var (text, icon) = _queue.Dequeue();
            if (notifLabel != null) notifLabel.text = text;
            AudioManager.Instance?.PlaySFX("notif_slide");

            // Slide in
            yield return StartCoroutine(SlideTo(0f));
            yield return new WaitForSeconds(displayTime);
            // Slide out
            yield return StartCoroutine(SlideTo(slideDistance));

            yield return new WaitForSeconds(0.1f);
        }
        _showing = false;
    }

    IEnumerator SlideTo(float targetX)
    {
        if (panelRoot == null) yield break;
        float startX = panelRoot.anchoredPosition.x;
        float elapsed = 0f;
        float duration = Mathf.Abs(targetX - startX) / slideSpeed;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float x = Mathf.Lerp(startX, targetX, elapsed / duration);
            panelRoot.anchoredPosition = new Vector2(x, panelRoot.anchoredPosition.y);
            yield return null;
        }
        panelRoot.anchoredPosition = new Vector2(targetX, panelRoot.anchoredPosition.y);
    }

    IEnumerator AutoTriggerLoop()
    {
        while (true)
        {
            yield return new WaitForSeconds(Random.Range(minInterval, maxInterval));
            if (GameManager.Instance == null || !GameManager.Instance.IsPlaying) continue;

            int lvl   = GameManager.Instance.Level;
            int score = GameManager.Instance.Score;
            int min   = (int)(GameManager.Instance.SessionTimer / 60);

            // Pick a random auto notification
            int pick = Random.Range(0, 5);
            string text = pick switch
            {
                0 => $"{score / 50} LIKES",
                1 => $"{Random.Range(lvl * 100, lvl * 500)} PROFILE VIEWS",
                2 => $"ENGAGEMENT {Random.Range(60, 99)}%",
                3 => $"{min} MIN SCROLLED",
                _ => $"NEW FOLLOWERS: {GameManager.Instance.AppIconsThisLevel * 10}"
            };

            TriggerNotification(text, "auto");
        }
    }
}
