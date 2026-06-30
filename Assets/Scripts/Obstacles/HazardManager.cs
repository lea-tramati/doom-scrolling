using UnityEngine;
using System.Collections;
using System.Collections.Generic;

// Attach to: persistent GameObject "HazardManager" in GameScene
// Dependencies: GameManager, MazeLoader (to get valid spawn positions)
public class HazardManager : MonoBehaviour
{
    [Header("Prefabs")]
    [SerializeField] GameObject popupAdPrefab;
    [SerializeField] GameObject autoPlayPrefab;
    [SerializeField] GameObject trendingTrapPrefab;

    [Header("Config")]
    [SerializeField] float minInterval    = 45f;
    [SerializeField] float maxInterval    = 90f;
    [SerializeField] int   maxSimultaneous = 3;

    List<GameObject> _active = new();
    MazeLoader       _loader;

    void Start()
    {
        _loader = FindObjectOfType<MazeLoader>();
        StartCoroutine(SpawnLoop());
    }

    IEnumerator SpawnLoop()
    {
        while (true)
        {
            yield return new WaitForSeconds(Random.Range(minInterval, maxInterval));

            if (GameManager.Instance == null || !GameManager.Instance.IsPlaying) continue;

            // Cleanup destroyed hazards
            _active.RemoveAll(g => g == null);
            if (_active.Count >= maxSimultaneous) continue;

            SpawnRandomHazard();
        }
    }

    void SpawnRandomHazard()
    {
        Vector3 pos = GetRandomWalkablePosition();
        if (pos == Vector3.zero) return;

        int roll = Random.Range(0, 3);
        GameObject prefab = roll switch
        {
            0 => popupAdPrefab,
            1 => autoPlayPrefab,
            _ => trendingTrapPrefab
        };

        if (prefab == null) return;
        var go = Instantiate(prefab, pos, Quaternion.identity, transform);
        _active.Add(go);
    }

    Vector3 GetRandomWalkablePosition()
    {
        if (_loader == null) return Vector3.zero;
        var grid = _loader.GetWalkabilityGrid();
        if (grid == null) return Vector3.zero;

        for (int attempt = 0; attempt < 30; attempt++)
        {
            int x = Random.Range(1, MazeData.Width - 1);
            int y = Random.Range(1, MazeData.Height - 1);
            if (grid[x, y])
                return new Vector3(x + 0.5f, MazeData.Height - 1 - y + 0.5f, 0f);
        }
        return Vector3.zero;
    }

    public void RegisterHazard(GameObject hazard) => _active.Add(hazard);
}
