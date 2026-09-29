from pathlib import Path
import re
p=Path(r'C:\MMUnityPort\Assets\Editor\BuildEnrothFullWorld.cs')
s=p.read_text(encoding='utf-8')
s=s.replace('const float TerrainBaseY=-12f;','const float TerrainBaseY=-28f;')
s=s.replace('const float TerrainHeight=64f;','const float TerrainHeight=300f;')
s=s.replace('const float CoastPad=320f;','const float CoastPad=520f;')

def replace_method(src, signature, new_text):
    i=src.index(signature)
    brace=src.index('{',i)
    depth=0
    j=brace
    while j<len(src):
        if src[j]=='{': depth+=1
        elif src[j]=='}':
            depth-=1
            if depth==0:
                return src[:i]+new_text+src[j+1:]
        j+=1
    raise RuntimeError('unclosed '+signature)

s=s.replace('var dragon=BuildDragonIsle(world.transform,mainland.terrainData.terrainLayers);','var dragon=BuildDragonIsle(world.transform,mainland,mainland.terrainData.terrainLayers);')
s=s.replace('if(keptCamera){keptCamera.farClipPlane=18000f;keptCamera.tag="MainCamera";}','if(keptPlayer){float py=mainland.SampleHeight(keptPlayer.transform.position)+mainland.transform.position.y; keptPlayer.transform.position=new Vector3(keptPlayer.transform.position.x,py+.18f,keptPlayer.transform.position.z);}\n        if(keptCamera){keptCamera.farClipPlane=18000f;keptCamera.tag="MainCamera";}')helpers=r'''
    static readonly Vector2[] ContinentPoly={
        new Vector2(.035f,.29f),new Vector2(.075f,.46f),new Vector2(.115f,.65f),
        new Vector2(.18f,.82f),new Vector2(.30f,.94f),new Vector2(.46f,.985f),
        new Vector2(.63f,.955f),new Vector2(.78f,.89f),new Vector2(.90f,.77f),
        new Vector2(.965f,.63f),new Vector2(.95f,.50f),new Vector2(.89f,.39f),
        new Vector2(.82f,.31f),new Vector2(.76f,.20f),new Vector2(.68f,.105f),
        new Vector2(.57f,.075f),new Vector2(.48f,.12f),new Vector2(.39f,.105f),
        new Vector2(.30f,.045f),new Vector2(.20f,.075f),new Vector2(.115f,.16f)
    };
    static bool PointInPoly(Vector2 p)
    {
        bool inside=false; int j=ContinentPoly.Length-1;
        for(int i=0;i<ContinentPoly.Length;j=i++)
        {
            Vector2 a=ContinentPoly[i],b=ContinentPoly[j];
            if(((a.y>p.y)!=(b.y>p.y)) && p.x<(b.x-a.x)*(p.y-a.y)/(b.y-a.y)+a.x) inside=!inside;
        }
        return inside;
    }
'''
s=s.replace('    static Terrain BuildMainlandTerrain',helpers+'\n    static Terrain BuildMainlandTerrain',1)more_helpers=r'''
    static float DistSeg(Vector2 p,Vector2 a,Vector2 b)
    {
        Vector2 ab=b-a; float t=Mathf.Clamp01(Vector2.Dot(p-a,ab)/Mathf.Max(.000001f,Vector2.Dot(ab,ab)));
        return Vector2.Distance(p,a+ab*t);
    }
    static float ContinentMask(float nx,float nz)
    {
        Vector2 p=new Vector2(nx,nz); bool inside=PointInPoly(p); float d=99f;
        for(int i=0;i<ContinentPoly.Length;i++) d=Mathf.Min(d,DistSeg(p,ContinentPoly[i],ContinentPoly[(i+1)%ContinentPoly.Length]));
        float noise=(Mathf.PerlinNoise(nx*8.7f+3.1f,nz*7.9f+6.2f)-.5f)*.020f;
        float signed=(inside?d:-d)+noise;
        return Mathf.SmoothStep(0f,1f,Mathf.InverseLerp(-.015f,.045f,signed));
    }
    static TerrainLayer MasterLayer(string name,string texture,float tile)
    {
        string path="Assets/World/Enroth/Generated/Master_"+name+".terrainlayer";
        var l=AssetDatabase.LoadAssetAtPath<TerrainLayer>(path); if(!l){l=new TerrainLayer();AssetDatabase.CreateAsset(l,path);}
        l.diffuseTexture=AssetDatabase.LoadAssetAtPath<Texture2D>(texture); l.tileSize=new Vector2(tile,tile); l.metallic=0f; l.smoothness=name=="Road"?.18f:.06f; EditorUtility.SetDirty(l); return l;
    }
'''
s=s.replace('    static Terrain BuildMainlandTerrain',more_helpers+'\n    static Terrain BuildMainlandTerrain',1)more_helpers2=r'''
    static TerrainLayer[] CreateMasterLayers()
    {
        return new[]{
            MasterLayer("Temperate","Assets/EnvironmentAssets/Biomes/temperate_grass.png",14f),
            MasterLayer("Tropical","Assets/EnvironmentAssets/Biomes/tropical_grass.png",13f),
            MasterLayer("DarkForest","Assets/EnvironmentAssets/Biomes/darkforest_ground.png",12f),
            MasterLayer("Swamp","Assets/EnvironmentAssets/Biomes/swamp_ground.png",11f),
            MasterLayer("Sand","Assets/EnvironmentAssets/Biomes/sand.png",12f),
            MasterLayer("Snow","Assets/EnvironmentAssets/Biomes/snow.png",13f),
            MasterLayer("Rock","Assets/EnvironmentAssets/UnitySamples/rock_boulder_cracked_c.png",10f),
            MasterLayer("Ash","Assets/EnvironmentAssets/Biomes/ash_ground.png",11f),
            MasterLayer("Road","Assets/EnvironmentAssets/UnitySamples/stone_ground_CH.png",8f)
        };
    }
    static int BiomeLayer(int r,int c)
    {
        if(r==2&&c==0)return 7; if(r==2&&c==1)return 2; if(r==2&&c==2)return 5; if(r==2&&c==4)return 1;
        if(r==1&&c==1)return 2; if(r==1&&c==3)return 1; if(r==1&&c==4)return 1;
        if(r==0&&c==0)return 6; if(r==0&&c==1)return 4; if(r==0&&c==2)return 3;
        return 0;
    }
'''
s=s.replace('    static Terrain BuildMainlandTerrain',more_helpers2+'\n    static Terrain BuildMainlandTerrain',1)mainland=r'''    static Terrain BuildMainlandTerrain(Transform parent,Terrain[,] src)
    {
        float minX=src[0,0].transform.position.x, minZ=src[0,0].transform.position.z;
        float maxX=src[0,Cols-1].transform.position.x+src[0,Cols-1].terrainData.size.x;
        float maxZ=src[Rows-1,0].transform.position.z+src[Rows-1,0].terrainData.size.z;
        float spanX=maxX-minX, spanZ=maxZ-minZ;
        string path="Assets/World/Enroth/Generated/EnrothFullTerrain.asset";
        var td=AssetDatabase.LoadAssetAtPath<TerrainData>(path); if(!td){td=new TerrainData();AssetDatabase.CreateAsset(td,path);}
        td.heightmapResolution=MasterResolution; td.size=new Vector3(spanX+CoastPad*2f,TerrainHeight,spanZ+CoastPad*2f);
        td.terrainLayers=CreateMasterLayers();
        var heights=new float[MasterResolution,MasterResolution];
        for(int y=0;y<MasterResolution;y++) for(int x=0;x<MasterResolution;x++)
        {
            float wx=Mathf.Lerp(minX-CoastPad,maxX+CoastPad,x/(float)(MasterResolution-1));
            float wz=Mathf.Lerp(minZ-CoastPad,maxZ+CoastPad,y/(float)(MasterResolution-1));
            float nx=(wx-minX)/spanX, nz=(wz-minZ)/spanZ;
            float cx=Mathf.Clamp(wx,minX,maxX), cz=Mathf.Clamp(wz,minZ,maxZ);
            float source=SampleGrid(src,cx,cz,minX,minZ);
            float mask=ContinentMask(nx,nz);
            float oceanFloor=-8f-(Mathf.PerlinNoise(nx*5.1f+2f,nz*5.3f+9f)*2.2f);
            float wy=Mathf.Lerp(oceanFloor,source,mask);
            if(source<OceanY+.18f) wy=Mathf.Min(wy,source);
            heights[y,x]=Mathf.Clamp01((wy-TerrainBaseY)/TerrainHeight);
        }
        td.SetHeights(0,0,heights);
'''        td.alphamapResolution=1024;
        var sourceAlpha=new Dictionary<int,float[,,]>();
        for(int r=0;r<Rows;r++) for(int c=0;c<Cols;c++) sourceAlpha[r*Cols+c]=src[r,c].terrainData.GetAlphamaps(0,0,src[r,c].terrainData.alphamapWidth,src[r,c].terrainData.alphamapHeight);
        var alpha=new float[1024,1024,9];
        for(int y=0;y<1024;y++) for(int x=0;x<1024;x++)
        {
            float wx=Mathf.Lerp(minX-CoastPad,maxX+CoastPad,x/1023f), wz=Mathf.Lerp(minZ-CoastPad,maxZ+CoastPad,y/1023f);
            float nx=(wx-minX)/spanX,nz=(wz-minZ)/spanZ,mask=ContinentMask(nx,nz);
            float cx=Mathf.Clamp(wx,minX,maxX),cz=Mathf.Clamp(wz,minZ,maxZ);
            int c=Mathf.Clamp(Mathf.FloorToInt((cx-minX)/RegionSize),0,Cols-1), r=Mathf.Clamp(Mathf.FloorToInt((cz-minZ)/RegionSize),0,Rows-1);
            var sa=sourceAlpha[r*Cols+c];
            float road=SampleAlpha(src[r,c],sa,cx,cz,2), rock=SampleAlpha(src[r,c],sa,cx,cz,3);
            int baseLayer=BiomeLayer(r,c); float coast=Mathf.Clamp01((.70f-mask)/.70f);
            float roadW=road*.92f*mask, rockW=Mathf.Clamp01(rock*.78f + coast*.18f)*mask;
            float sandW=(baseLayer==4?1f:0f)*mask + coast*.65f*mask;
            float baseW=Mathf.Max(0f,mask-roadW-rockW-sandW*.45f);
            alpha[y,x,baseLayer]+=baseW;
            alpha[y,x,4]+=sandW;
            alpha[y,x,6]+=rockW;
            alpha[y,x,8]+=roadW;
            if(mask<.08f) alpha[y,x,4]=1f;
            float sum=0f; for(int l=0;l<9;l++) sum+=alpha[y,x,l]; if(sum<.001f){alpha[y,x,4]=1f;sum=1f;}
            for(int l=0;l<9;l++) alpha[y,x,l]/=sum;
        }
        td.SetAlphamaps(0,0,alpha); EditorUtility.SetDirty(td);
        var go=Terrain.CreateTerrainGameObject(td); go.name="Enroth - Continuous Authored Mainland"; go.transform.SetParent(parent);
        go.transform.position=new Vector3(minX-CoastPad,TerrainBaseY,minZ-CoastPad);
        var terrain=go.GetComponent<Terrain>(); terrain.drawInstanced=true; terrain.heightmapPixelError=3f; terrain.basemapDistance=2600f;
        terrain.detailObjectDistance=240f; terrain.detailObjectDensity=1f;
        Debug.Log($"ENROTH_MAINLAND size={td.size.x:F0}x{td.size.z:F0} height={TerrainHeight:F0}");
        return terrain;
    }
