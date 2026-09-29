from pathlib import Path
import struct
for fn in ['dtile.bin','dtile2.bin','dtile3.bin']:
 p=Path(r'C:\MMUnityPort\Extract\TileData')/fn;b=p.read_bytes()
 print('\n',fn,len(b))
 for i in range(min(12,len(b)//26)):
  q=i*26; name=b[q:q+16].split(b'\0',1)[0].decode('latin1','replace')
  vals=struct.unpack_from('<5h',b,q+16)
  print(i,name,vals)
