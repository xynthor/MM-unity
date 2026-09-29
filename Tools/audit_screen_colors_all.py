from PIL import Image
from pathlib import Path
root=Path(r"C:\MMUnityPort\Validation\AuditScreens")
for p in sorted(root.glob("*.png")):
    im=Image.open(p).convert("RGB").resize((256,256))
    pix=list(im.getdata()); n=len(pix)
    green=sum(1 for r,g,b in pix if g>r*1.06 and g>b*1.03 and g>55)
    blue=sum(1 for r,g,b in pix if b>r*1.1 and b>g*.9 and b>50)
    brown=sum(1 for r,g,b in pix if r>g*.9 and g>b*1.05 and r>65)
    white=sum(1 for r,g,b in pix if r>175 and g>175 and b>170)
    dark=sum(1 for r,g,b in pix if r+g+b<120)
    mag=sum(1 for r,g,b in pix if r>180 and b>150 and g<100)
    mean=tuple(round(sum(q[i] for q in pix)/n,1) for i in range(3))
    print(f"{p.name:42s} green={green/n*100:5.1f} blue={blue/n*100:5.1f} brown={brown/n*100:5.1f} white={white/n*100:5.1f} dark={dark/n*100:4.1f} mag={mag/n*100:4.2f} mean={mean}")
