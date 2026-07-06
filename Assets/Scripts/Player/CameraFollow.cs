using UnityEngine;
using System.Collections;

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
    Vector3 _basePosition;   // follow position before shake is applied
    Vector3 _shakeOffset;

    public static CameraFollow Instance { get; private set; }

    void Awake()
    {
        Instance = this;
        _cam = GetComponent<Camera>();
        _cam.orthographicSize = orthographicSize;
        _basePosition = transform.position;
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
        Vector3 desired = new Vector3(tx, ty, _basePosition.z);

        if (_snapNextFrame)
        {
            _basePosition  = desired;   // instant snap after tunnel warp
            _snapNextFrame = false;
        }
        else
        {
            _basePosition = Vector3.Lerp(_basePosition, desired,
                smoothSpeed * Time.deltaTime);
        }

        transform.position = _basePosition + _shakeOffset;
    }

    // Called by PlayerController when the player warps through a tunnel
    public void SnapOnce() => _snapNextFrame = true;

    // ── Game-feel juice ──────────────────────────────────────────
    const string REDUCE_SHAKE_KEY = "ReduceScreenShake"; // set from the title screen's settings panel

    public void Shake(float duration, float magnitude)
    {
        if (PlayerPrefs.GetInt(REDUCE_SHAKE_KEY, 0) == 1) magnitude *= 0.25f;
        StopCoroutine(nameof(ShakeRoutine));
        StartCoroutine(ShakeRoutine(duration, magnitude));
    }

    IEnumerator ShakeRoutine(float duration, float magnitude)
    {
        float t = 0f;
        while (t < duration)
        {
            t += Time.unscaledDeltaTime;
            float damper = 1f - Mathf.Clamp01(t / duration);
            _shakeOffset = (Vector3)(Random.insideUnitCircle * magnitude * damper);
            yield return null;
        }
        _shakeOffset = Vector3.zero;
    }

    // Brief slow-motion punch on impact moments. Uses unscaled wait so it
    // still resolves correctly while Time.timeScale is held near zero.
    public void HitStop(float duration, float slowScale = 0.02f)
    {
        StopCoroutine(nameof(HitStopRoutine));
        StartCoroutine(HitStopRoutine(duration, slowScale));
    }

    IEnumerator HitStopRoutine(float duration, float slowScale)
    {
        Time.timeScale = slowScale;
        yield return new WaitForSecondsRealtime(duration);
        // Don't stomp the pause menu's freeze if the player paused mid-hitstop —
        // this realtime wait keeps ticking regardless of Time.timeScale.
        if (!PauseMenuController.IsPaused) Time.timeScale = 1f;
    }
}
