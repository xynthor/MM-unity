import json, struct
from pathlib import Path

SRC = Path(r"C:\MMUnityPort\Extract\NewSorpigal\oute3.odm")
DST = Path(r"C:\MMUnityPort\Assets\World\NewSorpigal\Blueprint\new_sorpigal.json")
N = 128
SCALE = 1.0 / 128.0

def u32(d, o): return struct.unpack_from('<I', d, o)[0]
def clean(b): return b.split(b'\0',1)[0].decode('latin1','replace').strip()

d = SRC.read_bytes()
height = list(d[180:180+N*N])
tiles = list(d[180+N*N:180+2*N*N])
o = 180 + 3*N*N
normal_count = u32(d, o)
o += 4 + N*N*2*4 + N*N*2*2 + normal_count*12
model_count = u32(d, o)
headers_off = o + 4
models = []
for i in range(model_count):
    h = headers_off + i*188
    models.append((u32(d,h+68), u32(d,h+76), u32(d,h+92)))
o = headers_off + model_count*188
for nv,nf,nn in models:
    o += nv*12 + nf*308 + nf*2 + nn*8 + nf*10

dec_count = u32(d, o)
dec_off = o + 4
name_off = dec_off + dec_count*32

decs = []
for i in range(dec_count):
    q = dec_off + i*32
    vals = struct.unpack_from('<HH3iiHHHhhh', d, q)
    desc, flags, x, y, z, yaw, cog, eventid, trig, deg, eventvar, extra = vals
    name = clean(d[name_off+i*32:name_off+(i+1)*32])
    decs.append({
        'index': i, 'name': name, 'descId': desc, 'flags': flags,
        'x': x*SCALE, 'y': z*SCALE, 'z': -y*SCALE,
        'yawRaw': yaw, 'yawDeg': deg, 'eventId': eventid, 'trigger': trig
    })
tile_sets = []
for i in range(4):
    group, offset = struct.unpack_from('<hh', d, 160+i*4)
    tile_sets.append({'group': group, 'offset': offset})

payload = {
    'size': N,
    'worldScale': SCALE,
    'terrainWorldSize': 512.0,
    'waterLevel': 0.05,
    'tileSets': tile_sets,
    'height': height,
    'tiles': tiles,
    'decorations': decs,
}
DST.parent.mkdir(parents=True, exist_ok=True)
DST.write_text(json.dumps(payload, separators=(',',':')), encoding='utf-8')
print('wrote', DST, 'decorations', dec_count, 'bytes', DST.stat().st_size)
from collections import Counter
print('top decorations', Counter(x['name'] for x in decs).most_common(20))
