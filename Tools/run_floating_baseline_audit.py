import pathlib,subprocess,sys
root=pathlib.Path(r"C:/MMUnityPort"); out=root/"Validation/FloatingTreeCheck_20260920/Baseline"; out.mkdir(parents=True,exist_ok=True)
scenes=["Blackshire_SourceGrid","BootlegBay_SourceGrid","CastleIronfist_SourceGrid","DragonIsle_Reference","Dragonsand_SourceGrid","EelInfestedWaters_SourceGrid","FreeHaven_SourceGrid","FrozenHighlands_SourceGrid","HermitsIsle_SourceGrid","Kriegspire_SourceGrid","MireOfTheDamned_SourceGrid","MistyIslands_SourceGrid","NewSorpigal_OpenWorld","ParadiseValley_SourceGrid","SilverCove_SourceGrid","SweetWater_SourceGrid"]
for n in scenes:
 (out/"next.txt").write_text(f"Assets/Scenes/{n}.unity")
 r=subprocess.run([sys.executable,"-u","Tools/probe_unity_mcp.py","180","Tools/FloatingTreeBaselineAudit.cs"],cwd=root,text=True,capture_output=True)
 (out/f"{n}_mcp.log").write_text(r.stdout+r.stderr)
 print(n+(": PASS" if (out/f"{n}.txt").exists() else ": FAIL"),flush=True)
 if not (out/f"{n}.txt").exists(): break
