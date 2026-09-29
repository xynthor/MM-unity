import struct
from pathlib import Path

SRC = Path(r"C:\MMUnityPort\Extract\NewSorpigal\oute3.odm")
OBJDIR = Path(r"C:\MMUnityPort\Assets\World\NewSorpigal\Objects")
TEXDIR = Path(r"C:\MMUnityPort\Assets\MMOriginal\NewSorpigal\Bitmaps")
SCALE = 1.0 / 128.0
N = 128
EPS = 1e-9

def u32(d,o): return struct.unpack_from('<I', d, o)[0]
def i32x3(d,o): return struct.unpack_from('<3i', d, o)
def clean(b): return b.split(b'\0',1)[0].decode('latin1','replace').strip()
def bmp_size(path):
    b = path.read_bytes()[:26]
    return struct.unpack_from('<ii', b, 18)

def project_polygon(points):
    nx=ny=nz=0.0
    for i,p in enumerate(points):
        q=points[(i+1)%len(points)]
        nx+=(p[1]-q[1])*(p[2]+q[2])
        ny+=(p[2]-q[2])*(p[0]+q[0])
        nz+=(p[0]-q[0])*(p[1]+q[1])
    ax,ay,az=abs(nx),abs(ny),abs(nz)
    if ax>=ay and ax>=az: return [(p[1],p[2]) for p in points]
    if ay>=az: return [(p[0],p[2]) for p in points]
    return [(p[0],p[1]) for p in points]

def area2(a,b,c):
    return (b[0]-a[0])*(c[1]-a[1])-(b[1]-a[1])*(c[0]-a[0])

def polygon_area(poly):
    s=0.0
    for i,p in enumerate(poly):
        q=poly[(i+1)%len(poly)]
        s += p[0]*q[1]-q[0]*p[1]
    return 0.5*s

def point_in_tri(p,a,b,c,orient):
    ab=area2(a,b,p)*orient
    bc=area2(b,c,p)*orient
    ca=area2(c,a,p)*orient
    return ab>=-EPS and bc>=-EPS and ca>=-EPS

def earclip(points):
    if len(points)==3: return [(0,1,2)]
    poly=project_polygon(points)
    orient=1.0 if polygon_area(poly)>=0 else -1.0
    idx=list(range(len(poly)))
    tris=[]
    guard=0
    while len(idx)>3 and guard < len(poly)*len(poly)*4:
        guard+=1
        clipped=False
        for ii in range(len(idx)):
            ia,ib,ic=idx[ii-1],idx[ii],idx[(ii+1)%len(idx)]
            a,b,c=poly[ia],poly[ib],poly[ic]
            if area2(a,b,c)*orient <= EPS: continue
            if any(j not in (ia,ib,ic) and point_in_tri(poly[j],a,b,c,orient) for j in idx):
                continue
            tris.append((ia,ib,ic))
            del idx[ii]
            clipped=True
            break
        if not clipped:
            # Drop the least significant nearly-collinear corner and continue.
            best=None
            for ii in range(len(idx)):
                ia,ib,ic=idx[ii-1],idx[ii],idx[(ii+1)%len(idx)]
                score=abs(area2(poly[ia],poly[ib],poly[ic]))
                if best is None or score<best[0]: best=(score,ii)
            if best and best[0] < 1e-6:
                del idx[best[1]]
                continue
            return None
    if len(idx)==3:
        tris.append(tuple(idx))
    return tris

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
fixed_faces=0
fallback_faces=0
for mi,(name,nv,nf,nn) in enumerate(models):
    verts = [i32x3(d,o+j*12) for j in range(nv)]
    faces_off = o + nv*12
    tex_off = faces_off + nf*308 + nf*2 + nn*8
    out = ["mtllib NewSorpigal.mtl", f"o M{mi:03}_{name}"]
    for x,y,z in verts:
        out.append(f"v {-x*SCALE:.6f} {z*SCALE:.6f} {y*SCALE:.6f}")
    vt_index = 0
    face_lines = []
    for fi in range(nf):
        q = faces_off + fi*308
        count = d[q+0x12e]
        if count < 3 or count > 20: continue
        ids = list(struct.unpack_from('<20h',d,q+0x20)[:count])
        if any(v < 0 or v >= nv for v in ids): continue
        ul = struct.unpack_from('<20h',d,q+0x48)[:count]
        vl = struct.unpack_from('<20h',d,q+0x70)[:count]
        bu,bv = struct.unpack_from('<hh',d,q+0x112)
        tex = clean(d[tex_off+fi*10:tex_off+(fi+1)*10]).lower() or 'pending'
        if tex not in sizes: tex='pending'
        materials.add(tex)
        bw,bh=sizes.get(tex,(64,64))
        uv_ids=[]
        for u0,v0 in zip(ul,vl):
            u=(bu+u0)/float(bw)
            v=1.0-(bv+v0)/float(bh)
            out.append(f"vt {u:.8f} {v:.8f}")
            vt_index += 1
            uv_ids.append(vt_index)
        pts=[verts[i] for i in ids]
        tris=earclip(pts)
        if tris is None:
            fallback_faces += 1
            continue
        elif count>3:
            fixed_faces += 1
        face_lines.append(f"usemtl {tex}")
        for a,b,c in tris:
            face_lines.append(f"f {ids[a]+1}/{uv_ids[a]} {ids[b]+1}/{uv_ids[b]} {ids[c]+1}/{uv_ids[c]}")
            # MM6 outdoor facets are effectively two-sided. Unity culls backfaces,
            # so emit reverse winding too instead of losing wall/facade planes.
            face_lines.append(f"f {ids[c]+1}/{uv_ids[c]} {ids[b]+1}/{uv_ids[b]} {ids[a]+1}/{uv_ids[a]}")
    out.extend(face_lines)
    matches=list(OBJDIR.glob(f"{mi+1:03}_*.obj"))
    if not matches: raise FileNotFoundError(mi)
    matches[0].write_text('\n'.join(out)+'\n',encoding='utf-8')
    o += nv*12 + nf*308 + nf*2 + nn*8 + nf*10

mtl=[]
for mat in sorted(materials):
    mtl += [f"newmtl {mat}","Ka 0.2 0.2 0.2","Kd 1 1 1","Ks 0.05 0.05 0.05","Ns 8"]
    if mat in sizes:
        mtl.append(f"map_Kd ../../../MMOriginal/NewSorpigal/Bitmaps/{mat}.bmp")
    mtl.append('')
(OBJDIR/'NewSorpigal.mtl').write_text('\n'.join(mtl),encoding='utf-8')
print('models',model_count,'materials',len(materials),'triangulated_faces',fixed_faces,'fallback_faces',fallback_faces)

