from pathlib import Path

ROOT = Path(r"C:\MMUnityPort")
CI = ROOT / "Assets/Editor/BuildCastleIronfistOpenWorld.cs"
NS = ROOT / "Assets/Editor/BuildNewSorpigalOpenWorld.cs"
TZ = ROOT / "Assets/Editor/BuildEnrothTwoZonePrototype.cs"

def rep(path, old, new, n=1):
    text = path.read_text(encoding="utf-8-sig")
    found = text.count(old)
    if found != n:
        raise RuntimeError(f"{path.name}: expected {n} matches, found {found}: {old[:90]!r}")
    path.write_text(text.replace(old, new, n), encoding="utf-8")

def add_common_layers(path, zone):
    rep(path,
        'public TerrainLayer grassLayer, dirtLayer, roadLayer, rockLayer, wetBankLayer;',
        'public TerrainLayer grassLayer, dirtLayer, roadLayer, rockLayer, wetBankLayer, volcanicLayer, swampLayer;')
    rep(path,
        f'static readonly string PlacementPath = "Assets/World/{zone}/Data/model_placement_audit.csv";',
        f'static readonly string PlacementPath = "Assets/World/{zone}/Data/model_placement_audit.csv";\n    static readonly string GroupPath = "Assets/World/{zone}/Data/tile_groups_u8.bin";')
    rep(path,
        f'e.wetBankLayer = CreateTerrainLayer("WetBank", "Assets/World/{zone}/Generated/wet_bank.png", "Assets/EnvironmentAssets/UnitySamples/dry_soil_NOH.png", new Vector2(5,5), 0.78f);',
        f'e.wetBankLayer = CreateTerrainLayer("WetBank", "Assets/World/{zone}/Generated/wet_bank.png", "Assets/EnvironmentAssets/UnitySamples/dry_soil_NOH.png", new Vector2(5,5), 0.78f);\n        e.volcanicLayer = CreateTerrainLayer("Volcanic", "Assets/EnvironmentAssets/Biomes/ash_ground.png", "Assets/EnvironmentAssets/UnitySamples/rock_boulder_cracked_n.png", new Vector2(8,8), 0.72f);\n        e.swampLayer = CreateTerrainLayer("Swamp", "Assets/EnvironmentAssets/Biomes/swamp_ground.png", "Assets/EnvironmentAssets/UnitySamples/dry_soil_NOH.png", new Vector2(9,9), 0.55f);')
    rep(path,
        'static bool IsDirt(byte tile) => (Sem(tile) & 16) != 0 && !IsWater(tile) && !IsRoad(tile);',
        'static bool IsDirt(byte tile) => (Sem(tile) & 16) != 0 && !IsWater(tile) && !IsRoad(tile);\n    static byte[] groupCache;\n    static byte TileGroup(byte tile)\n    {\n        if (groupCache == null) groupCache = File.ReadAllBytes(GroupPath);\n        return groupCache[tile];\n    }\n    static bool IsVolcanic(byte tile) => TileGroup(tile) == 3;\n    static bool IsSwamp(byte tile) => TileGroup(tile) == 7;')
    rep(path,
        'td.heightmapResolution=TerrainResolution;td.size=new Vector3(TerrainSize,TerrainHeight,TerrainSize);td.terrainLayers=new[]{e.grassLayer,e.dirtLayer,e.roadLayer,e.rockLayer};',
        'td.heightmapResolution=TerrainResolution;td.size=new Vector3(TerrainSize,TerrainHeight,TerrainSize);td.terrainLayers=new[]{e.grassLayer,e.dirtLayer,e.roadLayer,e.rockLayer,e.volcanicLayer,e.swampLayer};')
    rep(path,
        'td.SetHeights(0,0,hm);td.alphamapResolution=512;float[,,] alpha=new float[512,512,4];',
        'td.SetHeights(0,0,hm);td.alphamapResolution=512;float[,,] alpha=new float[512,512,6];')

add_common_layers(CI, "CastleIronfist")
add_common_layers(NS, "NewSorpigal")
rep(CI,
    '    static float CreativeLandHeight(float raw,float sx,float sy)\n    {',
    '    static float SourceFaithfulLandHeight(byte[] heights,float sx,float sy)\n    {\n        float sourceByte=SampleNaturalTerrainHeight(heights,sx,sy);\n        return BlueprintBaseHeight(sourceByte*.25f);\n    }\n\n    static float CreativeLandHeight(float raw,float sx,float sy)\n    {')
rep(CI,
    'float landY=CreativeLandHeight(rawY,sx,sy);',
    'float landY=SourceFaithfulLandHeight(heights,sx,sy);')
