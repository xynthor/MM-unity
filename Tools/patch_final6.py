from pathlib import Path
src=Path(r'C:\MMUnityPort\Assets\Editor\BuildEnrothFullWorldFinal5.cs')
dst=Path(r'C:\MMUnityPort\Assets\Editor\BuildEnrothFullWorldFinal6.cs')
s=src.read_text(encoding='utf-8-sig')
s=s.replace('BuildEnrothFullWorldFinal5','BuildEnrothFullWorldFinal6')
s=s.replace('const int MasterResolution=2049;','const int MasterResolution=1025;')
old='''        new RegionSpec("Eel Infested Waters","Assets/Scenes/EelInfestedWaters_OpenWorld.unity",4,2),
        new RegionSpec("Paradise Valley","Assets/Scenes/ParadiseValley_OpenWorld.unity",0,1),
        new RegionSpec("Blackshire","Assets/Scenes/Blackshire_OpenWorld.unity",1,1),
        new RegionSpec("Free Haven","Assets/Scenes/FreeHaven_OpenWorld.unity",2,1),
        new RegionSpec("Bootleg Bay","Assets/Scenes/BootlegBay_OpenWorld.unity",3,1),
        new RegionSpec("Misty Islands","Assets/Scenes/MistyIslands_OpenWorld.unity",4,1),
        new RegionSpec("Hermit's Isle","Assets/Scenes/HermitsIsle_OpenWorld.unity",0,0),
        new RegionSpec("Dragonsand","Assets/Scenes/Dragonsand_OpenWorld.unity",1,0,false,-120f,-100f),
        new RegionSpec("Mire of the Damned","Assets/Scenes/MireOfTheDamned_OpenWorld.unity",2,0,false,100f,-250f),
        new RegionSpec("Castle Ironfist","Assets/Scenes/CastleIronfist_OpenWorld.unity",3,0,false,160f,80f),
        new RegionSpec("New Sorpigal","Assets/Scenes/NewSorpigal_OpenWorld.unity",4,0,true,240f,-300f),'''
new='''        new RegionSpec("Eel Infested Waters","Assets/Scenes/EelInfestedWaters_OpenWorld.unity",4,2,false,820f,220f),
        new RegionSpec("Paradise Valley","Assets/Scenes/ParadiseValley_OpenWorld.unity",0,1),
        new RegionSpec("Blackshire","Assets/Scenes/Blackshire_OpenWorld.unity",1,1),
        new RegionSpec("Free Haven","Assets/Scenes/FreeHaven_OpenWorld.unity",2,1),
        new RegionSpec("Bootleg Bay","Assets/Scenes/BootlegBay_OpenWorld.unity",3,1),
        new RegionSpec("Misty Islands","Assets/Scenes/MistyIslands_OpenWorld.unity",4,1,false,850f,-120f),
        new RegionSpec("Hermit's Isle","Assets/Scenes/HermitsIsle_OpenWorld.unity",0,0,false,-180f,-900f),
        new RegionSpec("Dragonsand","Assets/Scenes/Dragonsand_OpenWorld.unity",1,0,false,-120f,-150f),
        new RegionSpec("Mire of the Damned","Assets/Scenes/MireOfTheDamned_OpenWorld.unity",2,0,false,160f,-420f),
        new RegionSpec("Castle Ironfist","Assets/Scenes/CastleIronfist_OpenWorld.unity",3,0,false,130f,220f),
        new RegionSpec("New Sorpigal","Assets/Scenes/NewSorpigal_OpenWorld.unity",4,0,true,280f,-320f),'''
if old not in s: raise SystemExit('regions block not found')
s=s.replace(old,new,1)
insert='''    static bool IsOffshoreCell(int r,int c)
    {
        return (r==0&&c==0)||(r==1&&c==4)||(r==2&&c==4);
    }
'''
mark='''    [MenuItem("MMUnity/Build Full Enroth + Dragon Isle")]
'''
if insert not in s:
    s=s.replace(mark,insert+'\n'+mark,1)
