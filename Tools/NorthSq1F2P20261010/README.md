# North Square 1 F2P accepted terrain input (2026-10-10)

These two binary float32 LE files are 513 x 513 samples in C# [z,x] row-major order, with Square 1 terrain origin (-768, 768) in world X/Z. Samples are **world-space elevation in meters**, not normalized heightmap values.

- `height.f32`: exact saved local height before F2P. SHA-256 `fc6c7e05b82a2a34505a4340736e8a1c8f9da7311ac02682f0d652fe8f3a5c41`.
- `F2P_rebuild.f32`: accepted heightfield (the F2 mountain shoulders with additional real water-mesh vertex protection). SHA-256 `e24b70ed4c4cc95e0a80ceac4c84f2023639e58ce728ab72cfe7196006ca1c8e`.

The pre-application source scene, terrain and rock mesh were already different from the previous Git HEAD. For this reason the publication history first preserves the nine other pre-existing modified world assets, followed by a **cumulative** commit of the three saved Square 1 scene/terrain/rock assets and their F2P authoring inputs. The cumulative commit includes older, pre-F2P changes already present in those three assets. It is not presented as an isolated delta against the earlier Git HEAD. The script at `Assets/Editor/Core/MMNorthSq1F2PKeep20261010.cs` is a one-time authoring/verification tool, not an at-runtime dependency. It requires the saved baseline terrain, is intentionally non-idempotent, and refuses to run if its local pre-apply backup folder already exists.

F2P modifies only Square 1's TerrainData heights, the linked Square 1 rock mesh and Y positions of existing dressing roots in `Enroth.unity`. Its height borders are exactly unchanged. Scene painting, linked legacy terrain, other extension tiles, water and other rock meshes are not written by the F2P apply script.

Saved live QA from independent Unity process: 30 terrains, 49 terrain joins, 0 mismatches, 0 seams over 1mm; 376 tested water-mesh vertices with 57 pre-existing buried and 0 newly worsened; 12 protected asset SHA checks; 561 existing decoration roots regrounded; successful saved-scene reload. Full local logs in `Validation/NorthSq1F2PKept20261010/` are not committed as production source.

The F2P source heights are preserved here even if the local ignored Validation folder is later cleaned.
