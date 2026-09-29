from pathlib import Path

p = Path(r"C:\MMUnityPort\Assets\Editor\BuildNewSorpigalOpenWorld.cs")
s = p.read_text(encoding="utf-8")
s = s.replace("const float ArchitectureScale = 1.50f;", "const float ArchitectureScale = 1.35f;")

insert_marker = "    static Bounds BuildBuildings(Transform parent, Dictionary<string, Material> mats, Terrain terrain)\n"
helper = r'''    static string CompositeStructureKey(string name)
    {
        string n=Path.GetFileNameWithoutExtension(name).ToLowerInvariant();
        if(n.StartsWith("064_m063_sgntavernw")||n.StartsWith("080_m079_tavbckw")||n.StartsWith("081_m080_tavfrntw")) return "TavernW";
        if(n.StartsWith("083_m082_smkeepbck")||n.StartsWith("084_m083_smkeepfrnt")) return "SmallKeep";
        if(n.StartsWith("071_m070_evilkc")||n.StartsWith("072_m071_evilkl")||n.StartsWith("073_m072_evilkr")) return "EvilKeep";
        if(n.StartsWith("044_m043_sgnstble")||n.StartsWith("050_m049_stablese")) return "StablesE";
        return null;
    }

'''
if "static string CompositeStructureKey" not in s:
    s = s.replace(insert_marker, helper + insert_marker)

start = s.index(insert_marker)
end = s.index("\n\n    static bool OverlapXZ", start)
new_method = r'''    static Bounds BuildBuildings(Transform parent, Dictionary<string, Material> mats, Terrain terrain)
    {
        var group=new GameObject("Architecture - MM6 Original Layout Expanded");
        group.transform.SetParent(parent);
        var paths=AssetDatabase.FindAssets("t:GameObject",new[]{ObjFolder})
            .Select(AssetDatabase.GUIDToAssetPath)
            .Where(p=>p.EndsWith(".obj",StringComparison.OrdinalIgnoreCase))
            .Where(p=>!Path.GetFileName(p).StartsWith("000_"))
            .OrderBy(p=>p).ToArray();
        var instances=new List<GameObject>();
        foreach(string path in paths)
        {
            var asset=AssetDatabase.LoadAssetAtPath<GameObject>(path); if(!asset) continue;
            var go=(GameObject)PrefabUtility.InstantiatePrefab(asset);
            go.name=Path.GetFileNameWithoutExtension(path);
            go.transform.SetParent(group.transform,true);
            instances.Add(go);
        }
        var compositeBounds=new Dictionary<string,Bounds>();
        var compositeMembers=new Dictionary<string,List<GameObject>>();
        foreach(var go in instances)
        {
            string key=CompositeStructureKey(go.name); if(key==null) continue;
            Bounds b=GetRendererBounds(go);
            if(!compositeBounds.ContainsKey(key)){compositeBounds[key]=b;compositeMembers[key]=new List<GameObject>();}
            else {Bounds cb=compositeBounds[key];cb.Encapsulate(b);compositeBounds[key]=cb;}
            compositeMembers[key].Add(go);
        }
        foreach(var go in instances)
        {
            Bounds original=GetRendererBounds(go);
            string key=CompositeStructureKey(go.name);
            Vector3 pivot=key!=null?compositeBounds[key].center:original.center;
            go.transform.localScale=Vector3.one*ArchitectureScale;
            go.transform.position+=new Vector3(pivot.x*(WorldScale-ArchitectureScale),0f,pivot.z*(WorldScale-ArchitectureScale));
            foreach(var r in go.GetComponentsInChildren<Renderer>(true))
            {
                var src=r.sharedMaterials;var dst=new Material[src.Length];
                for(int i=0;i<src.Length;i++){string mk=src[i]?src[i].name.ToLowerInvariant():"pending";dst[i]=mats.TryGetValue(mk,out var m)?m:mats["pending"];}
                r.sharedMaterials=dst;r.shadowCastingMode=ShadowCastingMode.On;r.receiveShadows=true;
            }
            foreach(var mf in go.GetComponentsInChildren<MeshFilter>(true))
            { if(!mf.sharedMesh)continue;var mc=mf.GetComponent<MeshCollider>();if(!mc)mc=mf.gameObject.AddComponent<MeshCollider>();mc.sharedMesh=mf.sharedMesh; }
        }
        int grounded=0;
        foreach(var go in instances)
        {
            if(CompositeStructureKey(go.name)!=null) continue;
            Bounds b=GetRendererBounds(go);var placement=PlacementFor(go.name);
            if(placement!=null&&b.size.y>0.15f)
            {
                float surface=WaterAffiliated(go.name)?0.12f:SampleTerrainY(terrain,b.center.x,b.center.z);
                float desiredBase=surface+placement.sourceBaseOffset*ArchitectureScale;
                go.transform.position+=Vector3.up*(desiredBase-b.min.y);grounded++;
            }
        }
        foreach(var kv in compositeMembers)
        {
            var members=kv.Value;Bounds b=GetRendererBounds(members[0]);
            for(int i=1;i<members.Count;i++)b.Encapsulate(GetRendererBounds(members[i]));
            float surface=SampleTerrainY(terrain,b.center.x,b.center.z);
            float baseOffset=0f;bool have=false;
            foreach(var m in members){var pl=PlacementFor(m.name);if(pl!=null){baseOffset=have?Mathf.Min(baseOffset,pl.sourceBaseOffset):pl.sourceBaseOffset;have=true;}}
            float desiredBase=surface+baseOffset*ArchitectureScale;
            float dy=desiredBase-b.min.y;
            foreach(var m in members)m.transform.position+=Vector3.up*dy;
            grounded+=members.Count;
            Debug.Log($"NS_COMPOSITE {kv.Key} parts={members.Count} pivot={compositeBounds[kv.Key].center} dy={dy:F3}");
        }
        Bounds all=new Bounds(Vector3.zero,Vector3.zero);bool hasBounds=false;
        foreach(var go in instances){Bounds b=GetRendererBounds(go);if(!hasBounds){all=b;hasBounds=true;}else all.Encapsulate(b);}
        if(group.transform.childCount!=paths.Length)throw new InvalidOperationException($"Architecture import incomplete: expected {paths.Length}, instantiated {group.transform.childCount}");
        Physics.SyncTransforms();
        Debug.Log($"NS_BUILDINGS count={paths.Length} grounded={grounded} architectureScale={ArchitectureScale} composites={compositeMembers.Count} bounds={all}");
        return all;
    }
'''
s = s[:start] + new_method + s[end:]
s = s.replace("terrain.detailObjectDensity = 0.92f;", "terrain.detailObjectDensity = 1.0f;")
s = s.replace("dense[y,x]=Mathf.Clamp(Mathf.RoundToInt(2f+n*3.2f),1,5);", "dense[y,x]=Mathf.Clamp(Mathf.RoundToInt(4f+n*4.5f),3,9);")
s = s.replace("if(n>.48f && slope<20f) tall[y,x]=n>.72f?2:1;", "if(n>.38f && slope<22f) tall[y,x]=n>.72f?3:(n>.52f?2:1);")
s = s.replace("if (SampleSmoothWaterMask(tiles,src.x,src.y)<0.5f) continue;", "if (SampleWaterMask(tiles,src.x,src.y)<0.52f) continue;")

