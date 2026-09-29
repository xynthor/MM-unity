from pathlib import Path
from PIL import Image,ImageDraw,ImageFont
root=Path(r"C:/MMUnityPort/Validation/CanalAudit_20260920")
rows=[]
for line in (root/"candidate_clusters.tsv").read_text().splitlines():
    p=line.split("\t"); rows.append((int(p[0]),p[1],float(p[2]),float(p[3]),p[9]))
font=ImageFont.load_default()
tileW,tileH=600,360
sheet=Image.new("RGB",(tileW*3,tileH*5),(20,20,20)); d=ImageDraw.Draw(sheet)
for idx,(i,scene,cx,cz,ids) in enumerate(rows):
    x=(idx%3)*tileW; y=(idx//3)*tileH
    top=Image.open(root/f"cluster_{i:02d}_{scene}_top.png").convert("RGB")
    obl=Image.open(root/f"cluster_{i:02d}_{scene}_oblique.png").convert("RGB")
    top.thumbnail((285,300)); obl.thumbnail((285,300))
    sheet.paste(top,(x+5,y+45)); sheet.paste(obl,(x+305,y+45))
    d.text((x+5,y+5),f"{i}. {scene}",fill=(255,255,255),font=font)
    d.text((x+5,y+20),f"center=({cx:.1f},{cz:.1f}) ids={ids}",fill=(255,255,255),font=font)
sheet.save(root/"MASTER_REVIEW.jpg",quality=88,optimize=True)
print(root/"MASTER_REVIEW.jpg")
