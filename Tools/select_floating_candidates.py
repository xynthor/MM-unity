from pathlib import Path
from collections import defaultdict
from statistics import median
root=Path(r"C:/MMUnityPort/Validation/FloatingTreeCheck_20260920/Baseline")
rows=[]
for f in root.glob("*.txt"):
    if f.name=="next.txt": continue
    for line in f.read_text(errors="ignore").splitlines():
        p=line.split("|")
        if len(p)<10: continue
        gap=float(p[3]); fam="|".join(p[9:])
        rows.append([p[0],p[1],gap,fam])
g=defaultdict(list)
for r in rows:g[r[3]].append(r[2])
sel=[]
for scene,full,gap,fam in rows:
    vals=g[fam]; med=median(vals); mad=median([abs(v-med) for v in vals])
    resid=gap-med
    if len(vals)>=4 and gap>0.10 and resid>max(0.20,6*mad+0.05):
        sel.append((scene,full,resid,gap,med))
out=root.parent/"floating_selected.tsv"
out.write_text("\n".join(f"{s}\t{p}\t{d:.9f}\t{g0:.9f}\t{m:.9f}" for s,p,d,g0,m in sel))
print(len(sel))
