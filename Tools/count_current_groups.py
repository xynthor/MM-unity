from pathlib import Path
from collections import Counter,defaultdict
ROOT=Path(r"C:\MMUnityPort\Assets\World")
zones=["SweetWater","ParadiseValley","HermitsIsle","Kriegspire","Blackshire","Dragonsand","FrozenHighlands","FreeHaven","MireOfTheDamned","SilverCove","BootlegBay","CastleIronfist","EelInfestedWaters","MistyIslands","NewSorpigal"]
for z in zones:
 d=ROOT/z/"Data"; tm=(d/"tilemap_u8.bin").read_bytes(); gr=(d/"tile_groups_u8.bin").read_bytes()
 c=Counter(); q=defaultdict(Counter)
 for y in range(128):
  for x in range(128):
   g=gr[tm[y*128+x]]; c[g]+=1; q[("N" if y<64 else "S")+("W" if x<64 else "E")][g]+=1
 print(z,dict(c))
 print(" ",{k:dict(v) for k,v in q.items()})
