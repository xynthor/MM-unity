import pathlib,subprocess,sys
root=pathlib.Path(r"C:/MMUnityPort")
scenes=[
"Blackshire_SourceGrid","BootlegBay_SourceGrid","CastleIronfist_SourceGrid","DragonIsle_Reference","Dragonsand_SourceGrid","EelInfestedWaters_SourceGrid","FreeHaven_SourceGrid","FrozenHighlands_SourceGrid","HermitsIsle_SourceGrid","Kriegspire_SourceGrid","MireOfTheDamned_SourceGrid","MistyIslands_SourceGrid","NewSorpigal_OpenWorld","ParadiseValley_SourceGrid","SilverCove_SourceGrid","SweetWater_SourceGrid"]
out=root/"Validation/FinalWorld"; probe=root/"Tools/probe_unity_mcp.py"; script=root/"Tools/FinalPineBCPass.cs"
for name in scenes:
 p=f"Assets/Scenes/{name}.unity"; (out/"next_pinebc.txt").write_text(p)
 if (out/f"{name}_pinebc_done.txt").exists(): print(name+": SKIP"); continue
 r=subprocess.run([sys.executable,"-u",str(probe),"240",str(script)],cwd=root,text=True,capture_output=True)
 (out/f"{name}_pinebc_mcp.log").write_text(r.stdout+r.stderr)
 print(name+(": PASS" if (out/f"{name}_pinebc_done.txt").exists() else ": FAIL"),flush=True)
 if not (out/f"{name}_pinebc_done.txt").exists(): break
