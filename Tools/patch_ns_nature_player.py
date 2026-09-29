from pathlib import Path
import re

P=Path(r'C:\MMUnityPort\Assets\Editor\BuildNewSorpigalOpenWorld.cs')
s=P.read_text(encoding='utf-8')


def rep(old,new,label):
    global s
    n=s.count(old)
    if n!=1:
        raise SystemExit(f'{label}: expected 1 match, found {n}')
    s=s.replace(old,new,1)
    print('PATCH',label)

old='''        public Material waterMaterial, grassMaterial, rockMaterial, treeBark, treeLeaves, treeAtlas, treeSmallBark, treeSmallLeaves, pineBark, pineLeaves, shrubMaterial, fernMaterial;
        public GameObject grassPrefab, treePrefab, treePrefab2, treePrefab3, treePrefab4, treeSmallPrefab, pinePrefab, deadTreePrefab, shrubPrefab, fernPrefab, rockA, rockB;'''
new='''        public Material waterMaterial, grassMaterial, rockMaterial, treeBark, treeLeaves, treeAtlas, treeSmallBark, treeSmallLeaves, pineBark, pineLeaves, shrubMaterial, fernMaterial;
        public Material botdGrassMaterial, botdFernMaterial, botdShrubMaterial, botdCloverMaterial;
        public GameObject grassPrefab, treePrefab, treePrefab2, treePrefab3, treePrefab4, treeSmallPrefab, pinePrefab, deadTreePrefab, shrubPrefab, fernPrefab, rockA, rockB;
        public GameObject botdGrassPrefab, botdFernPrefab, botdShrubPrefab, botdCloverPrefab, botdPineGroundPrefab, botdRockPrefab, botdLogPrefab;'''
rep(old,new,'env fields')
old='''        e.shrubMaterial = CreateCutoutMaterial("Shrub", "Assets/Environment/PolyHaven/Models/shrub_02/shrub_02_rgba_1k.png", 0.42f);
        e.fernMaterial = CreateCutoutMaterial("Fern", "Assets/Environment/PolyHaven/Models/fern_02/fern_02_rgba_1k.png", 0.38f);

        e.treePrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/EnvironmentAssets/UnitySamples/BanyanTree.fbx");
        e.treePrefab2 = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/EnvironmentAssets/Gobkit/TreeHigh001.fbx");
        e.treePrefab3 = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/EnvironmentAssets/Gobkit/TreeHigh002.fbx");
        e.treePrefab4 = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/EnvironmentAssets/Gobkit/TreeHigh003.fbx");
        e.pinePrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Environment/PolyHaven/Models/pine_sapling_small/pine_sapling_small_1k.fbx");
        e.deadTreePrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Environment/PolyHaven/Models/dead_tree_trunk_02/dead_tree_trunk_02_1k.fbx");
        e.treeSmallPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/EnvironmentAssets/PolyHaven/TreeSmall02/GameLOD2/tree_small_02_LOD1.fbx");
        e.shrubPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Environment/PolyHaven/Models/shrub_02/shrub_02_1k.fbx");
        e.fernPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Environment/PolyHaven/Models/fern_02/fern_02_1k.fbx");
        e.rockA = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/EnvironmentAssets/Gobkit/Rock001.fbx");
        e.rockB = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/EnvironmentAssets/Gobkit/Rock002.fbx");'''
