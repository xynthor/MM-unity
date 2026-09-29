from pathlib import Path
p=Path(r'C:\MMUnityPort\Assets\Editor\BuildEnrothTwoZonePrototype.cs')
s=p.read_text(encoding='utf-8')
s=s.replace('const float Gap=80f;','const float Gap=320f;',1)
s=s.replace('    const float CiRoadJoinZ=181.4f;', '''    const float CiRoadJoinZ=181.4f;
    const float SourceStep=RegionSize/127f;
    static readonly string NsTilePath="Assets/World/NewSorpigal/Data/tilemap_u8.bin";
    static readonly string NsSemPath="Assets/World/NewSorpigal/Data/tile_semantics_u8.bin";
    static readonly string CiTilePath="Assets/World/CastleIronfist/Data/tilemap_u8.bin";
    static readonly string CiSemPath="Assets/World/CastleIronfist/Data/tile_semantics_u8.bin";
    static byte[] nsTiles,nsSem,ciTiles,ciSem;''',1)

def replace_method(src,signature,new_text):
    i=src.index(signature); b=src.index('{',i); depth=0
    for j in range(b,len(src)):
        if src[j]=='{': depth+=1
        elif src[j]=='}':
            depth-=1
            if depth==0: return src[:i]+new_text+src[j+1:]
    raise RuntimeError(signature)

helpers=r'''    static void EnsureEdgeMasks()
    {
        if(nsTiles!=null)return;
        nsTiles=File.ReadAllBytes(NsTilePath);nsSem=File.ReadAllBytes(NsSemPath);
        ciTiles=File.ReadAllBytes(CiTilePath);ciSem=File.ReadAllBytes(CiSemPath);
    }
helpers+=r'''    static float EdgeLandStrength(bool castle,float wz)
    {
        EnsureEdgeMasks();
        float sy=castle?(CastleZ+RegionSize*.5f-wz)/SourceStep:(wz+RegionSize*.5f)/SourceStep;
        sy=Mathf.Clamp(sy,0f,127f);int cy=Mathf.RoundToInt(sy);float sum=0f,ws=0f;
        for(int o=-2;o<=2;o++)
        {
            int y=Mathf.Clamp(cy+o,0,127);float w=o==0?3f:(Mathf.Abs(o)==1?2f:1f);
            int x=castle?127:0;byte tile=castle?ciTiles[y*128+x]:nsTiles[y*128+x];
            byte sem=castle?ciSem[tile]:nsSem[tile];sum+=((sem&1)==0?1f:0f)*w;ws+=w;
        }
        return Mathf.SmoothStep(0f,1f,Mathf.InverseLerp(.18f,.82f,sum/ws));
    }
    static float TransitionLandMask(float u,float wz)
    {
        float t=Mathf.SmoothStep(0f,1f,u),edge=Mathf.Lerp(EdgeLandStrength(false,wz),EdgeLandStrength(true,wz),t);
        float mid=Mathf.Sin(Mathf.PI*u),roadZ=Mathf.Lerp(NsRoadZ,CiRoadEdgeZ,t);
        float noise=(Mathf.PerlinNoise(u*3.1f+4.2f,wz*.0105f+7.7f)-.5f)*34f*mid;
        float half=Mathf.Lerp(262f,184f,mid),d=Mathf.Abs(wz-(roadZ+noise));
        float authored=1f-Mathf.SmoothStep(half-24f,half+30f,d);
        return Mathf.Clamp01(Mathf.Lerp(edge,authored,mid*.76f));
    }
'''
helpers+=r'''    static float SampleAlphaArray(Terrain t,float[,,] a,float wx,float wz,int layer)
    {
        float nx=Mathf.Clamp01((wx-t.transform.position.x)/t.terrainData.size.x);
        float nz=Mathf.Clamp01((wz-t.transform.position.z)/t.terrainData.size.z);
        int x=Mathf.Clamp(Mathf.RoundToInt(nx*(a.GetLength(1)-1)),0,a.GetLength(1)-1);
        int y=Mathf.Clamp(Mathf.RoundToInt(nz*(a.GetLength(0)-1)),0,a.GetLength(0)-1);
        return layer<a.GetLength(2)?a[y,x,layer]:0f;
    }
