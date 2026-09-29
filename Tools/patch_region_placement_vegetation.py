from pathlib import Path
ROOT=Path(r'C:\MMUnityPort\Assets\Editor')
FILES=['BuildNewSorpigalOpenWorld.cs','BuildCastleIronfistOpenWorld.cs','BuildMistyIslandsOpenWorld.cs','BuildBootlegBayOpenWorld.cs']
for fn in FILES:
    p=ROOT/fn; s=p.read_text(encoding='utf-8')
    zone={'BuildNewSorpigalOpenWorld.cs':'NewSorpigal','BuildCastleIronfistOpenWorld.cs':'CastleIronfist','BuildMistyIslandsOpenWorld.cs':'MistyIslands','BuildBootlegBayOpenWorld.cs':'BootlegBay'}[fn]
    anchor='    static readonly string MatFolder = '
    pos=s.index(anchor); end=s.index('\n',pos)+1
    if 'PlacementPath' not in s:
        s=s[:end]+f'    static readonly string PlacementPath = "Assets/World/{zone}/Data/model_placement_audit.csv";\n'+s[end:]
    s=s.replace('public GameObject grassPrefab, treePrefab, rockA, rockB;','public GameObject grassPrefab, treePrefab, shrubPrefab, fernPrefab, rockA, rockB;')
    s=s.replace('public Material waterMaterial, grassMaterial, rockMaterial, treeBark, treeLeaves, treeAtlas;','public Material waterMaterial, grassMaterial, rockMaterial, treeBark, treeLeaves, treeAtlas, shrubMaterial, fernMaterial;')
    p.write_text(s,encoding='utf-8')
    print('header',fn)
for fn in FILES:
    p=ROOT/fn; s=p.read_text(encoding='utf-8')
    if 'CreateCutoutMaterial("Shrub"' not in s:
        s=s.replace('        e.treeAtlas = CreateTexturedMaterial("TreeAtlas", "Assets/EnvironmentAssets/Gobkit/TreeAtlas.png", 0.03f);',
'''        e.treeAtlas = CreateTexturedMaterial("TreeAtlas", "Assets/EnvironmentAssets/Gobkit/TreeAtlas.png", 0.03f);
        e.shrubMaterial = CreateCutoutMaterial("Shrub", "Assets/Environment/PolyHaven/Models/shrub_02/shrub_02_rgba_1k.png", 0.42f);
        e.fernMaterial = CreateCutoutMaterial("Fern", "Assets/Environment/PolyHaven/Models/fern_02/fern_02_rgba_1k.png", 0.38f);''')
        s=s.replace('        e.treePrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/EnvironmentAssets/UnitySamples/BanyanTree.prefab");',
'''        e.treePrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/EnvironmentAssets/UnitySamples/BanyanTree.prefab");
        e.shrubPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Environment/PolyHaven/Models/shrub_02/shrub_02_1k.fbx");
        e.fernPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Environment/PolyHaven/Models/fern_02/fern_02_1k.fbx");''')
    if 'static Material CreateCutoutMaterial' not in s:
        marker='    static GameObject MakeGrassPrefab(Material mat)'
        idx=s.index(marker)
        helper='''    static Material CreateCutoutMaterial(string name, string texturePath, float cutoff)\n    {\n        var mat=CreateSimpleMaterial(name,Shader.Find("Standard"),Color.white);\n        mat.SetTexture("_MainTex",AssetDatabase.LoadAssetAtPath<Texture2D>(texturePath));\n        mat.SetFloat("_Mode",1f); mat.SetFloat("_Cutoff",cutoff); mat.SetFloat("_Glossiness",0.05f);\n        mat.SetOverrideTag("RenderType","TransparentCutout"); mat.EnableKeyword("_ALPHATEST_ON");\n        mat.DisableKeyword("_ALPHABLEND_ON"); mat.DisableKeyword("_ALPHAPREMULTIPLY_ON"); mat.renderQueue=2450;\n        EditorUtility.SetDirty(mat); return mat;\n    }\n\n'''
        s=s[:idx]+helper+s[idx:]
    p.write_text(s,encoding='utf-8')
    print('env',fn)
