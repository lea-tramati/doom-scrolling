using UnityEngine;
using System.Collections.Generic;

// Attach to: LikeEnemy GameObject (or called from LikeEnemy)
// No Unity components required — pure BFS logic
// Dependencies: none (receives walkability grid from LikeEnemy)
public class EnemyPathfinder : MonoBehaviour
{
    bool[,] _walkable;
    int     _width;
    int     _height;

    public void Init(bool[,] walkabilityGrid)
    {
        _walkable = walkabilityGrid;
        _width    = MazeData.Width;
        _height   = MazeData.Height;
    }

    // Returns the next grid cell to move toward target.
    // anticipate: if true (Level 3+) target = playerPos + 4*playerDir
    public Vector2Int GetNextCell(Vector2Int from, Vector2Int target, bool anticipate,
                                  Vector2Int playerDir)
    {
        Vector2Int realTarget = target;
        if (anticipate)
            realTarget = new Vector2Int(
                Mathf.Clamp(target.x + playerDir.x * 4, 0, _width - 1),
                Mathf.Clamp(target.y + playerDir.y * 4, 0, _height - 1));

        return BFS(from, realTarget);
    }

    // BFS returning first step on shortest path
    Vector2Int BFS(Vector2Int from, Vector2Int to)
    {
        if (_walkable == null) return from;
        if (from == to) return from;

        var queue    = new Queue<Vector2Int>();
        var visited  = new HashSet<Vector2Int>();
        var parent   = new Dictionary<Vector2Int, Vector2Int>();

        queue.Enqueue(from);
        visited.Add(from);

        var dirs = new Vector2Int[]
        {
            Vector2Int.right, Vector2Int.left, Vector2Int.up, Vector2Int.down
        };

        while (queue.Count > 0)
        {
            var cur = queue.Dequeue();
            if (cur == to) return Backtrack(parent, from, to);

            foreach (var d in dirs)
            {
                var next = cur + d;
                if (next.x < 0 || next.x >= _width || next.y < 0 || next.y >= _height) continue;
                if (!_walkable[next.x, next.y]) continue;
                if (visited.Contains(next)) continue;
                visited.Add(next);
                parent[next] = cur;
                queue.Enqueue(next);
            }
        }

        // No path — try a random walkable neighbor
        foreach (var d in dirs)
        {
            var n = from + d;
            if (n.x >= 0 && n.x < _width && n.y >= 0 && n.y < _height && _walkable[n.x, n.y])
                return n;
        }
        return from;
    }

    Vector2Int Backtrack(Dictionary<Vector2Int, Vector2Int> parent, Vector2Int from, Vector2Int to)
    {
        var path = new List<Vector2Int>();
        var cur  = to;
        while (cur != from)
        {
            path.Add(cur);
            cur = parent[cur];
        }
        path.Reverse();
        return path.Count > 0 ? path[0] : from;
    }
}
