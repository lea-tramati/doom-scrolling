// Generates placeholder 16×16 pixel art sprites for all game assets.
// Run via: Tools > Doom Scrolling > Generate Placeholder Sprites
// These are functional stand-ins — replace with real pixel art later.

#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System.IO;

public static class DoomScrollingSpriteGenerator
{
    // Palette
    static Color32 Pink   = new(0xFF, 0x4D, 0x90, 0xFF);
    static Color32 Purple = new(0x81, 0x15, 0xFF, 0xFF);
    static Color32 Violet = new(0x78, 0x6C, 0xF6, 0xFF);
    static Color32 Black  = new(0x12, 0x0F, 0x1E, 0xFF);
    static Color32 White  = new(0xF7, 0xD8, 0xFF, 0xFF);
    static Color32 Blue   = new(0x00, 0xF5, 0xFF, 0xFF);
    static Color32 Red    = new(0xFF, 0x3A, 0x5E, 0xFF);
    static Color32 Clear  = new(0,0,0,0);

    [MenuItem("Tools/Doom Scrolling/Generate Placeholder Sprites")]
    public static void GenerateAll()
    {
        EnsureDir("Assets/_Sprites/Tilesheet");
        EnsureDir("Assets/_Sprites/Enemies");
        EnsureDir("Assets/_Sprites/Character");
        EnsureDir("Assets/_Sprites/Collectibles");
        EnsureDir("Assets/_Sprites/Hazards");

        GenerateTilesheet();
        GeneratePlayer();
        GenerateEnemy();
        GenerateCollectibles();
        GenerateHazards();

        AssetDatabase.Refresh();
        Debug.Log("[DoomScrolling] Placeholder sprites generated in Assets/_Sprites/");
    }

    // ── Tilesheet (11 wall tiles + 3 floor tiles in one 16×224 sheet) ──

    static void GenerateTilesheet()
    {
        // 11 wall variants + FLOOR_PLAIN + FLOOR_FEED + MALUS = 14 tiles tall
        int tileCount = 14;
        var tex = new Texture2D(16, 16 * tileCount, TextureFormat.RGBA32, false);

        // Wall tile (solid purple with violet border) — rows 0–10
        for (int t = 0; t < 11; t++)
            DrawWallTile(tex, t);

        // FLOOR_PLAIN
        DrawFloorPlain(tex, 11);

        // FLOOR_FEED
        DrawFloorFeed(tex, 12);

        // MALUS
        DrawMalusTile(tex, 13);

        SaveTexture(tex, "Assets/_Sprites/Tilesheet/DoomScrolling_Tiles.png", 16, tileCount * 16);
    }

    static void DrawWallTile(Texture2D tex, int row)
    {
        int yOff = row * 16;
        for (int y = 0; y < 16; y++)
            for (int x = 0; x < 16; x++)
            {
                bool border = x == 0 || x == 15 || y == 0 || y == 15;
                bool inner  = x == 1 || x == 14 || y == 1 || y == 14;
                tex.SetPixel(x, yOff + y, border ? Black : inner ? Violet : Purple);
            }
    }

    static void DrawFloorPlain(Texture2D tex, int row)
    {
        int yOff = row * 16;
        for (int y = 0; y < 16; y++)
            for (int x = 0; x < 16; x++)
                tex.SetPixel(x, yOff + y, Black);
        // 2×2 corner dots
        foreach (var (cx, cy) in new[]{(1,1),(1,14),(14,1),(14,14)})
        {
            tex.SetPixel(cx,   yOff + cy,   Violet);
            tex.SetPixel(cx+1, yOff + cy,   Violet);
            tex.SetPixel(cx,   yOff + cy+1, Violet);
            tex.SetPixel(cx+1, yOff + cy+1, Violet);
        }
    }

    static void DrawFloorFeed(Texture2D tex, int row)
    {
        int yOff = row * 16;
        for (int y = 0; y < 16; y++)
            for (int x = 0; x < 16; x++)
                tex.SetPixel(x, yOff + y, y % 3 == 0 ? Violet : Black);
    }

