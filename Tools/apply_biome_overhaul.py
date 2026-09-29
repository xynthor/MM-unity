from pathlib import Path
import re
from PIL import Image, ImageEnhance, ImageOps

ROOT=Path(r'C:\MMUnityPort')
ED=ROOT/'Assets'/'Editor'
BIOME=ROOT/'Assets'/'EnvironmentAssets'/'Biomes'
BIOME.mkdir(parents=True,exist_ok=True)

cfg={
 'SweetWater':(120,1.05,18,'ash',2),
 'ParadiseValley':(95,1.18,12,'temperate',3),
 'HermitsIsle':(115,1.05,18,'rocky',1),
 'Kriegspire':(155,1.05,22,'darkforest',3),
 'Blackshire':(105,1.10,16,'darkforest',4),
 'Dragonsand':(175,1.00,28,'desert',0),
 'FrozenHighlands':(185,1.00,20,'snow',2),
 'FreeHaven':(75,1.22,10,'temperate',3),
 'MireOfTheDamned':(34,1.65,4,'swamp',4),
 'SilverCove':(85,1.20,10,'temperate',2),
 'BootlegBay':(58,1.30,7,'tropical',3),
 'CastleIronfist':(110,1.12,15,'temperate',2),
 'EelInfestedWaters':(38,1.35,5,'tropical',2),
 'MistyIslands':(42,1.30,5,'tropical',2),
 'NewSorpigal':(72,1.25,8,'temperate',3),
}
rawmax={'SweetWater':31.75,'ParadiseValley':25,'HermitsIsle':25,'Kriegspire':27.75,'Blackshire':31.75,'Dragonsand':20,'FrozenHighlands':25,'FreeHaven':24,'MireOfTheDamned':23.5,'SilverCove':29.75,'BootlegBay':29,'CastleIronfist':31.75,'EelInfestedWaters':5,'MistyIslands':5,'NewSorpigal':31.75}

def tint(src,out,mul,bright=1.0,sat=1.0):
 im=Image.open(src).convert('RGB')
 im=ImageEnhance.Color(im).enhance(sat)
 im=ImageEnhance.Brightness(im).enhance(bright)
 px=im.load()
 for y in range(im.height):
  for x in range(im.width):
   r,g,b=px[x,y]; px[x,y]=(min(255,int(r*mul[0])),min(255,int(g*mul[1])),min(255,int(b*mul[2])))
 im.save(out)

U=ROOT/'Assets'/'EnvironmentAssets'/'UnitySamples'
tint(U/'ground_grass_fells_mossy_CH.png',BIOME/'temperate_grass.png',(1.0,1.0,.92),1.02,1.0)
tint(U/'ground_grass_fells_mossy_CH.png',BIOME/'tropical_grass.png',(.86,1.16,.78),1.08,1.12)
tint(U/'ground_rockgrass_fellsdirty_CH.png',BIOME/'darkforest_ground.png',(.58,.72,.55),.78,.92)
tint(U/'ground_rockgrass_fellsdirty_CH.png',BIOME/'swamp_ground.png',(.52,.65,.48),.67,.82)
tint(U/'dry_soil_CH.png',BIOME/'sand.png',(1.24,1.12,.78),1.14,.82)
tint(U/'dry_soil_CH.png',BIOME/'ash_ground.png',(.72,.62,.58),.76,.55)
tint(U/'rock_boulder_cracked_c.png',BIOME/'cold_rock.png',(.78,.84,.92),1.03,.55)
tint(U/'stone_ground_CH.png',BIOME/'snow.png',(1.08,1.10,1.14),1.72,.18)
print('biome textures generated')# Pine twig RGBA for correct cutout material
pine=ROOT/'Assets'/'Environment'/'PolyHaven'/'Models'/'pine_sapling_small'
diff=Image.open(pine/'pine_sapling_small_twig_diff_1k.png').convert('RGB')
alpha=Image.open(pine/'pine_sapling_small_twig_alpha_1k.png').convert('L')
rgba=diff.convert('RGBA'); rgba.putalpha(alpha); rgba.save(BIOME/'pine_twig_rgba.png')

biome_tex={
 'temperate':('temperate_grass.png','dry_soil_CH.png'),
 'tropical':('tropical_grass.png','sand.png'),
 'darkforest':('darkforest_ground.png','ground_rockgrass_fellsdirty_CH.png'),
 'swamp':('swamp_ground.png','mud_cracked_dry_c.png'),
 'desert':('sand.png','dry_soil_CH.png'),
 'snow':('snow.png','cold_rock.png'),
 'ash':('ash_ground.png','dry_soil_CH.png'),
 'rocky':('cold_rock.png','dry_soil_CH.png'),
}

def asset_path(name):
 if name in {'dry_soil_CH.png','ground_rockgrass_fellsdirty_CH.png','mud_cracked_dry_c.png'}:
  return 'Assets/EnvironmentAssets/UnitySamples/'+name
 return 'Assets/EnvironmentAssets/Biomes/'+name

print('pine rgba generated')

