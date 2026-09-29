import csv, json, math, struct, subprocess, sys
from pathlib import Path
import numpy as np
from PIL import Image, ImageFilter, ImageEnhance
from mm_polygon import earclip

ROOT = Path(r"C:\MMUnityPort")
MMARCH = ROOT / "Tools/mmarch.exe"
BITMAP_LOD = Path(r"C:\Program Files (x86)\GOG Galaxy\Games\Might and Magic 8\Data\mm6.bitmaps.lod")
# Some Merge source ODMs retain texture names that live in the original MM6/MM7
# archives rather than mm6.bitmaps.lod. Search all canonical local archives before
# declaring a source texture missing.
BITMAP_LODS = [
    BITMAP_LOD,
    Path(r"C:\MM1Port\work\lod_tmp_20260525_171258\bitmaps.lod"),
    Path(r"C:\Program Files (x86)\GOG Galaxy\Games\Might and Magic 8\Data\mm7.bitmaps.lod"),
    Path(r"C:\Program Files (x86)\GOG Galaxy\Games\Might and Magic 8\Data\mm8.bitmaps.lod"),
    Path(r"C:\Program Files (x86)\GOG Galaxy\Games\Might and Magic 8\Data\mmmerge.bitmaps.lod"),
]
N = 128
SCALE = 1.0 / 128.0
CONFIG = {
    "SweetWater": ("Sweet Water", ROOT / "Extract/SweetWater/outa1.odm"),
    "ParadiseValley": ("Paradise Valley", ROOT / "Extract/ParadiseValley/outa2.odm"),
    "HermitsIsle": ("Hermit's Isle", ROOT / "Extract/HermitsIsle/outa3.odm"),
    "Kriegspire": ("Kriegspire", ROOT / "Extract/Kriegspire/outb1.odm"),
    "Blackshire": ("Blackshire", ROOT / "Extract/Blackshire/outb2.odm"),
    "Dragonsand": ("Dragonsand", ROOT / "Extract/Dragonsand/outb3.odm"),
    "FrozenHighlands": ("Frozen Highlands", ROOT / "Extract/FrozenHighlands/outc1.odm"),
    "FreeHaven": ("Free Haven", ROOT / "Extract/FreeHaven/outc2.odm"),
    "MireOfTheDamned": ("Mire of the Damned", ROOT / "Extract/MireOfTheDamned/outc3.odm"),
    "SilverCove": ("Silver Cove", ROOT / "Extract/SilverCove/outd1.odm"),
    "BootlegBay": ("Bootleg Bay", ROOT / "Extract/BootlegBay/outd2.odm"),
    "CastleIronfist": ("Castle Ironfist", ROOT / "Extract/CastleIronfist/outd3.odm"),
    "EelInfestedWaters": ("Eel Infested Waters", ROOT / "Extract/EelInfestedWaters/oute1.odm"),
    "MistyIslands": ("Misty Islands", ROOT / "Extract/MistyIslands/oute2.odm"),
    "NewSorpigal": ("New Sorpigal", ROOT / "Extract/NewSorpigal/oute3.odm"),
}

def u32(d, o): return struct.unpack_from('<I', d, o)[0]
def i32x3(d, o): return struct.unpack_from('<3i', d, o)
def clean(b): return b.split(b'\0', 1)[0].decode('latin1', 'replace').strip()

def bmp_size(path):
    b = path.read_bytes()[:26]
    return struct.unpack_from('<ii', b, 18)

zone = sys.argv[1]
if zone not in CONFIG: raise SystemExit(f"Unknown zone {zone}")
display, ODM = CONFIG[zone]
raw = ODM.read_bytes()
DATA = ROOT / f"Assets/World/{zone}/Data"
OBJDIR = ROOT / f"Assets/World/{zone}/Objects"
BMPDIR = ROOT / f"Assets/MMOriginal/{zone}/Bitmaps"
TEXOUT = ROOT / f"Assets/Textures/{zone}/Buildings"
for p in (DATA, OBJDIR, BMPDIR, TEXOUT): p.mkdir(parents=True, exist_ok=True)
for p in OBJDIR.glob('*.obj'): p.unlink()
for p in OBJDIR.glob('*.mtl'): p.unlink()

height = raw[180:180 + N*N]
tilemap = raw[180 + N*N:180 + 2*N*N]
(DATA / "heightmap_u8.bin").write_bytes(height)
(DATA / "tilemap_u8.bin").write_bytes(tilemap)

