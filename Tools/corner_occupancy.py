import csv,glob,os,math
print("FIXED OBJECTS WITHIN 12m OF REGION CORNERS")
for f in glob.glob(r"C:\MMUnityPort\Validation\PositionFreeze\*_baseline.csv"):
    z=os.path.basename(f).replace("_baseline.csv","")
    rows=list(csv.DictReader(open(f,encoding="utf-8")))
    hit=[]
    for r in rows:
        try:x=float(r["x"]);zz=float(r["z"])
        except:continue
        if abs(x)>244 and abs(zz)>244: hit.append((r["key"],x,zz))
    if hit: print(z,len(hit),hit[:8])
print("VEGETATION WITHIN 12m OF REGION CORNERS")
p=r"C:\MMUnityPort\Validation\RealisticTreePlacementAudit.csv"
rows=list(csv.DictReader(open(p,encoding="utf-8")))
from collections import Counter
c=Counter()
for r in rows:
    if r["status"].startswith("FAILED"): continue
    try:x=float(r["x"]);z=float(r["z"])
    except:continue
    if abs(x)>244 and abs(z)>244:c[r["zone"]]+=1
print(dict(c))