new='''        e.shrubMaterial = CreateCutoutMaterial("Shrub", "Assets/Environment/PolyHaven/Models/shrub_02/shrub_02_rgba_1k.png", 0.42f);
        e.fernMaterial = CreateCutoutMaterial("Fern", "Assets/Environment/PolyHaven/Models/fern_02/fern_02_rgba_1k.png", 0.38f);
        e.botdGrassMaterial = CreateCutoutMaterial("BOTD_MeadowGrass", "Assets/Art/Environment/_ExternalContent/Quixel/Forest-quixel/MeadowGrass_01/Materials/Meadow_Grass_01_Albedo_Opacity.tif", 0.30f);
        e.botdFernMaterial = CreateCutoutMaterial("BOTD_Fern", "Assets/Art/Environment/_ExternalContent/Quixel/Forest-quixel/Ferns/Materials/Ferns_Albedo_Opacity.tif", 0.30f);
        e.botdShrubMaterial = CreateCutoutMaterial("BOTD_Broadleaf", "Assets/Art/Environment/_ExternalContent/Quixel/Forest-quixel/BroadleafShrub_01/Materials/Broadleaf_Shrub_01_Albedo_Opacity.tif", 0.30f);
        e.botdCloverMaterial = CreateCutoutMaterial("BOTD_Clover", "Assets/Art/Environment/_ExternalContent/Quixel/Forest-quixel/Clover_01/Materials/Clover_01_Albedo_Opacity.tif", 0.28f);

        // Banyan and TreeSmall02 are intentionally not used in New Sorpigal.
        e.treePrefab = null;
        e.treePrefab2 = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Art/Environment/Vegetation/Trees/Pines/Pine_004_new/pine_004_1.FBX");
        e.treePrefab3 = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Art/Environment/Vegetation/Trees/Pines/Pine_005/Pine_005_01.FBX");
        e.treePrefab4 = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Art/Environment/Vegetation/Trees/Pines/Pine_006/pine_006_01.FBX");
        e.pinePrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Art/Environment/Vegetation/Trees/Pines/Pine_004_new/pine_004_4.FBX");
        e.deadTreePrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Art/Environment/Vegetation/Trees/PineDead_001/PineDead_02.FBX");
        e.treeSmallPrefab = null;
        e.shrubPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Art/Environment/_ExternalContent/Quixel/Forest-quixel/BroadleafShrub_01/Broadleaf_Shrub_01_Var1.FBX");
        e.fernPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Art/Environment/_ExternalContent/Quixel/Forest-quixel/Ferns/Fern_var01.FBX");
        e.rockA = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Art/Environment/_ExternalContent/Quixel/Megascans/Rocks/Rock_Granite_rcCwC/Rock_Granite_rcCwC.fbx");
        e.rockB = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/TerrainDemoScene_HDRP/Prefabs/Rocks/Models/Rock_A_02.fbx");
        e.botdGrassPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Art/Environment/_ExternalContent/Quixel/Forest-quixel/MeadowGrass_01/Meadow_Grass_01_Var1.FBX");
        e.botdFernPrefab = e.fernPrefab;
        e.botdShrubPrefab = e.shrubPrefab;
        e.botdCloverPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Art/Environment/_ExternalContent/Quixel/Forest-quixel/Clover_01/Clover_01_Var1.FBX");
        e.botdPineGroundPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Art/Environment/_ExternalContent/Quixel/Forest-quixel/PineGroundScatter_01/PineGroundScatter01_Var1.FBX");
        e.botdRockPrefab = e.rockA;
        e.botdLogPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Art/Environment/_ExternalContent/Quixel/Megascans/Wood/Wood_Log_qdhxa/Wood_Log_qdhxa.fbx");'''
rep(old,new,'BOTD environment assets')
old='''        Vector2 src=WorldToSource(x,z);
        byte t=TileAtSource(tileCache,src.x,src.y);
        if (IsWater(t)||IsRoad(t)) return false;
        if (requireGrass && IsDirt(t)) return false;
        if (HasNearbyForbiddenTile(tileCache,src.x,src.y,tree?1:0,false)) return false;

        float steep=terrain.terrainData.GetSteepness(nx,nz);'''
new='''        Vector2 src=WorldToSource(x,z);
        byte t=TileAtSource(tileCache,src.x,src.y);
        float[] riverCenter=GetRiverCenterline(tileCache);
        float water=SampleNaturalWaterMask(tileCache,riverCenter,src.x,src.y);
        if (water>0.16f || IsRoad(t)) return false;
        if (requireGrass && IsDirt(t)) return false;
        if (tree)
        {
            float edge=Mathf.Max(
                Mathf.Max(SampleNaturalWaterMask(tileCache,riverCenter,src.x+.45f,src.y),SampleNaturalWaterMask(tileCache,riverCenter,src.x-.45f,src.y)),
                Mathf.Max(SampleNaturalWaterMask(tileCache,riverCenter,src.x,src.y+.45f),SampleNaturalWaterMask(tileCache,riverCenter,src.x,src.y-.45f)));
            if(edge>0.28f) return false;
        }

        float steep=terrain.terrainData.GetSteepness(nx,nz);'''
