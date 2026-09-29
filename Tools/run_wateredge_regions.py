import pathlib,subprocess,sys
root=pathlib.Path(r"C:/MMUnityPort"); out=root/"Validation/FinalWorld"
scenes=["Blackshire_SourceGrid","BootlegBay_SourceGrid","CastleIronfist_SourceGrid","DragonIsle_Reference","Dragonsand_SourceGrid","EelInfestedWaters_SourceGrid","FreeHaven_SourceGrid","FrozenHighlands_SourceGrid","HermitsIsle_SourceGrid","Kriegspire_SourceGrid","MireOfTheDamned_SourceGrid","MistyIslands_SourceGrid","NewSorpigal_OpenWorld","ParadiseValley_SourceGrid","SilverCove_SourceGrid","SweetWater_SourceGrid"]
for name in scenes:
 (out/"next_water.txt").write_text(f"Assets/Scenes/{name}.unity")
 if (out/f"{name}_wateredge_done.txt").exists(): print(name+": SKIP"); continue
 r=subprocess.run([sys.executable,"-u","Tools/probe_unity_mcp.py","240","Tools/WaterBoundarySmooth.cs"],cwd=root,text=True,capture_output=True)
 (out/f"{name}_wateredge_mcp.log").write_text(r.stdout+r.stderr)
 print(name+(": PASS" if (out/f"{name}_wateredge_done.txt").exists() else ": FAIL"),flush=True)
 if not (out/f"{name}_wateredge_done.txt").exists(): break
