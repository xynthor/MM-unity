from PIL import Image,ImageEnhance,ImageOps
from pathlib import Path
root=Path(r"C:\MMUnityPort")
out=root/"Assets/EnvironmentAssets/Biomes/ReferenceWorld"
out.mkdir(parents=True,exist_ok=True)
def load(rel,size=(1024,1024)):
    return Image.open(root/rel).convert("RGB").resize(size,Image.Resampling.LANCZOS)
def save(im,name):
    p=out/name; im.save(p,quality=95); print(p)
grass=load("Assets/EnvironmentAssets/UnitySamples/ground_grass_fells_mossy_CH.png")
soil=load("Assets/EnvironmentAssets/UnitySamples/dry_soil_CH.png")
swamp=load("Assets/EnvironmentAssets/Biomes/swamp_ground.png")
black=load("Assets/EnvironmentAssets/Biomes/black_soil.png")
cold=load("Assets/EnvironmentAssets/Biomes/cold_rock.png")
sand=load("Assets/EnvironmentAssets/Biomes/sand.png")
coast=load("Assets/Environment/PolyHaven/Textures/coast_sand_02/coast_sand_02_diff_1k.jpg")
# Velen: muted grass-soil, visibly natural but not uniformly brown.
velen=Image.blend(grass,soil,.52)
velen=ImageEnhance.Color(velen).enhance(.82)
velen=ImageEnhance.Contrast(velen).enhance(.95)
save(velen,"velen_mud_grass.png")
# Wet green: same family as green world, darker/moister.
mire=Image.blend(grass,swamp,.30)
mire=ImageEnhance.Color(mire).enhance(1.06)
mire=ImageEnhance.Brightness(mire).enhance(.94)
save(mire,"mire_wet_green.png")
# Neutral volcanic grey: no white ash, no warm brown.
volc=Image.blend(black,cold,.26)
volc=ImageOps.grayscale(volc).convert("RGB")
volc=ImageEnhance.Contrast(volc).enhance(1.18)
volc=ImageEnhance.Brightness(volc).enhance(.88)
save(volc,"volcanic_grey.png")
# Warm desert: sand texture, brighter/yellower but still photographic.
des=Image.blend(sand,coast,.32)
r,g,b=des.split()
r=r.point(lambda x:min(255,int(x*1.10+4)))
g=g.point(lambda x:min(255,int(x*1.07+2)))
b=b.point(lambda x:min(255,int(x*.90)))
des=Image.merge("RGB",(r,g,b))
des=ImageEnhance.Contrast(des).enhance(.96)
save(des,"desert_warm.png")
