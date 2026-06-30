using UnityEngine;
using UnityEngine.Tilemaps;
using System.Collections.Generic;

// Attach to: GameObject "MazeLoader" in GameScene
// Required: Grid child with 3 Tilemaps (WallTilemap, FloorTilemap, MalusTilemap)
// Dependencies: GameManager, MazeData array, collectible/enemy/player prefabs
public class MazeLoader : MonoBehaviour
{
    [Header("Tilemaps")]
    [SerializeField] Tilemap wallTilemap;
    [SerializeField] Tilemap floorTilemap;
    [SerializeField] Tilemap malusTilemap;

    [Header("Tiles")]
    [SerializeField] TileBase[] wallTiles;   // 11 entries: H,V,TL,TR,BL,BR,T_TOP,T_BOT,T_LEFT,T_RIGHT,CROSS
    [SerializeField] TileBase tileFloorPlain;
    [SerializeField] TileBase tileFloorFeed;
    [SerializeField] TileBase tileMalus;

    [Header("Maze Layouts (A–E)")]
    [SerializeField] MazeData[] layouts;    // assign 5 assets in Inspector

    [Header("Prefabs")]
    [SerializeField] GameObject   playerPrefab;
    [SerializeField] GameObject[] enemyPrefabs;    // 4 enemies
    [SerializeField] GameObject   dotPrefab;       // dot fallback (legacy)
    [SerializeField] GameObject[] dotPrefabs;      // app icon dots — tirage aléatoire
    [SerializeField] GameObject[] appIconPrefabs;  // 4 social icons
    [SerializeField] GameObject bonusPointsPrefab;
    [SerializeField] GameObject smartphonePrefab;
    [SerializeField] GameObject malusMarkerPrefab; // TikTok icon shown on slow-zone tiles

    MazeData _data;
    GameObject _playerInstance;
    List<GameObject> _enemyInstances  = new();
    List<GameObject> _malusMarkers    = new();
    bool[,] _walkable;

    void Start() => LoadLevel();

    public void LoadLevel()
    {
        int idx = GameManager.Instance != null ? GameManager.Instance.MazeLayoutIndex : 0;
        _data = layouts != null && layouts.Length > idx ? layouts[idx] : null;
        if (_data == null) { Debug.LogError("MazeLoader: no MazeData assigned!"); return; }

        ClearAll();
        OpenTunnelCells();
        BuildWalkabilityGrid();
        PlaceTiles();
        ApplySmartphoneTheme();
        SpawnCollectibles();
        SpawnPlayer();
        SpawnEnemies();
    }

    public void RespawnPlayer()
    {
        if (_playerInstance != null)
        {
            var pos = GridToWorld(_data.playerSpawn);
            _playerInstance.transform.position = pos;
            _playerInstance.GetComponent<PlayerController>()?.ResetState();
        }
    }

    // ── Grid helpers ──────────────────────────────────────────────

    void BuildWalkabilityGrid()
    {
        _walkable = new bool[MazeData.Width, MazeData.Height];
        for (int y = 0; y < MazeData.Height; y++)
            for (int x = 0; x < MazeData.Width; x++)
                _walkable[x, y] = _data.GetWall(x, y) == 0;
    }

    public bool[,] GetWalkabilityGrid() => _walkable;

    // ── Smartphone theme ──────────────────────────────────────────

    void ApplySmartphoneTheme()
    {
        // Fond AMOLED noir profond
        if (Camera.main != null)
            Camera.main.backgroundColor = new Color(0.02f, 0.02f, 0.03f);

        // Murs : gris-bleu sombre (comme les bords d'un UI smartphone)
        if (wallTilemap  != null) wallTilemap.color  = new Color(0.40f, 0.42f, 0.50f);
        // Sol : quasi-noir
        if (floorTilemap != null) floorTilemap.color = new Color(0.06f, 0.06f, 0.09f);
        // Zone lente : gris légèrement plus clair pour rester lisible
        if (malusTilemap != null) malusTilemap.color = new Color(0.30f, 0.30f, 0.35f);
    }

