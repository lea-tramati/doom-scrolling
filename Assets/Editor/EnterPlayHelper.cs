#if UNITY_EDITOR
using UnityEditor;

// TEMPORARY — invoked via `-executeMethod EnterPlayHelper.Run` to auto-enter
// Play mode once the Editor finishes loading, so the game is immediately
// playable without having to click the Play button manually.
// Safe to delete once Unity is closed.
public static class EnterPlayHelper
{
    public static void Run()
    {
        EditorApplication.isPlaying = true;
    }
}
#endif
