from pathlib import Path
p=Path(r'C:\MMUnityPort\Assets\Editor\BuildEnrothFullWorldFinal5.cs')
s=p.read_text(encoding='utf-8-sig')

def rep(old,new,count=1):
    global s
    if old not in s:
        raise SystemExit('MISSING:\n'+old[:200])
    s=s.replace(old,new,count)

rep('public static class BuildEnrothFullWorldFinal4','public static class BuildEnrothFullWorldFinal5')
rep('''        public string display, scenePath; public int col,row; public bool keepPlayer;\n        public RegionSpec(string d,string s,int c,int r,bool p=false){display=d;scenePath=s;col=c;row=r;keepPlayer=p;}''','''        public string display, scenePath; public int col,row; public bool keepPlayer; public float dx,dz;\n        public RegionSpec(string d,string s,int c,int r,bool p=false,float x=0f,float z=0f){display=d;scenePath=s;col=c;row=r;keepPlayer=p;dx=x;dz=z;}''')
rep('new RegionSpec("Dragonsand","Assets/Scenes/Dragonsand_OpenWorld.unity",1,0),','new RegionSpec("Dragonsand","Assets/Scenes/Dragonsand_OpenWorld.unity",1,0,false,-120f,-100f),')
rep('new RegionSpec("Mire of the Damned","Assets/Scenes/MireOfTheDamned_OpenWorld.unity",2,0),','new RegionSpec("Mire of the Damned","Assets/Scenes/MireOfTheDamned_OpenWorld.unity",2,0,false,100f,-250f),')
rep('new RegionSpec("Castle Ironfist","Assets/Scenes/CastleIronfist_OpenWorld.unity",3,0),','new RegionSpec("Castle Ironfist","Assets/Scenes/CastleIronfist_OpenWorld.unity",3,0,false,160f,80f),')
rep('new RegionSpec("New Sorpigal","Assets/Scenes/NewSorpigal_OpenWorld.unity",4,0,true),','new RegionSpec("New Sorpigal","Assets/Scenes/NewSorpigal_OpenWorld.unity",4,0,true,240f,-300f),')
rep('clone.transform.position+=new Vector3((spec.col-(Cols-1))*RegionSize,0,spec.row*RegionSize);','clone.transform.position+=WorldOffset(spec);')
rep('''    static readonly Vector2[] ContinentPoly={\n        new Vector2(.035f,.29f),new Vector2(.075f,.46f),new Vector2(.115f,.65f),\n        new Vector2(.18f,.82f),new Vector2(.30f,.94f),new Vector2(.46f,.985f),\n        new Vector2(.63f,.955f),new Vector2(.78f,.89f),new Vector2(.90f,.77f),\n        new Vector2(.965f,.63f),new Vector2(.95f,.50f),new Vector2(.89f,.39f),\n        new Vector2(.88f,.36f),new Vector2(.965f,.27f),new Vector2(.985f,.14f),\n        new Vector2(.92f,.055f),new Vector2(.79f,.065f),new Vector2(.68f,.105f),\n        new Vector2(.57f,.075f),new Vector2(.48f,.12f),new Vector2(.39f,.105f),\n        new Vector2(.30f,.045f),new Vector2(.20f,.075f),new Vector2(.115f,.16f)\n    };''','''    static readonly Vector2[] ContinentPoly={\n        new Vector2(.018f,.20f),new Vector2(.042f,.42f),new Vector2(.080f,.62f),\n        new Vector2(.145f,.81f),new Vector2(.285f,.945f),new Vector2(.485f,1.00f),\n        new Vector2(.675f,.965f),new Vector2(.805f,.87f),new Vector2(.885f,.75f),\n        new Vector2(.915f,.63f),new Vector2(.875f,.53f),new Vector2(.895f,.44f),\n        new Vector2(.955f,.36f),new Vector2(.935f,.29f),new Vector2(.995f,.22f),\n        new Vector2(1.035f,.11f),new Vector2(1.040f,.00f),new Vector2(.960f,-.065f),\n        new Vector2(.850f,-.055f),new Vector2(.780f,.020f),new Vector2(.700f,-.015f),\n        new Vector2(.600f,-.070f),new Vector2(.530f,.015f),new Vector2(.450f,.000f),\n        new Vector2(.370f,-.045f),new Vector2(.270f,-.075f),new Vector2(.180f,-.030f),\n        new Vector2(.100f,.070f)\n    };''')
insert='''\n    static Vector3 WorldOffset(RegionSpec spec)\n    {\n        return new Vector3((spec.col-(Cols-1))*RegionSize+spec.dx,0f,spec.row*RegionSize+spec.dz);\n    }\n    static Vector2 MacroOffset(int r,int c)\n    {\n        var q=Regions.FirstOrDefault(v=>v.row==r&&v.col==c);\n        return q==null?Vector2.zero:new Vector2(q.dx,q.dz);\n    }\n'''
marker='''    [MenuItem("MMUnity/Build Full Enroth + Dragon Isle")]'''
rep(marker,insert+'\n'+marker)
rep('static void SmoothTransitionHeights(float[,] h,float minX,float minZ,float spanX,float spanZ)','static void SmoothTransitionHeights(float[,] h,float worldMinX,float worldMinZ,float worldMaxX,float worldMaxZ,float gridMinX,float gridMinZ)')
rep('''                float wx=Mathf.Lerp(minX-CoastPad,minX+spanX+CoastPad,x/(float)(n-1));\n                float wz=Mathf.Lerp(minZ-CoastPad,minZ+spanZ+CoastPad,y/(float)(n-1));\n                WarpGrid(Mathf.Clamp(wx,minX,minX+spanX),Mathf.Clamp(wz,minZ,minZ+spanZ),minX,minZ,out float gx,out float gz);''','''                float wx=Mathf.Lerp(worldMinX-CoastPad,worldMaxX+CoastPad,x/(float)(n-1));\n                float wz=Mathf.Lerp(worldMinZ-CoastPad,worldMaxZ+CoastPad,y/(float)(n-1));\n                WarpGrid(wx,wz,gridMinX,gridMinZ,out float gx,out float gz);''')
rep('''        float minX=src[0,0].transform.position.x, minZ=src[0,0].transform.position.z;\n        float maxX=src[0,Cols-1].transform.position.x+src[0,Cols-1].terrainData.size.x;\n        float maxZ=src[Rows-1,0].transform.position.z+src[Rows-1,0].terrainData.size.z;\n        float spanX=maxX-minX, spanZ=maxZ-minZ;''','''        Vector2 off00=MacroOffset(0,0);\n        float gridMinX=src[0,0].transform.position.x-off00.x, gridMinZ=src[0,0].transform.position.z-off00.y;\n        float gridMaxX=gridMinX+Cols*RegionSize, gridMaxZ=gridMinZ+Rows*RegionSize;\n        float minX=gridMinX,maxX=gridMaxX,minZ=gridMinZ,maxZ=gridMaxZ;\n        for(int rr=0;rr<Rows;rr++) for(int cc=0;cc<Cols;cc++){var t=src[rr,cc];minX=Mathf.Min(minX,t.transform.position.x);minZ=Mathf.Min(minZ,t.transform.position.z);maxX=Mathf.Max(maxX,t.transform.position.x+t.terrainData.size.x);maxZ=Mathf.Max(maxZ,t.transform.position.z+t.terrainData.size.z);}\n        float spanX=gridMaxX-gridMinX, spanZ=gridMaxZ-gridMinZ;\n        float worldSpanX=maxX-minX, worldSpanZ=maxZ-minZ;''')
rep('td.size=new Vector3(spanX+CoastPad*2f,TerrainHeight,spanZ+CoastPad*2f);','td.size=new Vector3(worldSpanX+CoastPad*2f,TerrainHeight,worldSpanZ+CoastPad*2f);')
rep('''            float nx=(wx-minX)/spanX, nz=(wz-minZ)/spanZ;\n            float cx=Mathf.Clamp(wx,minX,maxX), cz=Mathf.Clamp(wz,minZ,maxZ);\n            float source=SampleGrid(src,cx,cz,minX,minZ);''','''            float nx=(wx-gridMinX)/spanX, nz=(wz-gridMinZ)/spanZ;\n            float source=SampleGrid(src,wx,wz,gridMinX,gridMinZ);''')
rep('SmoothTransitionHeights(heights,minX,minZ,spanX,spanZ);','SmoothTransitionHeights(heights,minX,minZ,maxX,maxZ,gridMinX,gridMinZ);')
rep('''            float nx=(wx-minX)/spanX,nz=(wz-minZ)/spanZ,mask=ContinentMask(nx,nz);\n            float cx=Mathf.Clamp(wx,minX,maxX),cz=Mathf.Clamp(wz,minZ,maxZ);\n            float elev=SampleGrid(src,cx,cz,minX,minZ);''','''            float nx=(wx-gridMinX)/spanX,nz=(wz-gridMinZ)/spanZ,mask=ContinentMask(nx,nz);\n            float cx=wx,cz=wz;\n            float elev=SampleGrid(src,cx,cz,gridMinX,gridMinZ);''')
rep('SampleGridAlpha(src,sourceAlpha,cx,cz,minX,minZ,2)','SampleGridAlpha(src,sourceAlpha,cx,cz,gridMinX,gridMinZ,2)')
rep('SampleGridAlpha(src,sourceAlpha,cx,cz,minX,minZ,3)','SampleGridAlpha(src,sourceAlpha,cx,cz,gridMinX,gridMinZ,3)')
rep('float desert=EllipseField(nx,nz,.31f,.13f,.27f,.23f)*(.72f+.28f*macro);','float desert=EllipseField(nx,nz,.30f,.095f,.28f,.24f)*(.72f+.28f*macro);')
rep('float swamp=EllipseField(nx,nz,.53f,.17f,.24f,.23f)*(1f-Mathf.SmoothStep(0f,1f,Mathf.InverseLerp(14f,46f,elev)))*(.70f+.30f*detail);','float swamp=EllipseField(nx,nz,.54f,.075f,.25f,.22f)*(1f-Mathf.SmoothStep(0f,1f,Mathf.InverseLerp(14f,46f,elev)))*(.70f+.30f*detail);')
marker='    static Terrain BuildMainlandTerrain(Transform parent,Terrain[,] src)\n'
for hp in [r'C:\MMUnityPort\Tools\master_grass_helpers.txt',r'C:\MMUnityPort\Tools\master_transition_trees_helpers.txt']:
    helper=Path(hp).read_text(encoding='utf-8')
    if helper.strip().splitlines()[0].strip() not in s:
        s=s.replace(marker,helper+'\n'+marker,1)
