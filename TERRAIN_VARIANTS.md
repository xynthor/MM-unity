# Terrain variants

Open `Assets/Scenes/World/Enroth.unity` in Unity 6000.5.6f1.

- **Realistic** — `codex/northern-world-style`: stronger landforms with wider foothills and blended joins, retaining each region's climate. Complete legacy architecture sites and road approaches use the preserved canonical ground profile. Generated rock coverage must be rebuilt after terrain changes.
- **Canonical** — `codex/canonical-north-dragon`: legacy ground follows the canonical checkpoint including the user's manual north edits. New north, Dragon Isle and archipelago use lower, broader landforms to fit the legacy world.

Both variants use accumulated snow on existing tree models in snowy areas, including distant LODs. Other biomes retain their vegetation materials. Water meshes, routes and inland water are retained.

## Loading either variant

1. Save your work and close Unity.
2. Run `git fetch origin` in this project folder.
3. Run `git switch codex/northern-world-style` for Realistic or `git switch codex/canonical-north-dragon` for Canonical.
4. Run `git lfs pull`.
5. Reopen the project in Unity Hub, wait for imports, and open the Enroth scene.

Separate clones can also be added to Unity Hub. Each needs its own LFS assets and Library cache.

## Verification

Live Unity MCP checks compare complete architecture footprints against the preserved manual canonical checkpoint, rather than an already modified branch baseline. Rendered previews cover buildings, courtyards, terrain transitions and rock coverage. Numeric edge checks do not certify appearance or complete gameplay traversal.

Local reports and captures are under `Validation/BuildingTerrainRepair20261007/`, `Preview/BuildingTerrainRepair20261007/`, `Validation/TerrainRealism20261007/` and `Preview/TerrainRealism20261007/`.