placement_helper='''    sealed class PlacementInfo\n    {\n        public string kind; public float sourceBaseOffset; public float waterRatio;\n    }\n    static Dictionary<string,PlacementInfo> placementCache;\n    static PlacementInfo PlacementFor(string name)\n    {\n        if(placementCache==null)\n        {\n            placementCache=new Dictionary<string,PlacementInfo>(StringComparer.OrdinalIgnoreCase);\n            if(File.Exists(PlacementPath)) foreach(string line in File.ReadAllLines(PlacementPath).Skip(1))\n            {\n                var q=line.Split(','); if(q.Length<12) continue;\n                string key=Path.GetFileNameWithoutExtension(q[0]);\n                float.TryParse(q[8],NumberStyles.Float,CultureInfo.InvariantCulture,out float wr);\n                float.TryParse(q[11],NumberStyles.Float,CultureInfo.InvariantCulture,out float bo);\n                placementCache[key]=new PlacementInfo{kind=q[1],waterRatio=wr,sourceBaseOffset=bo};\n            }\n        }\n        placementCache.TryGetValue(Path.GetFileNameWithoutExtension(name),out var pi); return pi;\n    }\n    static bool WaterAffiliated(string name){var p=PlacementFor(name); return p!=null && p.kind!="LAND";}\n\n'''
for fn in FILES:
    p=ROOT/fn; s=p.read_text(encoding='utf-8')
    if 'sealed class PlacementInfo' not in s:
        idx=s.index('    static Bounds BuildBuildings(')
        s=s[:idx]+placement_helper+s[idx:]
    p.write_text(s,encoding='utf-8')
    print('placement helpers',fn)
for fn in FILES:
    p=ROOT/fn; s=p.read_text(encoding='utf-8')
    old='''            Bounds b=GetRendererBounds(go);\n            if (!IsSpecialPlacement(go.name) && b.size.y>0.8f)\n            {\n                float ground=Mathf.Max(SampleTerrainY(terrain,b.center.x,b.center.z),0.42f);\n                go.transform.position+=Vector3.up*(ground+0.04f-b.min.y);\n                grounded++;\n                b=GetRendererBounds(go);\n            }'''
    new='''            Bounds b=GetRendererBounds(go);\n            var placement=PlacementFor(go.name);\n            if (placement!=null && b.size.y>0.15f)\n            {\n                float surface=WaterAffiliated(go.name)?0.12f:SampleTerrainY(terrain,b.center.x,b.center.z);\n                float desiredBase=surface+placement.sourceBaseOffset*ArchitectureScale;\n                go.transform.position+=Vector3.up*(desiredBase-b.min.y);\n                grounded++;\n                b=GetRendererBounds(go);\n            }'''
    if old in s: s=s.replace(old,new)
    else: print('WARN grounding pattern missing',fn)
    s=s.replace('            if(IsSpecialPlacement(child.name)) continue; var rs=child.GetComponentsInChildren<Renderer>(true); if(rs.Length==0) continue;',
'''            if(WaterAffiliated(child.name)) continue; var rs=child.GetComponentsInChildren<Renderer>(true); if(rs.Length==0) continue;''')
    old2='''        float target=(0.42f-terrain.transform.position.y)/td.size.y;\n        foreach(Transform child in architectureRoot)'''
    new2='''        foreach(Transform child in architectureRoot)'''
    if old2 in s: s=s.replace(old2,new2)
    old3='''            Bounds b=rs[0].bounds; for(int i=1;i<rs.Length;i++) b.Encapsulate(rs[i].bounds); b.Expand(new Vector3(5f,0f,5f));\n            int x0='''
    new3='''            Bounds b=rs[0].bounds; for(int i=1;i<rs.Length;i++) b.Encapsulate(rs[i].bounds); b.Expand(new Vector3(5f,0f,5f));\n            float target=Mathf.Clamp01((b.min.y-0.04f-terrain.transform.position.y)/td.size.y);\n            int x0='''
    if old3 in s: s=s.replace(old3,new3)
    p.write_text(s,encoding='utf-8')
    print('building placement',fn)

