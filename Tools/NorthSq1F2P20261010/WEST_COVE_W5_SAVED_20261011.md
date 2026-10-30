# Accepted western Square 0 cove water correction W5 — 2026-10-11

**Status: SAVED, independently reloaded and visually verified** in Unity, following already accepted central cove water patch source commit `35327b1` and natural terrain-slope correction `dbacab4`. The new patch fills a large, formerly bright stepped and exposed submerged shore west of the central cove. **Do not undo or rerun accepted F2P, rock-fade, terrain or original-ocean authoring scripts.**

## Assets and scope

- Saved scene: `Assets/Scenes/World/Enroth.unity`, original SHA-256 `d2c81e6175b9eace805812f2740f55d83c5be227f7be6c02fb62489e08e56086`, new SHA-256 `9b58853a2348969d14dd858eff42794b86d995557609f91dfd918a96ed634f58`.
- New child object `North West Cove Continuous Water Extension 20261011` under existing linked world-ocean object, with identical original water material.
- New independent mesh asset: `Assets/World/WorldExtensions/Generated/NorthWestCoveWater20261011/WestCoveW5Safe.asset`.
- Accepted source coordinate list: `Tools/NorthSq1F2P20261010/Accepted_WestCove_W5Safe_triangles.csv`.
- Mesh: **1,304** constrained triangles, 3,912 independently indexed triangle vertices, **4,541.1777 m²** additional water. Approximately **332.2m original shoreline boundary contact**, **0m² geometric overlap** with original linked global-ocean mesh or existing central V2Safe cove extension. It covers the previous large western missing-water shape in world X=-1016..-812, Z1168..1240.
- There were **zero** changes to the original 28,142-vertex, 8,701-triangle connected sea mesh, the prior 44-triangle central patch, any terrain, ice, water material, rock mesh, forests, southern legacy maps or manual edits.

## Rejected western prototypes and safety correction

The west bay initially showed a **4,740–4,775m²** uncovered seabed footprint. Candidate W1/W2 were constructed from the original ocean polygon and 1m raster undersea topography with Shapely constrained Delaunay and **rejected**: in dense interpolated terrain checks, W1 and W2 would have newly covered rock up to ~**5m above** the water level. Likewise first hazard cutouts W3/W4 remained unsafe (narrow terrain peaks).

The accepted W5 performs **iterated geometry exclusion**: get terrain at triangle vertices, three edge midpoints, centroid, and a 0.25m raster; remove unsafe areas using buffered union of point hazards, retriangulate the clipped continuous polygon. Candidate W5 then had **zero** unsafe triangles/grid samples. It has two logical water-polygon components near the same original bay and no overlap with the ocean. In the dense validation W5's highest sampled terrain remained below -0.42m, versus sea Y=+0.12m. Do not apply W1-W4, nor the original 42-triangle central V2 draft.

## Independent Unity verification

`MMNorthSq01WestWaterKeep20261011.Preflight` passed against exact clean scene SHA, generated **3,912 mesh vertices** and protected **42** existing asset hashes. One-time `Apply` saved the new mesh/scene and passed first QA. Fresh independent batch-mode scene reopen with `Verify` confirmed:

- **30 terrains / 49 exact joins**, worst join 0.000000m, 0 terrain-collider mismatches;
- the original ocean retains **28,142** vertices, remains otherwise hash-invariant;
- **9,128** Unity water-terrain test points (3 vertices + 3 edge midpoints + 1 centroid per new triangle), **0 newly buried**;
- highest saved terrain under new patch **-0.2388m**, lowest -8.5987m, surface at +0.12m;
- all **42 protected source assets unchanged**, including the accepted earlier central `CoveGapV2.asset` and inland icy-water mesh;
- original ice-water mesh explicitly inspected with read-only local `MMNorthSq01InlandWaterFootprintAudit20261011`; **0 triangles in the west patch region**, so no inland-water overlap;
- saved scene clean, exact SHA matches post-apply report.

Evidence (local, ignored): `Validation/NorthSq01WestWaterKept20261011/{apply.txt,independent_verify.txt}` and `Preview/NorthSq01WestWaterSaved20261011/{top,sea,low}.png`, all rendered using freshly reopened saved scene. Earlier preview baseline versus candidate: `Preview/NorthSq01WestWaterSafe20261011/{baseline,W5_iterative_safe}/{top,sea,low}.png`. Both sea and overhead perspectives show the former white underwater shelf replaced by water, without a new straight line. Local backup: `Backups/NorthSq01WestWaterBefore20261011/Enroth.unity`.

## Remaining visual polish versus preserved design

The visible isolated pale floating polygon forms offshore are project-intentional **fractured ice floes** from `Assets/Editor/Core/MMReferenceIceShelf.cs`, object group `Reference Ice Floes - Fractured Shelf`; **do not delete them as if they were water holes**. Their sharp faceting may be a separate future art-polish discussion but is not a flaw in the present marine surface connection. Some snow-albedo periodicity and remote non-cove coastlines also remain outside scope.

The *two known western and central Square 0/1 ocean polygon gap repairs*, plus the accepted U2 terrain slope, are now saved, independently verified and reversible. The west authoring utility uses one-time guards and must NOT be re-run. Future changes require independent local art and water-safety testing.