veg_marker = "    static void BuildVegetation(Transform parent, Terrain terrain, EnvAssets e)\n"
veg_helper = r'''    static int NearbyTreeAnchorCount(string[] lines,float x,float z)
    {
        int count=0;
        for(int j=1;j<lines.Length;j++)
        {
            string[] q=lines[j].Split(',');if(q.Length<10)continue;
            string name=q[1].Trim().ToLowerInvariant();if(!(name.StartsWith("6tree")||name.StartsWith("tree")))continue;
            if(!float.TryParse(q[4],NumberStyles.Float,CultureInfo.InvariantCulture,out float ox))continue;
            if(!float.TryParse(q[6],NumberStyles.Float,CultureInfo.InvariantCulture,out float oz))continue;
            float d=Vector2.Distance(new Vector2(x,z),new Vector2(ox*WorldScale,oz*WorldScale));
            if(d>1f&&d<90f)count++;
        }
        return count;
    }

'''
if "static int NearbyTreeAnchorCount" not in s: s=s.replace(veg_marker,veg_helper+veg_marker)
s = s.replace("int extras=1+(i%3);", "int neighbours=NearbyTreeAnchorCount(lines,x,z); int extras=neighbours>=3?Mathf.Clamp(8+neighbours,10,16):(neighbours>=1?4+neighbours*2:1+(i%2));")
s = s.replace("float a=Deterministic01(i*101+k*17+5)*Mathf.PI*2f; float rad=Mathf.Lerp(7f,27f,Deterministic01(i*107+k*23+9));", "float a=Deterministic01(i*101+k*17+5)*Mathf.PI*2f; float maxRad=neighbours>=3?58f:(neighbours>=1?38f:22f); float rad=Mathf.Lerp(5f,maxRad,Deterministic01(i*107+k*23+9));")
s = s.replace("if(e.shrubPrefab && i%2==0)", "if(e.shrubPrefab && (i%2==0 || NearbyTreeAnchorCount(lines,x,z)>=2))")
s = s.replace("if(e.fernPrefab && i%3==0)", "if(e.fernPrefab && (i%3==0 || NearbyTreeAnchorCount(lines,x,z)>=3))")

p.write_text(s,encoding="utf-8")
print("PATCHED", p)

exp = Path(r"C:\MMUnityPort\Tools\rebuild_textured_new_sorpigal_objects_v2.py")
e = exp.read_text(encoding="utf-8")
e = e.replace("            tris=[(0,k,k+1) for k in range(1,count-1)]", "            continue")
exp.write_text(e,encoding="utf-8")
print("PATCHED", exp)
