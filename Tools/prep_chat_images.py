from PIL import Image
from pathlib import Path
root=Path(r"C:/MMUnityPort/Validation/CanalApproval_20260920")
for src,dst,size in [
("CANALS_before_after.jpg","CANALS_post_chat.jpg",(1000,650)),
("Linked_after_water_fix_small.jpg","LINKED_post_chat.jpg",(1000,650))
]:
 im=Image.open(root/src).convert("RGB")
 im.thumbnail(size)
 im.save(root/dst,quality=48,optimize=True)
 print(dst,(root/dst).stat().st_size)
