using UnityEngine;
using TMPro;

// Attach to: Timer label TextMeshProUGUI (child of HUD Canvas)
// Required: TextMeshProUGUI on same object
// Dependencies: GameManager
[RequireComponent(typeof(TextMeshProUGUI))]
public class TimerDisplay : MonoBehaviour
{
    TextMeshProUGUI _label;

    void Awake() => _label = GetComponent<TextMeshProUGUI>();

    void Update()
    {
        if (GameManager.Instance == null || _label == null) return;
        float t   = GameManager.Instance.SessionTimer;
        int min   = (int)(t / 60);
        int sec   = (int)(t % 60);
        _label.text = $"{min:D2}:{sec:D2}";
    }
}
