# MA-CANH-DEMON-REALM

A 2D action-platformer built with Unity (URP), focused on high-speed combat, cinematic cutscenes, and dynamic scene transitions.

This README was written after a comprehensive review of all C# code in:

- `Assets/Scripts` (runtime gameplay)
- `Assets/Editor` (editor tooling)
- `Assets/Tests` (EditMode tests)

## 1) Tech Stack

- **Engine:** Unity `6000.3.10f1`
- **Language:** C#
- **Render Pipeline:** URP (`com.unity.render-pipelines.universal`)
- **Timeline/Cutscene:** `PlayableDirector`, Signal Track
- **Audio:** `AudioSource`, `AudioMixer`
- **Testing:** Unity Test Framework (`com.unity.test-framework`)
- **Input:** Legacy Input API (Input System package available but not currently in use)

## 2) Scene and Build Settings

According to `ProjectSettings/EditorBuildSettings.asset`, the current scene order is:

1. `Assets/Scenes/Menu.unity` - Main menu
2. `Assets/Scenes/Game1.unity` - Main gameplay level
3. `Assets/Scenes/Boss_Test.unity` - Boss encounter test scene

## 3) Gameplay Systems

### Player (`Player/`)

#### PlayerMovement
- **Horizontal movement** with physics-based velocity control
- **Jump mechanics** including ground and wall jumps
- **Dash ability** with cooldown, capable of phasing through enemy/bullet layers
- **Wall sliding and wall jumping** with automatic wall snap positioning
- **Block/Parry input detection** - quick tap triggers parry, holding beyond threshold enables block
- **Knockback system** with directional feedback and lock-out timers
- **Animation state management** (speed, grounded state, wall sliding, blocking states)

#### PlayerAttack
- **3-hit ground combo** system with automatic cycling through attacks
- **Separate air attack** with independent cooldown
- **Parry mechanics** with timed window (default 0.25s)
- **Damage detection** via circle overlap targeting enemy layer
- **Special boss hand handling** and health system integration
- **SFX triggers** for each attack type

#### PlayerHealth
- **Health pool** with max health tracking and current HP
- **Defense integration** - both parry and block prevent damage
- **Invincibility timer** after taking hits (default 1.2s)
- **Knockback feedback** with directional force application
- **Visual hurt state** with blinking effect
- **Death flow** triggers game over UI and state
- **Respawn integration** with health percentage restoration

#### PlayerRespawn
- **Checkpoint persistence** using static variables across scene loads
- **Checkpoint activation** via collider-based trigger system
- **Health restoration** on respawn (configurable percentage)
- **Pit damage system** optional damage before resurrection
- **Scene-aware validation** ensures checkpoints are valid for current scene

### Enemy (`Enemy/`)

#### EnemyBase (Abstract Base)
- **Health pool** with damage intake and death flow
- **Patrol system** with left/right movement on X-axis from spawn point
- **Player detection** and automatic chase behavior
- **Sprite flipping** based on facing direction (configurable)
- **Safe animator triggering** with parameter existence checks
- **SFX integration** for hurt and death events
- **Death sequence** with delayed destruction
- **Optional collider disabling** on death

#### Slime
- **Detection range** (6 units default) triggers engagement
- **Attack range** (1.2 units) enables attack animation
- **Contact damage** system (5 damage, 0.75s cooldown)
- **Attack cooldown** (1.25s) between animation triggers
- **Patrol behavior** when player is out of detection range

#### BugEnemy
- **Hover height maintenance** above ground level
- **Ranged attack** via animation event triggers
- **Attack radius** for area damage calculation
- **Separate speeds** for follow versus patrol movement
- **Range-based engagement** with detection and attack thresholds

#### ShieldEnemy
- Similar structure to other enemies with patrol/chase mechanics
- Push attack causing knockback to player
- Melee attack with separate hitpoint and radius

### Boss (`Boss/`)

#### BossController
- **Intro cutscene integration** with one-time trigger per session
- **Attack loop system** with randomized attack selection
- **Two attack types:** Projectile shooting and slam wave attacks
- **Attack cooldown** (3s default, 1.2s lock duration)
- **Idle animation** alternation between two states (1.6s swap interval)
- **Safe animator parameter management** with caching
- **Hit interrupt system** (1.5s cooldown between interrupts)
- **Vulnerability tracking** for attack phase detection

#### BossHealth
- **Large health pool** (300 default)
- **Hit stun system** with 0.5s minimum cooldown between hurt reactions
- **Outro cutscene** playback on defeat
- **Credits roll system** with team information display
- **Player input lock** during death sequence
- **Hurt animation variants** support for variety
- **Death timeout** before scene transition