for region,(peak,curve,macro,biome,extra) in cfg.items():
 p=ED/f'Build{region}OpenWorld.cs'
 s=p.read_text(encoding='utf-8')
 s=re.sub(r'const float TerrainBaseY\s*=\s*-?\d+(?:\.\d+)?f;','const float TerrainBaseY = -18f;',s)
 s=re.sub(r'const float TerrainHeight\s*=\s*\d+(?:\.\d+)?f;','const float TerrainHeight = 240f;',s)
 s=s.replace('public GameObject grassPrefab, treePrefab, shrubPrefab, fernPrefab, rockA, rockB;','public GameObject grassPrefab, treePrefab, treePrefab2, treePrefab3, treePrefab4, pinePrefab, deadTreePrefab, shrubPrefab, fernPrefab, rockA, rockB;')
 s=s.replace('public Material waterMaterial, grassMaterial, rockMaterial, treeBark, treeLeaves, treeAtlas, shrubMaterial, fernMaterial;','public Material waterMaterial, grassMaterial, rockMaterial, treeBark, treeLeaves, treeAtlas, pineBark, pineLeaves, shrubMaterial, fernMaterial;')
 grass,dirt=biome_tex[biome]
 s=re.sub(r'e\.grassLayer = CreateTerrainLayer\("Grass", ".*?", new Vector2\(14,14\)\);',f'e.grassLayer = CreateTerrainLayer("Grass", "{asset_path(grass)}", new Vector2(14,14));',s)
 s=re.sub(r'e\.dirtLayer = CreateTerrainLayer\("Dirt", ".*?", new Vector2\(11,11\)\);',f'e.dirtLayer = CreateTerrainLayer("Dirt", "{asset_path(dirt)}", new Vector2(11,11));',s)
 s=s.replace('e.rockLayer = CreateTerrainLayer("Rock", "Assets/EnvironmentAssets/UnitySamples/mud_cracked_dry_c.png", new Vector2(10,10));','e.rockLayer = CreateTerrainLayer("Rock", "Assets/EnvironmentAssets/UnitySamples/rock_boulder_cracked_c.png", new Vector2(10,10));')
 p.write_text(s,encoding='utf-8')
print('basic constants and biome layers patched')

for region,(peak,curve,macro,biome,extra) in cfg.items():
 p=ED/f'Build{region}OpenWorld.cs'; s=p.read_text(encoding='utf-8')
 needle='e.treePrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/EnvironmentAssets/UnitySamples/BanyanTree.fbx");'
 add=needle+'\n        e.treePrefab2 = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/EnvironmentAssets/Gobkit/TreeHigh001.fbx");\n        e.treePrefab3 = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/EnvironmentAssets/Gobkit/TreeHigh002.fbx");\n        e.treePrefab4 = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/EnvironmentAssets/Gobkit/TreeHigh003.fbx");\n        e.pinePrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Environment/PolyHaven/Models/pine_sapling_small/pine_sapling_small_1k.fbx");\n        e.deadTreePrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Environment/PolyHaven/Models/dead_tree_trunk_02/dead_tree_trunk_02_1k.fbx");'
 if 'treePrefab2 = AssetDatabase' not in s: s=s.replace(needle,add)
 matneedle='e.treeAtlas = CreateTexturedMaterial("TreeAtlas", "Assets/EnvironmentAssets/Gobkit/TreeAtlas.png", 0.03f);'
 matadd=matneedle+'\n        e.pineBark = CreateTexturedMaterial("PineBark", "Assets/Environment/PolyHaven/Models/pine_sapling_small/pine_sapling_small_bark_diff_1k.png", 0.05f);\n        e.pineLeaves = CreateCutoutMaterial("PineLeaves", "Assets/EnvironmentAssets/Biomes/pine_twig_rgba.png", 0.42f);'
 if 'e.pineBark =' not in s: s=s.replace(matneedle,matadd)
 p.write_text(s,encoding='utf-8')
print('tree variety assets patched')

for idx,(region,(peak,curve,macro,biome,extra)) in enumerate(cfg.items()):
 p=ED/f'Build{region}OpenWorld.cs'; s=p.read_text(encoding='utf-8')
 if 'static float CreativeLandHeight' not in s:
  special=''
  if biome=='desert': special='h += (Mathf.PerlinNoise(sx*0.055f+7.1f,sy*0.055f+3.4f)-0.35f)*22f*(1f-n*0.35f);'
  elif biome=='snow': special='h += Mathf.Max(0f,Mathf.PerlinNoise(sx*0.032f+11.2f,sy*0.032f+4.8f)-0.48f)*34f*n;'
  elif biome=='swamp': special='h = Mathf.Min(h,28f); h += (Mathf.PerlinNoise(sx*0.025f,sy*0.025f)-0.5f)*2.5f;'
  elif biome in {'darkforest','rocky'}: special='h += Mathf.Max(0f,Mathf.PerlinNoise(sx*0.025f+5.7f,sy*0.025f+9.3f)-0.52f)*18f*n;'
  method=f'''    static float CreativeLandHeight(float raw,float sx,float sy)\n    {{\n        float n=Mathf.Clamp01(raw/{rawmax[region]:.3f}f);\n        float h=1.0f+Mathf.Pow(n,{curve:.3f}f)*{peak:.1f}f;\n        float macro=(Mathf.PerlinNoise(sx*0.018f+{idx*1.73:.2f}f,sy*0.018f+{idx*2.11:.2f}f)-0.5f)*2f*{macro:.1f}f*Mathf.SmoothStep(0.08f,1f,n);\n        h+=macro; {special}\n        return Mathf.Max(0.55f,h);\n    }}\n\n'''
  s=s.replace('    static Terrain BuildTerrain(Transform parent, EnvAssets e)',method+'    static Terrain BuildTerrain(Transform parent, EnvAssets e)')
 s=s.replace('float worldY=SampleByteBilinear(heights,sx,sy)*0.25f;','float rawY=SampleByteBilinear(heights,sx,sy)*0.25f;\n            float worldY=CreativeLandHeight(rawY,sx,sy);')
 s=s.replace('float h=SampleByteBilinear(heights,sx,sy)*0.25f;','float h=CreativeLandHeight(SampleByteBilinear(heights,sx,sy)*0.25f,sx,sy);')
 p.write_text(s,encoding='utf-8')
print('creative height profiles patched')