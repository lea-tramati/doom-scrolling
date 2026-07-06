// GameOverQuitButtonSetup.cs
// The GameOverScreen only ever had a PlayAgainButton — titleBtn/quitBtn were
// optional fields that were never actually wired to anything in the scene.
// This clones the "[ BACK ]" and "[ QUIT ]" buttons from the Credits screen
// (same visual style) and stacks them below PlayAgainButton, wiring both into
// GameOverScreenController.
#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

public static class GameOverQuitButtonSetup
{
    const string CREDITS_SCENE_PATH  = "Assets/_Scenes/CreditsScreen.unity";
    const string GAMEOVER_SCENE_PATH = "Assets/_Scenes/GameOverScreen.unity";

    static GameOverQuitButtonSetup()
    {
        EditorApplication.delayCall += () => Run(silentIfDone: true);
    }

    [MenuItem("Tools/Doom Scrolling/Setup Game Over Quit Button")]
    public static void RunFromMenu() => Run(silentIfDone: false);

    static void Run(bool silentIfDone)
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) return;
        if (!File.Exists(CREDITS_SCENE_PATH) || !File.Exists(GAMEOVER_SCENE_PATH)) return;

        var goAlready = EditorSceneManager.GetSceneByPath(GAMEOVER_SCENE_PATH);
        bool goWasLoaded = goAlready.IsValid() && goAlready.isLoaded;
        var goScene = goWasLoaded ? goAlready : EditorSceneManager.OpenScene(GAMEOVER_SCENE_PATH, OpenSceneMode.Additive);

        GameOverScreenController controller = null;
        foreach (var root in goScene.GetRootGameObjects())
        {
            controller = root.GetComponentInChildren<GameOverScreenController>(true);
            if (controller != null) break;
        }

        if (controller == null)
        {
            Debug.LogWarning("[GameOverQuitButtonSetup] No GameOverScreenController found.");
            if (!goWasLoaded) EditorSceneManager.CloseScene(goScene, true);
            return;
        }

        var so = new SerializedObject(controller);
        var quitProp     = so.FindProperty("quitBtn");
        var titleProp    = so.FindProperty("titleBtn");
        var playAgainProp = so.FindProperty("playAgainBtn");

        bool needsTitle = titleProp.objectReferenceValue == null;
        bool needsQuit  = quitProp.objectReferenceValue == null;

        if (!needsTitle && !needsQuit)
        {
            if (!silentIfDone) Debug.Log("[GameOverQuitButtonSetup] Nothing to do — both buttons already wired.");
            if (!goWasLoaded) EditorSceneManager.CloseScene(goScene, true);
            return;
        }

        var playAgainBtn = playAgainProp.objectReferenceValue as Button;
        if (playAgainBtn == null)
        {
            Debug.LogWarning("[GameOverQuitButtonSetup] GameOverScreenController.playAgainBtn isn't assigned — no layout anchor to position new buttons against.");
            if (!goWasLoaded) EditorSceneManager.CloseScene(goScene, true);
            return;
        }

        var creditsAlready = EditorSceneManager.GetSceneByPath(CREDITS_SCENE_PATH);
        bool creditsWasLoaded = creditsAlready.IsValid() && creditsAlready.isLoaded;
        var creditsScene = creditsWasLoaded ? creditsAlready : EditorSceneManager.OpenScene(CREDITS_SCENE_PATH, OpenSceneMode.Additive);

        Button refBack = null, refQuit = null;
        foreach (var root in creditsScene.GetRootGameObjects())
        {
            var creditsController = root.GetComponentInChildren<CreditsScreenController>(true);
            if (creditsController == null) continue;
            var creditsSo = new SerializedObject(creditsController);
            refBack = creditsSo.FindProperty("backButton")?.objectReferenceValue as Button;
            refQuit = creditsSo.FindProperty("quitButton")?.objectReferenceValue as Button;
            break;
        }

        var anchorRt = playAgainBtn.GetComponent<RectTransform>();
        float stepY = anchorRt.sizeDelta.y + 20f;
        int slot = 1; // next free row below PlayAgainButton

        if (needsTitle)
        {
            if (refBack == null)
            {
                Debug.LogWarning("[GameOverQuitButtonSetup] Couldn't find CreditsScreen's back button to clone.");
            }
            else
            {
                var titleClone = CloneButton(refBack, anchorRt, playAgainBtn.transform.parent, "TitleButton", slot++, stepY);
                titleProp.objectReferenceValue = titleClone;
                Debug.Log("[GameOverQuitButtonSetup] Added and wired the back-to-title button.");
            }
        }

        if (needsQuit)
        {
            if (refQuit == null)
            {
                Debug.LogWarning("[GameOverQuitButtonSetup] Couldn't find CreditsScreen's quit button to clone.");
            }
            else
            {
                var quitClone = CloneButton(refQuit, anchorRt, playAgainBtn.transform.parent, "QuitButton", slot++, stepY);
                quitProp.objectReferenceValue = quitClone;
                Debug.Log("[GameOverQuitButtonSetup] Added and wired the quit button.");
            }
        }

        so.ApplyModifiedProperties();
        EditorSceneManager.MarkSceneDirty(goScene);
        EditorSceneManager.SaveScene(goScene);

        if (!creditsWasLoaded) EditorSceneManager.CloseScene(creditsScene, true);
        if (!goWasLoaded) EditorSceneManager.CloseScene(goScene, true);
    }

    static Button CloneButton(Button reference, RectTransform anchorRt, Transform parent, string name, int slot, float stepY)
    {
        var clone = Object.Instantiate(reference.gameObject, parent);
        clone.name = name;
        var rt = clone.GetComponent<RectTransform>();
        rt.anchorMin = anchorRt.anchorMin;
        rt.anchorMax = anchorRt.anchorMax;
        rt.pivot     = anchorRt.pivot;
        rt.sizeDelta = anchorRt.sizeDelta;
        rt.anchoredPosition = anchorRt.anchoredPosition - new Vector2(0f, stepY * slot);
        return clone.GetComponent<Button>();
    }
}
#endif