#### BossBullet
- **Directional travel** in world space
- **Configurable lifetime** (5s default)
- **Damage on collision** with player
- **Self-destruction** on hit or lifetime expiry

#### BossHandHitbox
- **Damage receiver** for boss hand attack points (allows targeted damage to vulnerable parts)
- **Vulnerability checking** - only accepts damage when `BossController.AreHandsVulnerable` returns true
- **Auto-parent binding** - automatically finds BossHealth and BossController in parent hierarchy if not manually assigned
- **Dual damage method overloads** - accepts both `float` and `int` damage values
- **Dead state protection** - ignores damage if boss is already defeated
- **Validation** - rejects zero or negative damage values

### Hazards (`Hazards/`)

#### Checkpoint
- Updates player respawn point on collision
- Recheck cooldown prevents spam activation (0.25s default)
- Tag filtering (Player-only detection)

#### DeadZone
- **Massive damage** (9999) triggers immediate respawn
- Combined damage and respawn sequence
- Optional pit damage before resurrection

#### Trap
- **Continuous damage** on contact (10 damage default)
- **Damage interval cooldown** (5s default between hits)
- Separate OnTriggerEnter (immediate) vs OnTriggerStay (interval) behavior
- Optional tag filtering (Player-only)

### Scene Flow and UI

#### LevelManager (Singleton)
- Async scene loading with progress bar
- Scene transition effect orchestration
- Minimum loading screen duration (0.5s)
- Progress bar smoothing with visual feedback
- `DontDestroyOnLoad` persistence

#### SceneTransition + CrossFade
- Abstract base class for transition effects
- `AnimateTransitionIn()` - Fade to black (start of load)
- `AnimateTransitionOut()` - Fade from black (completion)
- CrossFade implementation using CanvasGroup alpha interpolation
- 1-second default duration with `unscaledDeltaTime` support

#### FinishPoint
- Triggers end-level completion
- Auto-resolves next scene based on build index or specified scene name
- Transition integration

#### MainMenu, PauseMenu, GameOverMenu
- Play/Restart/Home navigation
- Pause activation via ESC key with `Time.timeScale` manipulation
- Game over panel display on player death

## 4) Cutscene System

The cutscene stack is built around these core systems:

#### Core Components

- **CutsceneDirector** (Singleton Orchestrator)
  - Timeline playback support (optional Unity Timeline integration)
  - Camera waypoint following system
  - Subtitle display with timing control
  - Letterbox effect (12% top/bottom by default)
  - Vignette overlay visual effects
  - Fade in/out transitions (0.4s default)
  - Skip system with hold-to-skip input (1.2s hold duration)
  - Progress bar display
  - Runtime narrative configuration API
  - Player input locking during playback
  - Music fade-out integration

- **CutsceneSetup**
  - Auto-creates UI hierarchy if missing (Canvas, CutsceneUI, SubtitleContainer, SkipUI, ProgressBar)
  - Font auto-loading from Resources
  - Reuses existing components when present
  - Programmatic UI setup
  - Null-safe lifecycle management

- **CutsceneSkipUI**
  - Hold-to-skip progress indicator display
  - Skip hint text rendering
  - Progress bar management

- **SubtitleController** (Singleton)
  - Text fade in/out transitions
  - Timed subtitle sequence playback
  - Background panel with transparency control
  - Typewriter effect support

- **CutsceneStateStore**
  - PlayerPrefs-based persistence
  - Tracks watched cutscenes via unique IDs
  - Clear/mark watched functionality

- **CutsceneSignalReceiver**
  - Listens to Timeline Signal Track events
  - Triggers subtitle display, SFX, skip, and end events

#### Key Features

- **Event Lifecycle:** `OnCutsceneStarted`, `OnCutsceneSkipped`, `OnCutsceneEnded`
- **Skip Options:**
  - Single press or hold with ESC/SPACE
  - Auto-skip when checkpoint reached (configurable)
  - Auto-skip for previously viewed cutscenes (configurable)
- **Runtime Configuration:** Boss intro/outro can dynamically configure subtitle, camera, and timeline behavior
- **State Tracking:** Automatic marking of viewed cutscenes via `CutsceneStateStore.MarkWatched(cutsceneId)`

## 5) Audio System

#### MusicManager (Singleton)
- **Scene-based music switching** (Menu, Game1, Boss scenes)
- **Crossfade transitions** between tracks
- **Volume control** (0-1 normalized range)
- **Static clip caching** for performance
- **`DontDestroyOnLoad` persistence** across scenes
- **Convenience methods:** `PlayMenuMusic()`, `PlayGame1Music()`, `PlayBossMusic()`

