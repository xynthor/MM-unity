from pathlib import Path
ED=Path(r'C:\MMUnityPort\Assets\Editor')
files=[p for p in sorted(ED.glob('Build*OpenWorld.cs')) if p.name not in {'BuildEnrothOpenWorld.cs','BuildEnrothFullWorld.cs'}]
helper='''    static bool OverlapXZ(Bounds a, Bounds b, float margin)
    {
        return a.min.x-margin<=b.max.x && a.max.x+margin>=b.min.x &&
               a.min.z-margin<=b.max.z && a.max.z+margin>=b.min.z;
    }

    static List<List<GameObject>> ArchitectureClusters(Transform root)
    {
        var items=root.Cast<Transform>().Where(t=>!WaterAffiliated(t.name) && !t.name.StartsWith("SolidCore_",StringComparison.OrdinalIgnoreCase)).Select(t=>t.gameObject).ToList();
        var result=new List<List<GameObject>>(); var used=new bool[items.Count];
        for(int i=0;i<items.Count;i++) if(!used[i])
        {
            var group=new List<GameObject>(); var q=new Queue<int>(); q.Enqueue(i); used[i]=true;
            while(q.Count>0)
            {
                int k=q.Dequeue(); group.Add(items[k]); var a=GetRendererBounds(items[k]);
                for(int j=0;j<items.Count;j++) if(!used[j])
                {
                    var b=GetRendererBounds(items[j]);
                    float d=Vector2.Distance(new Vector2(a.center.x,a.center.z),new Vector2(b.center.x,b.center.z));
                    if(OverlapXZ(a,b,5f)||d<30f){used[j]=true;q.Enqueue(j);}
                }
            }
            result.Add(group);
        }
        return result;
    }

    static int GroundArchitectureClusters(Terrain terrain, Transform root)
    {
        int moved=0;
        foreach(var cluster in ArchitectureClusters(root))
        {
            Bounds b=GetRendererBounds(cluster[0]); for(int i=1;i<cluster.Count;i++) b.Encapsulate(GetRendererBounds(cluster[i]));
            float[] ys={SampleTerrainY(terrain,b.center.x,b.center.z),SampleTerrainY(terrain,b.min.x,b.min.z),SampleTerrainY(terrain,b.max.x,b.min.z),SampleTerrainY(terrain,b.min.x,b.max.z),SampleTerrainY(terrain,b.max.x,b.max.z)};
            Array.Sort(ys); float surface=ys[2]; float delta=surface-b.min.y;
            foreach(var go in cluster){go.transform.position+=Vector3.up*delta;moved++;}
        }
        return moved;
    }

    static Material ArchitectureCoreMaterial()
    {
        string path=MatFolder+"/Buildings/ArchitectureCore.mat"; var m=AssetDatabase.LoadAssetAtPath<Material>(path);
        if(!m){m=new Material(Shader.Find("Standard"));AssetDatabase.CreateAsset(m,path);}
        m.color=new Color(.18f,.13f,.10f); m.SetFloat("_Glossiness",.03f); m.SetFloat("_Metallic",0f); EditorUtility.SetDirty(m); return m;
    }

    static void AddArchitectureSolidCores(Transform root)
    {
        var mat=ArchitectureCoreMaterial(); int n=0;
        foreach(var cluster in ArchitectureClusters(root))
        {
            Bounds b=GetRendererBounds(cluster[0]); for(int i=1;i<cluster.Count;i++) b.Encapsulate(GetRendererBounds(cluster[i]));
            if(b.size.x<3f||b.size.z<3f||b.size.x>48f||b.size.z>48f||b.size.y<2f||b.size.y>22f) continue;
            var core=GameObject.CreatePrimitive(PrimitiveType.Cube); core.name="SolidCore_"+(n++); core.transform.SetParent(root,true);
            core.transform.position=new Vector3(b.center.x,b.min.y+b.size.y*.43f,b.center.z);
            core.transform.localScale=new Vector3(Mathf.Max(1f,b.size.x-1.4f),Mathf.Max(.8f,b.size.y*.76f),Mathf.Max(1f,b.size.z-1.4f));
            core.GetComponent<MeshRenderer>().sharedMaterial=mat;
        }
        Debug.Log("ARCHITECTURE_SOLID_CORES "+n);
    }

'''
for p in files:
    s=p.read_text(encoding='utf-8')
    a=s.find('    static bool OverlapXZ')
    b=s.find('    static float ArchitectureScaleFactor',a)
    if a<0 or b<0: raise RuntimeError(f'markers missing {p.name}')
    s=s[:a]+helper+s[b:]
    p.write_text(s,encoding='utf-8')
print('repaired',len(files))
