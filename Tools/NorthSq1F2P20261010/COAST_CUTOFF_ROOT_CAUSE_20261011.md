# Northern Square 0/1 cove: verified source-level hard-cutoff cause — 2026-10-11

**Production status:** user-approved Square 1 F2P and the independently QA'd Square 0/1 rock-overlay material feather are still the accepted scene. The shared northern cove's artificial geometric shelf is **NOT repaired**. This document is a diagnosis, **not approval** to rerun historical generators or apply any of the rejected preview arrays.

## Cause established from executed generator source

Two historical one-shot north *extension-only* terrain height tools used the same exact construction at the coastline:

- `Assets/Editor/Core/MMNorthManualEdgeHeightRebuild20261006.cs`, the first manual-legacy-edge north rebuild: in its last coastal pass `if(z>coast) ny=-7.5f;` following a shelf / bluff profile.
- `Assets/Editor/Core/MMNorthManualEdgeCliffRefine20261006.cs`, a subsequent coastal cliff refinement: also `if(z>coast) ny=-7.5f;` while blending `toeY≈1.8m` to a 18–38m high shelf via `cliffW≈13–34m`.

Their **actual execution reports are present**:
- `Validation/EdgeGrid20260923/NorthManualEdgeHeight20261006/apply.txt`: `PASS`, 1,306,327 extension height samples changed; 4 extension terrains; the manually authored legacy southern neighbor remained hash-invariant.
- `Validation/EdgeGrid20260923/NorthManualEdgeHeight20261006/cliff_refine.txt`: `PASS`, 207,787 more height samples changed; no edits to legacy TerrainData or alphamaps.

Independent October 11 read-only measurements of the **current accepted scene** match that construction at the Square 0/1 north coastline: shared exact height at X=-768, Z1192 = **+6.44m**, Z1196 = **-7.50m**, a **13.94m drop in 4m**. A broad band of nearby X samples crosses into the uniform -7.5m seabed at the almost exactly same Z=1194/1195 row. This is a manually/programmatically imposed *cliff-to-constant-seabed discontinuity*, not merely different snow layers or a broken mesh normal. The material feather from `b57e0e2` resolves the sharp **alpine rock-overlay seam** but does not physically reshape the cliff.

**Important preservation:** these already-executed historical `Run()` tools refuse repetition by checking report files. Their input legacy terrain was manually authored and is protected. Do **NOT** delete their reports, rerun them, reset the F2P source, or modify the manual legacy south-edge in an attempt to fix the cove.

## Further preview experiments on 2026-10-11, none saved

Using *temporary cloned* paired 513x513 TerrainData and rock meshes in Unity, the original F2P coast was compared with a set of cross-square candidate alterations. Every variant held the Square 0/1 joint height exactly at 0m mismatch and preserved the external edges, while the saved scene remained clean (`sceneDirty=False`).

1. `N1/N3`: bounded, irregular profile monotonic capping of only the upward coastal rim, 1,361/1,306 changed height samples and 5.36/4.03m maximum change. It shortened the artificial upper lip but left the ocean-facing very steep straight shelf and was not accepted.
2. `P1/P2`: nonlinear geometric bank shape using the connected ocean mesh footprint and coastal signed distance; a smoother cliff but an overly broad, rounded amphitheatre/bowl in seaward/top views. Water-vertex test found **2 new covered sea vertices** and many cases of shallowing; rejected.
3. `P3/P4`: reduced and locally corrected P1/P2, successfully reduced the *sampled newly buried ocean vertices to 0*, and maintained 0 outer-edge changes. However both still created a visually unnatural symmetrical coastal basin and preserved remaining straight seams; rejected.
4. `R1/R2`: added bathymetric uplift under existing ocean polygons and filled underwater bare-land gaps to smooth the land/sea transition. **0 newly buried ocean samples**, but formed new sharp and sawtooth white ridges at the mask boundary; rejected.
5. `S1/S2`: screened Gaussian smoothing plus iterative protection of original water vertex locations. **0 newly covered ocean vertices**, but the stepped cove shore and long shadow/shelf remained prominent overhead, and new coast white edges were visible; rejected.
6. `T1/T2`: original-source cliff reprofile with 25–32m inland anchor and a gradual nearshore transition, using the *first sustained wet run* of original ocean triangles to position the shore. It removed much of the flat face but generated vertical fins where adjacent x-columns had different detected shoreline; also produced **1 newly covered sea vertex** in the audit. Rejected.
7. `T3/T4`: explicitly median/gaussian-smoothed the detected shoreline path before gentler reprofile (5–6m maximum height change). Vertical fins were reduced, but an irregular bright horizontal shelf and obvious polygonal edges persisted, and **1 new ocean vertex** became buried. Rejected.

All these scripts and their candidate binary height arrays are local diagnostics under `C:\MMUnityPort\Tools\sq01_*_20261011.py`, `Assets/Editor/Diagnostics/MMNorthSq01*Preview20261011.cs`, `Validation/NorthSq01CoastSeam20261011/`, and matching ignored `Preview/NorthSq01*20261011/` folders. The original production assets and current saved scene were **never** written by any of these preview runners.

## Design correction still required

A safe finished correction must explicitly reconcile the **historical hard -7.5m cut** with a new physical coast design and the **actual coastal ocean polygon topology**. Merely interpolating height grids yields water gaps or a rounded artificial basin. Thin appended water strips leave harsh overhead edges. The next properly scoped production design should reconstruct both terrain and adjacent water **together as one coordinated cove**, preserving the external 30-terrain grid seams, the user's manually authored legacy neighbors and all inland water, forests and F2P peaks. It requires visual top/sea/oblique/gameplay QA AND true mesh triangle/water-coverage checks, not just sampled vertex coverage. Only then consider accepting and persisting a new terrain+water asset.

There is **no new accepted coastal geometry state** as of this note. The last accepted world change is still the material-only rock fade `b57e0e2`. Audit-only source history may have subsequent documentation commits. Do not claim this issue is fixed.
