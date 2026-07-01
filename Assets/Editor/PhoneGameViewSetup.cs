// PhoneGameViewSetup.cs
// Registers a fixed "Doom Scrolling Phone" resolution in the Game view's
// aspect dropdown and selects it automatically, so Scene/Game preview always
// renders the notched-phone portrait aspect the UI was designed for
// (1125x2436, ~19.5:9) instead of whatever shape the docked Game panel
// happens to be in "Free Aspect" mode.
//
// Run manually via Tools > Doom Scrolling > Lock Game View To Phone Aspect
// if the automatic run-on-load doesn't take effect (Editor Console will log
// either way).
#if UNITY_EDITOR
using System;
using System.Reflection;
using UnityEditor;
using UnityEngine;

[InitializeOnLoad]
public static class PhoneGameViewSetup
{
    const int    PHONE_WIDTH  = 1125;
    const int    PHONE_HEIGHT = 2436;
    const string SIZE_NAME    = "Doom Scrolling Phone (1125x2436)";

    static PhoneGameViewSetup()
    {
        EditorApplication.delayCall += Apply;
    }

    [MenuItem("Tools/Doom Scrolling/Lock Game View To Phone Aspect")]
    public static void Apply()
    {
        try
        {
            EnsureSizeExists();
            SelectSize();
            Debug.Log("[PhoneGameViewSetup] Game view locked to " + SIZE_NAME);
        }
        catch (Exception e)
        {
            Debug.LogWarning("[PhoneGameViewSetup] Could not auto-set Game view size " +
                "(Unity internal API may have changed). Pick it manually from the " +
                "Game view's aspect dropdown instead: " + SIZE_NAME + "\n" + e);
        }
    }

    static object GetGameViewSizesInstance(out Type gameViewSizesType)
    {
        gameViewSizesType = Type.GetType("UnityEditor.GameViewSizes,UnityEditor");
        var singleton = typeof(ScriptableSingleton<>).MakeGenericType(gameViewSizesType);
        var instanceProp = singleton.GetProperty("instance");
        return instanceProp.GetValue(null, null);
    }

    static object GetCurrentGroup(object gvsInstance, Type gameViewSizesType, out Type groupType)
    {
        var currentGroupTypeProp = gameViewSizesType.GetProperty("currentGroupType");
        var currentGroupType = currentGroupTypeProp.GetValue(gvsInstance, null);
        var getGroup = gameViewSizesType.GetMethod("GetGroup");
        groupType = Type.GetType("UnityEditor.GameViewSizeGroup,UnityEditor");
        return getGroup.Invoke(gvsInstance, new object[] { currentGroupType });
    }

    static void EnsureSizeExists()
    {
        var gvsInstance = GetGameViewSizesInstance(out var gameViewSizesType);
        var group = GetCurrentGroup(gvsInstance, gameViewSizesType, out var groupType);

        var getDisplayTexts = groupType.GetMethod("GetDisplayTexts");
        var displayTexts = (string[])getDisplayTexts.Invoke(group, null);
        foreach (var t in displayTexts)
            if (t.Contains(SIZE_NAME)) return; // already registered

        var gameViewSizeType   = Type.GetType("UnityEditor.GameViewSize,UnityEditor");
        var gameViewSizeType_e = Type.GetType("UnityEditor.GameViewSizeType,UnityEditor");
        var fixedResolution    = Enum.Parse(gameViewSizeType_e, "FixedResolution");
        var ctor = gameViewSizeType.GetConstructor(new[]
            { gameViewSizeType_e, typeof(int), typeof(int), typeof(string) });
        var newSize = ctor.Invoke(new object[] { fixedResolution, PHONE_WIDTH, PHONE_HEIGHT, SIZE_NAME });

        var addCustomSize = groupType.GetMethod("AddCustomSize");
        addCustomSize.Invoke(group, new object[] { newSize });
    }

    static void SelectSize()
    {
        var gameViewWndType = Type.GetType("UnityEditor.GameView,UnityEditor");

        // Only touch an already-open Game view — never force one open just to set its size.
        var openWindows = Resources.FindObjectsOfTypeAll(gameViewWndType);
        if (openWindows == null || openWindows.Length == 0) return;
        var gameViewWindow = (EditorWindow)openWindows[0];

        var gvsInstance = GetGameViewSizesInstance(out var gameViewSizesType);
        var group = GetCurrentGroup(gvsInstance, gameViewSizesType, out var groupType);

        var getDisplayTexts = groupType.GetMethod("GetDisplayTexts");
        var displayTexts = (string[])getDisplayTexts.Invoke(group, null);

        int idx = -1;
        for (int i = 0; i < displayTexts.Length; i++)
            if (displayTexts[i].Contains(SIZE_NAME)) { idx = i; break; }
        if (idx < 0) return;

        var selectedSizeIndexProp = gameViewWndType.GetProperty(
            "selectedSizeIndex", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
        selectedSizeIndexProp.SetValue(gameViewWindow, idx, null);
        gameViewWindow.Repaint();
    }
}
#endif
