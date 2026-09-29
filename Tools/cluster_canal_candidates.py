from pathlib import Path
import csv,math
root=Path(r"C:/MMUnityPort/Validation/CanalAudit_20260920")
by={}
for f in root.glob("*.csv"):
    for r in csv.DictReader(f.open()):
        if not r.get("id"): continue
        for k in ("cells","length_m","width_m","mean_width_m","waterY","endpoint1_x","endpoint1_z","endpoint2_x","endpoint2_z","center_x","center_z"):
            r[k]=float(r[k])
        by.setdefault(r["scene"],[]).append(r)
clusters=[]
for scene,rows in by.items():
    unused=set(range(len(rows)))
    while unused:
        seed=unused.pop(); members={seed}; changed=True
        while changed:
            changed=False
            for j in list(unused):
                if any(math.hypot(rows[j]["center_x"]-rows[i]["center_x"],rows[j]["center_z"]-rows[i]["center_z"])<=28 for i in members):
                    unused.remove(j);members.add(j);changed=True
        rr=[rows[i] for i in members]
        clusters.append(dict(scene=scene,n=len(rr),cells=sum(r["cells"] for r in rr),maxlen=max(r["length_m"] for r in rr),
            cx=sum(r["center_x"]*r["cells"] for r in rr)/sum(r["cells"] for r in rr),
            cz=sum(r["center_z"]*r["cells"] for r in rr)/sum(r["cells"] for r in rr),
            water=rr[0]["waterY"],ids=";".join(str(int(float(r["id"]))) for r in rr),
            minx=min(min(r["endpoint1_x"],r["endpoint2_x"]) for r in rr),maxx=max(max(r["endpoint1_x"],r["endpoint2_x"]) for r in rr),
            minz=min(min(r["endpoint1_z"],r["endpoint2_z"]) for r in rr),maxz=max(max(r["endpoint1_z"],r["endpoint2_z"]) for r in rr)))
clusters.sort(key=lambda c:(c["n"],c["cells"],c["maxlen"]),reverse=True)
(root/"candidate_clusters.txt").write_text("\n".join(f'{c["scene"]}|n={c["n"]}|cells={int(c["cells"])}|maxlen={c["maxlen"]:.1f}|center=({c["cx"]:.1f},{c["cz"]:.1f})|bbox=({c["minx"]:.1f},{c["minz"]:.1f})-({c["maxx"]:.1f},{c["maxz"]:.1f})|water={c["water"]:.3f}|ids={c["ids"]}' for c in clusters))
print("clusters",len(clusters))
for c in clusters:
    if c["n"]>=2 or c["maxlen"]>=12:
        print(f'{c["scene"]} n={c["n"]} cells={int(c["cells"])} maxlen={c["maxlen"]:.1f} center=({c["cx"]:.1f},{c["cz"]:.1f}) ids={c["ids"]}')
