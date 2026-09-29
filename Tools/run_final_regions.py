import pathlib,subprocess,sys,json
root=pathlib.Path('Validation/FinalWorld')
for scene in sorted(pathlib.Path('Assets/Scenes').glob('*.unity')):
 if scene.name.startswith('_Recovery_') or scene.stem=='Enroth_Linked_OpenWorld':continue
 if (root/(scene.stem+'_done.txt')).exists():continue
 (root/'next.txt').write_text(scene.as_posix())
 p=subprocess.run([sys.executable,'-u','Tools/probe_unity_mcp.py','240','Tools/FinalVegetationPass.cs'],capture_output=True,text=True,encoding='utf-8',timeout=280)
 (root/(scene.stem+'_mcp.log')).write_text(p.stdout+p.stderr,encoding='utf-8')
 print(scene.stem+(': PASS' if (root/(scene.stem+'_done.txt')).exists() else ': FAILED'),flush=True)
 if not (root/(scene.stem+'_done.txt')).exists():break