tilesets = [struct.unpack_from('<hh', raw, 160 + i*4) for i in range(4)]
groups = [g for g, off in tilesets]
sem = bytearray(256)
group_by_raw = bytearray([255] * 256)
for t in range(256):
    if 90 <= t <= 233:
        q = t - 90; slot = q // 36; local = q % 36; group = groups[slot]
        group_by_raw[t] = group if 0 <= group < 255 else 255
        if group == 5 and local <= 11: sem[t] |= 1
        if group == 5 and 12 <= local <= 23: sem[t] |= 2
        if 12 <= local <= 23: sem[t] |= 4
        if group >= 10: sem[t] |= 8
        if group in (0, 7): sem[t] |= 32
        else: sem[t] |= 16
    else:
        sem[t] |= 16
(DATA / "tile_semantics_u8.bin").write_bytes(sem)
(DATA / "tile_groups_u8.bin").write_bytes(group_by_raw)
o = 180 + 3*N*N
normal_count = u32(raw, o)
o += 4 + N*N*2*4 + N*N*2*2 + normal_count*12
model_count = u32(raw, o)
headers = o + 4
data_off = headers + model_count*188
models = []
for i in range(model_count):
    h = headers + i*188
    models.append({
        'name': clean(raw[h:h+32]) or f'Model_{i:03}',
        'nv': u32(raw, h+68), 'nf': u32(raw, h+76), 'nn': u32(raw, h+92)
    })

materials = set()
o = data_off
for m in models:
    faces_off = o + m['nv']*12
    tex_off = faces_off + m['nf']*308 + m['nf']*2 + m['nn']*8
    for fi in range(m['nf']):
        tex = clean(raw[tex_off+fi*10:tex_off+(fi+1)*10]).lower()
        if tex: materials.add(tex)
    o += m['nv']*12 + m['nf']*308 + m['nf']*2 + m['nn']*8 + m['nf']*10

remaining = set(materials)
for archive in BITMAP_LODS:
    if not archive.exists() or not remaining:
        continue
    archive_names = set(subprocess.check_output(
        [str(MMARCH), 'list', str(archive), '\n'], text=True, errors='ignore'
    ).lower().splitlines())
    extract_names = sorted(remaining & archive_names)
    if extract_names:
        subprocess.run([str(MMARCH), 'extract', str(archive), str(BMPDIR), *extract_names], check=True)
        remaining.difference_update(extract_names)