rep(old,new,'natural spot water')

old='''            bool grass=!IsWater(t)&&!IsRoad(t)&&!IsDirt(t)&&!HasNearbyForbiddenTile(tiles,sx,sy,1,false)&&slope<31f;
            if(!grass) continue;
            float n=Mathf.PerlinNoise(sx*.093f+5.7f,sy*.087f+1.9f);
            dense[y,x]=Mathf.Clamp(Mathf.RoundToInt(4f+n*4.5f),3,9);
            if(n>.38f && slope<22f) tall[y,x]=n>.72f?3:(n>.52f?2:1);'''
new='''            float water=SampleNaturalWaterMask(tiles,riverCenter,sx,sy);
            float riverD=RiverDistanceSource(riverCenter,sx,sy);
            bool grass=water<0.10f&&!IsRoad(t)&&!IsDirt(t)&&riverD>0.72f&&slope<34f;
            if(!grass) continue;
            float n=Mathf.PerlinNoise(sx*.093f+5.7f,sy*.087f+1.9f);
            float patch=Mathf.PerlinNoise(sx*.037f+18.4f,sy*.041f+3.2f);
            dense[y,x]=Mathf.Clamp(Mathf.RoundToInt(6f+n*5.5f+patch*2.0f),5,13);
            if(n>.30f && slope<24f) tall[y,x]=n>.72f?5:(n>.50f?3:2);'''
rep(old,new,'dense grass')
old='''        e.pineBark = CreateTexturedMaterial("PineBark", "Assets/Environment/PolyHaven/Models/pine_sapling_small/pine_sapling_small_bark_diff_1k.png", 0.05f);'''
new='''        e.pineBark = CreateSimpleMaterial("PineBark", Shader.Find("Standard"), new Color(0.29f,0.19f,0.11f));'''
rep(old,new,'pine bark')

old='''        e.pineLeaves = CreateCutoutMaterial("PineLeaves", "Assets/EnvironmentAssets/Biomes/pine_twig_rgba.png", 0.42f);'''
new='''        e.pineLeaves = CreateCutoutMaterial("PineLeaves", "Assets/Art/Environment/Vegetation/Trees/Pines/Pine_004_new/Materials/branches_albedo.tif", 0.34f);'''
rep(old,new,'pine leaves')

old='''        e.botdCloverMaterial = CreateCutoutMaterial("BOTD_Clover", "Assets/Art/Environment/_ExternalContent/Quixel/Forest-quixel/Clover_01/Materials/Clover_01_Albedo_Opacity.tif", 0.28f);'''
new='''        e.botdCloverMaterial = CreateCutoutMaterial("BOTD_Clover", "Assets/Art/Environment/_ExternalContent/Quixel/Forest-quixel/Clover_01/Materials/Clover_01_Albedo_Opacity.tif", 0.28f);
        e.shrubMaterial = e.botdShrubMaterial;
        e.fernMaterial = e.botdFernMaterial;'''
rep(old,new,'use BOTD plant materials')

old='''                int neighbours=NearbyTreeAnchorCount(lines,x,z); int extras=neighbours>=3?Mathf.Clamp(8+neighbours,10,16):(neighbours>=1?4+neighbours*2:1+(i%2));'''
new='''                int neighbours=NearbyTreeAnchorCount(lines,x,z); int extras=neighbours>=3?Mathf.Clamp(16+neighbours*2,20,34):(neighbours>=1?8+neighbours*3:1+(i%3));'''
rep(old,new,'denser source groves')

old='''                    float a=Deterministic01(i*101+k*17+5)*Mathf.PI*2f; float maxRad=neighbours>=3?58f:(neighbours>=1?38f:22f); float rad=Mathf.Lerp(5f,maxRad,Deterministic01(i*107+k*23+9));'''
new='''                    float a=Deterministic01(i*101+k*17+5)*Mathf.PI*2f; float maxRad=neighbours>=3?72f:(neighbours>=1?48f:24f); float rad=Mathf.Lerp(5f,maxRad,Mathf.Sqrt(Deterministic01(i*107+k*23+9)));'''
rep(old,new,'wider natural groves')
old='''        Debug.Log($"VEGETATION_REALISTIC anchorTrees={treeCount} groveTrees={extraTrees} shrubs={shrubs} ferns={ferns} rocks={rockCount} skippedTrees={skippedTrees} skippedRocks={skippedRocks}");
    }

    static void SpawnTree(GameObject prefab,Transform parent,EnvAssets e,float x,float z,float y,float yaw,float h,string name)'''
