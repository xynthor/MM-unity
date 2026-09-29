from pathlib import Path
import shutil, sys

PROJECT = Path(r"C:\MMUnityPort")
MODE = sys.argv[1]
SOURCE = Path(sys.argv[2])

if MODE == "book":
    PREFIXES = (
        "Assets/Art/",
        "Assets/Shaders/",
        "Assets/PhysicMaterials/",
    )
elif MODE == "hdrp":
    PREFIXES = (
        "Assets/TerrainDemoScene_HDRP/Prefabs/",
        "Assets/TerrainDemoScene_HDRP/Terrain/",
        "Assets/TerrainDemoScene_HDRP/ShaderGraphs/",
        "Assets/TerrainDemoScene_HDRP/VFXGraphs/",
        "Assets/TerrainDemoScene_HDRP/Volumes/",
        "Assets/TerrainDemoScene_HDRP/Settings/",
        "Assets/TerrainDemoScene_HDRP/LensFlares/",
    )
else:
    raise SystemExit(f"unknown mode {MODE}")
count=0; total=0; skipped=0
for item in SOURCE.iterdir():
    if not item.is_dir(): continue
    pp=item/'pathname'
    if not pp.exists(): continue
    raw=pp.read_bytes().decode('utf-8','ignore').replace('\x00','')
    lines=raw.splitlines(); rel=lines[0].strip().replace('\\','/') if lines else ''
    if not rel.startswith(PREFIXES): skipped+=1; continue
    if rel.lower().endswith(('.cs','.asmdef','.dll')): skipped+=1; continue
    dest=PROJECT/Path(rel)
    asset=item/'asset'; meta=item/'asset.meta'
    dest.parent.mkdir(parents=True,exist_ok=True)
    if asset.exists():
        if dest.exists() and dest.is_dir(): raise RuntimeError(f'collision {dest}')
        shutil.copy2(asset,dest); total+=asset.stat().st_size
    else:
        dest.mkdir(parents=True,exist_ok=True)
    if meta.exists(): shutil.copy2(meta,Path(str(dest)+'.meta'))
    count+=1
    if count%250==0: print('progress',count,round(total/1e9,2),'GB',flush=True)
print('MATERIALIZE_DONE',MODE,'count',count,'skipped',skipped,'GB',round(total/1e9,2),flush=True)