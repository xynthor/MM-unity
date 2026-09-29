from pathlib import Path
import subprocess,sys,json
root=Path('Validation/VisualRefinement')
scenes=sorted(p for p in Path('Assets/Scenes').glob('*.unity') if not p.name.startswith('_Recovery_') and not p.name.startswith('Enroth_Linked'))
for i,p in enumerate(scenes,1):
 root.joinpath('next.txt').write_text(p.as_posix())
 for stage,script in [('before','VisualRefinementCapture.cs'),('edit','VisualRefinementMaterials.cs'),('after','VisualRefinementCapture.cs')]:
  if stage=='before' and root.joinpath(p.stem+'_before_0.png').exists():continue
  if stage=='edit' and p.stem=='BootlegBay_SourceGrid':continue
  root.joinpath('stage.txt').write_text(stage)
  r=subprocess.run([sys.executable,'-u','Tools/probe_unity_mcp.py','180','Tools/'+script],capture_output=True,text=True)
  root.joinpath(p.stem+'_'+stage+'_mcp.txt').write_text(r.stdout+r.stderr)
  if r.returncode or '"success": true' not in r.stdout:print(p.stem,stage,r.stdout[-1500:],flush=True);raise SystemExit(1)
 print(str(i)+'/16 '+p.stem,flush=True)