'''
s=replace_method(s,'    static Terrain BuildMainlandTerrain',mainland)
dragon=r'''    static Terrain BuildDragonIsle(Transform parent,Terrain mainland,TerrainLayer[] layers)
    {
        const int res=513; const float sizeX=1050f,sizeZ=1900f;
        string path="Assets/World/Enroth/Generated/DragonIsleTerrain.asset";
        var td=AssetDatabase.LoadAssetAtPath<TerrainData>(path); if(!td){td=new TerrainData();AssetDatabase.CreateAsset(td,path);}
        td.heightmapResolution=res; td.size=new Vector3(sizeX,TerrainHeight,sizeZ); td.terrainLayers=layers;
        var h=new float[res,res];
        for(int y=0;y<res;y++) for(int x=0;x<res;x++)
        {
            float u=x/(float)(res-1),v=y/(float)(res-1);
            float shape=Mathf.Max(RotBlob(u,v,.38f,.18f,.32f,.18f,-8f),RotBlob(u,v,.45f,.39f,.31f,.24f,-8f));
            shape=Mathf.Max(shape,RotBlob(u,v,.53f,.61f,.26f,.23f,-11f));
            shape=Mathf.Max(shape,RotBlob(u,v,.62f,.81f,.27f,.17f,-14f));
            shape=Mathf.Max(shape,RotBlob(u,v,.73f,.95f,.21f,.075f,-18f));
            shape=Mathf.Max(shape,RotBlob(u,v,.19f,.045f,.085f,.030f,0f));
            float shore=Mathf.SmoothStep(0f,1f,Mathf.Clamp01((shape+.12f)/.24f));
            float noise=(Mathf.PerlinNoise(u*10.2f+2f,v*11.4f+5f)-.5f)*7f;
            float wy=Mathf.Lerp(-8f,4f+noise,shore);
            float southwest=Mathf.Exp(-((u-.24f)*(u-.24f)/.022f+(v-.18f)*(v-.18f)/.030f))*88f;
            float ridge=Mathf.Exp(-((u-.34f)*(u-.34f)/.035f+(v-.33f)*(v-.33f)/.075f))*42f;
            float cr=Mathf.Sqrt(((u-.59f)/.14f)*((u-.59f)/.14f)+((v-.79f)/.095f)*((v-.79f)/.095f));
            wy+=southwest+ridge+Mathf.Exp(-Mathf.Pow(cr-1f,2f)/.07f)*58f-Mathf.Exp(-(cr*cr)/.17f)*18f;
            float lake=Mathf.Sqrt(((u-.47f)/.075f)*((u-.47f)/.075f)+((v-.56f)/.052f)*((v-.56f)/.052f));
            if(lake<1f) wy=Mathf.Lerp(-2.5f,wy,Mathf.SmoothStep(0f,1f,Mathf.Clamp01((lake-.70f)/.30f)));
            h[y,x]=Mathf.Clamp01((wy-TerrainBaseY)/TerrainHeight);
        }
        td.SetHeights(0,0,h);
'''        td.alphamapResolution=512; var a=new float[512,512,9];
        for(int y=0;y<512;y++) for(int x=0;x<512;x++)
        {
            float u=x/511f,v=y/511f;
            float shape=Mathf.Max(RotBlob(u,v,.38f,.18f,.32f,.18f,-8f),RotBlob(u,v,.45f,.39f,.31f,.24f,-8f));
            shape=Mathf.Max(shape,RotBlob(u,v,.53f,.61f,.26f,.23f,-11f));
            shape=Mathf.Max(shape,RotBlob(u,v,.62f,.81f,.27f,.17f,-14f));
            shape=Mathf.Max(shape,RotBlob(u,v,.73f,.95f,.21f,.075f,-18f));
            float edge=Mathf.Clamp01((shape+.08f)/.28f), high=Mathf.Clamp01((v-.67f)*2.5f);
            float sand=(1f-edge)*.78f + Mathf.Clamp01((.20f-edge)*3f)*.35f;
            float rock=Mathf.Clamp01(high*.58f + Mathf.Max(0f,.32f-edge*.28f));
            float forest=Mathf.Max(0f,edge-rock*.55f);
            a[y,x,0]=forest*.72f; a[y,x,2]=forest*.28f; a[y,x,4]=sand; a[y,x,6]=rock;
            float sum=0f; for(int l=0;l<9;l++) sum+=a[y,x,l]; if(sum<.001f){a[y,x,4]=1f;sum=1f;}
            for(int l=0;l<9;l++) a[y,x,l]/=sum;
        }
        td.SetAlphamaps(0,0,a); EditorUtility.SetDirty(td);
        var go=Terrain.CreateTerrainGameObject(td); go.name="Dragon Isle - Map Proportion Reference"; go.transform.SetParent(parent);
        float px=mainland.transform.position.x-sizeX-620f;
        float pz=mainland.transform.position.z+mainland.terrainData.size.z*.53f;
        go.transform.position=new Vector3(px,TerrainBaseY,pz);
        var t=go.GetComponent<Terrain>(); t.drawInstanced=true; t.heightmapPixelError=2f; t.basemapDistance=1800f;
        PopulateDragonIsle(go.transform,t);
        Debug.Log($"DRAGON_ISLE size={sizeX}x{sizeZ} pos={go.transform.position}");
        return t;
    }
'''
s=replace_method(s,'    static Terrain BuildDragonIsle',dragon)
dragon_helpers=r'''
    static void AssignDragonTreeMaterials(GameObject go,bool pine)
    {
        Material bark=AssetDatabase.LoadAssetAtPath<Material>(pine?"Assets/Materials/FrozenHighlands/Environment/PineBark.mat":"Assets/Materials/NewSorpigal/Environment/TreeSmallBark.mat");
        Material leaves=AssetDatabase.LoadAssetAtPath<Material>(pine?"Assets/Materials/FrozenHighlands/Environment/PineLeaves.mat":"Assets/Materials/NewSorpigal/Environment/TreeSmallLeaves.mat");
        foreach(var r in go.GetComponentsInChildren<Renderer>(true))
        {
            var src=r.sharedMaterials; var dst=new Material[src.Length];
            for(int i=0;i<src.Length;i++){string n=src[i]?src[i].name.ToLowerInvariant():""; dst[i]=(n.Contains("leaf")||n.Contains("twig"))?leaves:bark;}
            r.sharedMaterials=dst; r.shadowCastingMode=ShadowCastingMode.On; r.receiveShadows=true;
        }
    }
'''
s=s.replace('    static void PopulateDragonIsle',dragon_helpers+'\n    static void PopulateDragonIsle',1)
populate=r'''    static void PopulateDragonIsle(Transform parent,Terrain t)
    {
        var tree=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/EnvironmentAssets/PolyHaven/TreeSmall02/GameLOD2/tree_small_02_LOD1.fbx");
        var pine=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Environment/PolyHaven/Models/pine_sapling_small/pine_sapling_small_1k.fbx");
        var rockA=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/EnvironmentAssets/UnitySamples/Rock_A_01.fbx");
        var rockB=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/EnvironmentAssets/UnitySamples/Rock_A_02.fbx");
        var veg=new GameObject("Dragon Isle - Forest, Pines and Rocks"); veg.transform.SetParent(parent);
        int trees=0,rocks=0;
        for(int iz=2;iz<50;iz++) for(int ix=2;ix<28;ix++)
        {
            float u=ix/28f,v=iz/50f, jitter=Mathf.PerlinNoise(ix*1.73f,iz*2.17f);
            float wx=t.transform.position.x+u*t.terrainData.size.x, wz=t.transform.position.z+v*t.terrainData.size.z;
            float wy=t.SampleHeight(new Vector3(wx,0,wz))+t.transform.position.y;
            bool lake=(u>.38f&&u<.56f&&v>.50f&&v<.62f);
            bool forest=wy>1.0f && wy<78f && v<.76f && !lake;
            if(forest && jitter>.32f && (tree||pine))
            {
                bool usePine=v>.52f || jitter>.70f; var prefab=usePine&&pine?pine:tree; if(!prefab)prefab=pine;
                var go=(GameObject)PrefabUtility.InstantiatePrefab(prefab); go.name=usePine?"DragonIsle_Pine":"DragonIsle_Tree"; go.transform.SetParent(veg.transform);
                float jx=(Mathf.PerlinNoise(ix*4.1f,iz*.7f)-.5f)*20f,jz=(Mathf.PerlinNoise(ix*.9f,iz*3.7f)-.5f)*20f;
                float xx=wx+jx,zz=wz+jz,yy=t.SampleHeight(new Vector3(xx,0,zz))+t.transform.position.y;
                go.transform.position=new Vector3(xx,yy,zz); go.transform.rotation=Quaternion.Euler(0,jitter*360f,0);
                ScaleToHeight(go,6.5f+Mathf.PerlinNoise(ix*2.2f,iz*1.4f)*5.5f); AssignDragonTreeMaterials(go,usePine); trees++;
            }
'''            if((rockA||rockB) && wy>22f && jitter>.70f)
            {
                var prefab=((ix+iz)&1)==0?rockA:rockB; if(!prefab)prefab=rockA?rockA:rockB;
                var go=(GameObject)PrefabUtility.InstantiatePrefab(prefab); go.name="DragonIsle_Rock"; go.transform.SetParent(veg.transform);
                go.transform.position=new Vector3(wx,wy,wz); go.transform.rotation=Quaternion.Euler(0,jitter*360f,0);
                ScaleToHeight(go,1.3f+jitter*4.0f); rocks++;
            }
        }
        Debug.Log($"DRAGON_ISLE_REFERENCE trees={trees} rocks={rocks}");
    }
'''
s=replace_method(s,'    static void PopulateDragonIsle',populate)
p.write_text(s,encoding='utf-8')
print('full Enroth generator patched',len(s.splitlines()))