new='''        int fillObjects=BuildNaturalFill(terrain,architectureRoot,trees.transform,rocks.transform,understory.transform,e);
        int riverObjects=BuildRiverside(terrain,architectureRoot,rocks.transform,understory.transform,e);
        Debug.Log($"VEGETATION_REALISTIC anchorTrees={treeCount} groveTrees={extraTrees} shrubs={shrubs} ferns={ferns} rocks={rockCount} fillObjects={fillObjects} riverObjects={riverObjects} skippedTrees={skippedTrees} skippedRocks={skippedRocks}");
    }

    static int BuildNaturalFill(Terrain terrain,Transform architectureRoot,Transform trees,Transform rocks,Transform understory,EnvAssets e)
    {
        int added=0; const int grid=34; float step=TerrainSize/grid;
        for(int gy=0;gy<grid;gy++) for(int gx=0;gx<grid;gx++)
        {
            int seed=90000+gy*grid+gx;
            float x=-TerrainSize*.5f+(gx+.5f)*step+(Deterministic01(seed*11+3)-.5f)*step*.70f;
            float z=-TerrainSize*.5f+(gy+.5f)*step+(Deterministic01(seed*13+7)-.5f)*step*.70f;
            Vector2 src=WorldToSource(x,z);
            float forest=Mathf.PerlinNoise(src.x*.047f+21.4f,src.y*.052f+6.8f);
            if(forest>.64f && IsSuitableNaturalSpot(terrain,architectureRoot,x,z,true,true))
            {
                SpawnTree(PickTreePrefab(e,seed),trees,e,x,z,SampleTerrainY(terrain,x,z),Deterministic01(seed*17)*360f,Mathf.Lerp(8.0f,14.5f,Deterministic01(seed*19)),"ForestFill_"+seed); added++;
                if(forest>.80f)
                {
                    float a=Deterministic01(seed*23)*Mathf.PI*2f, r=Mathf.Lerp(6f,18f,Deterministic01(seed*29));
                    float ex=x+Mathf.Cos(a)*r,ez=z+Mathf.Sin(a)*r;
                    if(IsSuitableNaturalSpot(terrain,architectureRoot,ex,ez,true,true)){SpawnTree(PickTreePrefab(e,seed+31),trees,e,ex,ez,SampleTerrainY(terrain,ex,ez),Deterministic01(seed*31)*360f,Mathf.Lerp(7.0f,12.5f,Deterministic01(seed*37)),"ForestFillCompanion_"+seed);added++;}
                }
            }
            else if(forest>.50f && e.botdShrubPrefab && IsSuitableNaturalSpot(terrain,architectureRoot,x,z,false,true))
            { SpawnPlant(e.botdShrubPrefab,understory,e.botdShrubMaterial,x,z,SampleTerrainY(terrain,x,z),seed,Mathf.Lerp(.75f,1.65f,Deterministic01(seed*41)),"ForestShrub"); added++; }
            float ground=Mathf.PerlinNoise(src.x*.089f+4.2f,src.y*.083f+33.7f);
            if(ground>.77f && e.botdGrassPrefab && IsSuitableNaturalSpot(terrain,architectureRoot,x,z,false,true))
            { SpawnPlant(e.botdGrassPrefab,understory,e.botdGrassMaterial,x,z,SampleTerrainY(terrain,x,z),seed+1,Mathf.Lerp(.35f,.85f,Deterministic01(seed*43)),"MeadowClump"); added++; }
            float rockN=Mathf.PerlinNoise(src.x*.071f+71.1f,src.y*.067f+12.6f);
            if(rockN>.88f && e.botdRockPrefab && IsSuitableNaturalSpot(terrain,architectureRoot,x,z,false,false))
            { SpawnPlant(e.botdRockPrefab,rocks,e.rockMaterial,x,z,SampleTerrainY(terrain,x,z),seed+2,Mathf.Lerp(.55f,1.9f,Deterministic01(seed*47)),"NaturalRock"); added++; }
        }
        return added;
    }

    static int BuildRiverside(Terrain terrain,Transform architectureRoot,Transform rocks,Transform understory,EnvAssets e)'''