s=s.replace('if(!spec.keepPlayer || (keptCamera!=null && a.gameObject!=keptCamera.gameObject)) UnityEngine.Object.DestroyImmediate(a);','if(!spec.keepPlayer || (keptCamera!=null && a.gameObject!=keptCamera.gameObject)) UnityEngine.Object.DestroyImmediate(a);')
s=s.replace('''        CullUnderwaterVegetation(regionRoot.transform,mainland);
        foreach(var t in terrains) if(t) t.gameObject.SetActive(false);''','''        CullUnderwaterVegetation(regionRoot.transform,mainland);
        for(int rr=0;rr<Rows;rr++) for(int cc=0;cc<Cols;cc++)
            if(terrains[rr,cc] && !IsOffshoreCell(rr,cc)) terrains[rr,cc].gameObject.SetActive(false);''',1)
s=s.replace('if(keptCamera){keptCamera.farClipPlane=18000f;keptCamera.tag="MainCamera";}','if(keptCamera){keptCamera.farClipPlane=4500f;keptCamera.tag="MainCamera";}',1)
s=s.replace('''        ConvertToStreamingMaster(world,regionRoot,keptPlayer,keptCamera);
        EnsureBuildSettings();
        EditorSceneManager.SaveScene(master,ScenePath);''','''        ConvertToStreamingMaster(world,regionRoot,keptPlayer,keptCamera);
        EnsureBuildSettings();
        ValidateFinal(world,mainland,dragon,keptPlayer,keptCamera);
        EditorSceneManager.SaveScene(master,ScenePath);''',1)
s=s.replace('''        foreach(Transform region in regions)
        {
            foreach(var veg in region.GetComponentsInChildren<Transform>(true).Where(t=>t.name.StartsWith("Vegetation -",StringComparison.OrdinalIgnoreCase)))''','''        foreach(Transform region in regions)
        {
            string rn=region.name;
            if(rn.StartsWith("Hermit's Isle",StringComparison.OrdinalIgnoreCase)||rn.StartsWith("Misty Islands",StringComparison.OrdinalIgnoreCase)||rn.StartsWith("Eel Infested Waters",StringComparison.OrdinalIgnoreCase)) continue;
            foreach(var veg in region.GetComponentsInChildren<Transform>(true).Where(t=>t.name.StartsWith("Vegetation -",StringComparison.OrdinalIgnoreCase)))''',1)
old='''        float minX=gridMinX,maxX=gridMaxX,minZ=gridMinZ,maxZ=gridMaxZ;
        for(int rr=0;rr<Rows;rr++) for(int cc=0;cc<Cols;cc++){var t=src[rr,cc];minX=Mathf.Min(minX,t.transform.position.x);minZ=Mathf.Min(minZ,t.transform.position.z);maxX=Mathf.Max(maxX,t.transform.position.x+t.terrainData.size.x);maxZ=Mathf.Max(maxZ,t.transform.position.z+t.terrainData.size.z);}'''
new='''        float minX=float.MaxValue,maxX=float.MinValue,minZ=float.MaxValue,maxZ=float.MinValue;
        for(int rr=0;rr<Rows;rr++) for(int cc=0;cc<Cols;cc++)
        {
            if(IsOffshoreCell(rr,cc)) continue;
            var t=src[rr,cc];
            minX=Mathf.Min(minX,t.transform.position.x); minZ=Mathf.Min(minZ,t.transform.position.z);
            maxX=Mathf.Max(maxX,t.transform.position.x+t.terrainData.size.x); maxZ=Mathf.Max(maxZ,t.transform.position.z+t.terrainData.size.z);
        }'''
