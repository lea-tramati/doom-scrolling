using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

// Self-bootstrapping, no scene wiring needed. Adapts the game to any screen size while
// keeping the phone-shaped design (1125x2436, see PhoneGameViewSetup / WebGLBuildScript)
// it was built around:
//   - A background camera (depth -100) clears the whole physical screen to black.
//   - The scene's gameplay camera (Camera.main) gets its viewport rect shrunk to a centered
//     box matching the phone aspect ratio — Camera.aspect then auto-derives from that rect,
//     so CameraFollow's own aspect-based maze clamping keeps working unchanged. On a wide
//     desktop monitor this reads as "a phone" with black bars either side, instead of a
//     stretched-out maze view.
//   - Every UI canvas is left in Screen Space - Overlay (untouched — that mode always
//     renders regardless of camera/layer setup, so it can't go dark) but its CanvasScaler
//     is normalized to Scale With Screen Size against that same phone reference resolution,
//     so HUD/menu text and layout stay proportionally correct on any screen instead of
//     appearing too large/small or clipping.
public class ResponsiveDisplay : MonoBehaviour
{
    const float TargetAspect = 1125f / 2436f; // width / height, portrait
    static readonly Vector2 ReferenceResolution = new Vector2(1125f, 2436f);

    static ResponsiveDisplay _instance;
    Camera _backgroundCam;
    int _lastWidth, _lastHeight;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void Bootstrap()
    {
        if (_instance != null) return;
        var go = new GameObject("ResponsiveDisplay");
        _instance = go.AddComponent<ResponsiveDisplay>();
        DontDestroyOnLoad(go);
    }

    void Awake()
    {
        BuildBackgroundCamera();
        SceneManager.sceneLoaded += (scene, mode) => ApplyToScene();
    }

    void BuildBackgroundCamera()
    {
        var go = new GameObject("LetterboxBackgroundCamera", typeof(Camera));
        go.transform.SetParent(transform, false);
        _backgroundCam = go.GetComponent<Camera>();
        _backgroundCam.clearFlags = CameraClearFlags.SolidColor;
        _backgroundCam.backgroundColor = Color.black;
        _backgroundCam.cullingMask = 0;
        _backgroundCam.depth = -100;
        _backgroundCam.rect = new Rect(0f, 0f, 1f, 1f);
    }

    void Update()
    {
        if (Screen.width == _lastWidth && Screen.height == _lastHeight) return;
        _lastWidth  = Screen.width;
        _lastHeight = Screen.height;
        ApplyToScene();
    }

    Rect ComputeLetterboxRect()
    {
        float windowAspect = (float)Screen.width / Screen.height;
        float scaleHeight   = windowAspect / TargetAspect;

        var rect = new Rect(0f, 0f, 1f, 1f);
        if (scaleHeight < 1f)
        {
            // Window is narrower/taller than the phone ratio — bars on top/bottom
            rect.height = scaleHeight;
            rect.y      = (1f - scaleHeight) * 0.5f;
        }
        else
        {
            // Window is wider than the phone ratio — bars on left/right
            float scaleWidth = 1f / scaleHeight;
            rect.width = scaleWidth;
            rect.x     = (1f - scaleWidth) * 0.5f;
        }
        return rect;
    }

    void ApplyToScene()
    {
        var rect = ComputeLetterboxRect();

        var gameplayCam = Camera.main;
        if (gameplayCam != null) gameplayCam.rect = rect;

        foreach (var canvas in FindObjectsByType<Canvas>(FindObjectsInactive.Include))
        {
            if (!canvas.isRootCanvas) continue;

            var scaler = canvas.GetComponent<CanvasScaler>();
            if (scaler == null) continue;

            scaler.uiScaleMode         = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = ReferenceResolution;
            scaler.screenMatchMode     = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight  = 1f; // match height — portrait-first design
        }
    }
}
