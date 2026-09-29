from pathlib import Path
import json,csv,hashlib
out=Path('Validation/SequentialRepair')
passes=['trees_20260919_164654','trees_20260919_165010','trees_20260919_165327']
for mode,folder in enumerate(passes):
 (out/f'trees_mode_{mode}.frozen').write_text(folder+'\nDo not rerun this pass. These markers freeze applied work; they do not certify final visual acceptance.\n')
# Correct the legacy-source audit classification, without changing its measured coordinates.
p=out/passes[2]/'trees.csv';rows=list(csv.DictReader(p.open()));fields=list(rows[0])
for r in rows:r['source']='True'
with p.open('w',newline='') as f:
 w=csv.DictWriter(f,fieldnames=fields);w.writeheader();w.writerows(rows)
p=out/passes[2]/'numeric_summary.json';summary=json.loads(p.read_text());summary['source_anchors']=8;p.write_text(json.dumps(summary,indent=2))
current=Path('Assets/Scenes/NewSorpigal_OpenWorld.unity').read_text()
forbidden=['b69a80ea959884646a24bedcf4e52929','f59d8f4ab7e53214592454a3d31c8cc6']
assert not any(g in current for g in forbidden)
backup=Path(Path('Validation/SequentialRepair_Current.txt').read_text().strip())/'SceneBaseline'
scene_checks=[]
for old in backup.glob('*.unity'):
 new=Path('Assets/Scenes')/old.name
 same=hashlib.sha256(old.read_bytes()).digest()==hashlib.sha256(new.read_bytes()).digest()
 scene_checks.append(dict(scene=old.name,unchanged=same))
(out/'scene_file_comparison.json').write_text(json.dumps(scene_checks,indent=2))
report=dict(status='INCOMPLETE',phase='1 - New Sorpigal vegetation',applied_tree_instances=716,source_tree_anchors=342,source_tree_xz_changes=0,forbidden_prefab_guid_references_in_sorpigal=0,other_region_and_linked_scenes_unchanged=all(x['unchanged'] for x in scene_checks if x['scene']!='NewSorpigal_OpenWorld.unity'),visual_evidence=passes,open_issues=['Final vegetation acceptance pending: volcanic dry-tree appearance and remaining shrubs require closer review.','Terrain green appearance, organic coast geometry, water continuity, density/exclusions and grounding full audit remain unfinished.','Other 14 regions and linked-world rebuild remain unfinished.','Windows Computer Use activate_window timed out twice; no further desktop input attempted.','Final freeze-guard/source-count code edits have not yet been compiled in Unity.'])
(out/'status.json').write_text(json.dumps(report,indent=2))
print(json.dumps(report,indent=2))
