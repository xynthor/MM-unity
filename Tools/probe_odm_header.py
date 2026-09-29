from pathlib import Path
import struct,binascii
for z,f in [('NewSorpigal','oute3.odm'),('ParadiseValley','outa2.odm')]:
 p=Path(r'C:\MMUnityPort\Extract')/z/f; b=p.read_bytes()
 print(z,'len',len(b),'first64',b[:64])
 print('strings', [b[i:i+32].split(b'\0',1)[0] for i in range(0,160,32)])
 print('u16@160',struct.unpack_from('<8H',b,160))
 print('bytes176-184',list(b[176:184]))
 print('height? 176 first8',list(b[176:184]),'180 first8',list(b[180:188]))
