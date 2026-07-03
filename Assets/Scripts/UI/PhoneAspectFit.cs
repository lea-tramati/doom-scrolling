using UnityEngine;

// Phone-shaped framing is now handled by the PhoneBezel UI overlay
// (Assets/_Sprites/UI/PhoneBezel.png, added to each scene's Canvas) combined
// with an AspectRatioFitter, instead of pillarboxing the camera itself.
// Running both systems at once made the bezel render enormous: this script
// used to squeeze the camera into a narrow phone-shaped viewport, so the
// bezel (anchored to the full screen) ended up scaled against that tiny
// viewport instead of the real window.
//
// Kept as a disabled no-op (rather than deleted) in case camera pillarboxing
// is wanted again later — mirrors how SmartphoneFrame.cs was retired.
public class PhoneAspectFit : MonoBehaviour
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Bootstrap()
    {
        // Intentionally does nothing. The game window/camera render at
        // whatever resolution Player Settings or the Editor Game view use
        // (1920x1080 by default) — no runtime letterboxing.
    }
}