    static void DrawMalusTile(Texture2D tex, int row)
    {
        int yOff = row * 16;
        for (int y = 0; y < 16; y++)
            for (int x = 0; x < 16; x++)
                tex.SetPixel(x, yOff + y, Black);
        // Red border
        for (int i = 0; i < 16; i++) { tex.SetPixel(i, yOff, Red); tex.SetPixel(i, yOff+15, Red); }
        for (int i = 0; i < 16; i++) { tex.SetPixel(0, yOff+i, Red); tex.SetPixel(15, yOff+i, Red); }
        // ⚠ triangle approximation
        tex.SetPixel(8, yOff+12, Red); tex.SetPixel(8, yOff+11, Red);
        tex.SetPixel(7, yOff+10, Red); tex.SetPixel(9, yOff+10, Red);
        tex.SetPixel(6, yOff+9,  Red); tex.SetPixel(10,yOff+9,  Red);
        tex.SetPixel(8, yOff+7,  Red);
    }

    // ── Player spritesheet (3 walk frames × 4 directions = 12 cells) ──

    static void GeneratePlayer()
    {
        var tex = new Texture2D(16 * 3, 16 * 4, TextureFormat.RGBA32, false);
        FillTexture(tex, Clear);

        // 4 directions × 3 frames — simple colored block + phone indicator
        Color32[] dirs = { White, White, White, White };
        for (int dir = 0; dir < 4; dir++)
            for (int frame = 0; frame < 3; frame++)
                DrawPlayerFrame(tex, frame * 16, dir * 16, frame);

        SaveTexture(tex, "Assets/_Sprites/Character/Player_Spritesheet.png", tex.width, tex.height);
    }

    static void DrawPlayerFrame(Texture2D tex, int xOff, int yOff, int frame)
    {
        // Body
        for (int y = 2; y < 14; y++)
            for (int x = 3; x < 13; x++)
                tex.SetPixel(xOff + x, yOff + y, White);

        // Hood (dark navy — approximate with purple tones)
        Color32 navy = new(0x1A, 0x10, 0x40, 0xFF);
        for (int y = 10; y < 16; y++)
            for (int x = 3; x < 13; x++)
                tex.SetPixel(xOff + x, yOff + y, navy);

        // Eyes
        tex.SetPixel(xOff+5, yOff+13, Black);
        tex.SetPixel(xOff+10, yOff+13, Black);

        // Phone (right hand, glowing blue)
        for (int py = 3; py < 8; py++)
            for (int px = 12; px < 15; px++)
                tex.SetPixel(xOff + px, yOff + py, Blue);

        // Leg bob animation offset
        int legY = frame == 1 ? 1 : 0;
        tex.SetPixel(xOff+5, yOff+2+legY, new Color32(0x1A,0x10,0x40,0xFF));
        tex.SetPixel(xOff+10, yOff+2, new Color32(0x1A,0x10,0x40,0xFF));
    }

    // ── Enemy (Like Creature heart, 16×16) ─────────────────────────

    static void GenerateEnemy()
    {
        var tex = new Texture2D(16 * 2, 16 * 4, TextureFormat.RGBA32, false); // 2 walk frames × 4 tiers
        FillTexture(tex, Clear);

        for (int tier = 0; tier < 4; tier++)
            for (int frame = 0; frame < 2; frame++)
                DrawHeart(tex, frame * 16, tier * 16,
                    tier == 1 ? new Color32(0xFF,0x30,0x60,0xFF) :
                    tier == 2 ? new Color32(0xFF,0x10,0x40,0xFF) :
                    tier == 3 ? new Color32(0x81,0x15,0xFF,0xFF) : Pink,
                    frame, tier == 3);

        SaveTexture(tex, "Assets/_Sprites/Enemies/LikeCreature_Spritesheet.png", tex.width, tex.height);
    }

