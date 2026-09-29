from pathlib import Path
from PIL import Image,ImageDraw,ImageFont
root=Path(r"C:/MMUnityPort/Validation/CanalAudit_20260920")
rows=[]
for line in (root/"candidate_clusters.tsv").read_text().splitlines():
    p=line.split("\t"); rows.append((int(p[0]),p[1],float(p[2]),float(p[3]),p[9]))
font=ImageFont.load_default()
for batch in range(3):
    subset=rows[batch*5:(batch+1)*5]
    W,rowH=1200,430
    sheet=Image.new("RGB",(W,rowH*len(subset)),(20,20,20)); d=ImageDraw.Draw(sheet)
    for j,(i,scene,cx,cz,ids) in enumerate(subset):
        top=Image.open(root/f"cluster_{i:02d}_{scene}_top.png").convert("RGB")
        obl=Image.open(root/f"cluster_{i:02d}_{scene}_oblique.png").convert("RGB")
        top.thumbnail((570,380)); obl.thumbnail((570,380))
        y=j*rowH+42
        sheet.paste(top,(10,y)); sheet.paste(obl,(610,y))
        d.text((10,j*rowH+8),f"{i}. {scene} center=({cx:.1f},{cz:.1f}) ids={ids}",fill=(255,255,255),font=font)
    sheet.save(root/f"review_{batch+1}.jpg",quality=82,optimize=True)
print("done")
