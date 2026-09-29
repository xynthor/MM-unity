import struct
from pathlib import Path
for zone,fn in [('NewSorpigal','oute3.odm'),('CastleIronfist','outd3.odm')]:
 p=Path(r'C:\MMUnityPort\Extract')/zone/fn; d=p.read_bytes(); N=128
 o=180+3*N*N; nc=struct.unpack_from('<I',d,o)[0]; o+=4+N*N*2*4+N*N*2*2+nc*12
 mc=struct.unpack_from('<I',d,o)[0]; h=o+4
 nv=struct.unpack_from('<I',d,h+68)[0]; nf=struct.unpack_from('<I',d,h+76)[0]; nn=struct.unpack_from('<I',d,h+92)[0]
 off=h+mc*188
 vs=[struct.unpack_from('<3i',d,off+j*12) for j in range(nv)]
 print(zone,'first model source mean',tuple(sum(v[i] for v in vs)/len(vs)/128 for i in range(3)),'minmax x',min(v[0] for v in vs)/128,max(v[0] for v in vs)/128,'y',min(v[1] for v in vs)/128,max(v[1] for v in vs)/128,'z',min(v[2] for v in vs)/128,max(v[2] for v in vs)/128)
