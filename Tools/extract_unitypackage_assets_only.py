import tarfile, pathlib, shutil, sys, os

project = pathlib.Path(r"C:\MMUnityPort")
packages = [pathlib.Path(p) for p in sys.argv[1:]]

def clean_path(raw):
    text = raw.decode("utf-8", "replace").replace("\x00", "")
    lines = text.splitlines()
    return lines[0].strip().replace("\\", "/") if lines else ""

def flush_group(group, parts):
    if not group or "pathname" not in parts:
        return 0, 0
    path = clean_path(parts["pathname"])
    if not path.startswith("Assets/"):
        return 0, 0
    dest = project / pathlib.PurePosixPath(path)
    dest.parent.mkdir(parents=True, exist_ok=True)
    data = parts.get("asset")
    if data is None:
        dest.mkdir(parents=True, exist_ok=True)
        size = 0
    else:
        if dest.exists() and dest.is_dir():
            raise RuntimeError(f"Destination collision: {dest}")
        dest.write_bytes(data)
        size = len(data)
    meta = parts.get("asset.meta")
    if meta is not None:
        pathlib.Path(str(dest) + ".meta").write_bytes(meta)
    return 1, size

for package in packages:
    print("STREAM_EXTRACT_START", package.name, flush=True)
    count = 0
    written = 0
    current = None
    parts = {}
    with tarfile.open(package, mode="r|gz") as tf:
        for member in tf:
            name = member.name.rstrip("/")
            bits = name.split("/")
            if len(bits) < 2:
                continue
            group, leaf = bits[0], "/".join(bits[1:])
            if current is None:
                current = group
            elif group != current:
                n, b = flush_group(current, parts)
                count += n
                written += b
                if count and count % 250 == 0:
                    print(" progress", count, round(written / 1e9, 2), "GB", flush=True)
                current = group
                parts = {}
            if member.isfile() and leaf in ("pathname", "asset", "asset.meta"):
                src = tf.extractfile(member)
                if src is not None:
                    parts[leaf] = src.read()
        n, b = flush_group(current, parts)
        count += n
        written += b
    print("STREAM_EXTRACT_DONE", package.name, count, round(written / 1e9, 2), "GB", flush=True)
