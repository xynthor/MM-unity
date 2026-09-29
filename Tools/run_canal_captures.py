import pathlib,subprocess,sys
root=pathlib.Path(r"C:/MMUnityPort"); out=root/"Validation/CanalAudit_20260920"
scenes=[]
for line in (out/"candidate_clusters.tsv").read_text().splitlines():
    s=line.split("\t")[1]
    if s not in scenes: scenes.append(s)
for s in scenes:
    (out/"capture_next.txt").write_text(s)
    r=subprocess.run([sys.executable,"-u","Tools/probe_unity_mcp.py","180","Tools/CanalCandidateCapture.cs"],cwd=root,text=True,capture_output=True)
    (out/f"{s}_capture_mcp.log").write_text(r.stdout+r.stderr)
    print(s, "done", flush=True)
