# Dragon Isle floating natural rocks — guarded repair, 2026-10-11

## User preservation instruction

**DO NOT MOVE ANY BUILDING PART ON THE GROUND.** The corrective passes touched **only seven small natural granite GameObject root transforms**, plus **only the geometry of ten separate natural rock instances inside one independently cloned outcrop mesh**. No building, ruin, landmark, structure or architecture parent transform moved. No other Transform, TerrainData, Collider, terrain source mesh, water, road or building mesh changed.

Source project: `C:\MMUnityPort`, active feature branch `codex/canonical-north-dragon`. This is a different, later canonical world branch than earlier `codex/northern-world-style` coastal polish. All inspections used `Assets/Scenes/World/Enroth.unity` in Unity 6000.5.6f1, and all preview processes were read-only with regard to source scene files.

## Confirmed and corrected defects

### Seven small granite prefab instances

Five individually suspended `Rock_Granite_rcCwC` natural props under `Dragon Lair - Eastern Ridge` in Dragon Isle North had rendering-bounds/terrain gaps of **1.439m, 3.587m, 5.626m, 6.021m and 10.091m**. Two similar natural granite props under `Vampire Ruins - SW Reference` in Dragon Isle South had gaps of **45.752m and 55.188m**. The rocks were each individually lowered vertically until their rendered bottom was embedded `0.12m` into the current saved ground directly beneath their center. All horizontal coordinates, rotations, scales and meshes were preserved. **The landmark and building parents stayed motionless.**

One-time guarded code: `Assets/Editor/Core/MMDragonGraniteGrounding20261011.cs`. Local backup of scene **before the seven positions changed:** `Backups/DragonGraniteGroundingBefore20261011/Enroth.unity`. Original scene SHA256 `51a9b7122621cb893ccc91703a9b2891fd0c30d5e3685ff483fc6b4c0ae1d595`; saved scene SHA256 `3ef15aa4d8690039ff6618ce40db235fb55a95513ea01fa4f6f87af3da0a7bde`.

An all-object, all-scene protected transform-pose SHA check (all Transforms except the seven targeted rock subtrees) remained **byte-identical**, including *every* building part. Pre-apply same-camera renders and one-time dry-run are under `Preview/DragonFloatingRocks20261011/{before,candidate}/`. Fresh independent reopen `Validation/DragonFloatingRocks20261011/independent_verify.txt` states **7 grounded, buildingPartsMoved=0, other pose invariant, scene clean**.

### Ten previously floating large inland outcrops

A more discriminating mesh-vertex audit inspected 14 Dragon/peripheral stone meshes. It found `Inland Outcrops Cell 1` under `Dragon South Reference Inland Outcrops 20260930` contained **18 separate authored natural rock instances**, of which **10 entire rock instances** were above current terrain by 1.3–30.4 metres even at their lowest mesh vertex. Several outcrops appeared visibly floating in the south ridge screenshot, up to around 35.4m above land in individual vertex samples.

The original mesh `Assets/World/WorldExtensions/Generated/DragonSouthInlandOutcrops20260930/Cell_1.asset` was backed by SHA256 `0120e636981eeed20c6deba350b81593871198c83ddb0dc928cac967b3740fbe` and **not edited**. Each actual rock instance's topology was reconstructed from groups of 5 or 2 connected submesh components. Exactly **10 complete instances / 3,726 mesh vertices** were shifted **rigidly** downward (maximum 30.5903m), to put their lowest ground clearance at `-0.18m` without bending, distorting or deleting any part of the shape. **The remaining eight partly grounded instances were left vertex-identical.** This changes vertex coordinates in a **new** generated Mesh asset only; the source object, its transform, buildings, terrain, colliders, and all other assets are untouched.

New asset: `Assets/World/WorldExtensions/Generated/DragonGroundedOutcrops20261011/Cell_1_Grounded.asset`, linked from the preexisting `Inland Outcrops Cell 1` MeshFilter in Enroth scene. Source one-time guarded code `Assets/Editor/Core/MMDragonGroundOutcropMeshKeep20261011.cs`. Backup scene **before the second mesh correction:** `Backups/DragonOutcropGroundingBefore20261011/Enroth.unity` (SHA256 `3ef15aa4d8690039ff6618ce40db235fb55a95513ea01fa4f6f87af3da0a7bde`). Post-save scene SHA256 `c9a1f8c4d41a15f63f13e6bc216f6363b2122ffbc4e78b0e6969fe7a1251a904`. Preview `Preview/DragonFloatingRocks20261011/outcrop_{before,candidate}/` includes top/side/oblique screenshots directly comparing the formerly hanging 30m stones to the newly grounded outcrops.

Separate Unity fresh scene reload `Validation/DragonFloatingRocks20261011/outcrop_independent_verify.txt` confirmed the 10 grounded instances, other 8 unchanged, all transform positions/rotations/scales identical, original Mesh asset SHA unchanged, and no building parts moved.

## Important diagnostic limits

The remaining large `Shore Rocks Cell` meshes and `Inland Outcrops Cell 2/3` have vertices elevated a little above the terrain, but their **original mesh components connect to or penetrate the terrain**; these were NOT moved as entire cells or globally flattened. `DragonRockSurface0/1` original material overlays both conform closely to the terrain (0.18–0.20m expected render separation); untouched.

Full original read-only diagnostic data `Validation/DragonFloatingRocks20261011/{all_rock_and_building_candidates.csv,shore_mesh_vertex_audit.csv,outcrop_components.csv}`; source inspection C# tools under `Assets/Editor/Diagnostics/MMDragon*20261011.cs`. No global automatic rock snap, terrain rebuild, building reposition, scene reset, or source prefab modification.

Final check `Assets/Editor/Diagnostics/MMDragonFloatingRocksFinalQA20261011.cs` verifies the seven small rocks, ten large outcrops, all 30 terrains/49 joins and unchanged colliders. This local repair does **not** certify every other world region visually flawless. Historical generators should not be rerun.
