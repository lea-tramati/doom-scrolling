using UnityEngine;
using TMPro;

// Attach to: a TextMeshProUGUI on the HUD (horizontal strip)
// Required: TextMeshProUGUI, RectTransform parent
public class ScrollingTagline : MonoBehaviour
{
    [SerializeField] TextMeshProUGUI label;
    [SerializeField] float scrollSpeed = 60f; // pixels per second
    [SerializeField] RectTransform container;

    static readonly string[] Lines =
    {
        "YOU ARE THE PRODUCT",
        "MORE CONTENT MORE YOU",
        "FEED THE SYSTEM",
        "KEEP SCROLLING",
        "ENGAGEMENT LVL ∞"
    };

    string _fullText;
    float  _offset;
    float  _textWidth;

    void Start()
    {
        // Build looping string
        _fullText = string.Join("   ·   ", Lines) + "   ·   ";
        _fullText += _fullText; // double it so we can loop
        if (label != null) label.text = _fullText;
        _offset = 0f;
    }

    void Update()
    {
        if (label == null) return;
        _offset += scrollSpeed * Time.deltaTime;

        // Get rendered text width
        _textWidth = label.preferredWidth / 2f; // half = one copy
        if (_textWidth <= 0f) return;

        if (_offset >= _textWidth) _offset -= _textWidth;

        var rt = label.rectTransform;
        rt.anchoredPosition = new Vector2(-_offset, rt.anchoredPosition.y);
    }
}
