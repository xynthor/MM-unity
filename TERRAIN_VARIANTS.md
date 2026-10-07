# Terrain variants

Open `Assets/Scenes/World/Enroth.unity` in Unity 6000.5.6f1.

- **Realistic** — `codex/northern-world-style`: stronger hills, valleys and ridges across the world, now with wider foothills, rounded tile joins and conservative talus smoothing. Existing grass, desert, volcanic and snow regions remain. The branch name is retained for existing checkouts.
- **Canonical** — `codex/canonical-north-dragon`: lower, broader, blunter northern and Dragon Isle landforms. The latest north refinement simplifies the four northern extension terrains further; other canonical regions retain their shapes.

Both variants use snow-covered versions of existing tree materials where the ground is snowy, including distant LODs. Branch and trunk snow accumulation preserves the original models and alpha coverage. Trees in other climates retain their materials.

Realistic transitions blend snow paint across neighboring tiles as well as terrain slopes. Terrain layer paths and road-layer weights are preserved; snow alphamaps intentionally change within the transition strips. Canonical terrain paint is retained. Existing road routes, protected road heights, shoreline heights, settlement foundations and inland banks are preserved. Shared realistic boundary heights may move together; adjacent edges remain identical.

## Loading either variant

1. Save your work and close Unity.
2. Run `git fetch origin` in this project folder.
3. Run `git switch codex/northern-world-style` for Realistic or `git switch codex/canonical-north-dragon` for Canonical.
4. Run `git lfs pull`.
5. Reopen the project in Unity Hub, wait for imports, and open the Enroth scene.

Separate clones can also be added to Unity Hub. Each needs its own LFS assets and Library cache.

## Verification

The passes are checked through live Unity MCP, rendered comparisons and Play Mode shader/water checks. Reports and checked captures for the latest revision are local under `Validation/TerrainRealism20261007/` and `Preview/TerrainRealism20261007/`. These checks cover terrain joins, protected road/shore heights, inland banks, foundations, material/shader references, colliders and authored tree footing. Existing roads and rivers are retained; this is not a complete gameplay traversal certification.