'''
s=s.replace('    static Terrain BuildTransitionTile',helpers+'\n    static Terrain BuildTransitionTile',1)
method=r'''    static Terrain BuildTransitionTile(Terrain ns,Terrain ci,Transform parent)
    {
        float left=ns.transform.position.x+ns.terrainData.size.x,right=ci.transform.position.x;
        float zMin=Mathf.Max(ns.transform.position.z,ci.transform.position.z);
        float zMax=Mathf.Min(ns.transform.position.z+ns.terrainData.size.z,ci.transform.position.z+ci.terrainData.size.z);
        const int hr=513,ar=512;float zSpan=zMax-zMin;
        string path=$"{Gen}/NS_CI_TransitionTerrain.asset";
        var td=AssetDatabase.LoadAssetAtPath<TerrainData>(path);
        if(!td){td=new TerrainData();AssetDatabase.CreateAsset(td,path);}
        td.heightmapResolution=hr;td.size=new Vector3(right-left,TerrainHeight,zSpan);
        td.terrainLayers=ns.terrainData.terrainLayers;
        td.detailPrototypes=ns.terrainData.detailPrototypes;
        td.treePrototypes=ns.terrainData.treePrototypes;
method+=r'''        var h=new float[hr,hr];
        float startRoadY=SampleTerrainWorld(ns,left-.05f,NsRoadZ);
        float endRoadY=SampleTerrainWorld(ci,right+.05f,CiRoadEdgeZ);
        for(int y=0;y<hr;y++)for(int x=0;x<hr;x++)
        {
            float u=x/(float)(hr-1),v=y/(float)(hr-1),wz=Mathf.Lerp(zMin,zMax,v);
            float t=Mathf.SmoothStep(0f,1f,u);
            float l=SampleTerrainWorld(ns,left-.05f,wz),r=SampleTerrainWorld(ci,right+.05f,wz);
            float land=Mathf.Lerp(Mathf.Max(.55f,l),Mathf.Max(.55f,r),t);
            float seam=Mathf.Sin(Mathf.PI*u),coast=TransitionLandMask(u,wz);
            land+=(Mathf.PerlinNoise(u*5.3f+2.6f,wz*.017f+8.2f)-.5f)*2.2f*seam;
            float bed=-4.4f-Mathf.PerlinNoise(u*4.1f+11.3f,wz*.013f+1.7f)*1.6f;
            float wy=Mathf.Lerp(bed,land,coast);
            float roadZ=Mathf.Lerp(NsRoadZ,CiRoadEdgeZ,t),roadD=Mathf.Abs(wz-roadZ);
            float roadBlend=1f-Mathf.SmoothStep(6f,19f,roadD);
            float roadY=Mathf.Lerp(startRoadY,endRoadY,t);
            wy=Mathf.Lerp(wy,roadY,roadBlend*.96f);
            h[y,x]=Mathf.Clamp01((wy-TerrainBaseY)/TerrainHeight);
        }
        td.SetHeights(0,0,h);td.alphamapResolution=ar;
        int layers=td.terrainLayers.Length;var alpha=new float[ar,ar,layers];
        var na=ns.terrainData.GetAlphamaps(0,0,ns.terrainData.alphamapWidth,ns.terrainData.alphamapHeight);
        var ca=ci.terrainData.GetAlphamaps(0,0,ci.terrainData.alphamapWidth,ci.terrainData.alphamapHeight);
'''
method+=r'''        for(int y=0;y<ar;y++)for(int x=0;x<ar;x++)
        {
            float u=x/(float)(ar-1),v=y/(float)(ar-1),wz=Mathf.Lerp(zMin,zMax,v);
            float t=Mathf.SmoothStep(0f,1f,u),coast=TransitionLandMask(u,wz);
            for(int q=0;q<layers;q++)
                alpha[y,x,q]=Mathf.Lerp(SampleAlphaArray(ns,na,left-.05f,wz,q),SampleAlphaArray(ci,ca,right+.05f,wz,q),t);
            if(coast<.55f&&layers>1)
            {
                float shore=1f-Mathf.SmoothStep(.12f,.62f,coast);
                for(int q=0;q<layers;q++)alpha[y,x,q]*=(1f-shore*.75f);
                alpha[y,x,1]+=shore*.75f;
            }
            float roadZ=Mathf.Lerp(NsRoadZ,CiRoadEdgeZ,t),d=Mathf.Abs(wz-roadZ);
            float rw=1f-Mathf.SmoothStep(6f,17f,d);
            if(layers>2&&rw>.001f){for(int q=0;q<layers;q++)alpha[y,x,q]*=(1f-rw);alpha[y,x,2]+=rw;}
'''
method+=r'''            float sum=0f;for(int q=0;q<layers;q++)sum+=alpha[y,x,q];
            if(sum<.001f){alpha[y,x,0]=1f;sum=1f;}
            for(int q=0;q<layers;q++)alpha[y,x,q]/=sum;
        }
        td.SetAlphamaps(0,0,alpha);BuildTransitionGrass(td,zMin,zMax);EditorUtility.SetDirty(td);
        var go=Terrain.CreateTerrainGameObject(td);go.name="Castle Ironfist - New Sorpigal Transition";
        go.transform.SetParent(parent);go.transform.position=new Vector3(left,TerrainBaseY,zMin);
        var tr=go.GetComponent<Terrain>();tr.drawInstanced=true;tr.heightmapPixelError=1f;
        tr.basemapDistance=1500f;tr.detailObjectDistance=160f;tr.detailObjectDensity=.85f;
        Debug.Log($"TWO_ZONE_TRANSITION x={left:F1}..{right:F1} z={zMin:F1}..{zMax:F1} width={right-left:F1}");
        return tr;
    }
'''
s=replace_method(s,'    static Terrain BuildTransitionTile',method)
grass=r'''    static void BuildTransitionGrass(TerrainData td,float zMin,float zMax)
    {
        if(td.detailPrototypes==null||td.detailPrototypes.Length==0)return;
        const int res=256;td.SetDetailResolution(res,16);int count=Mathf.Min(2,td.detailPrototypes.Length);
        for(int l=0;l<count;l++)
        {
            var d=new int[res,res];
            for(int y=0;y<res;y++)for(int x=0;x<res;x++)
            {
                float u=x/(float)(res-1),wz=Mathf.Lerp(zMin,zMax,y/(float)(res-1));
                float coast=TransitionLandMask(u,wz),t=Mathf.SmoothStep(0f,1f,u);
                float roadZ=Mathf.Lerp(NsRoadZ,CiRoadEdgeZ,t);
                if(coast<.72f||Mathf.Abs(wz-roadZ)<20f)continue;
                float n=Mathf.PerlinNoise(u*13.1f+3.2f,wz*.052f+5.9f);
                d[y,x]=l==0?Mathf.RoundToInt(4f+n*5f):(n>.56f?2:0);
            }
            td.SetDetailLayer(0,0,l,d);
        }
    }
'''
s=s.replace('    static void ExtendCastleRoad',grass+'\n    static void ExtendCastleRoad',1)
player=r'''    static GameObject ClonePersistentPlayer(GameObject nsRoot,Scene master,Transform parent,out Camera cam)
    {
        var src=nsRoot.GetComponentInChildren<MMThirdPersonController>(true);
        if(!src)throw new Exception("New Sorpigal player missing");
        var player=UnityEngine.Object.Instantiate(src.gameObject);player.name="Persistent Player";
        SceneManager.MoveGameObjectToScene(player,master);player.transform.SetParent(parent,true);
        var ctrl=player.GetComponent<MMThirdPersonController>();ctrl.playerCamera=null;cam=null;
        foreach(var stale in player.GetComponentsInChildren<Camera>(true))UnityEngine.Object.DestroyImmediate(stale.gameObject);
        Camera sc=src.playerCamera?src.playerCamera:nsRoot.GetComponentInChildren<Camera>(true);
        if(sc)
        {
            var cgo=UnityEngine.Object.Instantiate(sc.gameObject);cgo.name="Persistent Player Camera";
            SceneManager.MoveGameObjectToScene(cgo,master);cgo.transform.SetParent(parent,true);
            cam=cgo.GetComponent<Camera>();cam.gameObject.SetActive(true);ctrl.playerCamera=cam;
            var al=cam.GetComponent<AudioListener>();if(!al)al=cam.gameObject.AddComponent<AudioListener>();al.enabled=true;
        }
        return player;
    }
'''
s=replace_method(s,'    static GameObject ClonePersistentPlayer',player)
p.write_text(s,encoding='utf-8')
print('patched two-zone transition',len(s.splitlines()))

