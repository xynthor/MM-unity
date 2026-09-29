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
        scene,full,rel=p[0],p[1],p[2]
        gap,ground,bottom,y,x,z=map(float,p[3:9])
        family="|".join(p[9:])
        rows.append(dict(scene=scene,full=full,rel=rel,gap=gap,ground=ground,bottom=bottom,y=y,x=x,z=z,family=family))
groups=defaultdict(list)
for r in rows: groups[r["family"]].append(r["gap"])
for r in rows:
    vals=groups[r["family"]]
    med=median(vals)
    mad=median([abs(v-med) for v in vals]) if vals else 0
    r["n"]=len(vals);r["median"]=med;r["mad"]=mad;r["resid"]=r["gap"]-med
# conservative: family must have >=4 samples; residual must be clearly above its family norm
cand=[r for r in rows if r["n"]>=4 and r["gap"]>0.10 and r["resid"]>max(0.20,6*r["mad"]+0.05)]
cand.sort(key=lambda r:r["resid"],reverse=True)
out=root.parent/"floating_candidates_family_corrected.txt"
out.write_text("\n".join(f'{r["scene"]}|{r["full"]}|gap={r["gap"]:.6f}|family_median={r["median"]:.6f}|residual={r["resid"]:.6f}|MAD={r["mad"]:.6f}|n={r["n"]}|x={r["x"]:.6f}|z={r["z"]:.6f}' for r in cand))
top=root.parent/"floating_top_residuals.txt"
top.write_text("\n".join(f'{r["scene"]}|{r["full"]}|gap={r["gap"]:.6f}|median={r["median"]:.6f}|residual={r["resid"]:.6f}|MAD={r["mad"]:.6f}|n={r["n"]}' for r in sorted(rows,key=lambda r:r["resid"],reverse=True)[:80]))
print("rows",len(rows),"families",len(groups),"candidates",len(cand))
for r in cand[:60]:
    print(f'{r["scene"]} | {r["full"]} | gap {r["gap"]:.3f} | baseline {r["median"]:.3f} | residual {r["resid"]:.3f} | n {r["n"]}')
