from pathlib import Path
import csv,collections
p=Path(r"C:/MMUnityPort/Validation/LinkedWaterAudit_20260920/current.tsv")
rows=list(csv.DictReader(p.open(),delimiter="\t"))
print("ACTIVE ENABLED BY REGION/MATERIAL")
g=collections.Counter()
for r in rows:
    if r["active"]=="True" and r["enabled"]=="True":
        g[(r["region"],r["material"],r["matPath"],r["shader"],r["color"])]+=1
for k,n in g.most_common():
    print(n,"|"," | ".join(k))
