from pathlib import Path
import csv, math, json, sys
ROOT=Path(r'C:\MMUnityPort')
ZONES=['SweetWater','ParadiseValley','HermitsIsle','Kriegspire','Blackshire','Dragonsand','FrozenHighlands','FreeHaven','MireOfTheDamned','SilverCove','BootlegBay','CastleIronfist','EelInfestedWaters','MistyIslands','NewSorpigal']
N=128
if len(sys.argv)>1: ZONES=sys.argv[1:]

def sem_at(tiles,sem,x,z):
    sx=max(0,min(127,round(x/4.0+64))); sy=max(0,min(127,round(64-z/4.0)))
    t=tiles[sy*N+sx]; return sem[t], sx, sy

def terrain_y(heights,x,z):
    sx=max(0,min(127,x/4.0+64)); sy=max(0,min(127,64-z/4.0))
    x0=int(math.floor(sx)); y0=int(math.floor(sy)); x1=min(127,x0+1); y1=min(127,y0+1)
    tx=sx-x0; ty=sy-y0
    a=heights[y0*N+x0]*(1-tx)+heights[y0*N+x1]*tx
    b=heights[y1*N+x0]*(1-tx)+heights[y1*N+x1]*tx
    return (a*(1-ty)+b*ty)*0.25
def parse_obj(path):
    verts=[]; faces=[]
    for line in path.read_text(encoding='utf-8',errors='ignore').splitlines():
        if line.startswith('v '):
            _,x,y,z=line.split()[:4]; verts.append((float(x),float(y),float(z)))
        elif line.startswith('f '):
            ids=[]
            for p in line.split()[1:]: ids.append(int(p.split('/')[0])-1)
            if len(ids)==3: faces.append(tuple(ids))
    return verts,faces

def tri_proj_area(a,b,c):
    ax,az=a[0],a[2]; bx,bz=b[0],b[2]; cx,cz=c[0],c[2]
    return abs((bx-ax)*(cz-az)-(bz-az)*(cx-ax))*0.5

def tri_centroid(a,b,c):
    return ((a[0]+b[0]+c[0])/3,(a[1]+b[1]+c[1])/3,(a[2]+b[2]+c[2])/3)
for zone in ZONES:
    data=ROOT/f'Assets/World/{zone}/Data'
    objs=sorted((ROOT/f'Assets/World/{zone}/Objects').glob('*.obj'))
    tiles=(data/'tilemap_u8.bin').read_bytes(); sem=(data/'tile_semantics_u8.bin').read_bytes(); heights=(data/'heightmap_u8.bin').read_bytes()
    rows=[]
    for p in objs:
        if p.name.startswith('000_'): continue
        verts,faces=parse_obj(p)
        if not verts: continue
        xs=[v[0] for v in verts]; ys=[v[1] for v in verts]; zs=[v[2] for v in verts]
        obj_cx=(min(xs)+max(xs))/2; cx=obj_cx; cz=(min(zs)+max(zs))/2; miny=min(ys); maxy=max(ys)
        water=road=shore=land=0.0; surf_sum=surf_w=0.0
        for ia,ib,ic in faces:
            a,b,c=verts[ia],verts[ib],verts[ic]; w=tri_proj_area(a,b,c)
            if w<1e-5: continue
            x,y,z=tri_centroid(a,b,c); s,_,_=sem_at(tiles,sem,x,z)
            if s&1: water+=w
            else: land+=w
            if s&2: shore+=w
            if s&8: road+=w
            if not (s&1): surf_sum+=terrain_y(heights,x,z)*w; surf_w+=w
        total=water+land
        wr=water/total if total else 0.0; rr=road/total if total else 0.0; sr=shore/total if total else 0.0
        center_sem,_,_=sem_at(tiles,sem,cx,cz)
        center_surface=0.0 if (center_sem&1) else terrain_y(heights,cx,cz)
        land_surface=surf_sum/surf_w if surf_w else center_surface
        kind='WATER_STRUCTURE' if wr>=0.20 else ('SHORE_STRUCTURE' if wr>=0.03 or sr>=0.15 else 'LAND')
        ref_surface=0.0 if kind=='WATER_STRUCTURE' else land_surface
        rows.append(dict(file=p.name,kind=kind,cx=cx,cz=cz,min_y=miny,max_y=maxy,height=maxy-miny,water_ratio=wr,shore_ratio=sr,road_ratio=rr,source_surface_y=ref_surface,source_base_offset=miny-ref_surface))
    out=data/'model_placement_audit.csv'
    with out.open('w',newline='',encoding='utf-8') as f:
        w=csv.DictWriter(f,fieldnames=rows[0].keys()); w.writeheader(); w.writerows(rows)
    cand=sorted(rows,key=lambda r:r['water_ratio'],reverse=True)[:20]
    print('\n',zone,'models',len(rows),'water/shore',sum(r['kind']!='LAND' for r in rows))
    for r in cand[:12]: print(r['file'],r['kind'],f"water={r['water_ratio']:.2f}",f"shore={r['shore_ratio']:.2f}",f"baseoff={r['source_base_offset']:.2f}")