    Vector3 GridToWorld(Vector2Int cell) =>
        wallTilemap.CellToWorld(new Vector3Int(cell.x, MazeData.Height - 1 - cell.y, 0))
        + new Vector3(0.5f, 0.5f, 0f);

    Vector3Int GridToCell(Vector2Int cell) =>
        new Vector3Int(cell.x, MazeData.Height - 1 - cell.y, 0);

    // ── Tunnel openings ───────────────────────────────────────────
    // Forces left and right edge cells on TunnelRow to be floor in every layout.
    // Modifies only the in-memory ScriptableObject (not saved to disk).
    void OpenTunnelCells()
    {
        int left  = MazeData.TunnelRow * MazeData.Width;
        int right = MazeData.TunnelRow * MazeData.Width + MazeData.Width - 1;
        _data.wallGridFlat[left]  = 0;
        _data.wallGridFlat[right] = 0;
    }

    // ── Clear ────────────────────────────────────────────────────

    void ClearAll()
    {
        wallTilemap.ClearAllTiles();
        floorTilemap.ClearAllTiles();
        malusTilemap.ClearAllTiles();

        foreach (Transform t in transform)
            if (t.CompareTag("Collectible") || t.CompareTag("Enemy"))
                Destroy(t.gameObject);

        foreach (var m in _malusMarkers) if (m) Destroy(m);
        _malusMarkers.Clear();
        _enemyInstances.Clear();
    }

    // ── Tile placement ────────────────────────────────────────────

    void PlaceTiles()
    {
        for (int y = 0; y < MazeData.Height; y++)
        {
            for (int x = 0; x < MazeData.Width; x++)
            {
                var cell = new Vector3Int(x, MazeData.Height - 1 - y, 0);

                if (_data.GetWall(x, y) == 1)
                {
                    wallTilemap.SetTile(cell, PickWallTile(x, y));
                }
                else
                {
                    // 20% chance of feed-style floor
                    bool isFeed = Random.value < 0.2f;
                    floorTilemap.SetTile(cell, isFeed ? tileFloorFeed : tileFloorPlain);

                    if (_data.GetMalus(x, y) == 1)
                    {
                        malusTilemap.SetTile(cell, tileMalus);
                        var markerPrefab = malusMarkerPrefab != null
                            ? malusMarkerPrefab
                            : Resources.Load<GameObject>("MalusMarker");
                        if (markerPrefab != null)
                        {
                            var marker = Instantiate(markerPrefab,
                                GridToWorld(new Vector2Int(x, y)),
                                Quaternion.identity, transform);
                            _malusMarkers.Add(marker);
                        }
                    }
                }
            }
        }
    }

    // Auto-tiling: look at 4 neighbors to pick correct wall variant
    TileBase PickWallTile(int x, int y)
    {
        if (wallTiles == null || wallTiles.Length < 11) return null;

        bool up    = _data.GetWall(x, y - 1) == 1;
        bool down  = _data.GetWall(x, y + 1) == 1;
        bool left  = _data.GetWall(x - 1, y) == 1;
        bool right = _data.GetWall(x + 1, y) == 1;

        int idx = 10; // default CROSS
        if ( up &&  down && !left && !right) idx = 1; // V
        if (!up && !down &&  left &&  right) idx = 0; // H
        if (!up &&  down &&  left &&  right) idx = 6; // T_TOP
        if ( up && !down &&  left &&  right) idx = 7; // T_BOT
        if ( up &&  down && !left &&  right) idx = 8; // T_LEFT
        if ( up &&  down &&  left && !right) idx = 9; // T_RIGHT
        if (!up &&  down && !left &&  right) idx = 2; // CORNER_TL
        if (!up &&  down &&  left && !right) idx = 3; // CORNER_TR
        if ( up && !down && !left &&  right) idx = 4; // CORNER_BL
        if ( up && !down &&  left && !right) idx = 5; // CORNER_BR
        if (!up && !down && !left && !right) idx = 0; // isolated = H

        return wallTiles[idx];
    }