new_veg=r'''    static void BuildVegetation(Transform parent, Terrain terrain, EnvAssets e)
    {
        var root=new GameObject("Vegetation - MM Anchors + Natural Groves"); root.transform.SetParent(parent);
        var trees=new GameObject("Trees"); trees.transform.SetParent(root.transform);
        var rocks=new GameObject("Rocks"); rocks.transform.SetParent(root.transform);
        var understory=new GameObject("Understory"); understory.transform.SetParent(root.transform);
        Transform architectureRoot=parent.Find("Architecture - MM6 Original Layout Expanded");
        string[] lines=File.ReadAllLines(DecorPath);
        int treeCount=0,extraTrees=0,shrubs=0,ferns=0,rockCount=0,skippedTrees=0,skippedRocks=0;
        for(int i=1;i<lines.Length;i++)
        {
            string[] q=lines[i].Split(','); if(q.Length<10) continue;
            string name=q[1].Trim().ToLowerInvariant();
            if(!float.TryParse(q[4],NumberStyles.Float,CultureInfo.InvariantCulture,out float ox))continue;
            if(!float.TryParse(q[6],NumberStyles.Float,CultureInfo.InvariantCulture,out float oz))continue;
            float x=ox*WorldScale,z=oz*WorldScale; float yaw=Deterministic01(i*13+7)*360f;
            if((name.StartsWith("6tree")||name.StartsWith("tree"))&&e.treePrefab)
            {
                if(!IsSuitableNaturalSpot(terrain,architectureRoot,x,z,true,false)){skippedTrees++;continue;}
                SpawnTree(e.treePrefab,trees.transform,e,x,z,SampleTerrainY(terrain,x,z),yaw,Mathf.Lerp(7.5f,11.5f,Deterministic01(i*19+3)),name+"_"+i.ToString("000")); treeCount++;
                int extras=1+(i%3);
                for(int k=0;k<extras;k++)
                {
                    float a=Deterministic01(i*101+k*17+5)*Mathf.PI*2f; float rad=Mathf.Lerp(7f,27f,Deterministic01(i*107+k*23+9));
                    float ex=x+Mathf.Cos(a)*rad,ez=z+Mathf.Sin(a)*rad;
                    if(!IsSuitableNaturalSpot(terrain,architectureRoot,ex,ez,true,true))continue;
                    SpawnTree(e.treePrefab,trees.transform,e,ex,ez,SampleTerrainY(terrain,ex,ez),Deterministic01(i*109+k)*360f,Mathf.Lerp(6.8f,10.8f,Deterministic01(i*113+k)),"GroveTree_"+i.ToString("000")+"_"+k); extraTrees++;
                }
                if(e.shrubPrefab && i%2==0)
                {
                    int n=1+(i%2); for(int k=0;k<n;k++){float a=Deterministic01(i*127+k)*Mathf.PI*2f,rad=Mathf.Lerp(3f,12f,Deterministic01(i*131+k));float ex=x+Mathf.Cos(a)*rad,ez=z+Mathf.Sin(a)*rad;if(IsSuitableNaturalSpot(terrain,architectureRoot,ex,ez,false,true)){SpawnPlant(e.shrubPrefab,understory.transform,e.shrubMaterial,ex,ez,SampleTerrainY(terrain,ex,ez),i*17+k,Mathf.Lerp(0.75f,1.55f,Deterministic01(i*137+k)),"Shrub");shrubs++;}}
                }
                if(e.fernPrefab && i%3==0)
                {
                    for(int k=0;k<2;k++){float a=Deterministic01(i*139+k)*Mathf.PI*2f,rad=Mathf.Lerp(2.5f,10f,Deterministic01(i*149+k));float ex=x+Mathf.Cos(a)*rad,ez=z+Mathf.Sin(a)*rad;if(IsSuitableNaturalSpot(terrain,architectureRoot,ex,ez,false,true)){SpawnPlant(e.fernPrefab,understory.transform,e.fernMaterial,ex,ez,SampleTerrainY(terrain,ex,ez),i*29+k,Mathf.Lerp(0.35f,0.75f,Deterministic01(i*151+k)),"Fern");ferns++;}}
                }
            }
            else if(name.StartsWith("6rock")&&(e.rockA||e.rockB))
            {
                if(!IsSuitableNaturalSpot(terrain,architectureRoot,x,z,false,false)){skippedRocks++;continue;}
                var src=(i%2==0&&e.rockB)?e.rockB:e.rockA;if(!src)src=e.rockB;var go=(GameObject)PrefabUtility.InstantiatePrefab(src);go.name=name+"_"+i.ToString("000");go.transform.SetParent(rocks.transform);go.transform.position=new Vector3(x,SampleTerrainY(terrain,x,z),z);go.transform.rotation=Quaternion.Euler(0,yaw,0);
                float target=(i%11==0)?Mathf.Lerp(1.5f,2.6f,Deterministic01(i*53)):Mathf.Lerp(0.45f,1.35f,Deterministic01(i*53));ScaleToHeight(go,target);foreach(var r in go.GetComponentsInChildren<Renderer>(true))r.sharedMaterial=e.rockMaterial;rockCount++;
            }
        }
        Debug.Log($"VEGETATION_REALISTIC anchorTrees={treeCount} groveTrees={extraTrees} shrubs={shrubs} ferns={ferns} rocks={rockCount} skippedTrees={skippedTrees} skippedRocks={skippedRocks}");
    }

    static void SpawnTree(GameObject prefab,Transform parent,EnvAssets e,float x,float z,float y,float yaw,float h,string name)
    { var go=(GameObject)PrefabUtility.InstantiatePrefab(prefab);go.name=name;go.transform.SetParent(parent);go.transform.position=new Vector3(x,y,z);go.transform.rotation=Quaternion.Euler(0,yaw,0);ScaleToHeight(go,h);AssignTreeMaterials(go,e);var cc=go.AddComponent<CapsuleCollider>();cc.radius=0.35f;cc.height=Mathf.Min(4.5f,h*0.45f);cc.center=new Vector3(0,cc.height*0.5f,0); }

    static void SpawnPlant(GameObject prefab,Transform parent,Material mat,float x,float z,float y,int seed,float h,string prefix)
    { var go=(GameObject)PrefabUtility.InstantiatePrefab(prefab);go.name=prefix+"_"+seed;go.transform.SetParent(parent);KeepOneVariant(go,seed);go.transform.position=new Vector3(x,y,z);go.transform.rotation=Quaternion.Euler(0,Deterministic01(seed*43+7)*360f,0);ScaleToHeight(go,h);foreach(var r in go.GetComponentsInChildren<Renderer>(true)){r.sharedMaterial=mat;r.shadowCastingMode=ShadowCastingMode.On;r.receiveShadows=true;} }

    static void KeepOneVariant(GameObject go,int seed)
    { var rs=go.GetComponentsInChildren<Renderer>(true); if(rs.Length<=1)return; int keep=Mathf.Abs(seed)%rs.Length; for(int i=rs.Length-1;i>=0;i--)if(i!=keep)UnityEngine.Object.DestroyImmediate(rs[i].gameObject); }

'''
for fn in FILES:
    p=ROOT/fn; s=p.read_text(encoding='utf-8')
    a=s.index('    static void BuildVegetation('); b=s.index('    static void AssignTreeMaterials',a)
    s=s[:a]+new_veg+s[b:]
    p.write_text(s,encoding='utf-8'); print('vegetation',fn)
