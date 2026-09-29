from pathlib import Path
import csv, math, shutil

ROOT = Path(r"C:\MMUnityPort")
BACKUP = ROOT / "Backups" / "PreXHandFix_20260917_2305"
MARKER = ROOT / "Validation" / "ARCH_X_HANDEDNESS_FIXED_20260917.txt"
ZONES = [
    "NewSorpigal","CastleIronfist","MireOfTheDamned","Dragonsand","HermitsIsle",
    "MistyIslands","BootlegBay","FreeHaven","Blackshire","ParadiseValley",
    "EelInfestedWaters","SilverCove","FrozenHighlands","Kriegspire","SweetWater"
]
N = 128


def bilinear_height(data, x, z):
    sx = max(0.0, min(127.0, x / 4.0 + 64.0))
    sy = max(0.0, min(127.0, 64.0 - z / 4.0))
    x0, y0 = int(math.floor(sx)), int(math.floor(sy))
    x1, y1 = min(127, x0 + 1), min(127, y0 + 1)
    tx, ty = sx - x0, sy - y0
    v = lambda xx, yy: data[yy * N + xx] * 0.25
    a = v(x0, y0) * (1 - tx) + v(x1, y0) * tx
    b = v(x0, y1) * (1 - tx) + v(x1, y1) * tx
    return a * (1 - ty) + b * ty

def obj_bounds(path):
    xs, ys, zs = [], [], []
    with path.open("r", encoding="utf-8", errors="ignore") as f:
        for line in f:
            if line.startswith("v "):
                p = line.split()
                if len(p) >= 4:
                    xs.append(float(p[1])); ys.append(float(p[2])); zs.append(float(p[3]))
    if not xs:
        return None
    return {
        "cx": (min(xs) + max(xs)) * 0.5,
        "cz": (min(zs) + max(zs)) * 0.5,
        "min_y": min(ys), "max_y": max(ys), "height": max(ys) - min(ys)
    }


def flip_obj_x(path):
    out = []
    with path.open("r", encoding="utf-8", errors="ignore") as f:
        for line in f:
            if line.startswith("v ") or line.startswith("vn "):
                p = line.rstrip("\n").split()
                p[1] = f"{-float(p[1]):.9g}"
                out.append(" ".join(p) + "\n")
            elif line.startswith("f "):
                p = line.rstrip("\n").split()
                out.append("f " + " ".join(reversed(p[1:])) + "\n")
            else:
                out.append(line)
    path.write_text("".join(out), encoding="utf-8")

def backup_zone(zone):
    src_obj = ROOT / "Assets" / "World" / zone / "Objects"
    dst_obj = BACKUP / zone / "Objects"
    dst_obj.mkdir(parents=True, exist_ok=True)
    for p in src_obj.glob("*.obj"):
        shutil.copy2(p, dst_obj / p.name)
    csvp = ROOT / "Assets" / "World" / zone / "Data" / "model_placement_audit.csv"
    if csvp.exists():
        dst_data = BACKUP / zone / "Data"
        dst_data.mkdir(parents=True, exist_ok=True)
        shutil.copy2(csvp, dst_data / csvp.name)


def recalc_placement(zone):
    data_dir = ROOT / "Assets" / "World" / zone / "Data"
    obj_dir = ROOT / "Assets" / "World" / zone / "Objects"
    csvp = data_dir / "model_placement_audit.csv"
    heights = (data_dir / "heightmap_u8.bin").read_bytes()
    rows = list(csv.DictReader(csvp.open(newline="", encoding="utf-8-sig")))
    fields = list(rows[0].keys()) if rows else []
    for r in rows:
        p = obj_dir / r["file"]
        b = obj_bounds(p) if p.exists() else None
        if not b:
            continue
        r["cx"] = f"{b['cx']:.9g}"; r["cz"] = f"{b['cz']:.9g}"
        r["min_y"] = f"{b['min_y']:.9g}"; r["max_y"] = f"{b['max_y']:.9g}"
        r["height"] = f"{b['height']:.9g}"
        surface = bilinear_height(heights, b["cx"], b["cz"]) if r["kind"] == "LAND" else 0.0
        r["source_surface_y"] = f"{surface:.12g}"
        r["source_base_offset"] = f"{b['min_y'] - surface:.12g}"
    with csvp.open("w", newline="", encoding="utf-8") as f:
        w = csv.DictWriter(f, fieldnames=fields)
        w.writeheader(); w.writerows(rows)


def verify_raw_alignment():
    raw_csv = ROOT / "Validation" / "RawODM_Object_Position_Audit.csv"
    raw = list(csv.DictReader(raw_csv.open(newline="", encoding="utf-8-sig")))
    by_zone = {}
    for r in raw:
        by_zone.setdefault(r["zone"], []).append(r)
    failures = []
    for zone in ZONES:
        for r in by_zone.get(zone, []):
            p = ROOT / "Assets" / "World" / zone / "Objects" / r["obj"]
            if not p.exists():
                continue
            b = obj_bounds(p)
            ex = float(r["raw_expected_x"])
            ez = -float(r["raw_expected_z"])
            if abs(b["cx"] - ex) > 0.002 or abs(b["cz"] - ez) > 0.002:
                failures.append((zone, r["obj"], b["cx"], b["cz"], ex, ez))
    return failures


def main():
    if MARKER.exists():
        raise SystemExit(f"Already applied: {MARKER}")
    total = 0
    for zone in ZONES:
        backup_zone(zone)
        obj_dir = ROOT / "Assets" / "World" / zone / "Objects"
        objs = list(obj_dir.glob("*.obj"))
        for p in objs:
            flip_obj_x(p); total += 1
        recalc_placement(zone)
        print(zone, "flipped", len(objs))
    failures = verify_raw_alignment()
    if failures:
        for f in failures[:30]: print("FAIL", f)
        raise SystemExit(f"Raw ODM verification failed: {len(failures)} objects")
    MARKER.write_text(f"Applied one-time X handedness correction to {total} OBJ files.\nBackup: {BACKUP}\nRaw ODM X / Unity -Z verification: PASS\n", encoding="utf-8")
    print("DONE", total, "OBJ files; raw ODM verification PASS")

if __name__ == "__main__":
    main()
