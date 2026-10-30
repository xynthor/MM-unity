# Accepted Square 0/1 coastal slope correction — 2026-10-11

**Status:** REAL saved geometry change, independently verified after reopening Enroth scene, using the visual QA approved U2 variant of a tightly bounded two-terrain gaussian treatment. This is not a new northern global terrain build or a repeated historical coastal generator.

## The saved change

- Scene: `Assets/Scenes/World/Enroth.unity`. Only the two Square 0 and Square 1 alpine rock MeshFilter assignments were changed. The preexisting accepted world-space cove rock-fade materials and shader remain intact.
- Extension TerrainData: `Assets/World/WorldExtensions/Generated/NorthReference20261001/{SweetWater_NorthTerrain.asset,Kriegspire_NorthTerrain.asset}`. Exactly **12,808** height samples changed between them, max vertical delta **8.077m**. The Square 0/1 common boundary remains identical, and all external extension borders, including the manually authored legacy southern border, were unchanged.
- New separate rock overlay meshes: `Assets/World/WorldExtensions/Generated/NorthCoveRock20261011/{Surface0CoveSlope.asset,Surface1CoveSlope.asset}`. Recomputed overlay vertex heights and normals to follow the newly saved terrain; the previous accepted `NorthAlpine20261007/Surface0.asset` and `Surface1.asset` rock meshes were not overwritten and remain in Git and on disk.
- Existing ocean, inland icy water, all other 28 TerrainData assets, all rock shader/materials, alphamaps, vegetation and legacy maps were preserved.

## Why and what this actually solves

Two previously executed October 6 historical height-build scripts explicitly inserted hard `ny=-7.5f` seaward of their procedural `coast` transition. This created a 13.94m sudden cliff over 4m at world X=-768/Z1192..1196. A large series of alternate preview-only artificial embankments, fins, bowls and water-polygon patches were rejected after direct Unity visual QA.

The selected U2 correction blurs only that coast-facing 2D height discontinuity in an X/Z bounded mask derived from the original steepest drop, with an 8m north-south, 6m east-west Gaussian. Unlike previous broad cliff reconstruction it keeps the original macro mountain geometry and smooths the raw land-to-seabed face rather than constructing a new artificial bowl or geometrically stretching water triangles. It retains the originally accepted local rock-opacity transition. Source of deterministic U2 sample arrays (local, not Git): `Tools/sq01_coastal_natural_gaussian_20261011.py` and `Validation/NorthSq01CoastSeam20261011/U2_sq{0,1}.f32`.

## Preflight, saved scene and independent QA

- Verified preflight (clean F2P before saving) and post-save independent reopening in a separate Unity batch process:
  `Validation/NorthSq01NaturalCoastKept20261011/{apply.txt,independent_verify.txt}` (local ignored).
- `PASS=True`, **30 terrains / 49 adjacent terrain pairs**, zero bad seam samples, **0.000000 m worst gap**, zero TerrainCollider data mismatch.
- **3,984** water vertices across both cove extension squares tested: original **471** under elevated terrain, after correction **463**, **0 newly covered ocean or inland-water vertices**, and **0 inland-water coverage**. The physically unchanged water polygons were not stretched, supplemented or cut.
- Saved scene SHA-256: `5140f9748b7f6a9fc54886e9ab44cd17485927b88a3f03205d2d331e7534b5e2`.
- Saved scene before-correction SHA-256: `e3d60b76a9db8d28bbdf155e7e053fb9177f07329ea467a574b54de17a3a6fe3`.
- Local original scene + both TerrainData byte-preserving backups: `Backups/NorthSq01NaturalCoastBefore20261011/{Enroth.unity,Terrain0.asset,Terrain1.asset}`.
- Exactly **0 vegetation / rock dressing objects moved**, and 37 explicitly protected other assets passed SHA-256 validation.
- Freshly reopened saved scene matched-camera captures `Preview/NorthSq01NaturalCoastSaved20261011/{top,sea,low}.png` (local ignored) rendered in Unity with `sceneDirty=False`. Before/after clone-only visual comparison `Preview/NorthSq01NaturalGaussian20261011/{baseline,U1,U2}/{top,sea,oblique,low}.png`.

## Scoped limitations

The previous unnatural hard 13.94m near-vertical wall is materially softened into a terrain-following shoreline slope; **however the existing connected ocean mesh still has stepped shoreline polygons and some exposed low shore fragments**. These polygon edges are a separate water-topology issue, and **have NOT been claimed fixed**. The original source-water assets were deliberately preserved after numerous earlier unsafe mesh-stretch and detached patch experiments. Fixing that remaining detail requires a independently reviewed, topologically continuous mesh reconstruction, not blind replacement of the worldwide ocean mesh.

Never rerun the one-time apply automatically; the script checks its backup and new mesh asset folder guards. Use read-only `Verify` to re-check saved art/physical invariants. The historical global/legacy source remains protected.
