from pathlib import Path
p=Path(r"C:/MMUnityPort/Validation/CanalAudit_20260920/candidate_clusters.txt")
out=[]
for i,line in enumerate(p.read_text().splitlines(),1):
    parts=line.split("|")
    scene=parts[0]
    def val(prefix):
        return next(x[len(prefix):] for x in parts if x.startswith(prefix))
    center=val("center=").strip("()").split(",")
    bbox=val("bbox=")
    a,b=bbox.split("-(")
    a=a.strip("()");b=b.strip("()")
    minx,minz=map(float,a.split(","))
    maxx,maxz=map(float,b.split(","))
    out.append("\t".join([str(i),scene,center[0],center[1],str(minx),str(minz),str(maxx),str(maxz),val("water="),val("ids=")]))
Path(r"C:/MMUnityPort/Validation/CanalAudit_20260920/candidate_clusters.tsv").write_text("\n".join(out))
print(len(out))
