import csv,glob,os
print("FIXED within 22m of corners")
for f in glob.glob(r"C:\MMUnityPort\Validation\PositionFreeze\*_baseline.csv"):
 z=os.path.basename(f).replace("_baseline.csv",""); hit=[]
 for r in csv.DictReader(open(f,encoding="utf-8")):
  try:x=float(r["x"]);q=float(r["z"])
  except:continue
  if abs(x)>234 and abs(q)>234:hit.append((r["key"],round(x,2),round(q,2)))
 if hit:print(z,len(hit),hit[:10])
print("VEG within 22m corners")
from collections import Counter
c=Counter()
for r in csv.DictReader(open(r"C:\MMUnityPort\Validation\RealisticTreePlacementAudit.csv",encoding="utf-8")):
 if r["status"].startswith("FAILED"):continue
 try:x=float(r["x"]);q=float(r["z"])
 except:continue
 if abs(x)>234 and abs(q)>234:c[r["zone"]]+=1
print(dict(c))
