from PIL import Image,ImageOps,ImageDraw,ImageFont
from pathlib import Path
root=Path(r"C:/MMUnityPort/Validation/CanalApproval_20260920")
a=Image.open(root/"CANALS_post_chat_small.jpg").convert("RGB")
b=Image.open(root/"LINKED_post_chat_tiny.jpg").convert("RGB")
w=max(a.width,b.width)
canvas=Image.new("RGB",(w,a.height+b.height+44),"white")
canvas.paste(a,((w-a.width)//2,0))
d=ImageDraw.Draw(canvas)
d.text((10,a.height+8),"Linked world after water-layer fix",fill="black")
canvas.paste(b,((w-b.width)//2,a.height+34))
out=root/"VALIDATION_POST_CHAT.jpg"
canvas.save(out,quality=28,optimize=True)
print(out, out.stat().st_size, canvas.size)
