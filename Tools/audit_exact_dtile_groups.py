from pathlib import Path
import struct,collections
ROOT=Path(r"C:\MMUnityPort")
# parse dtile
b=(ROOT/"Extract/TileData/dtile.bin").read_bytes(); n=struct.unpack_from("<I",b,0)[0]
tiles=[]
for i in range(n):
 q=4+i*26
 name=b[q:q+16].split(b"\0",1)[0].decode("latin1","replace")
 id_,bmp,ts,sect,attr=struct.unpack_from("<5h",b,q+16)
 tiles.append((name,ts,sect,attr&0xffff))
cfg={"SweetWater":"outa1.odm","ParadiseValley":"outa2.odm","HermitsIsle":"outa3.odm","Kriegspire":"outb1.odm","Blackshire":"outb2.odm","Dragonsand":"outb3.odm","FrozenHighlands":"outc1.odm","FreeHaven":"outc2.odm","MireOfTheDamned":"outc3.odm","SilverCove":"outd1.odm","BootlegBay":"outd2.odm","CastleIronfist":"outd3.odm","EelInfestedWaters":"oute1.odm","MistyIslands":"oute2.odm","NewSorpigal":"oute3.odm"}
for z,f in cfg.items():
 raw=(ROOT/"Extract"/z/f).read_bytes(); td=struct.unpack_from("<8H",raw,160); tm=raw[180+128*128:180+2*128*128]
 c=collections.Counter(); wat=sho=road=0
 for v in tm:
  if 90<=v<=125: idx=v-90+td[1]
  elif 126<=v<=161: idx=v
  elif 162<=v<=197: idx=v-162+td[5]
  elif v>=198: idx=v-198+td[7]
  else: idx=v
  name,ts,sect,attr=tiles[idx];c[ts]+=1
  wat += bool(attr&0x0002 and not attr&0x0100)
  sho += bool(attr&0x0100)
  road += ts>=8
 print(z,'td',td,'groups',dict(sorted(c.items())),'deep',wat,'shore',sho,'road',road)
