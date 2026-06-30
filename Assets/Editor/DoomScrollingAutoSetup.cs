// DoomScrollingAutoSetup.cs
// Runs once automatically when Unity opens the project.
// Creates the folder structure, a default MazeData asset, and
// prints a checklist — it does NOT create scenes (that requires
// opening each scene in sequence, which the user does manually).
//
// Re-run manually: Tools > Doom Scrolling > Run Setup

#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System.IO;

[InitializeOnLoad]
public static class DoomScrollingAutoSetup
{
    const string PREFS_KEY = "DoomScrolling_AutoSetup_v2";

    static DoomScrollingAutoSetup()
    {
        if (EditorPrefs.GetBool(PREFS_KEY, false)) return;
        EditorApplication.delayCall += RunSetup;
    }

    [MenuItem("Tools/Doom Scrolling/Run Setup")]
    public static void RunSetup()
    {
        EditorPrefs.SetBool(PREFS_KEY, true);

        CreateFolders();
        CreateDefaultMazeLayouts();

        Debug.Log("[DoomScrolling] Auto-setup complete. See the checklist below.");
        PrintChecklist();

        AssetDatabase.Refresh();
    }

    [MenuItem("Tools/Doom Scrolling/Reset Setup Flag")]
    static void ResetFlag() => EditorPrefs.DeleteKey(PREFS_KEY);

    // ── Folder structure ─────────────────────────────────────────

    static void CreateFolders()
    {
        string[] folders =
        {
            "Assets/_Scenes",
            "Assets/_Scripts/Player",
            "Assets/_Scripts/Enemies",
            "Assets/_Scripts/Collectibles",
            "Assets/_Scripts/Hazards",
            "Assets/_Scripts/Managers",
            "Assets/_Scripts/UI",
            "Assets/_Scripts/Maze",
            "Assets/_Prefabs/Collectibles",
            "Assets/_Prefabs/Enemies",
            "Assets/_Prefabs/Player",
            "Assets/_Prefabs/Hazards",
            "Assets/_Prefabs/UI",
            "Assets/_Sprites/Tilesheet",
            "Assets/_Sprites/Character",
            "Assets/_Sprites/Enemies",
            "Assets/_Sprites/Collectibles",
            "Assets/_Sprites/Hazards",
            "Assets/_Sprites/UI",
            "Assets/_Tilemaps/Tiles",
            "Assets/_MazeLayouts",
            "Assets/_Audio/SFX",
            "Assets/_Audio/Music",
            "Assets/_Fonts",
            "Assets/_Materials",
            "Assets/_PostProcessing"
        };

        foreach (var path in folders)
        {
            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
                Debug.Log($"[DoomScrolling] Created folder: {path}");
            }
        }
    }

    // ── MazeData ScriptableObjects ───────────────────────────────

    static void CreateDefaultMazeLayouts()
    {
        string[] names = { "MazeLayout_A", "MazeLayout_B", "MazeLayout_C",
                           "MazeLayout_D", "MazeLayout_E" };

        foreach (var name in names)
        {
            string path = $"Assets/_MazeLayouts/{name}.asset";
            if (AssetDatabase.LoadAssetAtPath<MazeData>(path) != null) continue;

            var asset = ScriptableObject.CreateInstance<MazeData>();
            asset.PopulateDefaultLayoutA(); // all start as Layout A — customise in Inspector
            AssetDatabase.CreateAsset(asset, path);
            Debug.Log($"[DoomScrolling] Created {path}");
        }
    }

    // ── Checklist ─────────────────────────────────────────────────

    static void PrintChecklist()
    {
        Debug.Log(@"
╔══════════════════════════════════════════════════════╗
║         DOOM SCROLLING — MANUAL SETUP CHECKLIST      ║
╠══════════════════════════════════════════════════════╣
║ FONTS                                                 ║
║  1. Download 'Press Start 2P' from fonts.google.com   ║
║  2. Put .ttf in Assets/_Fonts/                        ║
║  3. Window > TextMeshPro > Font Asset Creator         ║
║                                                       ║
║ SPRITES (pixel art, 16×16, Point filter, 16 PPU)      ║
║  • DoomScrolling_Tiles.png  → Assets/_Sprites/        ║
║    Tilesheet/ (slice 16×16 in Sprite Editor)          ║
║  • Player_Spritesheet.png   → Character/              ║
║  • LikeCreature_Spritesheet.png → Enemies/            ║
║  • Collectible PNGs         → Collectibles/           ║
║  • Hazard PNGs              → Hazards/                ║
║                                                       ║
║ SCENES (File > New Scene > Basic 2D URP)              ║
║  Create these scenes in Assets/_Scenes/:              ║
║  • TitleScreen.unity                                  ║
║  • GameScene.unity                                    ║
║  • GameOverScreen.unity                               ║
║  • EndScreen.unity                                    ║
║  Add all 4 to File > Build Settings in order 0-3.    ║
║                                                       ║
║ GAMESCENE HIERARCHY                                   ║
║  [SpeedSystem] → SpeedSystem.cs                       ║
║  [GameManager] → GameManager.cs                       ║
║  [AudioManager] → AudioManager.cs (2x AudioSource)   ║
║  [NotificationManager] → NotificationManager.cs       ║
║  [HazardManager] → HazardManager.cs                  ║
║  [MazeLoader] → MazeLoader.cs                        ║
║    └─ Grid (Grid component)                           ║
║       ├─ WallTilemap  (TilemapCollider2D +            ║
║       │               CompositeCollider2D)            ║
║       ├─ FloorTilemap                                 ║
║       └─ MalusTilemap (TilemapCollider2D IsTrigger)   ║
║          └─ MalusZone.cs                              ║
║  [HUD Canvas] → HUDController.cs                     ║
║    ├─ Score label (TMP)                               ║
║    ├─ Timer label (TMP) + TimerDisplay.cs             ║
║    ├─ Level label (TMP)                               ║
║    ├─ Engagement fill (Image)                         ║
║    ├─ Overlay panel + text (TMP)                      ║
║    ├─ Tagline strip (TMP) + ScrollingTagline.cs       ║
║    └─ Notification panel → NotificationManager.cs     ║
║  [Main Camera] — Orthographic                         ║
║    Set Size to: (MazeHeight/2) + 2  ≈ 12.5            ║
║    Background: #120F1E                                ║
║                                                       ║
║ PHYSICS 2D LAYERS                                     ║
║  8=Wall  9=Player  10=Enemy  11=Collectible           ║
║  Tags: Player, Enemy, Wall, Collectible               ║
║                                                       ║
║ PREFABS                                               ║
║  Create prefabs and assign sprites/scripts per        ║
║  the spec. Assign to MazeLoader fields in Inspector.  ║
║                                                       ║
║ MAZE LAYOUTS                                          ║
║  5 assets created in Assets/_MazeLayouts/             ║
║  Edit wallGridFlat in Inspector to define each maze.  ║
║  Assign array to MazeLoader.layouts field.            ║
╚══════════════════════════════════════════════════════╝
");
    }
}
#endif
