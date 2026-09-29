from pathlib import Path
from PIL import Image,ImageDraw,ImageFont
root=Path(r"C:/MMUnityPort/Validation/CanalAudit_20260920")
rows=[]
for line in (root/"candidate_clusters.tsv").read_text().splitlines():
    p=line.split("\t")
    rows.append((int(p[0]),p[1],float(p[2]),float(p[3])))
font=ImageFont.load_default()
for batch in range(3):
    subset=rows[batch*5:(batch+1)*5]
    W,H=800,650
    sheet=Image.new("RGB",(W,H*len(subset)),(25,25,25))
    d=ImageDraw.Draw(sheet)
    for j,(i,scene,cx,cz) in enumerate(subset):
        f=root/f"cluster_{i:02d}_{scene}_top.png"
        im=Image.open(f).convert("RGB")
        im.thumbnail((W,H-40))
        y=j*H+40
        sheet.paste(im,((W-im.width)//2,y))
        d.text((10,j*H+10),f"{i}. {scene} center=({cx:.1f},{cz:.1f})",fill=(255,255,255),font=font)
    sheet.save(root/f"contact_top_{batch+1}.jpg",quality=90)
print("done")