rep(old,new,'natural fill insertion')
old='''    static int BuildRiverside(Terrain terrain,Transform architectureRoot,Transform rocks,Transform understory,EnvAssets e)
    { var go=(GameObject)PrefabUtility.InstantiatePrefab(prefab);go.name=name;go.transform.SetParent(parent);go.transform.position=new Vector3(x,y,z);go.transform.rotation=Quaternion.Euler(0,yaw,0);ScaleToHeight(go,h);if(prefab!=e.deadTreePrefab){if(prefab==e.treePrefab2||prefab==e.treePrefab3||prefab==e.treePrefab4){foreach(var r in go.GetComponentsInChildren<Renderer>(true))r.sharedMaterial=e.treeAtlas;}else AssignTreeMaterials(go,e);}var cc=go.AddComponent<CapsuleCollider>();cc.radius=0.35f;cc.height=Mathf.Min(4.5f,h*0.45f);cc.center=new Vector3(0,cc.height*0.5f,0); }'''
new='''    static int BuildRiverside(Terrain terrain,Transform architectureRoot,Transform rocks,Transform understory,EnvAssets e)
    {
        if(tileCache==null) tileCache=File.ReadAllBytes(TilePath);
        float[] center=GetRiverCenterline(tileCache); int added=0;
        for(int i=0;i<64;i++)
        {
            float sy=Mathf.Lerp(60f,91f,i/63f); if(!TryRiverCenter(center,sy,out float cx)) continue;
            foreach(int side in new[]{-1,1})
            {
                int seed=120000+i*7+(side>0?3:0);
                float bank=RiverHalfWidth(sy)+.52f+Deterministic01(seed*11)*.48f;
                float sx=cx+side*bank+(Deterministic01(seed*13)-.5f)*.16f;
                float wx=(64f-sx)*Cell, wz=(sy-64f)*Cell+(Deterministic01(seed*17)-.5f)*4.2f;
                if(!IsSuitableNaturalSpot(terrain,architectureRoot,wx,wz,false,false)) continue;
                int choice=Mathf.Abs(seed)%4; GameObject pf=null; Material mat=null; float h=.6f;
                if(choice==0){pf=e.botdFernPrefab;mat=e.botdFernMaterial;h=Mathf.Lerp(.45f,.95f,Deterministic01(seed*19));}
                else if(choice==1){pf=e.botdShrubPrefab;mat=e.botdShrubMaterial;h=Mathf.Lerp(.85f,1.7f,Deterministic01(seed*19));}
                else if(choice==2){pf=e.botdCloverPrefab;mat=e.botdCloverMaterial;h=Mathf.Lerp(.22f,.48f,Deterministic01(seed*19));}
                else {pf=e.botdGrassPrefab;mat=e.botdGrassMaterial;h=Mathf.Lerp(.35f,.78f,Deterministic01(seed*19));}
                if(pf){SpawnPlant(pf,understory,mat,wx,wz,SampleTerrainY(terrain,wx,wz),seed,h,"Riverbank");added++;}
                if(i%9==0 && e.botdRockPrefab)
                {
                    float rx=wx+side*Mathf.Lerp(1.2f,3.5f,Deterministic01(seed*23));
                    if(IsSuitableNaturalSpot(terrain,architectureRoot,rx,wz,false,false)){SpawnPlant(e.botdRockPrefab,rocks,e.rockMaterial,rx,wz,SampleTerrainY(terrain,rx,wz),seed+1,Mathf.Lerp(.45f,1.25f,Deterministic01(seed*29)),"RiverRock");added++;}
                }
            }
        }
        return added;
    }

    static void NormalizeTreeUpright(GameObject go)
    {
        Quaternion baseRot=go.transform.rotation,bestRot=baseRot; float best=-999f;
        Quaternion[] tests={baseRot,baseRot*Quaternion.Euler(90f,0,0),baseRot*Quaternion.Euler(-90f,0,0),baseRot*Quaternion.Euler(0,0,90f),baseRot*Quaternion.Euler(0,0,-90f)};
        foreach(var q in tests){go.transform.rotation=q;Bounds b=GetRendererBounds(go);float score=b.size.y-.12f*Mathf.Max(b.size.x,b.size.z);if(score>best){best=score;bestRot=q;}}
        go.transform.rotation=bestRot;
    }

    static void SpawnTree(GameObject prefab,Transform parent,EnvAssets e,float x,float z,float y,float yaw,float h,string name)
    {
        if(!prefab)return; var go=(GameObject)PrefabUtility.InstantiatePrefab(prefab);go.name=name;go.transform.SetParent(parent);go.transform.position=new Vector3(x,y,z);go.transform.rotation=Quaternion.Euler(0,yaw,0);
        NormalizeTreeUpright(go);ScaleToHeight(go,h);Bounds b=GetRendererBounds(go);go.transform.position+=Vector3.up*(y-b.min.y);
        if(prefab!=e.deadTreePrefab)AssignTreeMaterials(go,e);
        var cc=go.AddComponent<CapsuleCollider>();cc.radius=.38f;cc.height=Mathf.Min(5.2f,h*.48f);cc.center=new Vector3(0,cc.height*.5f,0);
    }'''
