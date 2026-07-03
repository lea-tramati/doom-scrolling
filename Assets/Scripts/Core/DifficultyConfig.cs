using UnityEngine;

// Pure static data — no MonoBehaviour needed.
// Read anywhere via DifficultyConfig.Get(level).
public static class DifficultyConfig
{
    public struct Tier
    {
        public string Name;             // "EASY" / "MEDIUM" / "HARD"
        public int    EnemyCount;       // enemies spawned this level
        public float  EnemySpeed;       // multiplier on LikeEnemy.baseSpeed
        public float  ScatterDuration;  // seconds enemies stay in scatter mode
        public bool   Anticipate;       // enemies try to cut off player path
        public float  SpeedBase;        // SpeedSystem starting multiplier
        public float  SpeedMax;         // SpeedSystem ceiling multiplier
        public float  HazardMinInterval; // seconds between hazard spawns, lower bound
        public float  HazardMaxInterval; // seconds between hazard spawns, upper bound
        public int    HazardMaxCount;    // max hazards active simultaneously
    }

    // Index 0 = Level 1, index 7 = Level 8
    static readonly Tier[] _tiers = new Tier[8]
    {
        // ── EASY ─────────────────────────────────────────────────
        // Level 1: 1 enemy, very slow, long scatter, no anticipation
        new Tier { Name="EASY",   EnemyCount=1, EnemySpeed=0.55f, ScatterDuration=10f, Anticipate=false, SpeedBase=1.0f, SpeedMax=1.8f, HazardMinInterval=70f, HazardMaxInterval=110f, HazardMaxCount=1 },
        // Level 2: 2 enemies, still slow
        new Tier { Name="EASY",   EnemyCount=2, EnemySpeed=0.70f, ScatterDuration=8f,  Anticipate=false, SpeedBase=1.0f, SpeedMax=2.0f, HazardMinInterval=60f, HazardMaxInterval=100f, HazardMaxCount=1 },
        // Level 3: 2 enemies, getting warmer
        new Tier { Name="EASY",   EnemyCount=2, EnemySpeed=0.85f, ScatterDuration=6f,  Anticipate=false, SpeedBase=1.1f, SpeedMax=2.2f, HazardMinInterval=55f, HazardMaxInterval=90f,  HazardMaxCount=2 },

        // ── MEDIUM ───────────────────────────────────────────────
        // Level 4: 3 enemies, anticipation ON, shorter scatter
        new Tier { Name="MEDIUM", EnemyCount=3, EnemySpeed=1.00f, ScatterDuration=5f,  Anticipate=true,  SpeedBase=1.2f, SpeedMax=2.5f, HazardMinInterval=50f, HazardMaxInterval=80f,  HazardMaxCount=2 },
        // Level 5: 3 enemies, noticeably faster
        new Tier { Name="MEDIUM", EnemyCount=3, EnemySpeed=1.15f, ScatterDuration=4f,  Anticipate=true,  SpeedBase=1.3f, SpeedMax=2.6f, HazardMinInterval=45f, HazardMaxInterval=75f,  HazardMaxCount=2 },

        // ── HARD ─────────────────────────────────────────────────
        // Level 6: all 4 enemies, aggressive
        new Tier { Name="HARD",   EnemyCount=4, EnemySpeed=1.30f, ScatterDuration=3f,  Anticipate=true,  SpeedBase=1.5f, SpeedMax=2.8f, HazardMinInterval=40f, HazardMaxInterval=65f,  HazardMaxCount=3 },
        // Level 7: 4 enemies, very fast
        new Tier { Name="HARD",   EnemyCount=4, EnemySpeed=1.45f, ScatterDuration=2f,  Anticipate=true,  SpeedBase=1.6f, SpeedMax=3.0f, HazardMinInterval=35f, HazardMaxInterval=55f,  HazardMaxCount=3 },
        // Level 8: max difficulty
        new Tier { Name="HARD",   EnemyCount=4, EnemySpeed=1.65f, ScatterDuration=1.5f,Anticipate=true,  SpeedBase=1.8f, SpeedMax=3.0f, HazardMinInterval=30f, HazardMaxInterval=50f,  HazardMaxCount=3 },
    };

    public static Tier Get(int level)
    {
        int idx = Mathf.Clamp(level - 1, 0, _tiers.Length - 1);
        return _tiers[idx];
    }
}
