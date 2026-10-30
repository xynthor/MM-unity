# Follow-on rock audit and three active props grounded — 2026-10-11

## Scope and absolute preservation rule

User: **“some rocks in the air, mostly on dragon isle; do not move any building part on the ground.”**

Working Unity source feature branch: `codex/canonical-north-dragon` (original commit `908cb5d` already independently verified 7 small Dragon Isle granite rocks and 10 entire isolated rock instances in Dragon South `Inland Outcrops Cell 1`). All new inspection used `Assets/Scenes/World/Enroth.unity` and Unity 6000.5.6f1.

**Architecture protected:** only three explicitly selected, active, small `WorldRock_...` child transforms under `Original Regions Rock Scatter 20260930` were moved in this continuation. No building, building foundation, ruins, bridge, walls, door or other architecture part moved, not even one component. All remaining transforms were independently hashed and preserved.

## Follow-on audits

1. `Assets/Editor/Diagnostics/MMDragonShoreComponentsAudit20261011.cs` inspected **243 connected shore-rock mesh components** in 4 Dragon South shore rock cells. **0 entire components had minimum mesh-vertex ground clearance greater than 0.8m**. The previous coarse bounding-box audit marked two huge grouped mesh cells with apparent +6.7m/+6.8m aggregate gaps, but their components are individually terrain-contacting; do not move these grouped meshes.
2. `Assets/Editor/Diagnostics/MMWorldNaturalRockAudit20261011.cs` extended the read-only natural rock scan outside Dragon Isle: 7,616 rock/building candidate transform nodes, of which 6,698 were conservatively classified as **protected buildings**, and 49 natural-rock-root bounding-box candidates had positive gaps. Of those, **35 `Region Dressing - Reference Matched/Rock_` rocks in four northern extension regions were `activeInHierarchy=False`**. Those intentionally non-rendering reference objects were **NOT moved or activated**, despite apparent gaps of 0.691–121.110m. Another 11 positive gaps came from grouped render roots, marine/shore meshes or mildly raised boulders not proved to float. The remaining **3 clearly active individual `WorldRock_` prefabs** were independently verified and corrected.
3. Three active single-renderer, stone-mesh rock props (not components of any building) were scoped and corrected: `WorldRock_064` at world X≈−1014.10,Z≈754.91 (2.1577m clearance); `WorldRock_069` at X≈−690.80,Z≈756.00 (2.8641m); `WorldRock_075` at X≈−160.11,Z≈756.00 (2.0556m). Each was moved **Y only**, down by 2.2777 / 2.9841 / 2.1756m to a stable **−0.12m** renderer-bottom ground embed. World X/Z, rotation, scale, meshes, materials and ALL other objects remain unchanged.

Source diagnostic CSVs (local, ignored): `Validation/DragonFloatingRocks20261011/{shore_components_review.csv,all_world_non_dragon_rock_candidates.csv}`. Those reports use the current saved world and supersede stale pre-fix raw candidate flags for the three rocks.

## Guarded application and QA

One-time helper `Assets/Editor/Core/MMWorldThreeFloatingRocksKeep20261011.cs` offers read-only reversible `Preview`, guarded `Apply`, and independent fresh-scene `Verify`. Preview same-camera before/candidate PNGs for 064/069/075: `Preview/WorldThreeFloatingRocks20261011/{before,candidate}/`, captured without writing the scene. Preview result `Validation/DragonFloatingRocks20261011/three_world_rock_preview.txt` shows all exact local/world pose changes.

- Source scene SHA256 `c9a1f8c4d41a15f63f13e6bc216f6363b2122ffbc4e78b0e6969fe7a1251a904` (Dragon Isle corrected scene at `908cb5d`).
- Saved new scene SHA256 `afa386f40d89571e9c8a0e7138064c7910f5c64127d3f8bc5d7645931cbd3d30`.
- Full scene backup: `Backups/ThreeNaturalWorldRocksBefore20261011/Enroth.unity` (SHA equals source scene).
- Protected all-other-transform pose SHA256 `0cfc0c715bcf509edcfe38ec7fb007654ec86cf961b1c08818428476174c7595` before and after.
- Independent fresh Unity scene reload `MMWorldThreeFloatingRocksKeep20261011.Verify`: `PASS=True threeWorldRocksGrounded=3 allOtherTransformPosesIdentical=True buildingPartsMoved=0 sceneDirty=False`.
- All older accepted Dragon Isle rock fixes and their original source meshes remain unchanged. Global `MMDragonFloatingRocksFinalQA20261011.Run` separately checks the 7 Dragon Isle natural granite prefab roots, 10 outcrop instances, 30 global terrain components, 49 exact adjacent joins, and 0 mismatched terrain colliders.

**Do not run Apply again or delete its backup/report guard.** Do not ground the inactive 35 reference placeholders or any of the large composite scene/mesh groups. Any new active floating-rock claim elsewhere should be assessed as an individual physical mesh instance, not inferred from a huge parent bounding box.