rep(old,new,'riverside and upright trees')
old='''    static void SpawnPlant(GameObject prefab,Transform parent,Material mat,float x,float z,float y,int seed,float h,string prefix)
    { var go=(GameObject)PrefabUtility.InstantiatePrefab(prefab);go.name=prefix+"_"+seed;go.transform.SetParent(parent);KeepOneVariant(go,seed);go.transform.position=new Vector3(x,y,z);go.transform.rotation=Quaternion.Euler(0,Deterministic01(seed*43+7)*360f,0);ScaleToHeight(go,h);foreach(var r in go.GetComponentsInChildren<Renderer>(true)){r.sharedMaterial=mat;r.shadowCastingMode=ShadowCastingMode.On;r.receiveShadows=true;} }'''
new='''    static void SpawnPlant(GameObject prefab,Transform parent,Material mat,float x,float z,float y,int seed,float h,string prefix)
    {
        if(!prefab)return;var go=(GameObject)PrefabUtility.InstantiatePrefab(prefab);go.name=prefix+"_"+seed;go.transform.SetParent(parent);go.transform.position=new Vector3(x,y,z);go.transform.rotation=Quaternion.Euler(0,Deterministic01(seed*43+7)*360f,0);ScaleToHeight(go,h);Bounds b=GetRendererBounds(go);go.transform.position+=Vector3.up*(y-b.min.y);
        foreach(var r in go.GetComponentsInChildren<Renderer>(true)){if(mat)r.sharedMaterial=mat;r.shadowCastingMode=ShadowCastingMode.On;r.receiveShadows=true;}
    }'''
rep(old,new,'plant spawn')

old='''                if(mn.Contains("atlas")) dst[i]=e.treeAtlas; else if(mn.Contains("twig")||mn.Contains("needle")) dst[i]=e.pineLeaves; else if(mn.Contains("pine")&&mn.Contains("bark")) dst[i]=e.pineBark; else if(mn.Contains("small")&&mn.Contains("leaf")) dst[i]=e.treeSmallLeaves; else if(mn.Contains("small")&&mn.Contains("branch")) dst[i]=e.treeSmallBark; else dst[i]=(mn.Contains("leaf")||mn.Contains("canopy")||mn.Contains("branches"))?e.treeLeaves:e.treeBark;'''
new='''                if(mn.Contains("atlas")) dst[i]=e.treeAtlas;
                else if(mn.Contains("branch")||mn.Contains("twig")||mn.Contains("needle")||mn.Contains("leaf")||mn.Contains("canopy")) dst[i]=e.pineLeaves;
                else if(mn.Contains("trunk")||mn.Contains("stump")||mn.Contains("bark")) dst[i]=e.pineBark;
                else dst[i]=e.treeBark;'''
rep(old,new,'tree materials')
old='''        var baseAsset = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Player/Warrior/MM_Warrior.fbx");'''
new='''        var baseAsset = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Player/Runner/Runner_Base.fbx");'''
rep(old,new,'runner base')
rep('''            visual.name = "Warrior Visual";''','''            visual.name = "Runner Visual";''','runner name')
rep('''            ScaleToHeight(visual,1.72f);''','''            ScaleToHeight(visual,1.78f);''','runner scale')

