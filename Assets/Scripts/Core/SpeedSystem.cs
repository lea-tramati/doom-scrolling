using UnityEngine;
using System;

// Singleton — attach to a persistent GameObject named "SpeedSystem"
// Dependencies: none (others depend on this)
public class SpeedSystem : MonoBehaviour
{
    public static SpeedSystem Instance { get; private set; }

    [Header("Config")]
    [SerializeField] float startMultiplier = 1f;
    [SerializeField] float maxMultiplier   = 3f;

    public float CurrentMultiplier { get; private set; }

    public event Action<float> OnSpeedChanged;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
        CurrentMultiplier = startMultiplier;
    }

    public void AddSpeed(float delta)
    {
        CurrentMultiplier = Mathf.Clamp(CurrentMultiplier + delta, startMultiplier, maxMultiplier);
        OnSpeedChanged?.Invoke(CurrentMultiplier);
    }

    // Called when starting a fresh session (new game, not level transition)
    public void ResetSpeed()
    {
        CurrentMultiplier = startMultiplier;
        OnSpeedChanged?.Invoke(CurrentMultiplier);
    }

    // Percentage 0–1 for HUD engagement bar
    public float NormalizedSpeed => (CurrentMultiplier - startMultiplier) / (maxMultiplier - startMultiplier);
}
