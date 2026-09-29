from pathlib import Path
p=Path(r'C:\MMUnityPort\Assets\Editor\BuildNewSorpigalOpenWorld.cs')
s=p.read_text(encoding='utf-8')
def rep(old,new,n=1):
    global s
    c=s.count(old)
    if c!=n: raise SystemExit(f'expected {n}, got {c}: {old[:80]!r}')
    s=s.replace(old,new,n)
old='''    static readonly float[] RiverSy={58f,62f,67f,73.2f,79f,84.5f,90f,95f,99f,100f,101f,102f,103f};\n    static readonly float[] RiverX={89f,90f,92f,95.5f,100f,103.5f,104.1f,104.2f,104f,103.8f,103.5f,103.2f,103f};\n    static readonly float[] RiverY={.14f,.18f,.24f,.34f,.44f,.58f,.82f,1.10f,3.0f,10f,27f,48f,66f};'''
new='''    static readonly float[] RiverSy={58f,61.5f,65f,68.5f,73.2f,76.5f,80f,84.5f,87.5f,90.5f,94f,97f,99f,100f,101f,102f,103f};\n    static readonly float[] RiverX={89f,91.1f,89.9f,93.0f,95.5f,98.4f,100.4f,103.5f,101.9f,104.4f,103.0f,104.7f,104f,103.8f,103.5f,103.2f,103f};\n    static readonly float[] RiverY={.14f,.17f,.21f,.27f,.34f,.39f,.47f,.58f,.70f,.86f,1.02f,1.75f,3.0f,10f,27f,48f,66f};'''
rep(old,new)
old='''        float broad=(Mathf.PerlinNoise(sy*.082f+7.2f,4.1f)-.5f)*2.05f;\n        float secondary=Mathf.Sin(sy*.305f+.9f)*.44f+Mathf.Sin(sy*.665f+2.4f)*.18f;'''
new='''        float broad=(Mathf.PerlinNoise(sy*.092f+7.2f,4.1f)-.5f)*.62f;\n        float secondary=Mathf.Sin(sy*.37f+.9f)*.16f+Mathf.Sin(sy*.79f+2.4f)*.08f;'''
rep(old,new)
rep('SpawnPlant(roots,understory,null,','SpawnPlant(roots,understory,e.treeBark,')
rep('SpawnPlant(debris,understory,null,','SpawnPlant(debris,understory,e.treeBark,')
rep('SpawnPlant(e.botdLogPrefab,understory,null,','SpawnPlant(e.botdLogPrefab,understory,e.treeBark,')
marker='''    static GameObject BuildWater(Transform parent,Terrain terrain,Material unused)\n    {'''
bank='''    static GameObject BuildRiverBanks(Transform parent,Terrain terrain)\n    {\n        const int segs=560; var verts=new List<Vector3>((segs+1)*4); var uvs=new List<Vector2>((segs+1)*4); var tris=new List<int>(segs*12);\n        for(int i=0;i<=segs;i++)\n        {\n            float sy=Mathf.Lerp(103f,58f,i/(float)segs),cx=RiverCenterX(sy);\n            float sy0=Mathf.Max(58f,sy-.06f),sy1=Mathf.Min(103f,sy+.06f),cx0=RiverCenterX(sy0),cx1=RiverCenterX(sy1);\n            Vector2 p0=new Vector2((64f-cx0)*Cell,(sy0-64f)*Cell),p1=new Vector2((64f-cx1)*Cell,(sy1-64f)*Cell);\n            Vector2 tangent=(p0-p1).normalized,perp=new Vector2(-tangent.y,tangent.x),c=new Vector2((64f-cx)*Cell,(sy-64f)*Cell);\n            float hw=RiverHalfWidthWorld(sy),d=RiverDownstream01(sy);\n            float lb=Mathf.Lerp(2.0f,4.6f,d)*Mathf.Lerp(.82f,1.22f,Mathf.PerlinNoise(sy*.16f+2.7f,5.1f));\n            float rb=Mathf.Lerp(2.2f,4.3f,d)*Mathf.Lerp(.82f,1.22f,Mathf.PerlinNoise(sy*.14f+8.9f,1.4f));\n            Vector2 li=c+perp*(hw-.10f),lo=c+perp*(hw+lb),ri=c-perp*(hw-.10f),ro=c-perp*(hw+rb);\n            float iy=RiverSurfaceY(sy)+.055f,loy=Mathf.Max(iy+.035f,SampleTerrainY(terrain,lo.x,lo.y)+.025f),roy=Mathf.Max(iy+.035f,SampleTerrainY(terrain,ro.x,ro.y)+.025f);\n            verts.Add(new Vector3(lo.x,loy,lo.y)); verts.Add(new Vector3(li.x,iy,li.y)); verts.Add(new Vector3(ri.x,iy,ri.y)); verts.Add(new Vector3(ro.x,roy,ro.y));\n            float v=i*.045f; uvs.Add(new Vector2(0,v));uvs.Add(new Vector2(1,v));uvs.Add(new Vector2(0,v));uvs.Add(new Vector2(1,v));\n        }\n'''
rep(marker,bank+marker)
bank2='''        for(int i=0;i<segs;i++)\n        {\n            int a=i*4,b=a+1,c=a+2,d=a+3,na=a+4,nb=b+4,nc=c+4,nd=d+4;\n            tris.Add(a);tris.Add(b);tris.Add(na);tris.Add(b);tris.Add(nb);tris.Add(na);\n            tris.Add(c);tris.Add(d);tris.Add(nc);tris.Add(d);tris.Add(nd);tris.Add(nc);\n        }\n        string path="Assets/World/NewSorpigal/Generated/RealRiverBanks.asset";\n        var mesh=AssetDatabase.LoadAssetAtPath<Mesh>(path);if(!mesh){mesh=new Mesh();AssetDatabase.CreateAsset(mesh,path);}\n        mesh.Clear();mesh.indexFormat=IndexFormat.UInt32;mesh.SetVertices(verts);mesh.SetTriangles(tris,0);mesh.SetUVs(0,uvs);mesh.RecalculateNormals();mesh.RecalculateBounds();EditorUtility.SetDirty(mesh);\n        var go=new GameObject("River Banks - Natural Soil");go.transform.SetParent(parent);go.AddComponent<MeshFilter>().sharedMesh=mesh;\n        var mat=CreateTexturedMaterial("RiverBankSoil","Assets/EnvironmentAssets/UnitySamples/dry_soil_CH.png",.025f);mat.color=new Color(.42f,.38f,.25f,1f);EditorUtility.SetDirty(mat);\n        var mr=go.AddComponent<MeshRenderer>();mr.sharedMaterial=mat;mr.shadowCastingMode=ShadowCastingMode.Off;mr.receiveShadows=true;return go;\n    }\n\n'''
s=s.replace(bank+marker,bank+bank2+marker,1)
old='''        var river=BuildRealRiver(root.transform);\n        Debug.Log($"NS_REAL_WATER ocean=yes river={(river?"yes":"no")} source=(103,103) upperBridge=(103.5,84.5) lowerBridge=(95.5,73.2) mouth=(89,58)");'''
new='''        var banks=BuildRiverBanks(root.transform,terrain);\n        var river=BuildRealRiver(root.transform);\n        Debug.Log($"NS_REAL_WATER ocean=yes river={(river?"yes":"no")} banks={(banks?"yes":"no")} source=(103,103) upperBridge=(103.5,84.5) lowerBridge=(95.5,73.2) mouth=(89,58)");'''
rep(old,new)
p.write_text(s,encoding='utf-8')
print('RIVER_BANKS_V5_PATCHED')
