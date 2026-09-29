from pathlib import Path
import shutil

SRC = Path(r"C:\MMUnityPort\Assets\World\NewSorpigal\NewSorpigal.obj")
OUT = Path(r"C:\MMUnityPort\Assets\World\NewSorpigal\Objects")

vertices = [None]
objects = []
current = None

for raw in SRC.read_text(encoding="utf-8", errors="ignore").splitlines():
    line = raw.strip()
    if line.startswith("v "):
        vertices.append(line)
    elif line.startswith("o "):
        current = {"name": line[2:].strip(), "faces": []}
        objects.append(current)
    elif line.startswith("f ") and current is not None:
        face = [int(x.split("/")[0]) for x in line[2:].split()]
        current["faces"].append(face)

if OUT.exists():
    shutil.rmtree(OUT)
OUT.mkdir(parents=True)

print(f"objects={len(objects)} global_vertices={len(vertices)-1}")

for idx, obj in enumerate(objects):
    used = []
    seen = set()
    for face in obj["faces"]:
        for vi in face:
            if vi not in seen:
                seen.add(vi)
                used.append(vi)

    remap = {old: new for new, old in enumerate(used, start=1)}
    safe = obj["name"].replace("/", "_").replace("\\", "_")
    path = OUT / f"{idx:03d}_{safe}.obj"

    with path.open("w", encoding="utf-8", newline="\n") as f:
        f.write(f"# Split from NewSorpigal.obj\no {obj['name']}\n")
        for old in used:
            f.write(vertices[old] + "\n")
        for face in obj["faces"]:
            f.write("f " + " ".join(str(remap[v]) for v in face) + "\n")

    print(f"{idx:03d} {obj['name']}: vertices={len(used)} faces={len(obj['faces'])}")

print(f"wrote={len(list(OUT.glob('*.obj')))} to {OUT}")
