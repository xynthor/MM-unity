# MM Unity

A fan-made Unity recreation/port project inspired by the classic Might and Magic games.

> This is a non-commercial fan project. Might and Magic and related original game assets are the property of their respective rights holders.

## Current project

The main playable world scene is:

`Assets/Scenes/World/Enroth.unity`

Canonical region scenes live in:

`Assets/Scenes/Regions/`

Directional and edge-world extension scenes live in:

`Assets/Scenes/Extensions/`

## Requirements

- Unity **6000.5.6f1**
- Git with **Git LFS**
- Windows is currently the primary development platform

## Clone and open

```powershell
git lfs install
git clone https://github.com/xynthor/MM-unity.git
cd MM-unity
git lfs pull
```

Open the cloned repository in Unity Hub with Unity 6000.5.6f1, or on Windows run:

```text
Start-MMUnity.cmd
```

The first Unity import can take time because the world contains large scenes and generated terrain assets.

## Play

Open `Assets/Scenes/World/Enroth.unity` and press **Play**.

The Enroth scene is also the first enabled scene in Unity Build Settings.

## Repository layout

```text
Assets/
  Editor/        Development and world-building tools
  Materials/     Project materials and terrain layers
  Prefabs/       Project prefabs
  Scenes/
    World/       Master linked world
    Regions/     Canonical region scenes
    Extensions/  Directional/off-map extensions
  Scripts/       Runtime code
  Shaders/       Project shaders
  World/         Generated world and terrain data
Packages/        Unity package manifest and lock file
ProjectSettings/ Unity project configuration
Tools/           Development/audit utilities
```

## Local-only content

The repository intentionally excludes Unity caches, backups, validation output, preview screenshots, extracted original-game data, and external/reference asset packs.

In particular, `Preview/` is local QA output. Tools may generate screenshots there, but it is not part of the game and is not committed.

Some development/reference assets are deliberately not redistributed. A clean clone therefore contains the project code, scenes, generated world state, and tracked project assets, but not proprietary original-game source data or unlicensed third-party packs.

## Third-party code

`Assets/ThirdParty/UnityMeshSimplifier/` is included under its MIT license because project editor tooling depends on it. Its original license is included with the source.

## Git LFS

Large canonical world assets are stored with Git LFS. If a large scene appears as a small text pointer after cloning, run:

```powershell
git lfs pull
```

## Development rule

Keep generated Unity folders and local diagnostic output out of Git. Commit reproducible source, canonical scenes, project settings, and intentional generated world assets only.
