from pathlib import Path
import shutil

root = Path(r"C:\MMUnityPort")
archive = root / "Backups" / "LegacyCombinedCleanup_20260915_1845"
archive.mkdir(parents=True, exist_ok=True)

editor = root / "Assets" / "Editor"
patterns = [
    "BuildEnrothFullWorld*.cs", "BuildEnrothOpenWorld.cs",
    "BuildEnrothTwoZonePrototype.cs", "BuildFullEnrothOneShot.cs",
    "RenderEnrothNorthUp.cs", "ValidateEnrothOpenWorld.cs",
    "LiveOneShotRebuild.cs", "OpenNewSorpigalOnLoad.cs",
    "RebuildCastleOneShot.cs", "RebuildNewSorpigalOneShot.cs",
    "RenderCIAllArchitectureAudit.cs", "RenderCIHouseCluster.cs",
    "RenderCIHouseDiagnostics.cs", "RenderNewSorpigalFacadeCheck.cs",
    "RenderNewSorpigalRiverCheck.cs", "RenderNSBridgeChecks.cs",
    "RenderNSCleanupChecks.cs", "RenderNSHighGroundCandidate.cs",
    "RenderNSWaterRoadDiagnostics.cs", "RenderSettlementChecks.cs",
    "SafeRebuildNSWaterRoad.cs", "ScanNewSorpigalHighGround.cs",
    "TriggerNSDiagOnce.cs", "DumpArchitectureClusters.cs",
    "DumpCIHouseCluster.cs", "AuditNewSorpigalFull.cs",
    "AuditNSTreeRoots.cs", "InspectNewSorpigalMaterials.cs",
    "MeasureNSBuildingScale.cs", "SetupEditableNewSorpigal.cs",
]
items = []
for pat in patterns:
    for f in editor.glob(pat):
        if f.name == "BuildEnrothLinkedOpenWorld.cs":
            continue
        items.append(f)
        meta = Path(str(f) + ".meta")
        if meta.exists():
            items.append(meta)

for name in [
    "BuildNewSorpigalScene.cs", "BuildNewSorpigalScene.cs.meta",
    "BuildNewSorpigalOpenWorld.cs.preroadsmooth_20260915_0038",
    "BuildNewSorpigalOpenWorld.cs.preroadsmooth_20260915_0038.meta",
]:
    f = editor / name
    if f.exists(): items.append(f)

for name in ["MMTwoZoneStreamer.cs", "MMTwoZoneStreamer.cs.meta"]:
    f = root / "Assets" / "Scripts" / "OpenWorld" / name
    if f.exists(): items.append(f)
for name in [
    "Enroth_NS_CI_Combined.unity", "Enroth_NS_CI_Combined.unity.meta",
    "Enroth_NS_CI_Combined_EditorPreview.unity",
    "Enroth_NS_CI_Combined_EditorPreview.unity.meta",
    "Enroth_Full_OpenWorld.unity", "Enroth_Full_OpenWorld.unity.meta",
    "Enroth_Full_OpenWorld_EditorPreview.unity",
    "Enroth_Full_OpenWorld_EditorPreview.unity.meta",
]:
    f = root / "Assets" / "Scenes" / name
    if f.exists(): items.append(f)

for f in [
    root / "Assets" / "World" / "EnrothTwoZone",
    root / "Assets" / "World" / "EnrothTwoZone.meta",
    root / "Assets" / "World" / "Enroth",
    root / "Assets" / "World" / "Enroth.meta",
]:
    if f.exists(): items.append(f)

seen = set()
moved = []
for f in items:
    if not f.exists():
        continue
    key = str(f.resolve()).lower()
    if key in seen:
        continue
    seen.add(key)
    rel = f.resolve().relative_to(root)
    dst = archive / rel
    dst.parent.mkdir(parents=True, exist_ok=True)
    shutil.move(str(f), str(dst))
    moved.append(str(rel))

print(f"ARCHIVE={archive}")
print(f"MOVED={len(moved)}")
for rel in moved:
    print(rel)
