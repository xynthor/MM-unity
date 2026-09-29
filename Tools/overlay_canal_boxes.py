from pathlib import Path
from PIL import Image,ImageDraw,ImageFont
root=Path(r"C:/MMUnityPort/Validation/CanalAudit_20260920")
rows=[]
for line in (root/"candidate_clusters.tsv").read_text().splitlines():
 p=line.split("\t"); i=int(p[0]); scene=p[1]; cx,cz=map(float,p[2:4]); minx,minz,maxx,maxz=map(float,p[4:8]); ids=p[9]
 span=max(maxx-minx,maxz-minz); ortho=max(18,span*.75+10); scale=500/ortho
 f=root/f"cluster_{i:02d}_{scene}_top.png"; im=Image.open(f).convert("RGB"); d=ImageDraw.Draw(im)
 def px(x,z): return (500+(x-cx)*scale,500-(z-cz)*scale)
 x1,y1=px(minx,minz); x2,y2=px(maxx,maxz)
 d.rectangle([min(x1,x2),min(y1,y2),max(x1,x2),max(y1,y2)],outline=(255,0,0),width=6)
 cc=px(cx,cz); d.ellipse([cc[0]-8,cc[1]-8,cc[0]+8,cc[1]+8],fill=(255,0,255))
 d.text((15,15),f"{i} {scene} ids={ids}",fill=(255,255,0))
 out=root/f"cluster_{i:02d}_{scene}_OVERLAY.jpg"; im.thumbnail((600,600)); im.save(out,quality=82)
 rows.append((i,scene,out))
font=ImageFont.load_default(); TW,TH=600,640
sheet=Image.new("RGB",(TW*3,TH*5),(20,20,20)); dd=ImageDraw.Draw(sheet)
for idx,(i,scene,f) in enumerate(rows):
 im=Image.open(f); x=(idx%3)*TW; y=(idx//3)*TH+35; sheet.paste(im,(x,y)); dd.text((x+5,y-25),f"{i}. {scene}",fill=(255,255,255),font=font)
sheet.save(root/"MASTER_OVERLAY.jpg",quality=86,optimize=True)
print("done")
