from pathlib import Path
src=Path(r'C:\MMUnityPort\Assets\Editor\BuildEnrothFullWorldFinal6.cs')
dst=Path(r'C:\MMUnityPort\Assets\Editor\BuildEnrothFullWorldFinal7.cs')
s=src.read_text(encoding='utf-8-sig').replace('BuildEnrothFullWorldFinal6','BuildEnrothFullWorldFinal7')
old='''        var mainland=BuildMainlandTerrain(world.transform,terrains);
        CullUnderwaterVegetation(regionRoot.transform,mainland);
        for(int rr=0;rr<Rows;rr++) for(int cc=0;cc<Cols;cc++)
            if(terrains[rr,cc] && !IsOffshoreCell(rr,cc)) terrains[rr,cc].gameObject.SetActive(false);'''
new='''        var mainland=BuildMainlandTerrain(world.transform,terrains);
        CullUnderwaterVegetation(regionRoot.transform,mainland);
        var offshoreTerrains=new List<Terrain>();
        for(int rr=0;rr<Rows;rr++) for(int cc=0;cc<Cols;cc++)
            if(IsOffshoreCell(rr,cc)) offshoreTerrains.Add(BuildOffshoreIslandTerrain(world.transform,terrains[rr,cc],Regions.First(q=>q.row==rr&&q.col==cc).display));
        for(int rr=0;rr<Rows;rr++) for(int cc=0;cc<Cols;cc++)
            if(terrains[rr,cc]) terrains[rr,cc].gameObject.SetActive(false);'''
if old not in s: raise SystemExit('build island insertion point missing')
s=s.replace(old,new,1)
s=s.replace('ValidateFinal(world,mainland,dragon,keptPlayer,keptCamera);','ValidateFinal(world,mainland,dragon,offshoreTerrains,keptPlayer,keptCamera);',1)
helper=r'''
    static float ReferenceWaterField(float nx,float nz)
    {
        float bootleg=EllipseField(nx,nz,.78f,.56f,.17f,.17f);
        float centralLake=EllipseField(nx,nz,.56f,.58f,.055f,.050f);
        float easternInlet=EllipseField(nx,nz,.91f,.43f,.10f,.16f);
        return Mathf.Max(bootleg,Mathf.Max(centralLake,easternInlet));
    }
    static float DrySupportHeight(Terrain[,] src,float wx,float wz)
    {
        float best=6f,bestD=float.MaxValue;
        for(int r=0;r<Rows;r++) for(int c=0;c<Cols;c++)
        {
            if(IsOffshoreCell(r,c)) continue;
            var t=src[r,c]; if(!t) continue;
            var p=t.transform.position; var z=t.terrainData.size;
            float margin=Mathf.Min(260f,z.x*.18f);
            float sx=Mathf.Clamp(wx,p.x+margin,p.x+z.x-margin);
            float sz=Mathf.Clamp(wz,p.z+margin,p.z+z.z-margin);
            float h=SampleWorld(t,sx,sz);
            if(h<=OceanY+.35f) continue;
            float d=(new Vector2(wx,wz)-new Vector2(sx,sz)).sqrMagnitude;
            if(d<bestD){bestD=d;best=h;}
        }
        return best;
    }
'''
anchor='    static float RotBlob(float u,float v,float cx,float cy,float rx,float ry,float deg)'
if anchor not in s: raise SystemExit('rotblob anchor missing')
s=s.replace(anchor,helper+'\n'+anchor,1)
oldh='''            float source=SampleGrid(src,wx,wz,gridMinX,gridMinZ);
            float mask=ContinentMask(nx,nz);
            float oceanFloor=-44.2f-(Mathf.PerlinNoise(nx*5.1f+2f,nz*5.3f+9f)*0.65f);
            float wy=Mathf.Lerp(oceanFloor,source,mask);
            if(source<OceanY+.18f) wy=Mathf.Min(wy,source);'''
newh='''            float source=SampleGrid(src,wx,wz,gridMinX,gridMinZ);
            float mask=ContinentMask(nx,nz);
            float referenceWater=ReferenceWaterField(nx,nz);
            if(mask>.38f && source<OceanY+.18f && referenceWater<.30f)
            {
                float support=DrySupportHeight(src,wx,wz);
                float rolling=(Mathf.PerlinNoise(nx*8.3f+2.7f,nz*7.7f+6.1f)-.5f)*5.5f;
                source=Mathf.Max(2.4f,support*.62f+rolling);
            }
            float oceanFloor=-44.2f-(Mathf.PerlinNoise(nx*5.1f+2f,nz*5.3f+9f)*0.65f);
            float wy=Mathf.Lerp(oceanFloor,source,mask);
            if(source<OceanY+.18f && referenceWater>.30f) wy=Mathf.Min(wy,source);'''