    static void DrawHeart(Texture2D tex, int xOff, int yOff, Color32 col,
                          int frame, bool frightened)
    {
        FillRect(tex, xOff, yOff, 16, 16, Clear);
        // Heart pixel pattern (centered in 16×16)
        int[] heartRows = { 0b0110011000000000, 0b1111111100000000,
                            0b1111111100000000, 0b0111111000000000,
                            0b0011110000000000, 0b0001100000000000,
                            0b0000000000000000 };
        for (int row = 0; row < 7; row++)
        {
            int bits = heartRows[row] >> 8;
            for (int hx = 0; hx < 8; hx++)
                if ((bits & (1 << (7 - hx))) != 0)
                    tex.SetPixel(xOff + 4 + hx, yOff + 12 - row, frightened ? Purple : col);
        }

        // Simple filled heart approximation
        for (int y = 6; y < 14; y++)
            for (int x = 3; x < 13; x++)
            {
                bool inHeart = (y < 10) ? ((x > 3 && x < 7) || (x > 8 && x < 13)) :
                               y < 12 ? (x > 3 && x < 13) :
                               y < 13 ? (x > 4 && x < 12) :
                               (x > 6 && x < 10);
                if (inHeart) tex.SetPixel(xOff + x, yOff + y, frightened ? Purple : Pink);
            }

        // Eyes
        tex.SetPixel(xOff+6,  yOff+10, White);
        tex.SetPixel(xOff+7,  yOff+10, White);
        tex.SetPixel(xOff+9,  yOff+10, White);
        tex.SetPixel(xOff+10, yOff+10, White);

        // Legs (2 pixels each, offset by frame)
        int legOff = frame == 0 ? 0 : 1;
        tex.SetPixel(xOff+5, yOff+5+legOff, frightened ? Purple : Pink);
        tex.SetPixel(xOff+10, yOff+5+(1-legOff), frightened ? Purple : Pink);
    }

    // ── Collectibles ──────────────────────────────────────────────

    static void GenerateCollectibles()
    {
        // NotifDot (8×8)
        var dot = new Texture2D(8, 8, TextureFormat.RGBA32, false);
        FillTexture(dot, Clear);
        for (int y = 1; y < 7; y++) for (int x = 1; x < 7; x++) dot.SetPixel(x, y, Pink);
        dot.SetPixel(3, 3, White); dot.SetPixel(4, 3, White); // "1" pixel
        dot.SetPixel(3, 4, White); dot.SetPixel(4, 4, White);
        SaveTexture(dot, "Assets/_Sprites/Collectibles/NotifDot.png", 8, 8);

        // App icon stubs (16×16 colored squares)
        SaveSolidColor(16, 16, new Color32(0xFF,0xFC,0x00,0xFF), "Assets/_Sprites/Collectibles/Snapchat_Icon.png");
        SaveSolidColor(16, 16, Purple,                            "Assets/_Sprites/Collectibles/Instagram_Icon.png");
        SaveSolidColor(16, 16, Black,                             "Assets/_Sprites/Collectibles/TikTok_Icon.png");
        SaveSolidColor(16, 16, Black,                             "Assets/_Sprites/Collectibles/Twitter_Icon.png");

        // BonusPoints (+++ stack in blue)
        var bonus = new Texture2D(16, 16, TextureFormat.RGBA32, false);
        FillTexture(bonus, Black);
        DrawPlus(bonus, 4, 4, Blue); DrawPlus(bonus, 7, 7, Blue); DrawPlus(bonus, 10, 10, Blue);
        SaveTexture(bonus, "Assets/_Sprites/Collectibles/BonusPoints.png", 16, 16);

        // Smartphone
        var phone = new Texture2D(16, 16, TextureFormat.RGBA32, false);
        FillTexture(phone, Black);
        for (int y = 2; y < 14; y++) for (int x = 4; x < 12; x++)
            phone.SetPixel(x, y, x == 4 || x == 11 || y == 2 || y == 13 ? White : Blue);
        SaveTexture(phone, "Assets/_Sprites/Collectibles/FullPhone.png", 16, 16);
    }

