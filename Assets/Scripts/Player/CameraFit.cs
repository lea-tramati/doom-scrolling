using UnityEngine;

// Attach to: Main Camera
// Adjusts orthographic size at startup so the 19x21 tile maze fills the screen tightly.
[RequireComponent(typeof(Camera))]
public class CameraFit : MonoBehaviour
{
    [Tooltip("Maze dimensions in world units (tiles)")]
    [SerializeField] float mazeWidth  = 19f;
    [SerializeField] float mazeHeight = 21f;

    [Tooltip("Padding added around the maze in world units")]
    [SerializeField] float padding = 0.5f;

    [Tooltip("Extra vertical room for top/bottom HUD bars (fraction of screen height)")]
    [SerializeField] float hudVerticalFraction = 0.13f;

    Camera _cam;

    void Awake()
    {
        _cam = GetComponent<Camera>();
        Fit();
    }

    void Fit()
    {
        float aspect = (float)Screen.width / Screen.height;

        // How much of the screen height is available for the maze (after HUD strips)
        float availableFraction = 1f - hudVerticalFraction * 2f;

        // Orthographic size to show full maze height in available area
        float sizeByHeight = (mazeHeight / 2f + padding) / availableFraction;

        // Orthographic size to show full maze width given the aspect ratio
        float sizeByWidth  = (mazeWidth  / 2f + padding) / (aspect * availableFraction);

        // Use the larger: ensures full maze is always visible
        _cam.orthographicSize = Mathf.Max(sizeByHeight, sizeByWidth);
    }
}