    // ── Collectible spawning ───────────────────────────────────────

    void SpawnCollectibles()
    {
        // Dots on every walkable non-special floor tile
        var specialSet = new HashSet<Vector2Int>();
        if (_data.appIconSpawns     != null) foreach (var p in _data.appIconSpawns)     specialSet.Add(p);
        if (_data.bonusPointsSpawns != null) foreach (var p in _data.bonusPointsSpawns) specialSet.Add(p);
        if (_data.smartphoneSpawns  != null) foreach (var p in _data.smartphoneSpawns)  specialSet.Add(p);
        specialSet.Add(_data.playerSpawn);

        // Utilise dotPrefabs[] (app icons) si disponible, sinon dotPrefab (legacy)
        bool hasAppDots = dotPrefabs != null && dotPrefabs.Length > 0;
        if (hasAppDots || dotPrefab != null)
        {
            for (int y = 0; y < MazeData.Height; y++)
                for (int x = 0; x < MazeData.Width; x++)
                    if (_data.GetWall(x, y) == 0 && _data.GetMalus(x, y) == 0 && !specialSet.Contains(new Vector2Int(x, y)))
                    {
                        var prefab = hasAppDots
                            ? dotPrefabs[Random.Range(0, dotPrefabs.Length)]
                            : dotPrefab;
                        SpawnAt(prefab, new Vector2Int(x, y));
                    }
        }

        if (appIconPrefabs != null && appIconPrefabs.Length > 0 && _data.appIconSpawns != null)
            for (int i = 0; i < _data.appIconSpawns.Length; i++)
                SpawnAt(appIconPrefabs[i % appIconPrefabs.Length], _data.appIconSpawns[i]);

        if (bonusPointsPrefab != null && _data.bonusPointsSpawns != null)
            foreach (var p in _data.bonusPointsSpawns) SpawnAt(bonusPointsPrefab, p);

        if (smartphonePrefab != null && _data.smartphoneSpawns != null)
            foreach (var p in _data.smartphoneSpawns) SpawnAt(smartphonePrefab, p);
    }

    void SpawnAt(GameObject prefab, Vector2Int cell)
    {
        if (prefab == null) return;
        var go = Instantiate(prefab, GridToWorld(cell), Quaternion.identity, transform);
        go.tag = "Collectible";
    }

    void SpawnPlayer()
    {
        if (playerPrefab == null) return;
        if (_playerInstance == null)
            _playerInstance = Instantiate(playerPrefab, GridToWorld(_data.playerSpawn), Quaternion.identity);
        else
            _playerInstance.transform.position = GridToWorld(_data.playerSpawn);

        _playerInstance.GetComponent<PlayerController>()?.Init(_walkable);
    }

    void SpawnEnemies()
    {
        if (enemyPrefabs == null || _data.enemySpawns == null) return;

        int level = GameManager.Instance?.Level ?? 1;
        var diff  = DifficultyConfig.Get(level);

        // Apply this level's speed range to SpeedSystem
        SpeedSystem.Instance?.ApplyDifficulty(level);

        int count = Mathf.Min(diff.EnemyCount, enemyPrefabs.Length, _data.enemySpawns.Length);

        for (int i = 0; i < count; i++)
        {
            if (enemyPrefabs[i] == null) continue;
            var go = Instantiate(enemyPrefabs[i], GridToWorld(_data.enemySpawns[i]), Quaternion.identity);
            go.tag = "Enemy";
            go.GetComponent<LikeEnemy>()?.Init(
                _walkable,
                _data.enemySpawns[i],
                diff.EnemySpeed,
                diff.ScatterDuration,
                diff.Anticipate);
            _enemyInstances.Add(go);
        }
    }
}
