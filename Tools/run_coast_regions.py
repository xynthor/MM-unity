import pathlib,subprocess,sys,json,re
root=pathlib.Path('Validation/Coast17')
for scene in sorted(pathlib.Path('Assets/Scenes').glob('*.unity')):
 if scene.name.startswith('_Recovery_') or scene.stem=='Enroth_Linked_OpenWorld':continue
 out=root/scene.stem
 if (out/'done.txt').exists():continue
 (root/'next.txt').write_text(scene.as_posix())
 p=subprocess.run([sys.executable,'-u','Tools/probe_unity_mcp.py','180','Tools/CoastRegionPass.cs'],capture_output=True,text=True,encoding='utf-8',timeout=220)
 out.mkdir(exist_ok=True)
 (out/'mcp.log').write_text(p.stdout+p.stderr,encoding='utf-8')
 response=json.loads(pathlib.Path('Validation/SequentialRepair/unity_mcp_last_call.json').read_text())
 (out/'mcp_response.json').write_text(json.dumps(response,indent=2))
 if not (out/'done.txt').exists():
  print(scene.stem+': FAILED (see mcp.log)',flush=True)
 else:print(scene.stem+': saved and invariant-checked',flush=True)
print('REGION_PASS_FINISHED',flush=True)
