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

### Public fallback mode

A clean public clone is designed to enter Play Mode without the maintainer's private/reference asset packs. When optional art is unavailable, the runtime fallback:

- uses the integrated `Enroth` world directly and disables source-scene streaming;
- keeps terrain, cameras, movement and gameplay scripts active;
- suppresses renderers whose external meshes are unavailable instead of leaving broken/pink objects;
- repairs null material slots with a neutral project fallback material;
- provides a simple built-in capsule visual for the active player when the optional character model is absent.

This is the redistributable public baseline. The maintainer's local project contains additional full-fidelity vegetation, source architecture and character assets that are intentionally not published in this repository.

The region and extension scenes remain in the repository as development/reconstruction sources. They can reference optional local asset packs and are not used by the public standalone build.

## Build a Windows player

On Windows, run:

```text
Build-Public-Windows.cmd
```

The public build packages only `Assets/Scenes/World/Enroth.unity` and writes:

```text
Builds/Windows/MMUnity.exe
```

You can also build from Unity via **MM Unity → Build → Public Windows x64**.

### Current controls

- **WASD** — move
- **Mouse** — camera
- **Shift** — sprint
- **Left Ctrl** — walk
- **Space** — jump
- **Left mouse** — attack combo
- **Right mouse** — block
- **F** — heavy attack
- **E** — kick
- **R** — cast animation
- **Q + movement direction** — dodge
- **Esc** — release the mouse cursor

## Repository layout

```text
Assets/
  Editor/
    Builders/     Region/world builders
    Core/         Active world-building and repair pipeline
    Diagnostics/  Audits and validation tools
    Experiments/  Historical/experimental editor utilities
  Materials/     Project materials and terrain layers
  Prefabs/       Project prefabs
  Scenes/
    World/       Master linked world
    Regions/     Canonical region scenes
    Extensions/  Directional/off-map extensions
  Scripts/       Runtime/gameplay code
  Shaders/       Project shaders
  ThirdParty/    Redistributable source dependencies and licenses
  World/         Generated world and terrain data
Packages/        Unity package manifest and lock file
ProjectSettings/ Unity project configuration
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

## Development notes

The large historical one-off repair scripts used during reconstruction are intentionally kept out of the public repository. They are archived locally by the maintainer rather than mixed into the playable project.

Keep generated Unity folders and local diagnostic output out of Git. Commit reproducible source, canonical scenes, project settings, redistributable dependencies, and intentional generated world assets only.
