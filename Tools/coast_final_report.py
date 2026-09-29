import pathlib,json,hashlib
from PIL import Image,ImageDraw
root=pathlib.Path('Validation/Coast17');backup=pathlib.Path('Backups/Coast17_20260919')
reports=[]
for folder in sorted(root.iterdir()):
 if not folder.is_dir() or not (folder/'done.txt').exists():continue
 data=dict(line.split('=',1) for line in (folder/'done.txt').read_text().splitlines() if '=' in line)
 data['scene']=folder.name
 a=backup/(folder.name+'.unity');b=pathlib.Path('Assets/Scenes')/(folder.name+'.unity')
 if a.exists():
  la=a.read_text().splitlines();lb=b.read_text().splitlines()
  changes=[(i+1,x,y) for i,(x,y) in enumerate(zip(la,lb)) if x!=y]
  ok=len(la)==len(lb) and all(x.strip()=='m_Enabled: 1' and y.strip()=='m_Enabled: 0' for _,x,y in changes)
  data['serialized_scene_only_renderer_disables']=ok;data['changed_scene_lines']=len(changes)
  (folder/'serialized_changes.json').write_text(json.dumps(changes,indent=2))
 reports.append(data)
(root/'results.json').write_text(json.dumps(reports,indent=2))
folders=[p for p in sorted(root.iterdir()) if p.is_dir() and (p/'after_detail.png').exists()]
for start in range(0,len(folders),4):
 page=Image.new('RGB',(1280,4*265),(30,30,30));draw=ImageDraw.Draw(page)
 for j,folder in enumerate(folders[start:start+4]):
  draw.text((6,j*265+3),folder.name+'    BEFORE / AFTER',fill='white')
  for k,kind in enumerate(['before','after']):
   im=Image.open(folder/(kind+'_detail.png'));im.thumbnail((630,238));page.paste(im,(k*640,j*265+24))
 page.save(root/('review_'+str(start//4+1)+'.jpg'))
print(json.dumps({'scenes':len(reports),'scene_diff_failures':[r['scene'] for r in reports if not r.get('serialized_scene_only_renderer_disables',False)],'hash_failures':[r['scene'] for r in reports if r['hash_before']!=r['hash_after']],'height_samples_changed':sum(int(r.get('changed_height_samples',0)) for r in reports),'overlay_triangles_disabled':sum(int(r.get('overlay_triangles_disabled',0)) for r in reports)}))
