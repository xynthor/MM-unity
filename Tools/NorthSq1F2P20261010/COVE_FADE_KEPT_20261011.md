# Square 0–1 shared coastal rock fade — saved 2026-10-11

**Status: ACCEPTED local material-only correction, independently verified in Unity.** The remaining *geometry terrace* is explicitly NOT fixed.

## What was changed

- Saved scene: `Assets/Scenes/World/Enroth.unity`.
- Dedicated rock-overlay shader: `Assets/Shaders/MMNorthAlpineCoveFade20261011.shader`, derived from the existing `MMUnity/NorthAlpineSurface20261007` shader. Existing shader is unchanged.
- Dedicated material instances: `Assets/World/WorldExtensions/Generated/NorthCoastFade20261011/Surface0CoveFade.mat`, `Surface1CoveFade.mat`. Both retain their original rock, snow, and normal textures, and use the same special shader.
- Square 0 and Square 1 north alpine rock-surface renderers now reference those new materials. No other rock renderers are changed.
- The shader uses a smooth radial opacity feather **only** around world X=-775, Z=1192, with `_CoveFade=.92`, `_FadeRX=68`, `_FadeRZ=59`. Away from that cove its normal rock surface behavior remains unchanged.

The fade is a **visual overlay only**. It does not alter any TerrainData, height, alpha paint, rock mesh, normal mesh, collider, ice, lake, ocean mesh, shoreline footprint, dressing/root transforms, or the original shared alpine shader/materials. The user-approved F2P heightfield is preserved.

## Confirmed cause, tests and rejected variants

Layer isolation of the earlier *paired-square K2 geometry preview* identified the vertical tile visual seam as coming from **the rock overlay**: with both overlays off the vertical line disappeared, despite identical tile height borders and equivalent painted snow. Rendering only Square 1's overlay exposed a strong straight vertical mask at the tile border.

The paired K2 candidate plus a feathered rock overlay looked much better, but the **water safety check rejected K2's geometry**: it would cover **20 previously exposed ocean mesh vertices** and increase burial at many existing shore vertices. K2 height data was **not** applied, saved, staged or published. Earlier G1/G2/H1/H2/I1-I4/J1/J2/K1/K2 geometry variations likewise remain rejected.

A subsequent preview applied the same rock opacity feather to the **original accepted F2P heights only**. This improved the straight rendering seam from seaward, top, oblique and low cameras while moving **zero terrain samples** or water vertices. The original straight *coastal terrace in the underlying heightfield* is still present; the feather does NOT constitute final natural coastline repair.

## Evidence and recovery

- Scene backup, byte-hash verified: `Backups/NorthSq01CoveFadeBefore20261011/Enroth.unity` (local only); its SHA-256 is `888cff5bfd920e5909c31a537537e0925fc73df7851bfbff2ebf2c5e3b1580f3`.
- Saved scene SHA-256: `e3d60b76a9db8d28bbdf155e7e053fb9177f07329ea467a574b54de17a3a6fe3`.
- Application utility: `Assets/Editor/Core/MMNorthSq01CoveFadeKeep20261011.cs`. Its `Apply` operation is one-time and uses pre-existing path guards. Its `VerifySaved` method is read-only. A post-save bookkeeping exception occurred because the first protected-source verifier enumerated only currently assigned materials; the old materials ceased to appear in that list after assignment. **Inspection showed only `Enroth.unity` dirty**. The verifier was repaired to protect the original material paths explicitly and `FinalizeSaved` completed successfully without resaving or replacing other game assets.
- `Validation/NorthSq01CoveFadeKept20261011/{apply.txt,independent_verify.txt}` (local only): saved and freshly reopened Unity scene `PASS=True`, 30 terrains, 49 adjacent joins, 0 mm worst gap, zero TerrainCollider mismatch, water sample totals 376 vertices, 57 existing ocean-covered vertices, 0 inland-water covered, 0 new coverage. All protected terrain/rock/water assets and original shader/materials were unchanged.
- Same-camera *saved* render: `Preview/NorthSq01CoveFadeSaved20261011/{top,sea,low}.png` (local only), Unity log explicitly `sceneDirty=False`.
- Preview comparison: `Preview/NorthSq01OriginalFade20261011/{baseline,F2P_soft_local,F2P_broad_local}`. The accepted material settings match the `F2P_soft_local` variant, no height differences.

## Remaining distinct issue

The underlying matched Square 0/1 coast has a nearly straight **13.94 m height fall over four meters** at X=-768 between Z1192 and Z1196. The new material feather fixes the vertical *rendering seam* only; it does not remove that shelf, jagged shoreline outline, snow texture repetition, or sea polygon stair stepping. Any actual geometric shoreline revision must be bounded, seam-matched, ocean-coverage safe and visually approved separately. Do not automatically rerun rejected K2 or the one-time saved shader apply.
