from pathlib import Path
import csv,collections
p=Path(r"C:/MMUnityPort/Validation/LinkedWaterAudit_20260920/current.tsv")
rows=list(csv.DictReader(p.open(),delimiter="\t"))
g=collections.Counter()
for r in rows:
    n=r["renderer"].lower()
    if not any(k in n for k in ("water","ocean","river","lake","sea")): continue
    if any(k in n for k in ("seabed","bathymetric","bottom")): continue
    g[(r["region"],r["renderer"],r["active"],r["enabled"],r["material"],r["matPath"],r["shader"],r["color"])]+=1
for k,n in g.most_common():
    print(n,"|"," | ".join(k))