    static void DrawPlus(Texture2D tex, int cx, int cy, Color32 c)
    {
        tex.SetPixel(cx, cy,   c); tex.SetPixel(cx+1, cy,   c);
        tex.SetPixel(cx, cy+1, c); tex.SetPixel(cx+1, cy+1, c);
        tex.SetPixel(cx-1, cy, c); tex.SetPixel(cx+2, cy,   c);
        tex.SetPixel(cx, cy-1, c); tex.SetPixel(cx, cy+2,   c);
        tex.SetPixel(cx+1, cy-1, c); tex.SetPixel(cx+1, cy+2, c);
    }

    // ── Hazards ───────────────────────────────────────────────────

    static void GenerateHazards()
    {
        // PopupAd — gray rectangle with "AD" suggestion
        var popup = new Texture2D(16, 32, TextureFormat.RGBA32, false);
        Color32 gray = new(0x44,0x44,0x44,0xFF);
        FillTexture(popup, gray);
        for (int x = 0; x < 16; x++) { popup.SetPixel(x, 31, White); popup.SetPixel(x, 16, White); }
        for (int y = 16; y < 32; y++) { popup.SetPixel(0, y, White); popup.SetPixel(15, y, White); }
        // Red title bar
        for (int y = 27; y < 31; y++) for (int x = 1; x < 15; x++) popup.SetPixel(x, y, Red);
        SaveTexture(popup, "Assets/_Sprites/Hazards/PopupAd_Sprite.png", 16, 32);

        // AutoPlay — purple play triangle
        var play = new Texture2D(16, 16, TextureFormat.RGBA32, false);
        FillTexture(play, Black);
        for (int row = 0; row < 8; row++)
            for (int col = 0; col <= row; col++)
                play.SetPixel(4 + col, 4 + row, Violet);
        SaveTexture(play, "Assets/_Sprites/Hazards/AutoPlay_Sprite.png", 16, 16);

        // TrendingTrap — pink flame shape
        var flame = new Texture2D(16, 16, TextureFormat.RGBA32, false);
        FillTexture(flame, Black);
        int[] flameH = { 3,4,5,4,3,5,7,7,5,3 };
        for (int row = 0; row < 10; row++)
            for (int col = 0; col < flameH[row]; col++)
                flame.SetPixel(6 + col - flameH[row]/2, 3 + row, Pink);
        SaveTexture(flame, "Assets/_Sprites/Hazards/TrendingTrap_Sprite.png", 16, 16);
    }

    // ── Utility ───────────────────────────────────────────────────

    static void FillTexture(Texture2D tex, Color32 c)
    {
        for (int y = 0; y < tex.height; y++)
            for (int x = 0; x < tex.width; x++)
                tex.SetPixel(x, y, c);
    }

    static void FillRect(Texture2D tex, int xOff, int yOff, int w, int h, Color32 c)
    {
        for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
                tex.SetPixel(xOff + x, yOff + y, c);
    }

    static void SaveSolidColor(int w, int h, Color32 c, string path)
    {
        var tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
        FillTexture(tex, c);
        SaveTexture(tex, path, w, h);
    }

    static void SaveTexture(Texture2D tex, string path, int w, int h)
    {
        tex.Apply();
        byte[] png = tex.EncodeToPNG();
        File.WriteAllBytes(path, png);
        Object.DestroyImmediate(tex);

        AssetDatabase.ImportAsset(path);
        var importer = (TextureImporter)AssetImporter.GetAtPath(path);
        if (importer != null)
        {
            importer.textureType         = TextureImporterType.Sprite;
            importer.spriteImportMode    = SpriteImportMode.Single;
            importer.filterMode          = FilterMode.Point;
            importer.textureCompression  = TextureImporterCompression.Uncompressed;
            importer.spritePixelsPerUnit = 16;
            importer.mipmapEnabled       = false;
            importer.SaveAndReimport();
        }
    }

    static void EnsureDir(string path)
    {
        if (!Directory.Exists(path)) Directory.CreateDirectory(path);
    }
}
#endif
