# DOOM SCROLLING — Lancer le jeu en 3 étapes

## Étape 1 — Créer le projet dans Unity Hub

1. Ouvre **Unity Hub**
2. Clique **New Project**
3. Template : **2D (Core)**
4. Version Unity : **2022.3 LTS** (recommandé)
5. Location : `C:\Users\Utilisateur\OneDrive\Bureau\`
6. Name : `Doom Scrolling`
7. Clique **Create project**

> Unity ouvre le projet vide.

---

## Étape 2 — Copier les fichiers du jeu

Dans l'**Explorateur Windows**, copie ces dossiers/fichiers du dossier actuel
vers le projet Unity que tu viens de créer :

```
Doom Scrolling/          ← dossier source (celui-ci)
│
├── Assets/              → copier TOUT dans [projet Unity]/Assets/
│   ├── Scripts/
│   ├── Resources/
│   └── Editor/
│
└── Packages/manifest.json → remplacer [projet Unity]/Packages/manifest.json
```

Retourne dans Unity → il recompile automatiquement (30-60 secondes).

**Quand Unity te demande "Import TMP Essentials" → clique YES.**

---

## Étape 3 — Appuie sur Play !

1. Dans Unity, une boîte de dialogue apparaît :
   **"Project configured! Press PLAY to start the game."**
   → Clique **LET'S PLAY!**

2. Un fichier `Assets/Scenes/Game.unity` est créé automatiquement.

3. Clique le bouton **▶ Play** en haut de Unity.

4. Le jeu démarre !

---

## Contrôles

| Touche | Action |
|---|---|
| `W` `A` `S` `D` ou `↑ ↓ ← →` | Déplacer le personnage |
| `ESC` | Pause |
| `SPACE` | (au menu) Lancer le jeu |

---

## Ce que tu vas voir

- **Écran titre** : "DOOM SCROLLING / YOU ARE THE PRODUCT"
- Cliquer **FEED THE SYSTEM** pour jouer
- Un labyrinthe 21×21 de style pixel neon
- Ton personnage (rose) doit collecter les **points cyan**
- Évite les **cœurs roses** (Likes) qui te chassent
- Chaque collectible **accélère** les ennemis
- Ramasse l'**étoile dorée** → mode power-up → mange les ennemis bleus !
- La barre "ENGAGEMENT LVL" monte → le jeu devient de plus en plus intense

---

## Si quelque chose ne fonctionne pas

**Erreur "TMP"** → Window → TextMeshPro → Import TMP Essentials

**Scène vide / pas de Game.unity** → Menu Unity → `DoomScrolling → Create Scene Manually`

**Personnage ne se déplace pas** → Vérifie que la scène `Game` est bien dans
File → Build Settings (elle doit y être automatiquement).

**Post-process absent** → Edit → Project Settings → Graphics →
Scriptable Render Pipeline Settings → assigne un URP Asset.

---

## Palette de couleurs du jeu

```
Fond          #0A000F  (noir violacé)
Murs          #1A0033  + lueur #7B2FBE (violet)
Joueur        #FF2D78  (rose néon)
Ennemis       #FF1458  (rouge-rose)
Ennemis peur  #334DFF  (bleu)
Points        #00F5FF  (cyan)
App icons     #FF2D78  (rose)
Power-up      #FFE020  (or)
```
