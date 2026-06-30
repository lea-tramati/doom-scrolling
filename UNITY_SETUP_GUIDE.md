# DOOM SCROLLING — Guide de Setup Unity

## 1. Créer le projet Unity

1. Ouvre Unity Hub → **New Project**
2. Template : **2D (Core)**
3. Unity version recommandée : **2022.3 LTS** ou **2023.x**
4. Nom : `DoomScrolling`
5. Copie le dossier `Assets/` de ce dossier dans ton projet Unity

---

## 2. Packages requis (Window → Package Manager)

| Package | Usage |
|---|---|
| **Universal RP** (URP) | Post-processing, glow, bloom |
| **2D Lights** (inclus URP) | Éclairage neon |
| **TextMeshPro** | Tout le texte UI |
| **Input System** (optionnel) | Contrôles manette |

---

## 3. Structure des Scenes

Crée 2 scènes :
- `Assets/Scenes/MainMenu.unity`
- `Assets/Scenes/Game.unity`

Ajoute les deux dans **File → Build Settings** (MainMenu en index 0, Game en 1).

---

## 4. Setup de la Scene MainMenu

### Hiérarchie :
```
MainMenu
├── [Camera] Main Camera
│   └── Script: ScreenShake
├── [Canvas] UI Canvas (Screen Space - Overlay)
│   ├── [Panel] Background (Image noire)
│   ├── [Panel] TitleGroup
│   │   ├── [Text] TitleText  (police pixel, 72pt, couleur #FF2D78)
│   │   ├── [Text] SubtitleText  ("YOU ARE THE PRODUCT", 18pt, #7B2FBE)
│   │   └── [Text] PressStartText  ("PRESS SPACE", 16pt, blink)
│   ├── [Panel] MenuGroup
│   │   ├── [Button] PlayButton → "FEED THE SYSTEM"
│   │   ├── [Button] CreditsButton → "CREDITS"
│   │   └── [Button] QuitButton → "DISCONNECT"
│   └── [Panel] CreditsPanel (caché par défaut)
├── [GameObject] AudioManager
│   └── Script: AudioManager
│   └── 2x AudioSource (Music + SFX)
├── [GameObject] GameManager
│   └── Script: GameManager
└── [GameObject] Bootstrapper
    └── Script: AssetBootstrapper
```

**Script MainMenuController** → sur le Canvas.

---

## 5. Setup de la Scene Game

### Hiérarchie :
```
Game
├── [Camera] Main Camera
│   ├── Script: CameraController
│   ├── Script: ScreenShake
│   └── [URP Camera Data]
│
├── [Grid] Maze Grid (Grid component)
│   ├── [Tilemap] Walls   (layer: Wall)
│   │   └── TilemapCollider2D + CompositeCollider2D
│   ├── [Tilemap] Floor
│   └── [Tilemap] Decorations
│
├── [GameObject] MazeRenderer
│   └── Script: MazeRenderer (assigner les tilemaps + prefabs)
│
├── [GameObject] Player
│   ├── Script: PlayerController
│   ├── Script: PlayerAnimator
│   ├── SpriteRenderer
│   ├── SpriteAutoAssign (type: Player)
│   ├── Animator
│   ├── Rigidbody2D (Gravity=0, Freeze Rotation)
│   └── CircleCollider2D (Trigger=true, r=0.4)
│
├── [GameObject] Enemies
│   ├── [GameObject] Enemy_Chaser
│   │   ├── Script: EnemyAI (Personality: Chaser)
│   │   ├── SpriteRenderer
│   │   ├── SpriteAutoAssign (type: Enemy)
│   │   ├── Rigidbody2D (Gravity=0)
│   │   └── CircleCollider2D (Trigger=true, r=0.4)
│   ├── Enemy_Predictor (Personality: Predictor)
│   ├── Enemy_Flanker   (Personality: Flanker)
│   └── Enemy_Wanderer  (Personality: Wanderer)
│
├── [GameObject] EnemyManager
│   └── Script: EnemyManager
│
├── [GameObject] Effects
│   ├── Script: WallDistortion
│   ├── Script: ScrollingBackground
│   ├── Script: NotificationPopup
│   ├── Script: GlitchEffect
│   ├── Script: AmbientLightPulse
│   └── Script: ParticleManager
│
├── [Light2D] Global Light  ← assigner dans AmbientLightPulse
│
└── [Canvas] HUD Canvas
    ├── Script: UIManager
    ├── [Text] ScoreText
    ├── [Text] HighScoreText
    ├── [Container] LivesContainer
    ├── [Slider] EngagementSlider
    │   └── Script: EngagementMeterUI
    ├── [Panel] PausePanel
    ├── [Panel] GameOverPanel
    │   └── Script: GameOverScreen
    └── [Panel] PowerUpOverlay
```

---

## 6. Prefabs Collectibles

Crée 3 prefabs dans `Assets/Prefabs/` :

### NotificationDot
- SpriteRenderer + SpriteAutoAssign (Dot)
- Script: Collectible (type: NotificationDot)
- CircleCollider2D (Trigger, r=0.3)

### AppIcon
- SpriteRenderer + SpriteAutoAssign (AppIcon)
- Script: AppIcon
- CircleCollider2D (Trigger, r=0.4)

### PowerUp
- SpriteRenderer + SpriteAutoAssign (PowerUp)
- Script: PowerUpItem
- CircleCollider2D (Trigger, r=0.45)

