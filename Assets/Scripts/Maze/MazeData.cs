using UnityEngine;

// Create via: Assets > Create > DoomScrolling > MazeData
// Attach: ScriptableObject asset (MazeLayout_A.asset … MazeLayout_E.asset)
[CreateAssetMenu(menuName = "DoomScrolling/MazeData", fileName = "MazeLayout_A")]
public class MazeData : ScriptableObject
{
    [Header("Grid — 19 wide × 21 tall. 1=wall, 0=floor, 2=malus")]
    // Serialized as flat array; MazeLoader reads via GetCell / SetCell helpers
    [SerializeField] public int[] wallGridFlat  = new int[19 * 21];
    [SerializeField] public int[] malusGridFlat = new int[19 * 21];

    public const int Width  = 19;
    public const int Height = 21;

    public int GetWall(int x, int y)  => InBounds(x,y) ? wallGridFlat [y * Width + x] : 1;
    public int GetMalus(int x, int y) => InBounds(x,y) ? malusGridFlat[y * Width + x] : 0;

    bool InBounds(int x, int y) => x >= 0 && x < Width && y >= 0 && y < Height;

    [Header("Spawns")]
    public Vector2Int playerSpawn       = new Vector2Int(9, 16);
    public Vector2Int[] enemySpawns     = new Vector2Int[]
    {
        new Vector2Int(8,  9),
        new Vector2Int(9,  9),
        new Vector2Int(10, 9),
        new Vector2Int(11, 9)
    };

    [Header("Collectible positions")]
    public Vector2Int[] appIconSpawns;
    public Vector2Int[] bonusPointsSpawns;
    public Vector2Int[] smartphoneSpawns;
    public Vector2Int[] hazardSpawnZones;

    // ── Default Layout A data ─────────────────────────────────────
    // Called from a Reset() so the asset starts populated
    void Reset() => PopulateDefaultLayoutA();

    public void PopulateDefaultLayoutA()
    {
        // 1 = wall, 0 = floor (dots placed procedurally on floor tiles)
        // 19 columns, 21 rows — row 0 = top
        int[] layout = new int[]
        {
            1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1, // row 0
            1,0,0,0,0,0,0,0,0,1,0,0,0,0,0,0,0,0,1, // row 1
            1,0,1,1,0,1,1,1,0,1,0,1,1,1,0,1,1,0,1, // row 2
            1,0,1,1,0,1,1,1,0,1,0,1,1,1,0,1,1,0,1, // row 3
            1,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,1, // row 4
            1,0,1,1,0,1,0,1,1,1,1,1,0,1,0,1,1,0,1, // row 5
            1,0,0,0,0,1,0,0,0,1,0,0,0,1,0,0,0,0,1, // row 6
            1,1,1,1,0,1,1,1,0,1,0,1,1,1,0,1,1,1,1, // row 7
            1,1,1,1,0,1,0,0,0,0,0,0,0,1,0,1,1,1,1, // row 8
            1,1,1,1,0,1,0,1,1,0,1,1,0,1,0,1,1,1,1, // row 9  (ghost house area)
            0,0,0,0,0,0,0,1,0,0,0,1,0,0,0,0,0,0,0, // row 10
            1,1,1,1,0,1,0,1,1,1,1,1,0,1,0,1,1,1,1, // row 11
            1,1,1,1,0,1,0,0,0,0,0,0,0,1,0,1,1,1,1, // row 12
            1,1,1,1,0,1,0,1,1,1,1,1,0,1,0,1,1,1,1, // row 13
            1,0,0,0,0,0,0,0,0,1,0,0,0,0,0,0,0,0,1, // row 14
            1,0,1,1,0,1,1,1,0,1,0,1,1,1,0,1,1,0,1, // row 15
            1,0,0,1,0,0,0,0,0,0,0,0,0,0,0,1,0,0,1, // row 16 (player spawn col9)
            1,1,0,1,0,1,0,1,1,1,1,1,0,1,0,1,0,1,1, // row 17
            1,0,0,0,0,1,0,0,0,1,0,0,0,1,0,0,0,0,1, // row 18
            1,0,1,1,1,1,1,1,0,1,0,1,1,1,1,1,1,0,1, // row 19
            1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1, // row 20
        };
        wallGridFlat = layout;

        // Simple malus grid — a few scattered malus tiles
        malusGridFlat = new int[Width * Height];
        int[] malusPositions = { 1*Width+4, 1*Width+14, 6*Width+4, 6*Width+14,
                                  14*Width+4, 14*Width+14, 18*Width+4, 18*Width+14 };
        foreach (int p in malusPositions)
            if (p < malusGridFlat.Length) malusGridFlat[p] = 1;

        playerSpawn = new Vector2Int(9, 16);
        enemySpawns = new Vector2Int[]
        {
            new Vector2Int(8,9), new Vector2Int(9,9),
            new Vector2Int(10,9), new Vector2Int(11,9)
        };

        appIconSpawns      = new Vector2Int[]{ new Vector2Int(1,1), new Vector2Int(17,1), new Vector2Int(1,19), new Vector2Int(17,19) };
        bonusPointsSpawns  = new Vector2Int[]{ new Vector2Int(9,4), new Vector2Int(9,18) };
        smartphoneSpawns   = new Vector2Int[]{ new Vector2Int(1,6), new Vector2Int(17,6) };
        hazardSpawnZones   = new Vector2Int[]{ new Vector2Int(4,4), new Vector2Int(14,4), new Vector2Int(4,16), new Vector2Int(14,16) };
    }
}
