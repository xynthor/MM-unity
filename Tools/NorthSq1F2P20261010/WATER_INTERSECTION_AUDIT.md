# Square 1 coastal water-intersection audit — 2026-10-10

**Status:** read-only audit complete; **no coastline, terrain, water or rock edit accepted**. Retain the approved F2P result at branch `codex/northern-world-style`.

## Findings

The earlier F2P preflight counted **57 water vertices under terrain**, but it did not distinguish intentionally covered shoreline/ocean mesh from visible water defects. The new read-only Unity inspector `Assets/Editor/Diagnostics/MMNorthSq1WaterIntersectionAudit20261010.cs` attributed all cases to **one ocean coast patch**. Its output lives in the local ignored `Validation/NorthSq1WaterIntersection20261010/{audit.txt,vertices.csv}`.

- North inland icy-water mesh: **168** vertices in Square 1, **0** buried; **43** fully-inside-square triangles, all with vertices above the sampled terrain by the audit's criterion.
- Connected ocean / lowland-water mesh at sea level **0.12 m**: **208** vertices in Square 1, **57** more than 0.30 m under terrain; among **54** fully-inside-square triangles, **21** have all three vertices under terrain and **8** have a mix of under/above-terrain vertices. Maximum terrain minus water height **7.249 m**.
- All 57 are in **one** clustered shoreline patch, world X **-764..-680 m**, Z **1178..1212 m**, just north of the western Square 1 edge; none is in the lake/river channel.
- These are **underlying ocean polygons**, not evidence that water is floating over ground or that the inland river is buried. Wholly occluded triangles are not a visible water defect. The eight partial shoreline triangles need not be culled just to make a raw vertex count zero.

Same-camera captures:
`Preview/NorthSq1WaterCoveAudit20261010/{cove_top,cove_seaward,cove_oblique,coast_band}.png` and
`Preview/NorthSq1CoveLayerIsolation20261010/{normal,without_rock,without_ocean}_{top,sea}.png`.

Visual inspection found a **separate** stair-stepped shoreline and a pronounced dark/horizontal terrace on the land surface. The staircase/terrace is still there when the connected ocean renderer is turned off, and changes appearance when the rock-surface renderer is turned off. Therefore simply deleting the 57 underlying ocean vertices would **not** correct this visible coast/rock/terrain treatment. Any aesthetic correction requires a new, bounded rock/coastal-geometry preview, full neighboring seam and water QA, and explicit review before persistence. Avoid any broad sea mesh rebuild or broad north height/alphamap smoothing.

## Preservation and next steps

The saved F2P world was **not changed** by this audit. All diagnostic GameObjects/renderers were restored after temporary screenshots; scene `isDirty=False` and Unity batch exited normally. The baseline global check for the accepted world remains **30 terrains, 49 height joins, zero gaps**. The original **57 buried-vertex metric is reclassified as shoreline-occluded geometry**, not an actionable 57-point inland-water-burial repair.

If revisiting visual coastal quality, start with a **single bounded cove** at the coordinates above; isolate terrain/rock coverage and coastline silhouette, compare before/after from seaward, overhead and gameplay-height cameras, check the full sea mesh outside the region is identical, and reject cosmetic variants that do not visibly improve the scene.
