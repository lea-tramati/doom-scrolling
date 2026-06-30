using UnityEngine;

// Attach to: Main Camera
// Smooth-follows the player and stays within maze bounds.
// Orthographic size set small so tiles appear large on screen.
[RequireComponent(typeof(Camera))]
public class CameraFollow : MonoBehaviour
{
    [Header("Target — auto-found at runtime if null")]
    [SerializeField] Transform target;

    [Header("Zoom (smaller = more zoomed in = tiles look bigger)")]
    [SerializeField] float orthographicSize = 5.5f;

    [Header("Follow")]
    [SerializeField] float smoothSpeed = 10f;

    Camera _cam;
    bool   _snapNextFrame;   // set by PlayerController when tunnel teleport fires

    public static CameraFollow Instance { get; private set; }

    void Awake()
    {
        Instance = this;
        _cam = GetComponent<Camera>();
        _cam.orthographicSize = orthographicSize;
    }

    void LateUpdate()
    {
        if (target == null)
        {
            var pc = Object.FindAnyObjectByType<PlayerController>();
            if (pc != null) target = pc.transform;
            else return;
        }

        float halfH   = _cam.orthographicSize;
        float halfW   = halfH * _cam.aspect;

        // Clamp so camera never shows outside the maze
        float clampMinX = halfW;
        float clampMaxX = MazeData.Width  - halfW;
        float clampMinY = halfH;
        float clampMaxY = MazeData.Height - halfH;

        float tx = Mathf.Clamp(target.position.x, clampMinX, clampMaxX);
        float ty = Mathf.Clamp(target.position.y, clampMinY, clampMaxY);
        Vector3 desired = new Vector3(tx, ty, transform.position.z);

        if (_snapNextFrame)
        {
            transform.position = desired;   // instant snap after tunnel warp
            _snapNextFrame = false;
        }
        else
        {
            transform.position = Vector3.Lerp(transform.position, desired,
                smoothSpeed * Time.deltaTime);
        }
    }

    // Called by PlayerController when the player warps through a tunnel
    public void SnapOnce() => _snapNextFrame = true;
}