#### SoundManager (Singleton)
- **One-shot SFX playback** via single AudioSource
- **Sound library integration** with clip caching
- **Category-based SFX methods:**
  - `PlayPlayerSfx(...)` - prefixes "Player_" to sound keys
  - `PlayEnemySfx(...)` - prefixes "Enemy_" to sound keys
  - `PlayBossSfx(...)` - prefixes "Boss_" to sound keys
- **Volume management** with normalized values
- **Dynamic library reloading** on scene load

#### Audio Libraries
- **MusicLibrary.cs** - ScriptableObject containing serialized background music tracks
- **SoundLibrary.cs** - ScriptableObject containing serialized SFX clips
- Both use key-based access for organized audio management

**Note:** Two legacy empty files exist in `Assets/Scripts/Music/`: `MusicLibrary.cs` (legacy) and `SoundLibrary.cs` (legacy). The actual active files are the correctly named `MusicLibrary.cs` and `SoundLibrary.cs`.

## 6) Default Input Bindings

Gameplay currently uses the **Legacy Input API** (`Input.GetKey/GetButton/GetAxisRaw`).

| Action | Input | Alternative |
|--------|-------|-------------|
| Move Left/Right | Horizontal Axis (A/D) | - |
| Jump | Jump Button | - |
| Attack | J | Left Mouse Button |
| Dash | K | Left Shift |
| Block/Parry | L | Right Mouse Button |
| Pause | Escape | - |
| Cutscene Skip | Escape or Space | - |

**Note:** The Input System package is included in the project but the runtime gameplay currently uses Legacy Input. Full migration to Input System would require updating all gameplay and cutscene script input calls.

## 7) Editor Tools

### Cutscene Editor Tools

#### CutscenePrefabCreator
- **Menu:** `Tools/Cutscene/Create Cutscene System in Scene`
- Rapidly generates Cutscene UI components and scene setup
- Establishes basic component bindings

#### CutsceneTimelineSetupTool
- **Menu:** `Tools/Cutscene/Create Timeline Setup On Selected Director`
- Creates Timeline instances with Signal Track
- Generates reaction assets and sample configurations

### Unity Bridge (Editor-Only)

Located in `Assets/Editor/UnityBridge/` folder.

#### UnityBridgeServer
- WebSocket local server with auto-start via `[InitializeOnLoad]`
- Default endpoint: `ws://127.0.0.1:6400` (with fallback port support)
- Menu restart: `Unity Copilot/Restart Bridge Server`

#### UnityCommandHandler
- Processes JSON commands on main thread
- Supported Actions:
  - `ping` - Health check
  - `createPrefab`, `createScene`, `createGameObject`, `createScript` - Asset creation
  - `addComponent`, `setProperty`, `setMaterial`, `setAnimatorController` - Component management
  - `openScene`, `saveScene` - Scene operations
  - `instantiatePrefab` - Runtime instantiation
  - `getSceneHierarchy` - Scene inspection
  - `listAssets` - Asset discovery
  - `deleteGameObject` - Object removal

## 8) Tests

EditMode tests currently available:

#### CutsceneStateStoreTests
- Tests `IsWatched()` functionality - verifies cutscene watched state tracking
- Tests `MarkWatched()` functionality - ensures cutscenes are properly marked as viewed
- Tests `ClearWatched()` functionality - validates clearing of watched state
- Tests null/empty input safety - ensures graceful handling of invalid inputs

## 9) Script Directory Structure