if old not in s: raise SystemExit('bounds block not found')
s=s.replace(old,new,1)
s=s.replace('td.alphamapResolution=1024;','td.alphamapResolution=512;',1)
s=s.replace('var alpha=new float[1024,1024,9];','var alpha=new float[512,512,9];',1)
s=s.replace('for(int y=0;y<1024;y++) for(int x=0;x<1024;x++)','for(int y=0;y<512;y++) for(int x=0;x<512;x++)',1)
s=s.replace('x/1023f','x/511f').replace('y/1023f','y/511f')
sampler_start=s.index('    static float SampleGrid(Terrain[,] src,float wx,float wz,float minX,float minZ)')
sampler_end=s.index('    static float SampleWorld(Terrain t,float wx,float wz)',sampler_start)
weighted=r'''    static float TerrainBlendWeight(Terrain t,float wx,float wz)
    {
        var p=t.transform.position; var sz=t.terrainData.size;
        float dx=wx<p.x?p.x-wx:(wx>p.x+sz.x?wx-(p.x+sz.x):0f);
        float dz=wz<p.z?p.z-wz:(wz>p.z+sz.z?wz-(p.z+sz.z):0f);
        float outside=Mathf.Sqrt(dx*dx+dz*dz);
        if(outside>0f) return Mathf.Exp(-outside/520f)*.70f;
        float edge=Mathf.Min(Mathf.Min(wx-p.x,p.x+sz.x-wx),Mathf.Min(wz-p.z,p.z+sz.z-wz));
        return 1.2f+Mathf.SmoothStep(0f,1f,Mathf.Clamp01(edge/320f))*7.8f;
    }
    static float SampleGrid(Terrain[,] src,float wx,float wz,float minX,float minZ)
    {
        float sum=0f,wsum=0f;
        for(int r=0;r<Rows;r++) for(int c=0;c<Cols;c++)
        {
            if(IsOffshoreCell(r,c)) continue;
            var t=src[r,c]; if(!t) continue;
            float w=TerrainBlendWeight(t,wx,wz); if(w<.0001f) continue;
            sum+=SampleWorld(t,wx,wz)*w; wsum+=w;
        }
        return wsum>.0001f?sum/wsum:OceanY-3f;
    }

'''
s=s[:sampler_start]+weighted+s[sampler_end:]
alpha_start=s.index('    static float SampleGridAlpha(Terrain[,] src,Dictionary<int,float[,,]> maps,float wx,float wz,float minX,float minZ,int layer)')
alpha_end=s.index('    static float SampleAlpha(Terrain t,float[,,] a,float wx,float wz,int layer)',alpha_start)
weighted_alpha=r'''    static float SampleGridAlpha(Terrain[,] src,Dictionary<int,float[,,]> maps,float wx,float wz,float minX,float minZ,int layer)
    {
        float sum=0f,wsum=0f;
        for(int r=0;r<Rows;r++) for(int c=0;c<Cols;c++)
        {
            if(IsOffshoreCell(r,c)) continue;
            var t=src[r,c]; if(!t) continue;
            float w=TerrainBlendWeight(t,wx,wz); if(w<.0001f) continue;
            sum+=SampleAlpha(t,maps[r*Cols+c],wx,wz,layer)*w; wsum+=w;
        }
        return wsum>.0001f?sum/wsum:0f;
    }

'''
s=s[:alpha_start]+weighted_alpha+s[alpha_end:]
s=s.replace('terrain.heightmapPixelError=5f; terrain.basemapDistance=1800f;','terrain.heightmapPixelError=7f; terrain.basemapDistance=1200f;',1)
s=s.replace('terrain.detailObjectDistance=115f; terrain.detailObjectDensity=.55f;','terrain.detailObjectDistance=95f; terrain.detailObjectDensity=.45f;',1)
s=s.replace('terrain.treeDistance=1400f; terrain.treeBillboardDistance=220f; terrain.treeCrossFadeLength=20f; terrain.treeMaximumFullLODCount=100;','terrain.treeDistance=1050f; terrain.treeBillboardDistance=180f; terrain.treeCrossFadeLength=16f; terrain.treeMaximumFullLODCount=60;',1)
old='''        float px=mainland.transform.position.x-sizeX-300f;
        float pz=mainland.transform.position.z+mainland.terrainData.size.z*.38f;'''
