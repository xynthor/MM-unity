from pathlib import Path
from PIL import Image,ImageDraw
folders=sorted(p for p in Path('Validation/SequentialRepair').glob('structural_qa_*') if p.is_dir())
out=Path('Validation/StructuralQA')
for shot in ['oblique','shoreline','settlement']:
 for page in range(2):
  canvas=Image.new('RGB',(1600,1240),'#222222');draw=ImageDraw.Draw(canvas)
  for j,folder in enumerate(folders[page*8:(page+1)*8]):
   x=j%2*800;y=j//2*310
   draw.text((x+4,y+4),folder.name.replace('structural_qa_',''),fill='white')
   im=Image.open(folder/(shot+'.png'));im.thumbnail((780,284));canvas.paste(im,(x+4,y+24))
  canvas.save(out/(shot+'_'+str(page+1)+'.jpg'))
print('Contact sheets written for '+str(len(folders))+' regions')
