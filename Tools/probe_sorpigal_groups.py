from pathlib import Path
from collections import deque,Counter
d=Path(r"C:\MMUnityPort\Assets\World\NewSorpigal\Data")
tm=(d/"tilemap_u8.bin").read_bytes(); gr=(d/"tile_groups_u8.bin").read_bytes(); se=(d/"tile_semantics_u8.bin").read_bytes()
N=128
for target in (0,3,255):
    mask=[]
    for sy in range(N):
      for sx in range(N):
        raw=tm[sy*N+sx]; g=gr[raw]; f=se[raw]
        if g==target and not (f&1) and not (f&8): mask.append((sx,sy))
    S=set(mask); comps=[]
    while S:
      seed=S.pop(); q=[seed]; c=[seed]
      for p in q:
        x,y=p
        for n in ((x+1,y),(x-1,y),(x,y+1),(x,y-1)):
          if n in S:S.remove(n);q.append(n);c.append(n)
      comps.append(c)
    comps.sort(key=len,reverse=True)
    print("GROUP",target,"cells",len(mask),"components",len(comps))
    for c in comps[:8]:
      xs=[p[0] for p in c]; ys=[p[1] for p in c]
      wx=[(x-64)*4 for x in xs]; wz=[(64-y)*4 for y in ys]
      print(" size",len(c),"sx",min(xs),max(xs),"sy",min(ys),max(ys),"worldX",min(wx),max(wx),"worldZ",min(wz),max(wz))
