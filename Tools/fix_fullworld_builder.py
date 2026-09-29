from pathlib import Path
p=Path(r'C:\MMUnityPort\Assets\Editor\BuildEnrothFullWorld.cs')
s=p.read_text(encoding='utf-8')
s=s.replace('var sourceAlpha=new float[Rows,Cols][,,];','var sourceAlpha=new Dictionary<int,float[,,]>();')
s=s.replace('sourceAlpha[r,c]=src[r,c].terrainData.GetAlphamaps(0,0,src[r,c].terrainData.alphamapWidth,src[r,c].terrainData.alphamapHeight);','sourceAlpha[r*Cols+c]=src[r,c].terrainData.GetAlphamaps(0,0,src[r,c].terrainData.alphamapWidth,src[r,c].terrainData.alphamapHeight);')
s=s.replace('static float SampleGridAlpha(Terrain[,] src,float[,][,,] maps,float wx,float wz,float minX,float minZ,int layer)','static float SampleGridAlpha(Terrain[,] src,Dictionary<int,float[,,]> maps,float wx,float wz,float minX,float minZ,int layer)')
s=s.replace('maps[r0,c0]','maps[r0*Cols+c0]').replace('maps[r0,c1]','maps[r0*Cols+c1]').replace('maps[r1,c0]','maps[r1*Cols+c0]').replace('maps[r1,c1]','maps[r1*Cols+c1]')
p.write_text(s,encoding='utf-8')
print('fixed sourceAlpha declarations/access')