import struct, json
from pathlib import Path
ROOT=Path(r"C:\MMUnityPort")
cfg={
"SweetWater":"outa1.odm","ParadiseValley":"outa2.odm","HermitsIsle":"outa3.odm","Kriegspire":"outb1.odm","Blackshire":"outb2.odm","Dragonsand":"outb3.odm","FrozenHighlands":"outc1.odm","FreeHaven":"outc2.odm","MireOfTheDamned":"outc3.odm","SilverCove":"outd1.odm","BootlegBay":"outd2.odm","CastleIronfist":"outd3.odm","EelInfestedWaters":"oute1.odm","MistyIslands":"oute2.odm","NewSorpigal":"oute3.odm"}
for z,f in cfg.items():
 d=(ROOT/"Extract"/z/f).read_bytes(); tm=d[180+128*128:180+2*128*128]
 ts=[struct.unpack_from("<hh",d,160+i*4) for i in range(4)]
 groups=[g for g,o in ts]
 counts={}; quad={}
 for y in range(128):
  for x in range(128):
   t=tm[y*128+x]
   if 90<=t<=233:
    slot=(t-90)//36; local=(t-90)%36; g=groups[slot]
   else: slot=-1;local=-1;g=-1
   counts[g]=counts.get(g,0)+1
   q=("N" if y<64 else "S")+("W" if x<64 else "E")
   quad.setdefault(q,{});quad[q][g]=quad[q].get(g,0)+1
 print("\n",z,"tilesets",ts,"counts",dict(sorted(counts.items())))
 print(" quadrants",quad)
