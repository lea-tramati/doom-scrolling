using UnityEngine;

// Auto-bootstraps in every scene (no scene wiring needed).
// Pillarboxes/letterboxes the main camera — and any Screen Space Overlay
// canvas found in the scene — to a fixed portrait "phone" aspect ratio, so
// the game keeps its phone-shaped frame with black bars on the sides when
// played fullscreen on a wide desktop monitor (e.g. 1920x1080).
public class PhoneAspectFit : MonoBehaviour
{
    // Matches ProjectSettings' default mobile resolution (1125x2436, ~19.5:9)
    public const float TargetAspect = 1125f / 2436f;

    Camera _cam;
    Camera _backdrop;
    int _lastW, _lastH;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Bootstrap()
    {
        var cam = Camera.main;
        if (cam == null || cam.GetComponent<PhoneAspectFit>() != null) return;
        cam.gameObject.AddComponent<PhoneAspectFit>();
    }

    void Awake()
    {
        _cam = GetComponent<Camera>();
        CreateBackdropCamera();
        HookOverlayCanvases();
        Apply();
    }

    void CreateBackdropCamera()
    {
        var go = new GameObject("LetterboxBackdrop");
        go.transform.SetParent(transform.parent);
        _backdrop = go.AddComponent<Camera>();
        _backdrop.clearFlags      = CameraClearFlags.SolidColor;
        _backdrop.backgroundColor = Color.black;
        _backdrop.cullingMask     = 0;              // renders nothing — pure clear
        _backdrop.orthographic    = true;
        _backdrop.depth           = _cam.depth - 10f; // draws first, behind everything
        _backdrop.rect            = new Rect(0, 0, 1, 1);
        _backdrop.allowHDR        = false;
        _backdrop.allowMSAA       = false;
    }

    void HookOverlayCanvases()
    {
        foreach (var canvas in Object.FindObjectsByType<Canvas>(FindObjectsSortMode.None))
        {
            if (canvas.renderMode != RenderMode.ScreenSpaceOverlay) continue;
            canvas.renderMode    = RenderMode.ScreenSpaceCamera;
            canvas.worldCamera   = _cam;
            canvas.planeDistance = 5f; // safely inside the camera's clip range
        }
    }

    void Update()
    {
        if (Screen.width != _lastW || Screen.height != _lastH) Apply();
    }

    void Apply()
    {
        _lastW = Screen.width;
        _lastH = Screen.height;
        if (_lastH <= 0) return;

        float windowAspect = (float)_lastW / _lastH;
        float scaleHeight  = windowAspect / TargetAspect;

        Rect r = scaleHeight < 1f
            ? new Rect(0f, (1f - scaleHeight) * 0.5f, 1f, scaleHeight)
            : new Rect((1f - 1f / scaleHeight) * 0.5f, 0f, 1f / scaleHeight, 1f);

        _cam.rect = r;
    }
}
