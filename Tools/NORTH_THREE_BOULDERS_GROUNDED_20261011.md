# Northern production small boulder grounding — 2026-10-11

**User preservation requirement:** never move any building part, building/foundation/ground-level architecture. This repair moved ONLY three individually identified, live **natural boulder** `GameObject` roots by world **Y**. All non-target transforms, including 100% of buildings, were stable-hash checked before/after. No terrain, water, authored rock mesh, boulder collider mesh, original shader/material, or vegetation changed.

## Scope and diagnosis

Current source branch: `codex/canonical-north-dragon`, immediately after accepted `203294a` rock grounding. Original scene path `Assets/Scenes/World/Enroth.unity`, SHA-256 `afa386f40d89571e9c8a0e7138064c7910f5c64127d3f8bc5d7645931cbd3d30`.

Source all-world scan flagged two small active `North Production Boulder Dressing 20261007` props with render-bound grounding gap about 0.68 and 0.77m. A more precise **per-vertex** read-only audit of **all 46** `AuthoredBoulder_*` meshes found **three actual wholly airborne boulders** (minimum world-vertex-to-terrain gaps, measured at every vertex), not two. Corrected targets:

| Rock root | Pre-ground min vertex gap | Y-only downward move | Post-ground min vertex gap |
|---|---:|---:|---:|
| `AuthoredBoulder_5` | +0.3493m | 0.4693m | -0.1200m |
| `AuthoredBoulder_24` | +0.5386m | 0.6586m | -0.1200m |
| `AuthoredBoulder_37` | +0.6609m | 0.7809m | -0.1200m |

All three are active, small isolated visual `Rock_A_02/03_LOD00` imported boulder meshes, each with a dedicated `MeshCollider` using the same mesh and the `NorthColdBoulder20261007` natural-rock material. No building/ruin/architectural colliders were targeted.

**After saving and independently reopening**, rerun `MMNorthSmallBoulderFootAudit20261011.Run` across all 46 authored boulders yielded **maximum** minimum terrain clearance = **-0.0445m**, and **zero** boulders with a >0.20m floating gap. All three newly grounded at -0.1200m. Results `Validation/DragonFloatingRocks20261011/north_boulder_foot_audit.csv` (local ignored). The other 43 boulder positions and mesh assets unchanged.

## Verification and preservation

- One-time guarded source authoring script: `Assets/Editor/Core/MMThreeNorthBouldersKeep20261011.cs`. Read-only diagnostic `Assets/Editor/Diagnostics/MMNorthSmallBoulderFootAudit20261011.cs`.
- Reversible BEFORE/CANDIDATE images, three cameras each: `Preview/NorthBoulders20261011/{before,candidate}/AuthoredBoulder_{5,24,37}.png` (local ignored). Candidate geometry improved base/terrain contact and was captured in memory; preview scene never saved.
- Original-scene backup: `Backups/NorthBouldersBefore20261011/Enroth.unity`, protected by original SHA.
- Saved new scene SHA-256 `5cf6148d16be167469a13f9859b5ed622459a9a6de7cf744c1999c05b3707acc`.
- All non-target transforms SHA256 `fb7da9f272e67f3900e376d921b34a0ffa186abec51f8b7ba4fbfa00b1e82154` is unchanged, including all architecture.
- Unity 6000.5.6f1 fresh scene reopen via `MMThreeNorthBouldersKeep20261011.Verify` PASS: groundedBoulders=3, all other poses invariant, buildingPartsMoved=0, clean scene.
- Existing `MMDragonFloatingRocksFinalQA20261011.Run` rerun PASS: **30 terrains / 49 exact joins**, 0 collider mismatches, previous Dragon Isle 7 individual granites and 10 whole outcrops still grounded, zero building movement.
- Rerun read-only 46-boulder vertex audit after save PASS: all genuinely resting on terrain (0 >20cm gaps).
- Saved source evidence: `Validation/NorthBoulderGrounding20261011/{preview.txt,kept.txt,independent_verify.txt}`, Unity logs `Logs/ThreeNorthBoulder{Preview,Apply,IndependentVerify}20261011.log`, `Logs/NorthBoulderGlobalRegression20261011.log`, `Logs/NorthSmallBoulderFootPostSave20261011.log`.

**Critical exclusions:** Previously audited 35 seemingly floating `Rock_*` objects in inactive reference dressing remain inactive and untouched; the original Dragon South shore-mesh audit of 243 components found them individually grounded, so group roots MUST NOT be moved. Remaining complex aggregations/building parts MUST NOT be automatically snap-to-ground corrected. No terrain height sculpting was done. Do not re-run one-time `Apply`; a directory backup guard prevents a second application. This is rock-polish QA, NOT overall world gameplay completion.
