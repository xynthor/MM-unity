from pathlib import Path
from PIL import Image,ImageDraw,ImageFont
root=Path(r"C:/MMUnityPort/Validation/CanalApproval_20260920")
pairs=[
("Free Haven",root/"FreeHaven_top.png",root/"FreeHaven_after_top.png"),
("Mire of the Damned",root/"Mire_top.png",root/"Mire_after_top.png"),
("White Cap / Frozen Highlands",root/"WhiteCap_candidate47_top.png",root/"WhiteCap_after_top.png"),
]
font=ImageFont.load_default()
W,H=900,360
sheet=Image.new("RGB",(W*2,H*3+40),(20,20,20));d=ImageDraw.Draw(sheet)
d.text((10,10),"BEFORE",fill=(255,255,255),font=font);d.text((W+10,10),"AFTER",fill=(255,255,255),font=font)
for i,(name,b,a) in enumerate(pairs):
 y=40+i*H
 for j,p in enumerate((b,a)):
  im=Image.open(p).convert("RGB"); im.thumbnail((W-10,H-30))
  sheet.paste(im,(j*W+(W-im.width)//2,y+25))
 d.text((10,y+5),name,fill=(255,255,0),font=font)
sheet.save(root/"CANALS_before_after.jpg",quality=84,optimize=True)
# linked
im=Image.open(root/"Linked_after_water_fix.png").convert("RGB"); im.thumbnail((1200,780)); im.save(root/"Linked_after_water_fix_small.jpg",quality=84,optimize=True)
print("done")
