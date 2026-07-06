using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Collections;

// Self-bootstrapping, no scene wiring needed. Keeps the whole game — gameplay camera AND
// every UI canvas — boxed to the phone aspect ratio the game was designed around (1125x2436,
// see PhoneGameViewSetup / WebGLBuildScript), regardless of the actual window/screen size,
// with black bars filling the rest. Without this, a wide desktop monitor either stretches
// the phone-shaped UI sideways or scatters it across the full window while gameplay stays
// boxed — the two need to move together.
//
// This project doesn't separate UI onto its own layer (every scene has ~everything on
// layer 0/Default, confirmed by inspecting the .unity files — moving canvases to
// Screen Space - Camera against a camera culling only a nonexistent "UI" layer is what
// caused a blank screen previously). So this script explicitly:
//   1. Reassigns every root Canvas (and all of its children) to the "UI" layer at runtime.
//   2. Excludes that layer from the gameplay camera's culling mask, so it doesn't also
//      render the UI geometry a second time from the wrong viewpoint.
//   3. Gives a dedicated UI camera exclusive rendering of that layer.
//   4. Boxes the gameplay camera, the UI camera, and a full-screen black background
//      camera together so gameplay and UI move and scale as one unit on any screen.
public class ResponsiveDisplay : MonoBehaviour
{
    const float TargetAspect = 1125f / 2436f; // width / height, portrait
    static readonly Vector2 ReferenceResolution = new Vector2(1125f, 2436f);

    static ResponsiveDisplay _instance;
    Camera _backgroundCam;
    Camera _uiCam;
    int _uiLayer;
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
        _uiLayer = LayerMask.NameToLayer("UI");
        BuildBackgroundCamera();
        BuildUICamera();
        SceneManager.sceneLoaded += (scene, mode) =>
        {
            ApplyToScene();
            // Panels built in various controllers' Start() (settings, pause menu,
            // notifications) don't exist yet when sceneLoaded fires — catch them
            // a frame later so they get moved onto the UI layer too.
            StartCoroutine(ReapplyNextFrame());
        };
    }

    IEnumerator ReapplyNextFrame()
    {
        yield return null;
        ApplyToScene();
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

    void BuildUICamera()
    {
        var go = new GameObject("ResponsiveUICamera", typeof(Camera));
        go.transform.SetParent(transform, false);
        _uiCam = go.GetComponent<Camera>();
        _uiCam.clearFlags = CameraClearFlags.Depth; // don't wipe what gameplay already drew
        _uiCam.cullingMask = 1 << _uiLayer;
        _uiCam.depth = 100; // always drawn last, on top
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

        if (_uiCam != null) _uiCam.rect = rect;

        var gameplayCam = Camera.main;
        if (gameplayCam != null)
        {
            gameplayCam.rect = rect;
            // Stop the gameplay camera from also rendering the UI layer a second time —
            // it renders "Everything" by default, and the UI canvases below are being
            // moved onto that layer.
            gameplayCam.cullingMask &= ~(1 << _uiLayer);
        }

        foreach (var canvas in FindObjectsByType<Canvas>(FindObjectsInactive.Include))
        {
            if (!canvas.isRootCanvas) continue;
            if (canvas.name == "SceneTransitionCanvas") continue; // stays full-screen overlay on purpose

            SetLayerRecursively(canvas.gameObject, _uiLayer);

            canvas.renderMode    = RenderMode.ScreenSpaceCamera;
            canvas.worldCamera   = _uiCam;
            canvas.planeDistance = 10f;

            var scaler = canvas.GetComponent<CanvasScaler>();
            if (scaler != null)
            {
                scaler.uiScaleMode         = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                scaler.referenceResolution = ReferenceResolution;
                scaler.screenMatchMode     = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
                scaler.matchWidthOrHeight  = 1f; // match height — portrait-first design, letterbox handles width
            }
        }
    }

    static void SetLayerRecursively(GameObject go, int layer)
    {
        go.layer = layer;
        foreach (Transform child in go.transform)
            SetLayerRecursively(child.gameObject, layer);
    }
}