---

## 7. Layers & Physics

**Edit → Project Settings → Physics 2D → Layer Collision Matrix :**

| | Default | Player | Enemy | Wall | Collectible |
|---|---|---|---|---|---|
| Player | ✗ | ✗ | ✓ | ✓ | ✓ |
| Enemy | | ✗ | ✗ | ✓ | ✗ |

**Tags à créer :** `Player`, `Enemy`, `Wall`, `Collectible`

Layers :
- 8 → `Wall`
- 9 → `Player`  
- 10 → `Enemy`
- 11 → `Collectible`

---

## 8. Palette de couleurs (Copy-Paste dans Unity)

```
Background   #0A000F  (dark purple-black)
Wall         #1A0033  + emission #7B2FBE
Player       #FF2D78  (neon pink)
Enemy        #FF1458  (hot red-pink)
Frightened   #334DFF  (blue)
Dot          #00F5FF  (cyan)
AppIcon      #7B2FBE  (purple)
PowerUp      #FFE020  (gold)
UI Text      #FFFFFF
Engagement   gradient: #7B2FBE → #FF2D78 → #FF3300
```

---

## 9. Post-Processing (URP)

Ajoute un **Volume** global dans la scène Game :

```
[Volume] PostProcess
  ├── Bloom (Intensity: 1.2, Threshold: 0.8, Scatter: 0.4)
  ├── Chromatic Aberration (Intensity: 0.15 → monte à 0.5 avec l'engagement)
  ├── Vignette (Intensity: 0.35, Color: #0A000F)
  └── Color Grading (Saturation: +20, Contrast: +15)
```

Pour que le Chromatic Aberration monte avec l'engagement, crée `PostProcessController.cs` :

```csharp
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class PostProcessController : MonoBehaviour
{
    [SerializeField] Volume volume;
    ChromaticAberration ca;
    Bloom bloom;

    void Start()
    {
        volume.profile.TryGet(out ca);
        volume.profile.TryGet(out bloom);
        GameManager.Instance.OnEngagementChanged += OnEngagement;
    }

    void OnEngagement(float level)
    {
        if (ca != null) ca.intensity.value = 0.1f + level * 0.6f;
        if (bloom != null) bloom.intensity.value = 1f + level * 2f;
    }
}
```

---

## 10. Police de caractères

1. Télécharge **"Press Start 2P"** (Google Fonts - gratuit)
2. Import dans Unity → Window → TextMeshPro → Font Asset Creator
3. Assigne à tous les TextMeshProUGUI

---

## 11. Animations Player (Animator Controller)

Crée `PlayerAC` dans Assets/Animations :

```
States:
  Idle      → loop (frame 0)
  WalkRight → loop (frames 1-4)
  WalkLeft  → loop (frames 1-4, flip X)
  WalkUp    → loop (frames 5-8)
  WalkDown  → loop (frames 9-12)
  Death     → one-shot (frames 13-20)
  PowerUp   → loop (tint pink, slight scale pulse)

Parameters: DirX (Float), DirY (Float), PowerUp (Bool), Death (Trigger)
```

---

## 12. Animations Enemy (Animator Controller)

```
States:
  Chase     → loop (bobbing heart)
  Frightened → loop (blue, trembling)
  Return    → loop (faded, moving fast)

Parameters: Frightened (Bool)
```

---

## 13. Audio (fichiers à ajouter)

Place des fichiers `.wav` ou `.ogg` dans `Assets/Audio/` :

| Clé AudioManager | Description |
|---|---|
| `collect` | Notification ding court |
| `notification` | Vibration buzz |
| `power_up` | Montée synthé |
| `power_up_ending` | Alarme descendante |
| `eat_enemy` | Glitch crunch |
| `death` | Buzz descendant + static |
| `footstep` | Tap digital |

**Music :**
- `menu_music.ogg` — ambient dark synthwave boucle
- `game_music.ogg` — tension lo-fi beats
- `power_up_music.ogg` — montée intense ~8s

---

## 14. Paramètres GameManager (Inspector)

| Champ | Valeur recommandée |
|---|---|
| Base Enemy Speed | 2.0 |
| Speed Increase Per Collectible | 0.015 |
| Max Enemy Speed | 6.0 |
| Power Up Duration | 8.0 |
| Dots Per Level | 180 |

---

## 15. Scatter Targets (EnemyAI Inspector)

| Ennemi | Scatter Target |
|---|---|
| Chaser (Blinky) | Top-right corner (10, 10) |
| Predictor (Pinky) | Top-left (-10, 10) |
| Flanker (Inky) | Bottom-right (10, -10) |
| Wanderer (Clyde) | Bottom-left (-10, -10) |

---

## 16. Test rapide

1. Lance la scène `MainMenu`
2. Clique **FEED THE SYSTEM**
3. Le labyrinthe se génère depuis `MazeData.Level1`
4. ZQSD ou flèches pour bouger
5. Collecte les points cyan → les cœurs roses accélèrent
6. Collecte l'étoile dorée → mode power-up, les cœurs deviennent bleus → mange-les
7. Échappe → Pause

---

## Résolution cible

- **320×180** (16:9 pixel art, scale ×4 = 1280×720)
- Pixel Per Unit : **16**
- Camera Orthographic Size : **5.625** (180/2/16)