old='''    static RuntimeAnimatorController CreatePlayerAnimatorController()
    {
        string path="Assets/Player/Warrior/MMPlayer.controller";
        AssetDatabase.DeleteAsset(path);
        var ac=AnimatorController.CreateAnimatorControllerAtPath(path);
        ac.AddParameter("Speed",AnimatorControllerParameterType.Float);
        ac.AddParameter("Grounded",AnimatorControllerParameterType.Bool);
        var clips=AssetDatabase.LoadAllAssetsAtPath("Assets/Player/Warrior/MM_Warrior.fbx")
            .OfType<AnimationClip>().Where(c=>!c.name.StartsWith("__preview__")).ToArray();
        Func<string,AnimationClip> get=n=>clips.FirstOrDefault(c=>c.name.IndexOf(n,StringComparison.OrdinalIgnoreCase)>=0);
        var idle=get("Idle") ?? clips.FirstOrDefault();
        var walk=get("Walk") ?? idle;
        var run=get("Run") ?? walk;
        var sm=ac.layers[0].stateMachine;
        var locomotion=sm.AddState("Locomotion");
        var blend=new BlendTree { name="LocomotionBlend", blendType=BlendTreeType.Simple1D,
            blendParameter="Speed", useAutomaticThresholds=false };
        AssetDatabase.AddObjectToAsset(blend,ac);
        if (idle) blend.AddChild(idle,0f);
        if (walk) blend.AddChild(walk,0.35f);
        if (run) blend.AddChild(run,1f);
        locomotion.motion=blend;
        sm.defaultState=locomotion;
        EditorUtility.SetDirty(ac);
        AssetDatabase.SaveAssets();
        return ac;
    }'''
new='''    static RuntimeAnimatorController CreatePlayerAnimatorController()
    {
        string path="Assets/Player/Runner/MMPlayer_NS.controller";
        AssetDatabase.DeleteAsset(path);
        var ac=AnimatorController.CreateAnimatorControllerAtPath(path);
        ac.AddParameter("Speed",AnimatorControllerParameterType.Float);
        ac.AddParameter("Grounded",AnimatorControllerParameterType.Bool);
        var idle=LoadClip("Assets/Player/Runner/Runner_Idle.fbx");
        var walk=LoadClip("Assets/Player/Runner/Runner_Walk.fbx") ?? idle;
        var run=LoadClip("Assets/Player/Runner/Runner_Run.fbx") ?? walk;
        var sprint=LoadClip("Assets/Player/Runner/Runner_Sprint.fbx") ?? run;
        var jumpClip=LoadClip("Assets/Player/Runner/Runner_Jump.fbx");
        var sm=ac.layers[0].stateMachine;
        var locomotion=sm.AddState("Locomotion");
        var blend=new BlendTree { name="LocomotionBlend", blendType=BlendTreeType.Simple1D,
            blendParameter="Speed", useAutomaticThresholds=false };
        AssetDatabase.AddObjectToAsset(blend,ac);
        if(idle)blend.AddChild(idle,0f);
        if(walk)blend.AddChild(walk,.42f);
        if(run)blend.AddChild(run,.71f);
        if(sprint)blend.AddChild(sprint,1f);
        locomotion.motion=blend; sm.defaultState=locomotion;
        if(jumpClip)
        {
            var jump=sm.AddState("Jump");jump.motion=jumpClip;
            var tj=locomotion.AddTransition(jump);tj.hasExitTime=false;tj.duration=.08f;tj.AddCondition(AnimatorConditionMode.IfNot,0,"Grounded");
            var tl=jump.AddTransition(locomotion);tl.hasExitTime=false;tl.duration=.12f;tl.AddCondition(AnimatorConditionMode.If,0,"Grounded");
        }
        EditorUtility.SetDirty(ac);AssetDatabase.SaveAssets();return ac;
    }'''
rep(old,new,'runner animator')

P.write_text(s,encoding='utf-8')
print('PATCH_NS_NATURE_PLAYER_DONE',len(s.splitlines()))