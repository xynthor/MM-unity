from pathlib import Path
from PIL import Image,ImageDraw
root=Path('Validation/VisualRefinement')
names=sorted(p.name.removesuffix('_after_0.png') for p in root.glob('*_after_0.png'))
for i in range(0,len(names),4):
 sheet=Image.new('RGB',(1650,1580),(30,30,30));draw=ImageDraw.Draw(sheet)
 for row,name in enumerate(names[i:i+4]):
  for col in range(3):
   p=root/(name+'_after_'+str(col)+'.png')
   if p.exists():
    im=Image.open(p);im.thumbnail((550,375));sheet.paste(im,(col*550,row*395+20))
   draw.text((col*550+4,row*395+3),name+' view '+str(col),fill='white')
 sheet.save(root/('ground_contact_'+str(i//4+1)+'.jpg'))
