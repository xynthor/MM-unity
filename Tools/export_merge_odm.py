import struct, sys
from pathlib import Path
from mm_polygon import earclip

SRC = Path(sys.argv[1])
DST = Path(sys.argv[2])
SCALE = 1.0 / 128.0
N = 128

def u32(d, o): return struct.unpack_from('<I', d, o)[0]
def i32x3(d, o): return struct.unpack_from('<3i', d, o)
def clean_name(b): return b.split(b'\0', 1)[0].decode('latin1', 'replace').replace(' ', '_')

d = SRC.read_bytes()
height_off = 180
height = d[height_off:height_off + N*N]
o = height_off + 3*N*N
normal_count = u32(d, o)
o += 4 + N*N*2*4 + N*N*2*2 + normal_count*12
model_count = u32(d, o)
headers_off = o + 4
models = []
for i in range(model_count):
    h = headers_off + i*188
    models.append({
        'name': clean_name(d[h:h+32]) or f'Model_{i:03}',
        'nv': u32(d, h+68), 'nf': u32(d, h+76), 'nn': u32(d, h+92),
        'pos': i32x3(d, h+112)
    })

data_off = headers_off + model_count*188
print(f'normalCount={normal_count} models={model_count}')
with DST.open('w', encoding='utf-8', newline='\n') as f:
    f.write('# MMMerge MM8-format ODM export\n')
    f.write('o Terrain\n')
    for y in range(N):
        for x in range(N):
            h = height[y*N+x] * 32.0
            mx = (x - 64) * 512.0
            my = (64 - y) * 512.0
            f.write(f'v {mx*SCALE:.6f} {h*SCALE:.6f} {-my*SCALE:.6f}\n')
    for y in range(N-1):
        for x in range(N-1):
            a = y*N + x + 1
            b = a + 1
            c = a + N
            e = c + 1
            f.write(f'f {a} {c} {e}\n')
            f.write(f'f {a} {e} {b}\n')

    base = N*N
    o = data_off
    total_faces = 0
    for mi, m in enumerate(models):
        verts = [i32x3(d, o+j*12) for j in range(m['nv'])]
        faces_off = o + m['nv']*12
        f.write(f'o M{mi:03}_{m["name"]}\n')
        for x,y,z in verts:
            f.write(f'v {x*SCALE:.6f} {z*SCALE:.6f} {-y*SCALE:.6f}\n')
        for fi in range(m['nf']):
            q = faces_off + fi*308
            count = d[q+0x12e]
            ids = struct.unpack_from('<20h', d, q+0x20)
            if count < 3 or count > 20:
                continue
            ids = list(ids[:count])
            if any(v < 0 or v >= m['nv'] for v in ids):
                continue
            triangles = earclip([verts[index] for index in ids])
            if triangles is None:
                raise ValueError(f'Cannot triangulate model {mi}, facet {fi}; refusing a crossing triangle fan')
            for ia, ib, ic in triangles:
                a = base + ids[ia] + 1
                b = base + ids[ib] + 1
                c = base + ids[ic] + 1
                f.write(f'f {a} {b} {c}\n')
                total_faces += 1
        o += m['nv']*12 + m['nf']*308 + m['nf']*2 + m['nn']*8 + m['nf']*10
        base += m['nv']

print(f'OBJ={DST} triangles={total_faces} bytes={DST.stat().st_size}')
print(f'decorationCount={u32(d,o)} at offset={o}')
