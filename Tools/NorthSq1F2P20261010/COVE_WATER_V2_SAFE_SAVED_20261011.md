# Accepted cove water gap correction V2Safe — 2026-10-11

**Saved scene: independently verified and visually inspected.** This is a deliberately small water polygon repair around the already accepted Square 0/1 F2P coast, not a new ocean or terrain generator.

## Physical correction

The saved scene `Assets/Scenes/World/Enroth.unity` now includes `North Cove Seamless Water Extension 20261011` as a child of the existing `Linked connected sea and existing lowland waters 20260929` object. It references a **new** Mesh asset `Assets/World/WorldExtensions/Generated/NorthCoastWater20261011/CoveGapV2.asset`, with the original ocean material. Original ocean Mesh asset, shader, terrain heights, rock meshes, water/ice and vegetation were NOT modified.

The patch adds **44 constrained triangles** (132 mesh vertex entries) spanning **203.824 m²** of missing water at world X approximately -804..-732, Z1194..1210, Y=0.12. The polygon intersects original ocean footprint in **0.000 m²** and joins **88.902 m** of its shared geometric boundary; there is no coplanar surface overlap or stretched original triangle. Its geometry is held in `Tools/NorthSq1F2P20261010/Accepted_CoveGap_V2Safe_triangles.csv`. The source diagnostic generator `Tools/sq01_ocean_gap_constrained_patch_20261011.py` constructs a smoothed water footprint, subtracts the exact ocean triangle union and uses Shapely constrained Delaunay triangulation. The accepted CSV, not the original rejected V1/V2, is the source of truth in the final one-time C# authoring utility.

## Safety issue caught and corrected before approval

The first save used a **rejected 42-triangle** V2 geometry, and a real Unity terrain sampling test caught **2** midpoint samples that were shallowly buried, at world (-744, 1194.0104) and (-768, 1194.0032), with elevations about +0.163/+0.174 m. The batch-run reported an exception after scene save. **This first version was not approved or committed.**

To fix the actual cause, the two long coastal triangles were subdivided and their problematic shoreline midpoint vertices were moved about 0.8/0.7m seawards, creating inward notches and preserving original sea contact. The resulting **44-triangle V2Safe** mesh:
- has zero intersections with the original sea footprint;
- retains **88.902m of contact boundary**, the same as the first version;
- has **0 newly buried** samples out of **308** Unity vertex/midpoint/centroid samples;
- highest sampled ground beneath patch **-0.1831m** (surface at +0.12m);
- lowest sampled ground -4.0769m;
- changes no TerrainData, water/ice mesh or original sea geometry.

The one-time authoring helper `Assets/Editor/Core/MMNorthSq01CoveGapWaterKeep20261011.cs` performed a guarded in-place correction **only** to the new patch mesh asset. Both prior F2P scene and failed first-pass mesh are backed up locally:
`Backups/NorthSq01CoveWaterBefore20261011/{Enroth.unity,RejectedV2WaterMesh.asset}`.
Do not rerun its Apply/RepairSaved methods; safety guards block it after successful saved report.

## Verified post-save state

Independent fresh Unity batch reload via `MMNorthSq01CoveGapWaterKeep20261011.Verify` resulted in `PASS=True`:
- all **30 terrains**, **49** neighboring terrain pairs, worst join 0.000000m, 0 bad TerrainColliders;
- original ocean **28,142** vertices and original triangles unchanged, plus 44 patch triangles;
- **308** new water checks, zero buried samples;
- 41 protected source assets byte-hash identical; no legacy terrain, existing rock, original water, ice or material altered;
- reopened saved scene clean.
- saved scene SHA-256 `d2c81e6175b9eace805812f2740f55d83c5be227f7be6c02fb62489e08e56086` after the new patch; before was `5140f9748b7f6a9fc54886e9ab44cd17485927b88a3f03205d2d331e7534b5e2`.

Audits: `Validation/NorthSq01CoveWaterKept20261011/{apply.txt,independent_verify.txt}` (local ignored). Clean reopened same-camera images: `Preview/NorthSq01CoastalWaterSaved20261011/{top,sea,low}.png`. Before/after comparisons: `Preview/NorthSq01ConstrainedGapWater20261011/{baseline,V1,V2}/` (V1/V2 were prototype shapes, final V2Safe specifically includes two corrected shore notches). The final images visually improve central water coverage and eliminate the large dark polygonal break visible in the previous terrain-only repair.

## Remaining distinct polish

This **central cove water gap** correction does not claim to reconstruct the entire existing ocean polygon network, remove all isolated white coastal rocks, cure material/snow-texture repetition, or cover additional outer-west submerged gaps near world X -900..-810. The original ocean remains the geographic authority. Do not apply global patches or alter legacy coastline merely to fill all below-sea land. Further work must remain local, separately visually reviewed and physically tested. The accepted F2P coastal slope from `dbacab4` remains unchanged.
