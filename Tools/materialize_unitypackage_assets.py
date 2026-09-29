from pathlib import Path
import shutil, sys

project = Path(r"C:\MMUnityPort")

for source_arg in sys.argv[1:]:
    source = Path(source_arg)
    count = 0
    total = 0
    skipped = 0
    print("MATERIALIZE_START", source, flush=True)
    for item in source.iterdir():
        if not item.is_dir():
            continue
        pathname = item / "pathname"
        if not pathname.exists():
            continue
        raw = pathname.read_bytes().decode("utf-8", "replace").replace("\x00", "")
        lines = raw.splitlines()
        rel = lines[0].strip().replace("\\", "/") if lines else ""
        if not rel.startswith("Assets/"):
            skipped += 1
            continue
        dest = project / Path(rel)
        asset = item / "asset"
        meta = item / "asset.meta"
        dest.parent.mkdir(parents=True, exist_ok=True)
        if asset.exists():
            if dest.exists() and dest.is_dir():
                raise RuntimeError(f"File/dir collision: {dest}")
            shutil.copy2(asset, dest)
            total += asset.stat().st_size
        else:
            dest.mkdir(parents=True, exist_ok=True)
        if meta.exists():
            shutil.copy2(meta, Path(str(dest) + ".meta"))
        count += 1
        if count % 250 == 0:
            print(" progress", count, round(total / 1e9, 2), "GB", flush=True)
    print("MATERIALIZE_DONE", source.name, "assets", count, "skipped", skipped, "GB", round(total/1e9,2), flush=True)
