from PIL import Image
from pathlib import Path
root=Path(r"C:/MMUnityPort/Validation/CanalApproval_20260920")
im=Image.open(root/"Linked_after_water_fix_small.jpg").convert("RGB")
im.thumbnail((500,330))
im.save(root/"LINKED_post_chat_tiny.jpg",quality=28,optimize=True)
print((root/"LINKED_post_chat_tiny.jpg").stat().st_size)
