from PIL import Image
from pathlib import Path
for p in Path('Assets/Environment/PolyHaven/Models/fir_sapling/textures').glob('*.png'):
 try:
  im=Image.open(p);print(p.name, im.format, im.mode,im.size)
 except Exception as e:print(p.name,p.read_bytes()[:24],str(e))
