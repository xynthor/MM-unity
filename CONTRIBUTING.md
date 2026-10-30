# Contributing

This project is under active reconstruction.

## Project conventions

- Use Unity **6000.5.6f1**.
- Install Git LFS before cloning or committing large tracked assets.
- Keep canonical gameplay scenes in `Assets/Scenes/Regions/`.
- Keep the master world in `Assets/Scenes/World/Enroth.unity`.
- Keep directional/off-map scenes in `Assets/Scenes/Extensions/`.
- Do not commit `Library/`, `Temp/`, `Logs/`, `Preview/`, `Validation/`, backups, extracted original-game data, or unlicensed asset packs.
- Preserve Unity `.meta` files when moving tracked assets.
- Prefer `git mv` for renames so history remains readable.

## Before committing

1. Open the project with Unity 6000.5.6f1.
2. Allow the Asset Database import to finish.
3. Confirm there are no compiler errors.
4. Open `Assets/Scenes/World/Enroth.unity` and perform a short Play Mode smoke test.
5. Check `git status` for accidental generated files.
