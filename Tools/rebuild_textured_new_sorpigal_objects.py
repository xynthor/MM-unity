import struct
from pathlib import Path

SRC = Path(r"C:\MMUnityPort\Extract\NewSorpigal\oute3.odm")
OBJDIR = Path(r"C:\MMUnityPort\Assets\World\NewSorpigal\Objects")
TEXDIR = Path(r"C:\MMUnityPort\Assets\MMOriginal\NewSorpigal\Bitmaps")
SCALE = 1.0 / 128.0
N = 128

def u32(d,o): return struct.unpack_from('<I', d, o)[0]
def i32x3(d,o): return struct.unpack_from('<3i', d, o)
def clean(b): return b.split(b'\0',1)[0].decode('latin1','replace').strip()
def bmp_size(path):
    b = path.read_bytes()[:26]
    return struct.unpack_from('<ii', b, 18)

d = SRC.read_bytes()
o = 180 + 3*N*N
normal_count = u32(d,o)
o += 4 + N*N*2*4 + N*N*2*2 + normal_count*12
model_count = u32(d,o)
headers = o + 4
models = []
for i in range(model_count):
    h = headers + i*188
    models.append((clean(d[h:h+32]) or f'Model_{i:03}', u32(d,h+68), u32(d,h+76), u32(d,h+92)))

data_off = headers + model_count*188
sizes = {p.stem.lower(): bmp_size(p) for p in TEXDIR.glob('*.bmp')}
materials = set()
o = data_off
for mi,(name,nv,nf,nn) in enumerate(models):
    verts = [i32x3(d,o+j*12) for j in range(nv)]
    faces_off = o + nv*12
    tex_off = faces_off + nf*308 + nf*2 + nn*8
    out = ["mtllib NewSorpigal.mtl", f"o M{mi:03}_{name}"]
    for x,y,z in verts:
        out.append(f"v {x*SCALE:.6f} {z*SCALE:.6f} {-y*SCALE:.6f}")
    vt_index = 0
    face_lines = []
    for fi in range(nf):
        q = faces_off + fi*308
        count = d[q+0x12e]
        if count < 3 or count > 20:
            continue
        ids = list(struct.unpack_from('<20h',d,q+0x20)[:count])
        if any(v < 0 or v >= nv for v in ids):
            continue
        ul = struct.unpack_from('<20h',d,q+0x48)[:count]
        vl = struct.unpack_from('<20h',d,q+0x70)[:count]
        bu,bv = struct.unpack_from('<hh',d,q+0x112)
        tex = clean(d[tex_off+fi*10:tex_off+(fi+1)*10]).lower() or 'pending'
        if tex not in sizes:
            tex = 'pending'
        materials.add(tex)
        bw,bh = sizes.get(tex,(64,64))
        uv_ids = []
        for u0,v0 in zip(ul,vl):
            u = (bu + u0) / float(bw)
            v = 1.0 - (bv + v0) / float(bh)
            out.append(f"vt {u:.8f} {v:.8f}")
            vt_index += 1
            uv_ids.append(vt_index)
        face_lines.append(f"usemtl {tex}")
        for k in range(1,count-1):
            a,b,c = 0,k,k+1
            face_lines.append(
                f"f {ids[a]+1}/{uv_ids[a]} {ids[b]+1}/{uv_ids[b]} {ids[c]+1}/{uv_ids[c]}"
            )
    out.extend(face_lines)
    path = OBJDIR / f"{mi+1:03}_{'M'+format(mi,'03')+'_'+name}.obj"
    if not path.exists():
        matches = list(OBJDIR.glob(f"{mi+1:03}_*.obj"))
        if not matches:
            raise FileNotFoundError(mi)
        path = matches[0]
    path.write_text('\n'.join(out)+'\n',encoding='utf-8')
    o += nv*12 + nf*308 + nf*2 + nn*8 + nf*10

mtl = []
for mat in sorted(materials):
    mtl += [f"newmtl {mat}","Ka 0.2 0.2 0.2","Kd 1 1 1","Ks 0.05 0.05 0.05","Ns 8"]
    if mat in sizes:
        mtl.append(f"map_Kd ../../../MMOriginal/NewSorpigal/Bitmaps/{mat}.bmp")
    mtl.append('')
(OBJDIR/'NewSorpigal.mtl').write_text('\n'.join(mtl),encoding='utf-8')
print('models',model_count,'materials',len(materials),'sizes',len(sizes))
