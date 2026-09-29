import csv
from pathlib import Path
d=Path(r"C:\MMUnityPort\Assets\World\NewSorpigal\Data")
tm=(d/"tilemap_u8.bin").read_bytes(); gr=(d/"tile_groups_u8.bin").read_bytes()
rows={int(r["raw"]):r for r in csv.DictReader(open(d/"tile_truth_resolved.csv",encoding="utf-8"))}
used=sorted(set(tm))
for gtarget in (0,3,255):
 print("GROUP",gtarget)
 for raw in used:
  if gr[raw]==gtarget:
   r=rows.get(raw,{})
   print(raw,r.get("name"),r.get("tileset"),r.get("section"),r.get("shore"),r.get("transition"),r.get("road"))
