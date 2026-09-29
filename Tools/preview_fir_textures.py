from PIL import Image
from pathlib import Path
for n in ['branches_diff','twigs_diff','twigs_alpha']:
 im=Image.open('Assets/Environment/PolyHaven/Models/fir_sapling/textures/fir_sapling_'+n+'_1k.png')
 if im.mode.startswith('I'):im=im.point(lambda p:p/256).convert('L')
 im.convert('RGB').save('Validation/VisualRefinement/inspect_'+n+'.jpg')
