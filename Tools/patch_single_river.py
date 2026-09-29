from pathlib import Path
P=Path(r'C:\MMUnityPort\Assets\Editor\BuildNewSorpigalOpenWorld.cs')
s=P.read_text(encoding='utf-8')

def rep(old,new,label):
    global s
    n=s.count(old)
    if n!=1:
        raise SystemExit(f'{label}: expected 1, found {n}')
    s=s.replace(old,new,1)
    print('PATCH',label)

rep('''    static float RiverHalfWidth(float sy)
    { return Mathf.Lerp(.30f,.43f,Mathf.PerlinNoise(sy*.21f+11.4f,2.8f)); }''','''    static float RiverHalfWidth(float sy)
    { return Mathf.Lerp(.16f,.23f,Mathf.PerlinNoise(sy*.21f+11.4f,2.8f)); }''','river width')
rep('''    static float SampleNaturalWaterMask(byte[] tiles,float[] center,float sx,float sy)
    {
        float original=SampleSmoothWaterMask(tiles,sx,sy);
        if(!TryRiverCenter(center,sy,out float cx)) return original;
        float d=Mathf.Abs(sx-cx);
        if(d>2.8f) return original;
        float half=RiverHalfWidth(sy);
        return 1f-Mathf.SmoothStep(half,half+.13f,d);
    }''','''    static float RiverOverrideStrength(float sy)
    {
        if(sy<58f||sy>92f) return 0f;
        return Mathf.Min(Mathf.Clamp01((sy-58f)/3f),Mathf.Clamp01((92f-sy)/3f));
    }

    static float SampleNaturalWaterMask(byte[] tiles,float[] center,float sx,float sy)
    {
        float original=SampleSmoothWaterMask(tiles,sx,sy);
        if(!TryRiverCenter(center,sy,out float cx)) return original;
        float d=Mathf.Abs(sx-cx);
        float suppressRadius=Mathf.Lerp(.75f,3.4f,RiverOverrideStrength(sy));
        if(d>=suppressRadius) return original;
        float half=RiverHalfWidth(sy);
        return 1f-Mathf.SmoothStep(half,half+.07f,d);
    }

    static float SampleNonRiverWaterMask(byte[] tiles,float[] center,float sx,float sy)
    {
        float original=SampleSmoothWaterMask(tiles,sx,sy);
        if(!TryRiverCenter(center,sy,out float cx)) return original;
        float d=Mathf.Abs(sx-cx);
        float suppressRadius=Mathf.Lerp(.75f,3.4f,RiverOverrideStrength(sy));
        return d<suppressRadius?0f:original;
    }''','single river mask')
old='''    static GameObject BuildWater(Transform parent, Terrain terrain, Material material)
    {'''
new='''    static GameObject BuildRiverRibbon(Transform parent,float[] center,Material material)
    {
        const int segs=220;
        var verts=new List<Vector3>((segs+1)*2);
        var uvs=new List<Vector2>((segs+1)*2);
        var tris=new List<int>(segs*6);
        for(int i=0;i<=segs;i++)
        {
            float sy=Mathf.Lerp(58.2f,91.8f,i/(float)segs);
            if(!TryRiverCenter(center,sy,out float cx)) continue;
            float sy0=Mathf.Max(58f,sy-.18f),sy1=Mathf.Min(92f,sy+.18f);
            TryRiverCenter(center,sy0,out float cx0); TryRiverCenter(center,sy1,out float cx1);
            Vector2 p0=new Vector2((64f-cx0)*Cell,(sy0-64f)*Cell);
            Vector2 p1=new Vector2((64f-cx1)*Cell,(sy1-64f)*Cell);
            Vector2 tangent=(p1-p0).normalized;
            Vector2 perp=new Vector2(-tangent.y,tangent.x);
            Vector2 c=new Vector2((64f-cx)*Cell,(sy-64f)*Cell);
            float hw=RiverHalfWidth(sy)*Cell;
            verts.Add(new Vector3(c.x+perp.x*hw,.12f,c.y+perp.y*hw));
            verts.Add(new Vector3(c.x-perp.x*hw,.12f,c.y-perp.y*hw));
            uvs.Add(new Vector2(0,i*.11f)); uvs.Add(new Vector2(1,i*.11f));
        }
'''
new+='''        for(int i=0;i<segs;i++)
        {
            int a=i*2,b=a+1,c=a+2,d=a+3;
            tris.Add(a);tris.Add(c);tris.Add(b);
            tris.Add(b);tris.Add(c);tris.Add(d);
        }
        string path="Assets/World/NewSorpigal/Generated/RiverRibbon.asset";
        var mesh=AssetDatabase.LoadAssetAtPath<Mesh>(path);
        if(!mesh){mesh=new Mesh();AssetDatabase.CreateAsset(mesh,path);}
        mesh.Clear();mesh.indexFormat=IndexFormat.UInt32;mesh.SetVertices(verts);mesh.SetTriangles(tris,0);mesh.SetUVs(0,uvs);
        mesh.SetNormals(Enumerable.Repeat(Vector3.up,verts.Count).ToList());mesh.RecalculateBounds();EditorUtility.SetDirty(mesh);
        var go=new GameObject("River - Single Narrow Channel");go.transform.SetParent(parent);
        go.AddComponent<MeshFilter>().sharedMesh=mesh;var mr=go.AddComponent<MeshRenderer>();mr.sharedMaterial=material;
        mr.shadowCastingMode=ShadowCastingMode.Off;mr.receiveShadows=false;
        return go;
    }

    static GameObject BuildWater(Transform parent, Terrain terrain, Material material)
    {'''
rep(old,new,'river ribbon helper')
rep('''            if (SampleNaturalWaterMask(tiles,riverCenter,src.x,src.y)<0.50f) continue;''','''            if (SampleNonRiverWaterMask(tiles,riverCenter,src.x,src.y)<0.50f) continue;''','exclude old river water')

old='''        var go=new GameObject("Water - Smoothed Sea Rivers Pond");
        go.transform.SetParent(parent);
        go.AddComponent<MeshFilter>().sharedMesh=mesh;
        var mr=go.AddComponent<MeshRenderer>();
        mr.sharedMaterial=material;
        mr.shadowCastingMode=ShadowCastingMode.Off;
        mr.receiveShadows=false;
        Debug.Log($"NS_WATER grid={grid} verts={verts.Count} tris={tris.Count/3}");
        return go;'''
new='''        var root=new GameObject("Water - Sea + Single River");
        root.transform.SetParent(parent);
        var sea=new GameObject("Sea + Ponds");sea.transform.SetParent(root.transform);
        sea.AddComponent<MeshFilter>().sharedMesh=mesh;
        var mr=sea.AddComponent<MeshRenderer>();mr.sharedMaterial=material;
        mr.shadowCastingMode=ShadowCastingMode.Off;mr.receiveShadows=false;
        var river=BuildRiverRibbon(root.transform,riverCenter,material);
        Debug.Log($"NS_WATER_SINGLE_RIVER grid={grid} seaTris={tris.Count/3} river={(river?"yes":"no")}");
        return root;'''
rep(old,new,'water root + river ribbon')

P.write_text(s,encoding='utf-8')
print('PATCH_SINGLE_RIVER_DONE',len(s.splitlines()))
