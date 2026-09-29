from pathlib import Path
import re

ROOT = Path(r"C:\MMUnityPort")
OBJ = ROOT / "Assets/World/NewSorpigal/Objects"
WALL_RE = re.compile(r"^\d+_M\d+_(?:Wopr|Wopl|Wer)(?:_\d+)?\.obj$", re.I)


def parse_ref(tok):
    p = tok.split('/')
    return tuple(int(x) if x else None for x in p)


def remap_ref(ref, vm, tm, nm):
    out = [str(vm[ref[0]])]
    if len(ref) > 1:
        out.append('' if ref[1] is None else str(tm[ref[1]]))
    if len(ref) > 2:
        out.append('' if ref[2] is None else str(nm[ref[2]]))
    return '/'.join(out)


def split_obj(path):
    lines = path.read_text(errors="ignore").splitlines()
    verts, vts, vns, faces = [], [], [], []
    material = "pending"
    mtllib = "NewSorpigal.mtl"
    for line in lines:
        if line.startswith("mtllib "):
            mtllib = line.split(None, 1)[1]
        elif line.startswith("v "):
            q = line.split(); verts.append(tuple(map(float, q[1:4])))
        elif line.startswith("vt "):
            vts.append(line.split(None, 1)[1])
        elif line.startswith("vn "):
            vns.append(line.split(None, 1)[1])
        elif line.startswith("usemtl "):
            material = line.split(None, 1)[1]
        elif line.startswith("f "):
            refs = [parse_ref(x) for x in line.split()[1:]]
            faces.append((material, refs))
    if not faces:
        return []
    xs = [v[0] for v in verts]; zs = [v[2] for v in verts]
    axis = 0 if (max(xs)-min(xs)) >= (max(zs)-min(zs)) else 2
    amin = min(v[axis] for v in verts); amax = max(v[axis] for v in verts)
    span = max(amax-amin, 1e-6)
    groups = [[], [], []]
    for mat, refs in faces:
        c = sum(verts[r[0]-1][axis] for r in refs) / len(refs)
        t = (c-amin)/span
        gi = 0 if t < 0.20 else (2 if t > 0.80 else 1)
        groups[gi].append((mat, refs))

    out_paths = []
    for gi, gfaces in enumerate(groups):
        if not gfaces:
            continue
        used_v = sorted({r[0] for _,rs in gfaces for r in rs})
        used_t = sorted({r[1] for _,rs in gfaces for r in rs if len(r)>1 and r[1] is not None})
        used_n = sorted({r[2] for _,rs in gfaces for r in rs if len(r)>2 and r[2] is not None})
        vm = {old:i+1 for i,old in enumerate(used_v)}
        tm = {old:i+1 for i,old in enumerate(used_t)}
        nm = {old:i+1 for i,old in enumerate(used_n)}
        suffix = chr(ord('A')+gi)
        out = path.with_name(path.stem + f"__SEG_{suffix}.obj")
        buf = [f"mtllib {mtllib}", f"o {path.stem}__SEG_{suffix}"]
        for old in used_v:
            v = verts[old-1]; buf.append(f"v {v[0]:.6f} {v[1]:.6f} {v[2]:.6f}")
        for old in used_t:
            buf.append("vt " + vts[old-1])
        for old in used_n:
            buf.append("vn " + vns[old-1])
        current = None
        for mat, refs in gfaces:
            if mat != current:
                buf.append("usemtl " + mat); current = mat
            buf.append("f " + " ".join(remap_ref(r,vm,tm,nm) for r in refs))
        out.write_text("\n".join(buf)+"\n", encoding="utf-8")
        out_paths.append(out)
    return out_paths
for old in OBJ.glob("*__SEG_?.obj"):
    old.unlink()
    meta = Path(str(old)+".meta")
    if meta.exists(): meta.unlink()

written = []
for p in sorted(OBJ.glob("*.obj")):
    if WALL_RE.match(p.name):
        parts = split_obj(p)
        written.extend(parts)
        print(p.name, "->", [x.name for x in parts])

print("WALL_SEGMENTS_WRITTEN", len(written))