rep('td.SetAlphamaps(0,0,alpha); EditorUtility.SetDirty(td);','td.SetAlphamaps(0,0,alpha); BuildMasterGrassDetails(td,alpha); EditorUtility.SetDirty(td);')
rep('''        terrain.drawInstanced=true; terrain.heightmapPixelError=3f; terrain.basemapDistance=2600f;\n        terrain.detailObjectDistance=240f; terrain.detailObjectDensity=1f;\n        Debug.Log($"ENROTH_MAINLAND size={td.size.x:F0}x{td.size.z:F0} height={TerrainHeight:F0}");''','''        terrain.drawInstanced=true; terrain.heightmapPixelError=5f; terrain.basemapDistance=1800f;\n        terrain.detailObjectDistance=115f; terrain.detailObjectDensity=.55f;\n        terrain.treeDistance=1400f; terrain.treeBillboardDistance=220f; terrain.treeCrossFadeLength=20f; terrain.treeMaximumFullLODCount=100;\n        BuildMasterTransitionTrees(terrain,gridMinX,gridMinZ);\n        Debug.Log($"ENROTH_MAINLAND size={td.size.x:F0}x{td.size.z:F0} height={TerrainHeight:F0}");''')
rep('const int res=513; const float sizeX=800f,sizeZ=1100f;','const int res=513; const float sizeX=1250f,sizeZ=1900f;')
rep('float px=mainland.transform.position.x-sizeX-520f;','float px=mainland.transform.position.x-sizeX-300f;')
rep('float pz=mainland.transform.position.z+mainland.terrainData.size.z*.67f;','float pz=mainland.transform.position.z+mainland.terrainData.size.z*.38f;')
rep('if(forest && jitter>.32f && (tree||pine))','if(forest && jitter>.24f && (tree||pine))')
rep('''        EditorSceneManager.SetActiveScene(master);\n        EditorSceneManager.SaveScene(master,ScenePath);\n        RenderMasterPreview(master);\n        AssetDatabase.SaveAssets();\n        Debug.Log("ENROTH_FULL_DONE regions=15 grid=5x3 dragonIsle=true player="+keptPlayer.transform.position);''','''        EditorSceneManager.SetActiveScene(master);\n        EditorSceneManager.SaveScene(master,"Assets/Scenes/Enroth_Full_OpenWorld_EditorPreview.unity");\n        RenderMasterPreview(master);\n        ConvertToStreamingMaster(world,regionRoot,keptPlayer,keptCamera);\n        EnsureBuildSettings();\n        EditorSceneManager.SaveScene(master,ScenePath);\n        AssetDatabase.SaveAssets();\n        Debug.Log("ENROTH_FULL_DONE regions=15 streamed=true dragonIsle=true player="+keptPlayer.transform.position);''')
stream_helpers=r'''    static void ConvertToStreamingMaster(GameObject world,GameObject regionRoot,GameObject player,Camera cam)
    {
        player.transform.SetParent(world.transform,true);
        if(cam) cam.transform.SetParent(world.transform,true);
        var streamer=world.GetComponent<MMWorldRegionStreamer>();
        if(!streamer) streamer=world.AddComponent<MMWorldRegionStreamer>();
        streamer.player=player.transform; streamer.loadDistance=2700f; streamer.unloadDistance=3400f; streamer.checkInterval=.45f;
        streamer.regions=Regions.Select(q=>new MMWorldRegionStreamer.Region{sceneName=Path.GetFileNameWithoutExtension(q.scenePath),offset=WorldOffset(q)}).ToArray();
        UnityEngine.Object.DestroyImmediate(regionRoot);
        Debug.Log("ENROTH_STREAMING_MASTER regions="+streamer.regions.Length+" load=2700 unload=3400");
    }
    static void EnsureBuildSettings()
    {
        var paths=new List<string>{ScenePath}; paths.AddRange(Regions.Select(q=>q.scenePath));
        EditorBuildSettings.scenes=paths.Distinct().Select(q=>new EditorBuildSettingsScene(q,true)).ToArray();
        Debug.Log("ENROTH_BUILD_SETTINGS scenes="+EditorBuildSettings.scenes.Length);
    }

'''
marker='    static Bounds GetWorldBounds(Scene scene)\n'
rep(marker,stream_helpers+marker)
p.write_text(s,encoding='utf-8')
print('patched final5',len(s.splitlines()))
