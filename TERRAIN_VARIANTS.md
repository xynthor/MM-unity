# Terrain variants

Both branches load `Assets/Scenes/World/Enroth.unity` in Unity 6000.5.6f1.

- `codex/canonical-north-dragon`: broader, lower, blunter mountain forms on the four northern extension terrains and Dragon Isle. The other canonical terrain regions retain their existing shapes.
- `codex/northern-world-style`: extends northern ridge, gully and exposed-rock treatment across the world's land regions. Existing grass, desert, volcanic and snow biomes remain; this is not an all-snow repaint. The northern extension is also refined with softer foothills, irregular faces and fewer narrow spikes.

Road paint and shoulders, settlement foundations, shore heights and inland water banks are protected. World-space blending smooths terrain slopes across tile boundaries while keeping adjacent edge heights identical. Existing roads and river routes are retained rather than rebuilt.

## Switch in the same checkout

1. Save your own work and close Unity.
2. In this project folder run `git fetch origin`.
3. Run either `git switch codex/canonical-north-dragon` or `git switch codex/northern-world-style`.
4. Run `git lfs pull`.
5. Open this project in Unity Hub, wait for imports, and open the Enroth scene above.

Separate clones can also be added to Unity Hub for opening the two versions independently. Each clone needs its own `git lfs pull` and Library cache.

## Verification on 2026-10-07

Both variants were checked in the live Unity editor through MCP and in Play Mode. All 49 terrain edge joins matched. On edited tiles the protected road heights, shore heights and terrain paint were unchanged. Northern-world checks additionally covered 328 building footprints and inland water bank buffers, with zero terrain height change. Checked authored trees had no floating footing; missing script, material/shader and mesh counts were zero. Water reflection remained enabled at 768 resolution with no water shader errors.

These are terrain variants, not a complete replacement of legacy road geometry or a full gameplay traversal certification.

2026-10-07 stronger geometry and transition revision: northern style means more pronounced hills, valleys and ridges while preserving every original biome, terrain layer and vegetation family. Canonical north and Dragon Isle use simpler, lower, broader landforms.

Northern transition refinement: world-space foothill smoothing and bounded face variation reduce tile-border slope changes; all 49 joins remain exact. Shared boundary heights may move together to remove the former creases. Original biome layer paths and alphamaps remain identical on all 30 terrains; road and shore heights, 328 building footprints and inland banks remain unchanged. 318 generated conifers on slopes above 38 degrees are disabled in place. Final captures and reports are local under NorthernStrong and SeamBlend. Live Play Mode reflection check passed at resolution 768.

The authoring helper refreshes native mesh GPU buffers explicitly after serialized copies and includes authored boulders in terrain-following dressing. The saved northern geometry already receives an explicit mesh vertex refresh in the seam pass.