```
Assets/
├── Scripts/
│   ├── Boss/                    # Boss AI and behavior
│   │   ├── BossController.cs
│   │   ├── BossHealth.cs
│   │   ├── BossBullet.cs
│   │   └── BossHandHitbox.cs
│   ├── Cutscene/                # Cutscene orchestration and UI
│   │   ├── CutsceneDirector.cs
│   │   ├── CutsceneSetup.cs
│   │   ├── CutsceneSignalReceiver.cs
│   │   ├── CutsceneSkipUI.cs
│   │   ├── SubtitleController.cs
│   │   └── CutsceneStateStore.cs
│   ├── Enemy/                   # Enemy types and base behavior
│   │   ├── EnemyBase.cs
│   │   ├── Slime.cs
│   │   ├── BugEnemy.cs
│   │   └── ShieldEnemy.cs
│   ├── Hazards/                 # Environmental hazards
│   │   ├── Checkpoint.cs
│   │   ├── DeadZone.cs
│   │   └── Trap.cs
│   ├── map/                     # Environmental effects
│   │   └── ParallaxScroll.cs
│   ├── Menu/                    # UI and menu systems
│   │   ├── MainMenu.cs
│   │   ├── PauseMenu.cs
│   │   ├── GameOverMenu.cs
│   │   └── MenuParallax.cs
│   ├── Music/                   # Audio management
│   │   ├── MusicManager.cs
│   │   ├── SoundManager.cs
│   │   ├── MusicLibrary.cs
│   │   ├── SoundLibrary.cs
│   │   ├── MusicLibrary.cs (legacy)
│   │   └── SoundLibrary.cs (legacy)
│   ├── Player/                  # Player mechanics
│   │   ├── PlayerMovement.cs
│   │   ├── PlayerAttack.cs
│   │   ├── PlayerHealth.cs
│   │   └── PlayerRespawn.cs
│   └── SceneTransition/         # Scene loading and transitions
│       ├── LevelManager.cs
│       ├── SceneTransition.cs
│       └── CrossFade.cs
├── Editor/                      # Editor-only utilities
│   ├── CutscenePrefabCreator.cs
│   ├── CutsceneTimelineSetupTool.cs
└── Tests/
    └── EditMode/                # Unit tests
        └── CutsceneStateStoreTests.cs
```

## 10) Running the Project

1. Open the project in Unity Hub using Unity version `6000.3.10f1`
2. Open the scene `Assets/Scenes/Menu.unity`
3. Press Play in the Unity Editor

The Menu scene is configured as the first scene in Build Settings and handles loading into the main gameplay.

## 11) Building

1. Go to `File > Build Settings`
2. Verify that scenes are in the correct order (see Section 2)
3. Select your target platform
4. Click `Build` or `Build And Run`

## 12) Architectural Patterns and Design Notes

### Core Architecture Patterns

| Pattern | Components |
|---------|-----------|
| **Singleton** | MusicManager, SoundManager, CutsceneDirector, LevelManager, SubtitleController |
| **Abstract Base Class** | EnemyBase (inherited by Slime, BugEnemy, ShieldEnemy) |
| **State Machine** | BossController (idle/attacking), PlayerMovement (grounded/wall-sliding/dashing) |
| **Event System** | CutsceneDirector events (OnCutsceneStarted, OnCutsceneSkipped, OnCutsceneEnded) |
| **Collider Callbacks** | OnTriggerEnter/Stay/Exit for damage, checkpoints, traps |
| **Coroutines** | Dash cooldown, attack animations, scene loading, transitions |
| **Scriptable Objects** | MusicLibrary, SoundLibrary (audio asset organization) |

### Important Technical Considerations

#### Singleton Management
- `LevelManager`, `MusicManager`, `SoundManager`, `CutsceneDirector`, and `SubtitleController` all implement singleton patterns
- **Avoid placing multiple instances** of these in the same scene unless explicitly required
- These components persist across scene loads using `DontDestroyOnLoad`

#### Time Scale Behavior
- Several game flows use `Time.timeScale = 0` for pause and game-over states
- Animations and coroutines involved with transitions and cutscenes **prioritize `unscaledDeltaTime`** throughout the codebase
- Ensure any new timed systems check whether they should use scaled or unscaled time

#### Input System Considerations
- If changing input key names or fully migrating to the new Input System package, you must **review all gameplay and cutscene scripts** that reference input
- This includes PlayerMovement, PlayerAttack, PauseMenu, CutsceneSkipUI, and all related systems
- A comprehensive audit is required before switching input backends

### Data Flow Summary

```
Player Input
    ↓
PlayerMovement/PlayerAttack (Physics, Damage Detection)
    ↓
Enemy Detection → EnemyBase.Move() → EnemyBase.TakeDamage()
    ↓
BossController (AI State Machine) → BossHealth.TakeDamage() → Outro Cutscene
    ↓
Scene Transition (CrossFade) → LevelManager
    ↓
MusicManager (Auto-switches based on scene)
    ↓
SoundManager (Plays SFX for all actions)
```

### Module Expansion Guide

The modular architecture supports easy expansion:
- **New enemy types:** Inherit from `EnemyBase`, override `UpdateBehavior()` and `Attack()`
- **New transitions:** Inherit from `SceneTransition`, implement `AnimateTransitionIn()` and `AnimateTransitionOut()`
- **New audio:** Add clips to `MusicLibrary` or `SoundLibrary` ScriptableObjects via Inspector
- **New UI menus:** Follow `MainMenu` pattern, reference `LevelManager` for scene loading
