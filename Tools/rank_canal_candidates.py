from pathlib import Path
import csv,math
root=Path(r"C:/MMUnityPort/Validation/CanalAudit_20260920")
rows=[]
for f in root.glob("*.csv"):
    try:
        for r in csv.DictReader(f.open()):
            if not r.get("id"): continue
            for k in ("cells","length_m","width_m","mean_width_m","waterY","contact_count","endpoint1_x","endpoint1_z","endpoint2_x","endpoint2_z","center_x","center_z"):
                r[k]=float(r[k])
            r["scene"]=r["scene"]
            r["score"]=r["length_m"]/(max(1,r["mean_width_m"])) + math.log1p(r["cells"])
            rows.append(r)
    except Exception: pass
rows.sort(key=lambda r:r["score"],reverse=True)
sel=[r for r in rows if r["length_m"]>=8 and r["cells"]>=8 and r["mean_width_m"]<=8]
out=root/"ranked_candidates.txt"
out.write_text("\n".join(f'{r["scene"]} id={int(float(r["id"]))} len={r["length_m"]:.1f} width={r["width_m"]:.1f} meanW={r["mean_width_m"]:.2f} cells={int(r["cells"])} center=({r["center_x"]:.1f},{r["center_z"]:.1f}) endpoints=({r["endpoint1_x"]:.1f},{r["endpoint1_z"]:.1f})->({r["endpoint2_x"]:.1f},{r["endpoint2_z"]:.1f}) water={r["waterY"]:.3f} comps={r["water_components"]}' for r in sel))
print("all",len(rows),"ranked",len(sel))
for r in sel[:80]:
    print(f'{r["scene"]} id={int(float(r["id"]))} len={r["length_m"]:.1f} w={r["width_m"]:.1f} mean={r["mean_width_m"]:.2f} cells={int(r["cells"])} center=({r["center_x"]:.1f},{r["center_z"]:.1f}) endpoints=({r["endpoint1_x"]:.1f},{r["endpoint1_z"]:.1f})->({r["endpoint2_x"]:.1f},{r["endpoint2_z"]:.1f})')
