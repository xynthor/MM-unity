from PIL import Image
from pathlib import Path
for z in ['NewSorpigal_OpenWorld','CastleIronfist_SourceGrid','MistyIslands_SourceGrid','Enroth_Linked_OpenWorld']:
 p=Path(r'C:\MMUnityPort\Validation\AuditScreens')/(z+'.png')
 im=Image.open(p).convert('RGB').resize((256,256))
 pix=list(im.getdata()); n=len(pix)
 cats={'green':0,'blue':0,'gray':0,'brown':0,'white':0,'dark':0}
 for r,g,b in pix:
  if g>r*1.06 and g>b*1.03 and g>55: cats['green']+=1
  if b>r*1.1 and b>g*.9 and b>50: cats['blue']+=1
  if max(r,g,b)-min(r,g,b)<14: cats['gray']+=1
  if r>g*.9 and g>b*1.05 and r>65: cats['brown']+=1
  if r>175 and g>175 and b>170: cats['white']+=1
  if r+g+b<120: cats['dark']+=1
 print(z,{k:round(v/n*100,1) for k,v in cats.items()},'mean',tuple(round(sum(px[i] for px in pix)/n,1) for i in range(3)))