if oldh not in s: raise SystemExit('height water block missing')
s=s.replace(oldh,newh,1)
island=r'''
    static Terrain BuildOffshoreIslandTerrain(Transform parent,Terrain src,string display)
    {
        const int res=257;
        string safe=new string(display.Where(char.IsLetterOrDigit).ToArray());
        string path="Assets/World/Enroth/Generated/Offshore_"+safe+".asset";
        var td=AssetDatabase.LoadAssetAtPath<TerrainData>(path);
        if(!td){td=new TerrainData();AssetDatabase.CreateAsset(td,path);}
        td.heightmapResolution=res; td.size=new Vector3(src.terrainData.size.x,TerrainHeight,src.terrainData.size.z);
        td.terrainLayers=CreateMasterLayers();
        var h=new float[res,res]; int land=0;
        for(int y=0;y<res;y++) for(int x=0;x<res;x++)
        {
            float u=x/(float)(res-1),v=y/(float)(res-1);
            float wx=src.transform.position.x+u*src.terrainData.size.x;
            float wz=src.transform.position.z+v*src.terrainData.size.z;
            float wh=SampleWorld(src,wx,wz);
            bool dry=wh>OceanY+.40f;
            if(dry) land++;
            else wh=-38f-(Mathf.PerlinNoise(u*6.1f+3f,v*5.7f+8f)*2f);
            h[y,x]=Mathf.Clamp01((wh-TerrainBaseY)/TerrainHeight);
        }
        td.SetHeights(0,0,h); td.alphamapResolution=256;
        var a=new float[256,256,9];
        int baseLayer=display.StartsWith("Hermit",StringComparison.OrdinalIgnoreCase)?4:1;
        for(int y=0;y<256;y++) for(int x=0;x<256;x++)
        {
            float u=x/255f,v=y/255f;
            float wx=src.transform.position.x+u*src.terrainData.size.x;
            float wz=src.transform.position.z+v*src.terrainData.size.z;
            float wh=SampleWorld(src,wx,wz);
            if(wh<=OceanY+.40f){a[y,x,4]=1f;continue;}
            float rocky=Mathf.Clamp01(Mathf.InverseLerp(26f,82f,wh));
            a[y,x,baseLayer]=1f-rocky*.68f; a[y,x,6]=rocky*.68f;
        }
        td.SetAlphamaps(0,0,a); EditorUtility.SetDirty(td);
        var go=Terrain.CreateTerrainGameObject(td); go.name=display+" - Clipped Offshore Terrain"; go.transform.SetParent(parent);
        go.transform.position=new Vector3(src.transform.position.x,TerrainBaseY,src.transform.position.z);
        var t=go.GetComponent<Terrain>(); t.drawInstanced=true; t.heightmapPixelError=6f; t.basemapDistance=1100f;
        t.detailObjectDistance=70f; t.treeDistance=900f;
        Debug.Log($"OFFSHORE_CLIPPED {display} landSamples={land} pos={go.transform.position}");
        return t;
    }
'''
anchor='    static float ReferenceWaterField(float nx,float nz)'
if anchor not in s: raise SystemExit('reference water anchor missing')
s=s.replace(anchor,island+'\n'+anchor,1)
s=s.replace('streamer.regions=Regions.Select(q=>new MMWorldRegionStreamer.Region{sceneName=Path.GetFileNameWithoutExtension(q.scenePath),offset=WorldOffset(q),keepTerrain=IsOffshoreCell(q.row,q.col)}).ToArray();','streamer.regions=Regions.Select(q=>new MMWorldRegionStreamer.Region{sceneName=Path.GetFileNameWithoutExtension(q.scenePath),offset=WorldOffset(q),keepTerrain=false}).ToArray();',1)
s=s.replace('Debug.Log("ENROTH_STREAMING_MASTER regions="+streamer.regions.Length+" load=2250 unload=2900 offshoreTerrains="+streamer.regions.Count(r=>r.keepTerrain));','Debug.Log("ENROTH_STREAMING_MASTER regions="+streamer.regions.Length+" load=2250 unload=2900 regionalTerrains=disabled clippedOffshore=3");',1)
oldv='''    static void ValidateFinal(GameObject world,Terrain mainland,Terrain dragon,GameObject player,Camera cam)
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
    }'''
newv='''    static void ValidateFinal(GameObject world,Terrain mainland,Terrain dragon,List<Terrain> offshore,GameObject player,Camera cam)
    {
        int fail=0;
        var streamer=world.GetComponent<MMWorldRegionStreamer>();
        if(!mainland||mainland.terrainData.heightmapResolution!=1025){Debug.LogError("FINAL_FAIL mainland resolution");fail++;}
        if(!dragon||Mathf.Abs(dragon.terrainData.size.x-1250f)>1f||Mathf.Abs(dragon.terrainData.size.z-1900f)>1f){Debug.LogError("FINAL_FAIL dragon size");fail++;}
        if(offshore==null||offshore.Count!=3||offshore.Any(t=>!t)){Debug.LogError("FINAL_FAIL clipped offshore terrains");fail++;}
        if(!streamer||streamer.regions==null||streamer.regions.Length!=15){Debug.LogError("FINAL_FAIL streamer regions");fail++;}
        else if(streamer.regions.Any(r=>r.keepTerrain)){Debug.LogError("FINAL_FAIL regional terrain should stream disabled");fail++;}
        if(!player||!player.GetComponent<MMThirdPersonController>()){Debug.LogError("FINAL_FAIL player controller");fail++;}
        if(!cam||cam.farClipPlane>5000f){Debug.LogError("FINAL_FAIL gameplay camera");fail++;}
        if(world.transform.Find("Regions - Streamable Content")){Debug.LogError("FINAL_FAIL embedded regions remain");fail++;}
        var anim=player?player.GetComponentInChildren<Animator>(true):null;
        if(!anim||!anim.runtimeAnimatorController){Debug.LogError("FINAL_FAIL player animator");fail++;}
        if(fail>0) throw new Exception("FINAL VALIDATION FAILED count="+fail);
        Debug.Log("ENROTH_FINAL_VALIDATION_PASS mainlandRes=1025 streamedRegions=15 clippedOffshore=3 dragon=1250x1900 cameraFar="+cam.farClipPlane);
    }'''
if oldv not in s: raise SystemExit('validation block missing')
s=s.replace(oldv,newv,1)
dst.write_text(s,encoding='utf-8')
print('patched final7',len(s.splitlines()))
