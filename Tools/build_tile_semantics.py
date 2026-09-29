import struct, json, pathlib, re
root=pathlib.Path(r'C:\MMUnityPort')
odm=(root/'Extract/NewSorpigal/oute3.odm').read_bytes()
dt=(root/'Extract/TileData/dtile.bin').read_bytes()
ts=[struct.unpack_from('<hh',odm,160+i*4) for i in range(4)]
n=struct.unpack_from('<I',dt,0)[0]
entries=[]
for i in range(n):
    b=4+i*26
    name=dt[b:b+16].split(b'\0',1)[0].decode('latin1')
    tileid,bitmapid,tileset,variant,flags=struct.unpack_from('<HHHHH',dt,b+16)
    entries.append(dict(name=name,tileset=tileset,variant=variant,flags=flags))
sem=bytearray(256); sets=bytearray([255]*256); resolved=[]
for j in range(256):
    idx=j
    if 90<=j<=233:
        q=j-90; idx=ts[q//36][1]+q%36
    resolved.append(idx)
    if 0<=idx<n:
        e=entries[idx]; f=0
        if e['flags'] & 0x2: f|=1
        if e['flags'] & 0x100: f|=2
        if e['flags'] & 0x200: f|=4
        if e['tileset']>=10: f|=8
        if e['tileset'] in (3,4,8,9): f|=16
        if e['tileset']==0: f|=32
        sem[j]=f; sets[j]=e['tileset'] if e['tileset']<256 else 255
(root/'Assets/World/NewSorpigal/Data/tile_semantics_u8.bin').write_bytes(sem)
(root/'Assets/World/NewSorpigal/Data/tile_tileset_u8.bin').write_bytes(sets)
tm=odm[180+128*128:180+2*128*128]
def tile_at_world(x,z):
    sx=max(0,min(127,round(x/4+64))); sy=max(0,min(127,round(z/4+64))); t=tm[sy*128+sx]
    return sx,sy,t,sem[t]
rows=[]
for p in sorted((root/'Assets/World/NewSorpigal/Objects').glob('*.obj')):
    if p.name.startswith('000_'): continue
    xs=[]; ys=[]; zs=[]
    for line in p.read_text(errors='ignore').splitlines():
        if line.startswith('v '):
            q=line.split(); xs.append(float(q[1])); ys.append(float(q[2])); zs.append(float(q[3]))
    if not xs: continue
    cx=(min(xs)+max(xs))/2; cy=(min(ys)+max(ys))/2; cz=(min(zs)+max(zs))/2
    sx,sy,t,f=tile_at_world(cx,cz)
    rows.append((p.name,cx,cy,cz,sx,sy,t,f,bool(f&1),bool(f&2),bool(f&8)))
water=[r for r in rows if r[8]]
shore=[r for r in rows if r[9]]
print('objects',len(rows),'center_on_TRUE_water',len(water),'center_on_shore',len(shore))
for r in water: print('WATER_CENTER',r[0], 'world',round(r[1],1),round(r[3],1),'cell',r[4],r[5],'tile',r[6])
print('sem files written')
