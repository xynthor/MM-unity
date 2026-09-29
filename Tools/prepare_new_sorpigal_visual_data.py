from pathlib import Path
import csv
import struct
import numpy as np
from PIL import Image, ImageFilter, ImageEnhance

ROOT = Path(r"C:\MMUnityPort")
SRC = ROOT / "Assets/MMOriginal/NewSorpigal/Bitmaps"
OUT = ROOT / "Assets/Textures/NewSorpigal/Buildings"
DATA = ROOT / "Assets/World/NewSorpigal/Data"
ODM = ROOT / "Extract/NewSorpigal/oute3.odm"
OUT.mkdir(parents=True, exist_ok=True)
DATA.mkdir(parents=True, exist_ok=True)

for bmp in SRC.glob("*.bmp"):
    im = Image.open(bmp).convert("RGB")
    scale = max(1, min(8, 512 // max(im.size)))
    size = (im.width * scale, im.height * scale)
    albedo = im.resize(size, Image.Resampling.LANCZOS)
    albedo = albedo.filter(ImageFilter.UnsharpMask(radius=1.1, percent=85, threshold=3))
    albedo = ImageEnhance.Contrast(albedo).enhance(1.04)
    albedo.save(OUT / f"{bmp.stem.lower()}_albedo.png")
    gray = np.asarray(albedo.convert("L"), dtype=np.float32) / 255.0
    dx = np.zeros_like(gray)
    dy = np.zeros_like(gray)
    dx[:, 1:-1] = (gray[:, 2:] - gray[:, :-2]) * 0.7
    dy[1:-1, :] = (gray[2:, :] - gray[:-2, :]) * 0.7
    nx = -dx
    ny = -dy
    nz = np.ones_like(gray)
    norm = np.sqrt(nx * nx + ny * ny + nz * nz)
    normal = np.stack(((nx / norm) * 0.5 + 0.5,
                       (ny / norm) * 0.5 + 0.5,
                       (nz / norm) * 0.5 + 0.5), axis=-1)
    normal_img = Image.fromarray(np.clip(normal * 255, 0, 255).astype(np.uint8), "RGB")
    normal_img.save(OUT / f"{bmp.stem.lower()}_normal.png")

raw = ODM.read_bytes()
height = raw[180:180 + 128 * 128]
tile = raw[180 + 128 * 128:180 + 2 * 128 * 128]
(DATA / "heightmap_u8.bin").write_bytes(height)
(DATA / "tilemap_u8.bin").write_bytes(tile)

def u32(offset):
    return struct.unpack_from('<I', raw, offset)[0]
o = 180 + 3 * 128 * 128
normal_count = u32(o)
o += 4 + 128 * 128 * 2 * 4 + 128 * 128 * 2 * 2 + normal_count * 12
model_count = u32(o)
o += 4
headers = o
o += model_count * 188

for i in range(model_count):
    h = headers + i * 188
    nv = u32(h + 68)
    nf = u32(h + 76)
    nn = u32(h + 92)
    o += nv * 12 + nf * 308 + nf * 2 + nn * 8 + nf * 10

dec_count = u32(o)
dec_off = o + 4
name_off = dec_off + dec_count * 32
rows = []
for i in range(dec_count):
    q = dec_off + i * 32
    vals = struct.unpack_from('<HH3iiHHHhhh', raw, q)
    desc, flags, x, y, z, direction, cog, eventid, radius, dirdeg, eventvar, extra = vals
    name_bytes = raw[name_off + i * 32:name_off + (i + 1) * 32]
    name = name_bytes.split(b'\0', 1)[0].decode('latin1', 'replace')
    yaw = direction * 360.0 / 2048.0 if direction else float(dirdeg)
    rows.append((i, name, desc, flags,
                 x / 128.0, z / 128.0, -y / 128.0,
                 yaw, eventid, radius))

with (DATA / "decorations.csv").open('w', newline='', encoding='utf-8') as f:
    writer = csv.writer(f)
    writer.writerow(['index', 'name', 'desc', 'flags', 'x', 'y', 'z', 'yaw', 'event', 'radius'])
    writer.writerows(rows)

print('building_textures', len(list(OUT.glob('*_albedo.png'))))
print('decorations', len(rows))
print('trees', sum('tree' in r[1].lower() for r in rows))
print('rocks', sum('rock' in r[1].lower() for r in rows))
print('data', DATA)
