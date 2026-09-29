from pathlib import Path
from collections import Counter
root=Path(r"C:\MMUnityPort\Assets\World")
zones=["NewSorpigal","CastleIronfist","MireOfTheDamned","MistyIslands","EelInfestedWaters","BootlegBay","SilverCove","Dragonsand","HermitsIsle","Blackshire","ParadiseValley","FreeHaven","SweetWater","FrozenHighlands","Kriegspire"]
for z in zones:
 d=root/z/"Data"
 tm=(d/"tilemap_u8.bin").read_bytes(); gr=(d/"tile_groups_u8.bin").read_bytes(); se=(d/"tile_semantics_u8.bin").read_bytes()
 c=Counter(); deep=shore=road=0
 for raw in tm:
  g=gr[raw]; f=se[raw]
  deep+=1 if f&1 else 0
  shore+=1 if f&2 else 0
  rd=bool(f&8 or (8<=g<255))
  road+=1 if rd else 0
  if not (f&1) and not (f&2) and not rd: c[g]+=1
 print(z,"deep",deep,"shore",shore,"road",road,"landGroups",dict(sorted(c.items())))
