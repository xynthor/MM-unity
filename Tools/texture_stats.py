from PIL import Image,ImageStat
from pathlib import Path
paths=[
r"C:\MMUnityPort\Assets\EnvironmentAssets\Biomes\sand.png",
r"C:\MMUnityPort\Assets\EnvironmentAssets\Biomes\ash_ground.png",
r"C:\MMUnityPort\Assets\EnvironmentAssets\Biomes\black_soil.png",
r"C:\MMUnityPort\Assets\EnvironmentAssets\Biomes\cold_rock.png",
r"C:\MMUnityPort\Assets\EnvironmentAssets\Biomes\swamp_ground.png",
r"C:\MMUnityPort\Assets\EnvironmentAssets\Biomes\temperate_grass.png",
r"C:\MMUnityPort\Assets\EnvironmentAssets\Biomes\tropical_grass.png",
r"C:\MMUnityPort\Assets\Environment\PolyHaven\Textures\grass_ground\grass_ground_diff_1k.jpg",
r"C:\MMUnityPort\Assets\Environment\PolyHaven\Textures\coast_sand_02\coast_sand_02_diff_1k.jpg",
r"C:\MMUnityPort\Assets\Environment\PolyHaven\Textures\damp_sand\damp_sand_diff_1k.jpg",
r"C:\MMUnityPort\Assets\Environment\PolyHaven\Textures\rocky_terrain_02\rocky_terrain_02_diff_1k.jpg",
r"C:\MMUnityPort\Assets\Environment\PolyHaven\Textures\grass_path_2\grass_path_2_diff_1k.jpg",
r"C:\MMUnityPort\Assets\EnvironmentAssets\UnitySamples\dry_soil_CH.png",
r"C:\MMUnityPort\Assets\EnvironmentAssets\UnitySamples\mud_cracked_dry_c.png",
r"C:\MMUnityPort\Assets\EnvironmentAssets\UnitySamples\rock_boulder_cracked_c.png"]
for s in paths:
 p=Path(s)
 try:
  im=Image.open(p).convert('RGB').resize((64,64)); st=ImageStat.Stat(im); mean=tuple(round(v,1) for v in st.mean)
  print(f"{p.name:32s} {mean}")
 except Exception as e: print("ERR",p,e)
