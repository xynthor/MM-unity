from PIL import Image
from pathlib import Path
root=Path(r"C:\MMUnityPort\Validation\AuditScreens")
names=["NewSorpigal_OpenWorld.png","CastleIronfist_SourceGrid.png","MistyIslands_SourceGrid.png","EelInfestedWaters_SourceGrid.png","MireOfTheDamned_SourceGrid.png","Dragonsand_SourceGrid.png","HermitsIsle_SourceGrid.png","Kriegspire_SourceGrid.png","SweetWater_SourceGrid.png","FrozenHighlands_SourceGrid.png","FreeHaven_SourceGrid.png","Blackshire_SourceGrid.png","ParadiseValley_SourceGrid.png"]
def cat(rgb):
 r,g,b=rgb
 if r+g+b<105:return ' '
 if r>175 and g>175 and b>170:return 'W'
 if b>r*1.12 and b>g*.98 and b>65:return '~'
 if g>r*1.10 and g>b*1.05 and g>65:return 'G'
 if r>125 and g>105 and b<90 and r/g<1.35:return 'Y'
 if r>g*1.08 and g>b*1.08 and r>70:return 'M'
 if max(r,g,b)-min(r,g,b)<20:return 'D'
 return '.'
for fn in names:
 im=Image.open(root/fn).convert('RGB').resize((32,32))
 print('\n===',fn,'===')
 for y in range(32):
  print(''.join(cat(im.getpixel((x,y))) for x in range(32)))
