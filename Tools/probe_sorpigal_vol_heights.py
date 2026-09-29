from pathlib import Path
d=Path(r"C:\MMUnityPort\Assets\World\NewSorpigal\Data")
tm=(d/"tilemap_u8.bin").read_bytes(); gr=(d/"tile_groups_u8.bin").read_bytes(); se=(d/"tile_semantics_u8.bin").read_bytes(); hm=(d/"heightmap_u8.bin").read_bytes()
N=128; S=set()
for sy in range(N):
 for sx in range(N):
  raw=tm[sy*N+sx]; f=se[raw]
  if gr[raw]==3 and not(f&1):S.add((sx,sy))
comps=[]
while S:
 seed=S.pop(); q=[seed]; c=[seed]
 for x,y in q:
  for n in ((x+1,y),(x-1,y),(x,y+1),(x,y-1)):
   if n in S:S.remove(n);q.append(n);c.append(n)
 comps.append(c)
comps.sort(key=len,reverse=True)
for i,c in enumerate(comps):
 vals=[hm[y*N+x] for x,y in c]
 xs=[x for x,y in c];ys=[y for x,y in c]
 print(i,len(c),"bbox",min(xs),max(xs),min(ys),max(ys),"havg",sum(vals)/len(vals),"hmax",max(vals),"hmin",min(vals))
