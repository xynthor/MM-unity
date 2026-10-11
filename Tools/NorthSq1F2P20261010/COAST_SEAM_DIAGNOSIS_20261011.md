# Square 0 / Square 1 shared cove investigation — 2026-10-11

**Status: the root cause is localized; no proposed repair has been accepted or saved.**

The user requested continued improvement of the accepted F2P northern mountain world. The visually abrupt shore at world X ~ -768, Z ~1190 spans the border between northern extension **Square 0 (SweetWater)** and **Square 1 (Kriegspire)**.

## Read-only discovery

The read-only Unity export routine `Assets/Editor/Diagnostics/MMNorthSq01CoastSeamExport20261011.cs` opened the saved scene and exported both actual 513×513 terrain heightfields into ignored local `Validation/NorthSq01CoastSeam20261011/`. The saved source was on branch `codex/northern-world-style` at `f72d71d`, and the working tree was clean.

On the exact shared tile seam world X = **-768**:
- World Z=1184: 1.56 m
- World Z=1188: 2.83 m
- World Z=1192: 6.44 m
- World Z=1196: -7.50 m
- World Z=1200: -7.50 m

The **13.94 m drop in 4 m** (1192→1196) is present identically in both saved tiles. Square 0 at X=-776 shows a similar 13.73 m drop and Square 1 at X=-760 a 13.87 m drop. Consequently, preserving the shared border while altering only Square 1 cannot remove the long straight coastal terrace. This is a cross-tile problem, not a buried-inland-water problem.

## Preview experiments, all rejected

- Broad Gaussian + isotonic smoothing (G1/G2) generated ripple rings, preserved part of the spike.
- Downhill-only cove smoothing (H1/H2) removed some height discontinuities but exposed inland turquoise water patches behind the shoreline.
- Shore-filling PCHIP profiles (I1-I4) replaced the cliff with a rectangular/polygonal embankment or left the spike.
- Irregular Poisson masks restricted to Square 1 (J1/J2) generated new humps and unnatural polygonal plateaux because the defect continues through its fixed west boundary.
- Seam-aware Poisson patches across **both extension terrains** (K1/K2), using a shared stitched height grid, achieved exactly zero shared-seam mismatch and zero change at the external square borders. K1 temporarily changed 4,560 samples (max 17.386 m); K2 6,408 samples (max 20.319 m). Both rebuilt temporary rock meshes and rendered paired overhead, oblique, low and sea views. K1 still formed unnatural terraces, while K2 made a more gradual coast but revealed a sharply rectangular/vertical tile surface boundary.
- A separate K2 rock-normal stitching preview set identical calculated vertex normals on both sides of the joint; the visible rectangular surface boundary **did not disappear**. Terrain-material/layer coverage, edge geometry and transparent overlay appearance require further investigation. This is not proven to be solely a normal mismatch.

The tested `TerrainData` and rock mesh modifications were **temporary clones only**. The original terrain, sea, lake, river, materials, alphamaps, vegetation, scene, and legacy assets were not saved or overwritten. The Unity preview routines completed with `sceneDirty=False`. **Do not apply any of these rejected candidate arrays as production terrain.**

## Local preview / report paths

- `Preview/NorthSq1CoveHeightRepair20261011/`: baseline and downhill-only previews.
- `Preview/NorthSq1CoveCoastFill20261011/`: shore-filling previews.
- `Preview/NorthSq1CoveInpaint20261011/`: Square 1 irregular fill previews.
- `Preview/NorthSq01CoveSeam20261011/`: paired K1/K2 previews.
- `Preview/NorthSq01CoveSeamNormals20261011/`: matched-normals K2 comparison.
- `Validation/NorthSq01CoastSeam20261011/{audit.txt,preview.txt}`: exact terrain profiles and temporary seam invariants.
- `Validation/NorthSq1CoveProfileRepair20261011/`: rejected single-square candidate height arrays and reports.

These ignored local outputs are *not* portable committed game assets. The source commit contains only this investigation note and the read-only height export utility.

## Next correct work

Do a bounded **two-tile coastline material/rock overlay seam audit** before any more height sculpting. Compare the terrain layer/alphamap edge and the two rock materials/triangulation over X≈-850..-680, Z≈1165..1225. Isolate the precise cause of the rectangular line by rendering both tile overlays jointly disabled and inspecting adjacent painted layers. Then design an integrated **geometry + material transition** (not a rectangular mask, not hard triangle deletion), with external borders preserved and inland water kept intact. Require clear improvement in joint top, sea, low and oblique previews before ever saving. A numerical zero seam alone is not artistic approval.

The accepted F2P source remains published on the private feature branch and is not to be reverted or regenerated.
