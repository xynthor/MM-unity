from pathlib import Path
import subprocess,sys
out=Path('Validation/VisualRefinement')
for name in ['HermitsIsle_SourceGrid','ParadiseValley_SourceGrid','SweetWater_SourceGrid']:
 out.joinpath('next.txt').write_text('Assets/Scenes/'+name+'.unity');out.joinpath('stage.txt').write_text('after')
 for script in ['VisualRefinementDeadPines.cs','VisualRefinementCapture.cs']:
  r=subprocess.run([sys.executable,'-u','Tools/probe_unity_mcp.py','150','Tools/'+script],capture_output=True,text=True)
  out.joinpath(name+'_'+script+'_mcp.txt').write_text(r.stdout+r.stderr)
  if r.returncode or '"success": true' not in r.stdout:print(r.stdout[-2000:],flush=True);raise SystemExit(1)
 print(name+' dead bark repaired',flush=True)
