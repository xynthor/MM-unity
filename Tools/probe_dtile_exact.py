from pathlib import Path
import struct
p=Path(r'C:\MMUnityPort\Extract\TileData\dtile.bin'); b=p.read_bytes()
count=struct.unpack_from('<I',b,0)[0]; print('count',count,(len(b)-4)//26)
for idx in [0,1,2,89,90,91,125,126,127,137,138,149,150,161,162,163,197,198,199,233,234,235,269,270,271,305,306,307,341,342,343,773,774,775,809]:
 q=4+idx*26
 if q+26>len(b): continue
 name=b[q:q+16].split(b'\0',1)[0].decode('latin1','replace')
 id_,bitmap,ts,section,attr=struct.unpack_from('<5h',b,q+16)
 print(idx,repr(name),'id',id_,'bmp',bitmap,'ts',ts,'sect',section,'attr',hex(attr & 0xffff))
