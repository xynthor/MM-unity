from pathlib import Path

curp=Path(r'C:\MMUnityPort\Assets\Editor\BuildNewSorpigalOpenWorld.cs')
backp=Path(r'C:\MMUnityPort\Backups\NS_RiverWallsPlayer_20260913_235321\BuildNewSorpigalOpenWorld.cs')
cur=curp.read_text(encoding='utf-8-sig')
back=backp.read_text(encoding='utf-8-sig')

def block(text, marker):
    s=text.index(marker)
    b=text.index('{',s)
    depth=0
    for i in range(b,len(text)):
        if text[i]=='{': depth+=1
        elif text[i]=='}':
            depth-=1
            if depth==0: return s,i+1
    raise RuntimeError('unclosed '+marker)

def replace_method(dst, src, marker):
    ds,de=block(dst,marker)
    ss,se=block(src,marker)
    return dst[:ds]+src[ss:se]+dst[de:]

cur=cur.replace('const int TerrainResolution = 1025;','const int TerrainResolution = 513;')
cur=replace_method(cur,back,'static float CreativeLandHeight')
cur=replace_method(cur,back,'static Terrain BuildTerrain')
cur=replace_method(cur,back,'static GameObject BuildWater')
cur=cur.replace('var water = BuildWater(root.transform, terrain, env);','var water = BuildWater(root.transform, terrain, env.waterMaterial);')
old='''            if(placement!=null&&b.size.y>0.15f)
            {
                float surface;
                if(go.name.IndexOf("Bridge",StringComparison.OrdinalIgnoreCase)>=0)
                { RiverNearestWorld(b.center.x,b.center.z,out _,out float rt,out _,out _);surface=RiverSurfaceT(rt); }
                else surface=WaterAffiliated(go.name)?.12f:SampleTerrainY(terrain,b.center.x,b.center.z);
                float desiredBase=surface+placement.sourceBaseOffset*ArchitectureScale;
                go.transform.position+=Vector3.up*(desiredBase-b.min.y);grounded++;
            }'''
new='''            if(placement!=null&&b.size.y>0.15f)
            {
                float surface=WaterAffiliated(go.name)?0.12f:SampleTerrainY(terrain,b.center.x,b.center.z);
                float desiredBase=surface+placement.sourceBaseOffset*ArchitectureScale;
                go.transform.position+=Vector3.up*(desiredBase-b.min.y);grounded++;
            }'''
if old not in cur:
    raise RuntimeError('bridge grounding block not found')
cur=cur.replace(old,new,1)
curp.write_text(cur,encoding='utf-8')
print('RESTORED_ORIGINAL_TERRAIN_WATER_BRIDGES')
print('TerrainResolution=513')
print('BuildTerrain=pre-custom')
print('BuildWater=pre-custom MM6 tile mask')
print('Bridge grounding=pre-custom')