new='''        float landMinX=mainland.transform.position.x+CoastPad;
        float landMinZ=mainland.transform.position.z+CoastPad;
        float landSpanX=mainland.terrainData.size.x-CoastPad*2f;
        float landSpanZ=mainland.terrainData.size.z-CoastPad*2f;
        float px=landMinX+landSpanX*.04f-sizeX-360f;
        float pz=landMinZ+landSpanZ*.50f;'''
if old not in s: raise SystemExit('dragon placement block not found')
s=s.replace(old,new,1)
s=s.replace('streamer.player=player.transform; streamer.loadDistance=2700f; streamer.unloadDistance=3400f; streamer.checkInterval=.45f;','streamer.player=player.transform; streamer.loadDistance=2250f; streamer.unloadDistance=2900f; streamer.checkInterval=.55f;',1)
s=s.replace('''streamer.regions=Regions.Select(q=>new MMWorldRegionStreamer.Region{sceneName=Path.GetFileNameWithoutExtension(q.scenePath),offset=WorldOffset(q)}).ToArray();''','''streamer.regions=Regions.Select(q=>new MMWorldRegionStreamer.Region{sceneName=Path.GetFileNameWithoutExtension(q.scenePath),offset=WorldOffset(q),keepTerrain=IsOffshoreCell(q.row,q.col)}).ToArray();''',1)
s=s.replace('Debug.Log("ENROTH_STREAMING_MASTER regions="+streamer.regions.Length+" load=2700 unload=3400");','Debug.Log("ENROTH_STREAMING_MASTER regions="+streamer.regions.Length+" load=2250 unload=2900 offshoreTerrains="+streamer.regions.Count(r=>r.keepTerrain));',1)
validate=r'''    static void ValidateFinal(GameObject world,Terrain mainland,Terrain dragon,GameObject player,Camera cam)
    {
        int fail=0;
        var streamer=world.GetComponent<MMWorldRegionStreamer>();
        if(!mainland||mainland.terrainData.heightmapResolution!=1025){Debug.LogError("FINAL_FAIL mainland resolution");fail++;}
        if(!dragon||Mathf.Abs(dragon.terrainData.size.x-1250f)>1f||Mathf.Abs(dragon.terrainData.size.z-1900f)>1f){Debug.LogError("FINAL_FAIL dragon size");fail++;}
        if(!streamer||streamer.regions==null||streamer.regions.Length!=15){Debug.LogError("FINAL_FAIL streamer regions");fail++;}
        else if(streamer.regions.Count(r=>r.keepTerrain)!=3){Debug.LogError("FINAL_FAIL offshore terrain count");fail++;}
        if(!player||!player.GetComponent<MMThirdPersonController>()){Debug.LogError("FINAL_FAIL player controller");fail++;}
        if(!cam||cam.farClipPlane>5000f){Debug.LogError("FINAL_FAIL gameplay camera");fail++;}
        if(world.transform.Find("Regions - Streamable Content")){Debug.LogError("FINAL_FAIL embedded regions remain");fail++;}
        var anim=player?player.GetComponentInChildren<Animator>(true):null;
        if(!anim||!anim.runtimeAnimatorController){Debug.LogError("FINAL_FAIL player animator");fail++;}
        if(fail>0) throw new Exception("FINAL VALIDATION FAILED count="+fail);
        Debug.Log("ENROTH_FINAL_VALIDATION_PASS mainlandRes=1025 streamedRegions=15 offshoreTerrains=3 dragon=1250x1900 cameraFar="+cam.farClipPlane);
    }

'''
mark='''    static void EnsureBuildSettings()
'''
if validate not in s: s=s.replace(mark,validate+mark,1)
dst.write_text(s,encoding='utf-8')
print('patched final6',len(s.splitlines()))
