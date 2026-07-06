using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

// Attach to: a full-screen RectTransform behind the title content (first
// sibling in the Canvas so it renders behind everything else).
// Tiles a grid of dimmed app icons and scrolls them slowly upward on a loop —
// an "endless feed" ambiance for the title screen, replacing the old static
// bouncing "99+" badge.
public class ScrollingFeedBackground : MonoBehaviour
{
    [SerializeField] Sprite[] icons;
    [SerializeField] float scrollSpeed = 18f;
    [SerializeField] float cellSize    = 90f;
    [SerializeField] float iconAlpha   = 0.16f;

    RectTransform _rt;
    readonly List<RectTransform> _tiles = new();
    float _fullHeight;

    void Awake()
    {
        _rt = GetComponent<RectTransform>();
        BuildGrid();
    }

    void BuildGrid()
    {
        if (icons == null || icons.Length == 0 || _rt == null) return;

        float w = _rt.rect.width, h = _rt.rect.height;
        if (w <= 0f || h <= 0f) return;

        int cols = Mathf.CeilToInt(w / cellSize) + 1;
        int rows = Mathf.CeilToInt(h / cellSize) + 3; // extra rows so the loop seam stays offscreen
        _fullHeight = rows * cellSize;

        var rng = new System.Random(7);
        for (int r = 0; r < rows; r++)
        {
            for (int c = 0; c < cols; c++)
            {
                var go = new GameObject("Icon", typeof(RectTransform), typeof(Image));
                go.transform.SetParent(transform, false);
                var rt = go.GetComponent<RectTransform>();
                rt.anchorMin = rt.anchorMax = new Vector2(0f, 1f);
                rt.pivot     = new Vector2(0.5f, 0.5f);
                rt.sizeDelta = new Vector2(cellSize * 0.55f, cellSize * 0.55f);
                rt.anchoredPosition = new Vector2(
                    c * cellSize + cellSize * 0.5f,
                    -(r * cellSize + cellSize * 0.5f));

                var img = go.GetComponent<Image>();
                img.sprite          = icons[rng.Next(icons.Length)];
                img.color           = new Color(1f, 1f, 1f, iconAlpha);
                img.preserveAspect  = true;
                img.raycastTarget   = false;
                _tiles.Add(rt);
            }
        }
    }

    void Update()
    {
        if (_tiles.Count == 0) return;
        float dy = scrollSpeed * Time.deltaTime;
        for (int i = 0; i < _tiles.Count; i++)
        {
            var pos = _tiles[i].anchoredPosition;
            pos.y += dy;
            if (pos.y > 0f) pos.y -= _fullHeight;
            _tiles[i].anchoredPosition = pos;
        }
    }
}
