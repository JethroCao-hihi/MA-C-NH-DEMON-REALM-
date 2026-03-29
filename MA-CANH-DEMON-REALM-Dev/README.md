# MA-CANH-DEMON-REALM

2D action-platformer built with Unity. This README is rewritten after reviewing the full C# codebase (runtime + editor tools).

## Overview

- Engine: Unity `6000.3.10f1`
- Language: C#
- Render Pipeline: URP (`com.unity.render-pipelines.universal`)
- Main scenes:
  - `Assets/Scenes/Menu.unity`
  - `Assets/Scenes/Game1.unity`
  - `Assets/Scenes/Boss_Test.unity`

## Current Gameplay Systems

### Player

- Horizontal movement + jump
- Dash with cooldown and optional ghost-collision layers (`dashIgnoreLayers`)
- Wall slide + wall jump
- Ground combo attack (3-step chain) + separate air attack
- Block / parry input split by hold duration:
  - quick tap on block input -> parry window
  - hold block input -> blocking state
- Health, hurt, invincibility timer, blink feedback, knockback, death

### Enemies

- `EnemyBase`: shared HP, movement, optional patrol, hurt/die flow, animator parameter safety checks
- `Slime`: detect/chase + close attack + contact damage with cooldown
- `ShieldEnemy`: patrol/chase + push attack (with player knockback) + close attack
- `BugEnemy`: flying patrol (hover) + chase + attack via animation-event hit check

### Boss

- `BossController`: random pattern between shoot and slam attacks on cooldown
- `BossBullet`: directional projectile with lifetime and player damage
- `BossHealth`: boss HP, optional hurt variants, hurt cooldown, death handling

### Hazards and Respawn

- `Checkpoint`: saves respawn point
- `DeadZone`: heavy raw damage + immediate respawn call
- `Trap`: periodic damage via trigger/collision
- `PlayerRespawn`: restores position and movement state, applies respawn logic

### Scene Flow

- `LevelManager`: async scene loading + progress bar + transition entry/exit
- `FinishPoint`: level end trigger to load next scene (by build index or explicit name)
- `CrossFade`: concrete transition based on `CanvasGroup`
- `MainMenu`, `PauseMenu`, `GameOverMenu`: menu flow, pause flow, retry/home flow

## Default Controls (from code)

- Move: `Horizontal` axis (`A/D`, `Left/Right`)
- Jump: `Jump` input (`Space` by default)
- Attack: `J` or mouse left
- Dash: `K` or `Left Shift`
- Block/Parry: `L` or mouse right
- Pause: `Escape`

Important behavior notes:

- While blocking, attack input is ignored.
- Parry is a short window and has its own cooldown.
- Dash can ignore selected layer collisions while active.

## Audio Architecture

- `MusicManager` (singleton, `DontDestroyOnLoad`): scene-driven BGM selection
- `SoundManager` (singleton, `DontDestroyOnLoad`): one-shot SFX playback
- Convenience prefixes:
  - `PlayPlayerSfx("...")`
  - `PlayEnemySfx("...")`
  - `PlayBossSfx("...")`
- Audio libraries:
  - `MusicLirbary`
  - `SoundLirbary`

Note: `MusicLirbary` and `SoundLirbary` are intentionally spelled as in code to match current references.

## Project Structure (Scripts)

```text
Assets/
  Scripts/
    Player/
      PlayerMovement.cs
      PlayerAttack.cs
      PlayerHealth.cs
      PlayerRespawn.cs
    Enemy/
      EnemyBase.cs
      Slime.cs
      ShieldEnemy.cs
      BugEnemy.cs
    Boss/
      BossController.cs
      BossBullet.cs
      BossHealth.cs
    SceneTransition/
      SceneTransition.cs
      CrossFade.cs
      LevelManager.cs
      FinishPoint.cs
      SceneController.cs
    Menu/
      MainMenu.cs
      PauseMenu.cs
      GameOverMenu.cs
      MenuParallax.cs
    map/
      ParallaxScroll.cs
    Music/
      MusicManager.cs
      MusicLirbary.cs
      SoundManager.cs
      SoundLirbary.cs
    New Folder/
      Checkpoint.cs
      DeadZone.cs
      Trap.cs

  Editor/UnityBridge/
    UnityBridgeServer.cs
    UnityCommandHandler.cs
```

## Run In Unity

1. Open project from Unity Hub with Unity `6000.3.10f1`.
2. Open `Assets/Scenes/Menu.unity` (normal flow) or `Assets/Scenes/Game1.unity` (quick gameplay test).
3. Press Play.

## Build

1. Open `File > Build Settings`.
2. Ensure scene order in build settings:
   - `Assets/Scenes/Menu.unity`
   - `Assets/Scenes/Game1.unity`
   - `Assets/Scenes/Boss_Test.unity`
3. Choose platform and run Build or Build And Run.

## UnityBridge (Editor-only)

`Assets/Editor/UnityBridge` contains a local WebSocket bridge for editor automation.

- Auto-start in editor via `[InitializeOnLoad]`
- Default endpoint: `ws://127.0.0.1:6400`
- Main-thread command processing via `EditorApplication.update`
- Restart command in Unity menu: `Unity Copilot/Restart Bridge Server`
- Wrapped in `#if UNITY_EDITOR` (not included in player runtime build)

Supported command actions in `UnityCommandHandler` include:

- `ping`
- `createPrefab`
- `createScene`
- `addComponent`
- `createGameObject`
- `createScript`
- `setProperty`
- `openScene`
- `instantiatePrefab`
- `getSceneHierarchy`
- `listAssets`
- `deleteGameObject`
- `setMaterial`
- `setAnimatorController`
- `saveScene`

## Known Technical Notes

- There are currently two scene-loader singletons: `LevelManager` and `SceneController`.
- Runtime input is still using legacy `Input` API (`GetKey`, `GetAxisRaw`, `GetButtonDown`) even though Input System package is present.
- Some script files contain duplicated `using UnityEngine;` lines.
- Folder name `Assets/Scripts/New Folder` is still generic and can be renamed later for clarity.

## Quick Start Checklist

- Player object has tag `Player`
- Ground/wall layers are set correctly for movement checks
- Enemy/player layer masks are configured for hit detection
- `MusicLirbary` and `SoundLirbary` exist in the scene or are discoverable
- `LevelManager` references (transition container, progress bar) are assigned