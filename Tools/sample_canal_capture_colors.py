from pathlib import Path
from PIL import Image
root=Path(r"C:/MMUnityPort/Validation/CanalAudit_20260920")
for line in (root/"candidate_clusters.tsv").read_text().splitlines():
 p=line.split("\t"); i=int(p[0]); scene=p[1]
 f=root/f"cluster_{i:02d}_{scene}_top.png"; im=Image.open(f).convert("RGB")
 w,h=im.size
 pts={"c":(w//2,h//2),"n":(w//2,h//2-80),"s":(w//2,h//2+80),"e":(w//2+80,h//2),"w":(w//2-80,h//2)}
 vals=[]
 for k,(x,y) in pts.items():
  # mean 9x9
  pix=[im.getpixel((xx,yy)) for xx in range(max(0,x-4),min(w,x+5)) for yy in range(max(0,y-4),min(h,y+5))]
  avg=tuple(sum(v[j] for v in pix)//len(pix) for j in range(3))
  vals.append(f"{k}={avg}")
 print(i,scene," ".join(vals))
