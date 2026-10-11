# Square 0/1 coast water footprint and safe-shore diagnostics — 2026-10-11

## Current production state

**Saved and pushed at source feature branch `codex/northern-world-style@b57e0e2`**: accepted F2P elevation and the safer localized rock-overlay material fade. Terrain, rock geometry, ice, lake, and ocean remain in their accepted state. This investigation made **no production changes**. All tools here are read-only or transient Unity clones. All previews below are REJECTED and not suitable for production.

## Coast geometry versus ocean footprint

The straight artificial shelf spans the matching Square 0/Square 1 border world X=-768. At world Z1192 the saved height is +6.44 m and at Z1196 it is -7.50 m, a 13.94 m drop across 4 m. This is an underlying terrain geometry issue, distinct from the now-mitigated vertical rock *rendering* seam.

A new independent read-only ocean mesh inspector `Assets/Editor/Diagnostics/MMNorthSq01OceanFootprintAudit20261011.cs` exported triangle positions from the **original connected ocean mesh** under `Validation/NorthSq01OceanFootprint20261011/triangles.csv`. Full mesh: **28,142 vertices, 8,701 triangles**, with **118 triangles** intersecting the bounded region world X -900..-630, Z1120..1260. The mesh itself was not modified. A local 1m footprint raster compared the triangles with the current saved two-tile heightfields. It found about **692 m²** of below-sea-level terrain not actually covered by a water triangle within that broad cropped region. Of these, **607 m²** were in the targeted contiguous near-ocean shoreline gap mask bounded to X -864..-680, Z1160..1240. The true sea-mesh footprint and the heightfield shoreline do not perfectly coincide.

Local evidence: `Validation/NorthSq01OceanFootprint20261011/{audit.txt,triangles.csv,footprint_top.png,ocean_gap_patch_mask_25.png}`. Render-layer context: the existing ocean uses `MMUnity/Linked Natural Water` and global Y≈0.12m.

## Unsafe or artistically unacceptable candidate tests (all REJECTED, preview only)

1. **M1/M2/M3 one-sided erosion of the saved paired terrain**. Working in a narrow cross-square coast mask, taking only downward displacement above ~0.8m kept 0 new ocean vertices buried, 0 external border changes, exact shared Sq0/Sq1 heights. M1 changed approximately 8,312 height samples, max 3.38m lowering; M2 8,598, max 5.07m lowering. **Visual comparison:** the straight shoreline terrace remained prominent; only mild shading changes resulted. None was saved. The source height arrays and 1m water vertex locations are unchanged. Local `Preview/NorthSq01WaterSafeErosion20261011/{baseline,M1,M2}` has matched top/sea/low/oblique camera captures. The candidates are `Validation/NorthSq01CoastSeam20261011/M1_sq*.f32`, `M2_sq*.f32`, all ignored/local only.

2. **Separate supplemental ocean water grid mesh**. A 1m raster selected 607 uncovered submerged shoreline cells; a transient GameObject with 1,214 triangles using the same ocean water material was rendered at sea Y≈0.14. It produced visible new broken white shore edges, discontinuous turquoise strips and staircase aliasing, especially seaward and overhead. **Rejected.** Existing connected ocean mesh untouched, GameObject/mesh destroyed and scene remained clean. Local `Preview/NorthSq01OceanGapPatch20261011/{baseline,patch}`.

3. **Stretch existing ocean-mesh boundary vertices inland**. A clone of the existing 28,142-vertex ocean mesh was made in memory, and only cove boundary vertices were shifted toward underwater terrain. Max permitted 9m moved 60 vertices (mean shift ~4.99m); max 18m moved the same 60 (mean shift ~5.97m, largest ~16.5m). This produced long streaking/overlapping stretched triangles and obviously unacceptable water stripes. **Rejected.** All original vertices and triangles were restored, no scene or mesh persistence. Local `Preview/NorthSq01OceanEdgeStretch20261011/{baseline,extended_9,extended_18}` and `Validation/NorthSq01OceanEdgeStretch20261011/preview.txt`.

All three test methods demonstrate why raw vertex counts or simply extending the waterline are not sufficient. The supplemental grid fails continuity, and moving only old border vertices distorts inherited triangulation. The F2P and saved rock material fade are still approved, independently validated, committed and pushed. DO NOT run or apply any of these rejected test methods to production.

## Recommended next bounded experiment

If addressing the actual shoreline, build a **single integrated, triangulated replacement for only the affected water polygon portion** plus smooth coastal terrain, preserving the original ocean mesh everywhere else, with shared boundary vertices and no water overlap/Z-fighting. Prefer to reconstruct an irregular 2D coastline contour with a continuous triangle surface rather than detached 1m cells or extended long polygons. Independently compare water coverage, original neighboring 30 terrains/49 seams, all legacy terrain assets, current ice/lake, and top/sea/low/oblique art before saving. An alternative could retain existing water exactly and make a purely local natural rock-cliff face mask, but not procedural rectangular trimming.

**No new production commit should be made until a candidate visibly improves the coast and passes those checks.**


## Subsequent connected water-strip preview, also NOT accepted

A more promising third water approach cloned the existing ocean mesh **without moving any of its original 28,142 vertices**. Along 48 eligible coastal boundary edges only, it appended short triangulated strips at roughly 1.5m increments:
- 9m maximum boundary extension: **392 appended triangles**, average local edge extension 5.80m;
- 16m maximum: **480 appended triangles**, average extension 7.01m.

This improved continuity from the seaward view compared with both the detached 1m supplemental grid and the severe original-boundary stretching. However the **overhead view still shows dark abrupt polygonal cutoffs**, and the original long straight rock/terrain shelf remains unchanged. It was **NOT accepted** as the user's requested realistic coastline. Neither the original sea mesh asset nor the scene was saved; the clone was destroyed, and Unity reported `sceneDirty=False`.

Local-only diagnostic `Assets/Editor/Diagnostics/MMNorthSq01OceanConnectedStripPreview20261011.cs`; same-camera images `Preview/NorthSq01OceanConnectedStrip20261011/{baseline,strip_9,strip_16}/{top,sea,low}.png`, report `Validation/NorthSq01OceanConnectedStrip20261011/preview.txt`. Do not run the candidate as a production apply.
