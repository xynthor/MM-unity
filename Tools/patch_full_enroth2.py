from pathlib import Path
root=Path(r'C:\MMUnityPort')
p=root/'Assets'/'Editor'/'BuildEnrothFullWorld.cs'
backup=root/'Backups'/'BuildEnrothFullWorld_before_continent_rewrite.cs'
s=backup.read_text(encoding='utf-8')
s=s.replace('const float TerrainBaseY=-12f;','const float TerrainBaseY=-28f;')
s=s.replace('const float TerrainHeight=64f;','const float TerrainHeight=300f;')
s=s.replace('const float CoastPad=320f;','const float CoastPad=520f;')
s=s.replace('var dragon=BuildDragonIsle(world.transform,mainland.terrainData.terrainLayers);','var dragon=BuildDragonIsle(world.transform,mainland,mainland.terrainData.terrainLayers);')
s=s.replace('if(keptCamera){keptCamera.farClipPlane=18000f;keptCamera.tag="MainCamera";}','if(keptPlayer){float py=mainland.SampleHeight(keptPlayer.transform.position)+mainland.transform.position.y; keptPlayer.transform.position=new Vector3(keptPlayer.transform.position.x,py+.18f,keptPlayer.transform.position.z);}\n        if(keptCamera){keptCamera.farClipPlane=18000f;keptCamera.tag="MainCamera";}')

def replace_method(src,signature,new_text):
    i=src.index(signature); brace=src.index('{',i); depth=0
    for j in range(brace,len(src)):
        if src[j]=='{': depth+=1
        elif src[j]=='}':
            depth-=1
            if depth==0: return src[:i]+new_text+src[j+1:]
    raise RuntimeError('unclosed '+signature)

helpers=(root/'Tools'/'master_helpers.txt').read_text(encoding='utf-8')
s=s.replace('    static Terrain BuildMainlandTerrain',helpers+'\n    static Terrain BuildMainlandTerrain',1)
mainland=(root/'Tools'/'master_mainland.txt').read_text(encoding='utf-8')
s=replace_method(s,'    static Terrain BuildMainlandTerrain',mainland)
dragon=(root/'Tools'/'master_dragon.txt').read_text(encoding='utf-8')
s=replace_method(s,'    static Terrain BuildDragonIsle',dragon)
dh=(root/'Tools'/'master_dragon_helpers.txt').read_text(encoding='utf-8')
s=s.replace('    static void PopulateDragonIsle',dh+'\n    static void PopulateDragonIsle',1)
pop=(root/'Tools'/'master_populate_dragon.txt').read_text(encoding='utf-8')
s=replace_method(s,'    static void PopulateDragonIsle',pop)
p.write_text(s,encoding='utf-8')
print('patched master lines',len(s.splitlines()))
