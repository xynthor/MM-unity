import pathlib,subprocess,sys
root=pathlib.Path(r"C:/MMUnityPort"); out=root/"Validation/FloatingTreeCheck_20260920"
scenes=sorted({line.split("\t")[0] for line in (out/"floating_selected.tsv").read_text().splitlines() if line.strip()})
for n in scenes:
 (out/"apply_next.txt").write_text(f"Assets/Scenes/{n}.unity")
 r=subprocess.run([sys.executable,"-u","Tools/probe_unity_mcp.py","180","Tools/ApplyFloatingTreeYOnlySelected.cs"],cwd=root,text=True,capture_output=True)
 (out/f"{n}_apply_mcp.log").write_text(r.stdout+r.stderr)
 print(n+(": PASS" if (out/f"{n}_confirmed_y_only.txt").exists() else ": FAIL"),flush=True)
 if not (out/f"{n}_confirmed_y_only.txt").exists(): break
