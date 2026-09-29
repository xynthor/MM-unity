from pathlib import Path
from PIL import Image
root=Path(r"C:/MMUnityPort/Validation/CanalAudit_20260920")
for f in root.glob("cluster_*_*.png"):
    im=Image.open(f).convert("RGB")
    im.thumbnail((640,640))
    im.save(root/(f.stem+"_small.jpg"),quality=72,optimize=True)
print("done")
