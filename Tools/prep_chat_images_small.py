from PIL import Image
from pathlib import Path
root=Path(r"C:/MMUnityPort/Validation/CanalApproval_20260920")
for src,dst,size in [
("CANALS_before_after.jpg","CANALS_post_chat_small.jpg",(650,430)),
("Linked_after_water_fix_small.jpg","LINKED_post_chat_small.jpg",(650,430))
]:
 im=Image.open(root/src).convert("RGB")
 im.thumbnail(size)
 im.save(root/dst,quality=34,optimize=True)
 print(dst,(root/dst).stat().st_size)
