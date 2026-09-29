import csv,glob,os,collections
base=r"C:\MMUnityPort\Validation\SixTerrainAudit"
all=[]
for f in glob.glob(base+r"\*.csv"):
 if os.path.basename(f)=="summary.csv":continue
 z=os.path.basename(f).replace(".csv","")
 for r in csv.DictReader(open(f,encoding="utf-8")):
  if r["status"]!="OK":
   sx=int(r["sx"]);sy=int(r["sy"]);dist=min(sx,sy,127-sx,127-sy)
   weights={k:float(r[k]) for k in ["green","light_green","desert","volcanic","snow","arid","road"]}
   dom=max(weights,key=weights.get)
   all.append((z,sx,sy,dist,r["expected"],r["dominant"],float(r["expectedWeight"]),dom,weights[dom],r["group"],r["flags"]))
print("TOTAL",len(all))
print("BY BORDER DIST",collections.Counter(x[3] for x in all))
print("BY expected->dominant",collections.Counter((x[4],x[5]) for x in all))
for z in sorted(set(x[0] for x in all)):
 xs=[x for x in all if x[0]==z]
 if not xs:continue
 print("\n",z,len(xs),"border<=6",sum(x[3]<=6 for x in xs),"interior",sum(x[3]>6 for x in xs))
 for x in xs[:60]:print(x)