rep(CI,
    'float h=CreativeLandHeight(SampleByteBilinear(heights,sx,sy)*.25f,sx,sy),slope=td.GetSteepness(x/511f,y/511f);',
    'float h=SourceFaithfulLandHeight(heights,sx,sy),slope=td.GetSteepness(x/511f,y/511f);')
rep(CI,
    '            else if(IsDirt(tile)){alpha[y,x,1]=.70f;alpha[y,x,0]=.22f;alpha[y,x,3]=.08f;}',
    '            else if(IsSwamp(tile)){alpha[y,x,5]=.82f;alpha[y,x,0]=.12f;alpha[y,x,1]=.06f;}\n            else if(IsVolcanic(tile)){alpha[y,x,4]=.82f;alpha[y,x,3]=.12f;alpha[y,x,1]=.06f;}\n            else if(IsDirt(tile)){alpha[y,x,1]=.70f;alpha[y,x,0]=.22f;alpha[y,x,3]=.08f;}')
rep(NS,
    '            else if(IsDirt(tile)){alpha[y,x,1]=.70f;alpha[y,x,0]=.22f;alpha[y,x,3]=.08f;}',
    '            else if(IsVolcanic(tile)){alpha[y,x,4]=.84f;alpha[y,x,3]=.12f;alpha[y,x,1]=.04f;}\n            else if(IsSwamp(tile)){alpha[y,x,5]=.82f;alpha[y,x,0]=.12f;alpha[y,x,1]=.06f;}\n            else if(IsDirt(tile)){alpha[y,x,1]=.70f;alpha[y,x,0]=.22f;alpha[y,x,3]=.08f;}')
rep(TZ, 'const float Gap=320f;', 'const float Gap=160f;')
rep(TZ, 'const float CastleX=RegionSize+Gap;', 'const float CastleX=-(RegionSize+Gap);')
rep(TZ, 'const float CastleZ=387.02362f;', 'const float CastleZ=0f;')
rep(TZ,
    '"Castle Ironfist - Persistent Ground",new Vector3(CastleX-768f,TerrainBaseY,CastleZ-768f),true,ground.transform);',
    '"Castle Ironfist - Persistent Ground",new Vector3(CastleX-768f,TerrainBaseY,CastleZ-768f),false,ground.transform);')
rep(TZ,
    '        ExtendCastleRoad(ciTile);\n        var transition=BuildTransitionTile(nsTile,ciTile,ground.transform);',
    '        var transition=BuildTransitionTile(ciTile,nsTile,ground.transform);')
rep(TZ,
    'var ciPreview=CloneZone(ciRoot,master,previewZones.transform,new Vector3(CastleX,0f,CastleZ),180f,"Castle Ironfist Zone");',
    'var ciPreview=CloneZone(ciRoot,master,previewZones.transform,new Vector3(CastleX,0f,CastleZ),0f,"Castle Ironfist Zone");')
rep(TZ,
    'rotationY=180f}',
    'rotationY=0f}')
rep(TZ,
    'Debug.Log($"TWO_ZONE_DONE NS=(0,0) CI=({CastleX:F1},{CastleZ:F1}) gap={Gap:F1} rotationCI=180 streamed=true");',
    'Debug.Log($"TWO_ZONE_DONE CI=({CastleX:F1},{CastleZ:F1}) WEST_OF_NS NS=(0,0) gap={Gap:F1} rotationCI=0 streamed=true");')
