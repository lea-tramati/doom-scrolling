using UnityEngine;
using System;

// Singleton — attach to a persistent GameObject named "SpeedSystem"
public class SpeedSystem : MonoBehaviour
{
    public static SpeedSystem Instance { get; private set; }

    [Header("Default config (overridden by DifficultyConfig at level load)")]
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

    // Called on new game — resets to level-1 defaults
    public void ResetSpeed()
    {
        var cfg = DifficultyConfig.Get(1);
        startMultiplier   = cfg.SpeedBase;
        maxMultiplier     = cfg.SpeedMax;
        CurrentMultiplier = startMultiplier;
        OnSpeedChanged?.Invoke(CurrentMultiplier);
    }

    // Called by MazeLoader when a level loads — sets base & ceiling for this level's tier
    public void ApplyDifficulty(int level)
    {
        var cfg = DifficultyConfig.Get(level);
        startMultiplier   = cfg.SpeedBase;
        maxMultiplier     = cfg.SpeedMax;
        CurrentMultiplier = startMultiplier;
        OnSpeedChanged?.Invoke(CurrentMultiplier);
    }

    // 0–1 for HUD engagement bar
    public float NormalizedSpeed =>
        maxMultiplier > startMultiplier
            ? (CurrentMultiplier - startMultiplier) / (maxMultiplier - startMultiplier)
            : 0f;
}
