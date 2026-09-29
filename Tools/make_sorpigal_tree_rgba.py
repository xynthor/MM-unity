from PIL import Image
from pathlib import Path
root=Path(r"C:\MMUnityPort")
out=root/"Assets/Materials/RealisticWorld/Sorpigal/Textures"
out.mkdir(parents=True,exist_ok=True)
pairs=[
("island_tree_01",root/"Assets/Environment/PolyHaven/Downloaded/island_tree_01/textures/island_tree_01_leaves_diff_1k.png",root/"Assets/Environment/PolyHaven/Downloaded/island_tree_01/textures/island_tree_01_leaves_alpha_1k.png"),
("island_tree_02",root/"Assets/Environment/PolyHaven/Downloaded/island_tree_02/textures/island_tree_02_leaves_diff_1k.png",root/"Assets/Environment/PolyHaven/Downloaded/island_tree_02/textures/island_tree_02_leaves_alpha_1k.png"),
("island_tree_03",root/"Assets/Environment/PolyHaven/Downloaded/island_tree_03/textures/island_tree_03_leaves_diff_1k.png",root/"Assets/Environment/PolyHaven/Downloaded/island_tree_03/textures/island_tree_03_leaves_alpha_1k.png"),
("jacaranda_tree",root/"Assets/Environment/PolyHaven/Downloaded/jacaranda_tree/textures/jacaranda_tree_leaves_diff_1k.png",root/"Assets/Environment/PolyHaven/Downloaded/jacaranda_tree/textures/jacaranda_tree_leaves_alpha_1k.png"),
]
for name,diff,alpha in pairs:
    rgb=Image.open(diff).convert("RGB")
    a=Image.open(alpha).convert("L")
    if a.size!=rgb.size:a=a.resize(rgb.size)
    rgba=rgb.copy().convert("RGBA"); rgba.putalpha(a)
    rgba.save(out/f"{name}_leaves_rgba.png")
    print(name,rgba.size)