new_transition = r'''    static Terrain BuildTransitionTile(Terrain west,Terrain east,Transform parent)
    {
        float left=west.transform.position.x+west.terrainData.size.x,right=east.transform.position.x;
        if(right<=left) throw new Exception($"Invalid west/east layout: left={left} right={right}");
        float zMin=Mathf.Max(west.transform.position.z,east.transform.position.z);
        float zMax=Mathf.Min(west.transform.position.z+west.terrainData.size.z,east.transform.position.z+east.terrainData.size.z);
        const int hr=513,ar=512;float zSpan=zMax-zMin;
        string path=$"{Gen}/CI_NS_TransitionTerrain.asset";
        var td=AssetDatabase.LoadAssetAtPath<TerrainData>(path);
        if(!td){td=new TerrainData();AssetDatabase.CreateAsset(td,path);}
        td.heightmapResolution=hr;td.size=new Vector3(right-left,TerrainHeight,zSpan);
        td.terrainLayers=east.terrainData.terrainLayers;
        td.detailPrototypes=east.terrainData.detailPrototypes;
        td.treePrototypes=east.terrainData.treePrototypes;
        var h=new float[hr,hr];
        for(int y=0;y<hr;y++)for(int x=0;x<hr;x++)
        {
            float u=x/(float)(hr-1),v=y/(float)(hr-1),wz=Mathf.Lerp(zMin,zMax,v);
            float t=Mathf.SmoothStep(0f,1f,u);
            float l=SampleTerrainWorld(west,left-.05f,wz),r=SampleTerrainWorld(east,right+.05f,wz);
            float seam=Mathf.Sin(Mathf.PI*u);
            float wy=Mathf.Lerp(l,r,t)+(Mathf.PerlinNoise(u*4.2f+2.6f,wz*.014f+8.2f)-.5f)*.8f*seam;
            if(l<OceanY+.15f&&r<OceanY+.15f) wy=Mathf.Min(wy,OceanY-.30f);
            h[y,x]=Mathf.Clamp01((wy-TerrainBaseY)/TerrainHeight);
        }
        td.SetHeights(0,0,h);td.alphamapResolution=ar;
        int layers=td.terrainLayers.Length;var alpha=new float[ar,ar,layers];
        var wa=west.terrainData.GetAlphamaps(0,0,west.terrainData.alphamapWidth,west.terrainData.alphamapHeight);
        var ea=east.terrainData.GetAlphamaps(0,0,east.terrainData.alphamapWidth,east.terrainData.alphamapHeight);
        for(int y=0;y<ar;y++)for(int x=0;x<ar;x++)
        {
            float u=x/(float)(ar-1),v=y/(float)(ar-1),wz=Mathf.Lerp(zMin,zMax,v);
            float t=Mathf.SmoothStep(0f,1f,u);
            for(int q=0;q<layers;q++)
                alpha[y,x,q]=Mathf.Lerp(SampleAlphaArray(west,wa,left-.05f,wz,q),SampleAlphaArray(east,ea,right+.05f,wz,q),t);
            float sum=0f;for(int q=0;q<layers;q++)sum+=alpha[y,x,q];
            if(sum<.001f){alpha[y,x,0]=1f;sum=1f;}
            for(int q=0;q<layers;q++)alpha[y,x,q]/=sum;
        }
        td.SetAlphamaps(0,0,alpha);EditorUtility.SetDirty(td);
        var go=Terrain.CreateTerrainGameObject(td);go.name="Castle Ironfist - New Sorpigal Transition";
        go.transform.SetParent(parent);go.transform.position=new Vector3(left,TerrainBaseY,zMin);
        var tr=go.GetComponent<Terrain>();tr.drawInstanced=true;tr.heightmapPixelError=1f;
        tr.basemapDistance=1500f;tr.detailObjectDistance=160f;tr.detailObjectDensity=.70f;
        Debug.Log($"TWO_ZONE_TRANSITION WEST=CI EAST=NS x={left:F1}..{right:F1} z={zMin:F1}..{zMax:F1} width={right-left:F1}");
        return tr;
    }
'''
text = TZ.read_text(encoding="utf-8-sig")
start = text.index('    static Terrain BuildTransitionTile(')
end = text.index('\n\n\n    static void BuildTransitionGrass', start)
text = text[:start] + new_transition + text[end:]
TZ.write_text(text, encoding="utf-8")
rep(TZ, 'RenderSeamPreview(master,nsTile,ciTile,transition);', 'RenderSeamPreview(master,transition);')
text = TZ.read_text(encoding="utf-8-sig")
start = text.index('    static void RenderSeamPreview(')
end = text.index('\n\n    static void SaveCamera', start)
new_preview = r'''    static void RenderSeamPreview(Scene scene,Terrain transition)
    {
        bool fog=RenderSettings.fog;RenderSettings.fog=false;
        var go=new GameObject("__TWO_ZONE_SEAM_PREVIEW__");SceneManager.MoveGameObjectToScene(go,scene);var cam=go.AddComponent<Camera>();
        cam.clearFlags=CameraClearFlags.Skybox;cam.fieldOfView=48f;cam.nearClipPlane=.3f;cam.farClipPlane=3500f;
        Vector3 target=transition.transform.position+new Vector3(transition.terrainData.size.x*.5f,8f,transition.terrainData.size.z*.5f);
        go.transform.position=target+new Vector3(-260f,330f,-470f);go.transform.LookAt(target);
        SaveCamera(cam,"Preview/Enroth_NS_CI_Transition.png",1500,950);
        UnityEngine.Object.DestroyImmediate(go);RenderSettings.fog=fog;
    }'''
text = text[:start] + new_preview + text[end:]
TZ.write_text(text, encoding="utf-8")

print("Applied source-faithful terrain layers and CI-west-of-NS orientation fix")