sizes = {}
for bmp in BMPDIR.glob('*.bmp'):
    try:
        sizes[bmp.stem.lower()] = bmp_size(bmp)
        im = Image.open(bmp).convert('RGB')
    except Exception:
        continue
    scale = max(1, min(8, 512 // max(im.size)))
    out_size = (im.width * scale, im.height * scale)
    albedo = im.resize(out_size, Image.Resampling.LANCZOS)
    albedo = albedo.filter(ImageFilter.UnsharpMask(radius=1.1, percent=85, threshold=3))
    albedo = ImageEnhance.Contrast(albedo).enhance(1.04)
    albedo.save(TEXOUT / f'{bmp.stem.lower()}_albedo.png')
    gray = np.asarray(albedo.convert('L'), dtype=np.float32) / 255.0
    dx = np.zeros_like(gray); dy = np.zeros_like(gray)
    dx[:,1:-1] = (gray[:,2:] - gray[:,:-2]) * 0.7
    dy[1:-1,:] = (gray[2:,:] - gray[:-2,:]) * 0.7
    nx, ny, nz = -dx, -dy, np.ones_like(gray)
    norm = np.sqrt(nx*nx + ny*ny + nz*nz)
    normal = np.stack(((nx/norm)*0.5+0.5, (ny/norm)*0.5+0.5, (nz/norm)*0.5+0.5), axis=-1)
    Image.fromarray(np.clip(normal*255,0,255).astype(np.uint8), 'RGB').save(
        TEXOUT / f'{bmp.stem.lower()}_normal.png')

missing = sorted(materials - set(sizes))
print(display, 'textures', len(materials), 'extracted', len(sizes), 'missing', len(missing))
if missing: print('MISSING_TEXTURES', ','.join(missing[:40]))
mtl_name = f'{zone}.mtl'
all_used = set()
o = data_off
for mi, m in enumerate(models):
    nv, nf, nn = m['nv'], m['nf'], m['nn']
    verts = [i32x3(raw, o+j*12) for j in range(nv)]
    faces_off = o + nv*12
    tex_off = faces_off + nf*308 + nf*2 + nn*8
    out = [f'mtllib {mtl_name}', f'o M{mi:03}_{m["name"]}']
    for x,y,z in verts:
        out.append(f'v {-x*SCALE:.6f} {z*SCALE:.6f} {-y*SCALE:.6f}')
    vt_index = 0; face_lines = []
    for fi in range(nf):
        q = faces_off + fi*308
        count = raw[q+0x12e]
        if count < 3 or count > 20: continue
        ids = list(struct.unpack_from('<20h', raw, q+0x20)[:count])
        if any(v < 0 or v >= nv for v in ids): continue
        ul = struct.unpack_from('<20h', raw, q+0x48)[:count]
        vl = struct.unpack_from('<20h', raw, q+0x70)[:count]
        bu,bv = struct.unpack_from('<hh', raw, q+0x112)
        tex = clean(raw[tex_off+fi*10:tex_off+(fi+1)*10]).lower() or 'pending'
        if tex not in sizes: tex = 'pending'
        all_used.add(tex)
        bw,bh = sizes.get(tex, (64,64)); uv_ids = []
        for u0,v0 in zip(ul,vl):
            out.append(f'vt {(bu+u0)/float(bw):.8f} {1.0-(bv+v0)/float(bh):.8f}')
            vt_index += 1; uv_ids.append(vt_index)
        face_lines.append(f'usemtl {tex}')
        triangles = earclip([verts[index] for index in ids])
        if triangles is None:
            raise ValueError(f'Cannot triangulate {zone}, model {mi}, face {fi}; refusing a crossing triangle fan')
        for a,b,c in triangles:
            face_lines.append(
                f'f {ids[a]+1}/{uv_ids[a]} {ids[b]+1}/{uv_ids[b]} {ids[c]+1}/{uv_ids[c]}'
            )
    out.extend(face_lines)
    safe = ''.join(ch if ch.isalnum() or ch in '_-' else '_' for ch in m['name'])
    (OBJDIR / f'{mi+1:03}_M{mi:03}_{safe}.obj').write_text('\n'.join(out)+'\n', encoding='utf-8')
    o += nv*12 + nf*308 + nf*2 + nn*8 + nf*10

mtl = []
for mat in sorted(all_used):
    mtl += [f'newmtl {mat}', 'Ka 0.2 0.2 0.2', 'Kd 1 1 1', 'Ks 0.05 0.05 0.05', 'Ns 8', '']
(OBJDIR / mtl_name).write_text('\n'.join(mtl), encoding='utf-8')

dec_count = u32(raw, o)
dec_off = o + 4
name_off = dec_off + dec_count*32
rows = []
for i in range(dec_count):
    q = dec_off + i*32
    vals = struct.unpack_from('<HH3iiHHHhhh', raw, q)
    desc, flags, x, y, z, direction, cog, eventid, radius, dirdeg, eventvar, extra = vals
    name_bytes = raw[name_off+i*32:name_off+(i+1)*32]
    name = name_bytes.split(b'\0',1)[0].decode('latin1','replace')
    yaw = direction*360.0/2048.0 if direction else float(dirdeg)
    rows.append((i,name,desc,flags,x/128.0,z/128.0,-y/128.0,yaw,eventid,radius))
with (DATA / 'decorations.csv').open('w', newline='', encoding='utf-8') as f:
    w = csv.writer(f)
    w.writerow(['index','name','desc','flags','x','y','z','yaw','event','radius'])
    w.writerows(rows)

summary = {
    'zone': zone, 'display': display, 'models': model_count,
    'decorations': len(rows), 'trees': sum('tree' in r[1].lower() for r in rows),
    'rocks': sum('rock' in r[1].lower() for r in rows),
    'materials': len(materials), 'textures_extracted': len(sizes),
    'missing_textures': missing, 'tileset_groups': groups,
    'true_water_cells': sum(1 for t in tilemap if sem[t] & 1),
    'shore_cells': sum(1 for t in tilemap if sem[t] & 2),
    'road_cells': sum(1 for t in tilemap if sem[t] & 8),
}
(DATA / 'source_summary.json').write_text(json.dumps(summary, indent=2), encoding='utf-8')
print(json.dumps(summary, indent=2))
