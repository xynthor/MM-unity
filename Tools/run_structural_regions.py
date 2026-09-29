import pathlib,subprocess,sys
root=pathlib.Path('Validation/StructuralQA')
for scene in sorted(pathlib.Path('Assets/Scenes').glob('*.unity')):
 if scene.name.startswith('_Recovery_') or scene.stem=='Enroth_Linked_OpenWorld':continue
 if (root/(scene.stem+'_done.txt')).exists():continue
 (root/'next.txt').write_text(scene.as_posix())
 p=subprocess.run([sys.executable,'-u','Tools/probe_unity_mcp.py','150','Tools/StructuralCleanup.cs'],capture_output=True,text=True,encoding='utf-8',timeout=190)
 (root/(scene.stem+'_mcp.log')).write_text(p.stdout+p.stderr,encoding='utf-8')
 if not (root/(scene.stem+'_done.txt')).exists():
  print(scene.stem+': FAILED',flush=True);break
 print(scene.stem+': PASS',flush=True)
