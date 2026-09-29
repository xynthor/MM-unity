from pathlib import Path
import struct
for z,f in [('NewSorpigal','oute3.odm'),('CastleIronfist','outd3.odm'),('ParadiseValley','outa2.odm')]:
 b=(Path(r'C:\MMUnityPort\Extract')/z/f).read_bytes()
 base=176+3*128*128
 print(z,'base',base,'u32 seq',[struct.unpack_from('<I',b,base+i*4)[0] for i in range(8)])
 print('at180base', [struct.unpack_from('<I',b,base+4+i*4)[0] for i in range(8)])
