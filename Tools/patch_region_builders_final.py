from pathlib import Path
root=Path(r'C:\MMUnityPort\Assets\Editor')
files=['BuildNewSorpigalOpenWorld.cs','BuildCastleIronfistOpenWorld.cs','BuildMistyIslandsOpenWorld.cs','BuildBootlegBayOpenWorld.cs']
for fn in files:
    p=root/fn; s=p.read_text(encoding='utf-8-sig')
    s=s.replace('var water = BuildWater(root.transform, terrain, env.waterMaterial);\n        Bounds cityBounds = BuildBuildings(root.transform, buildingMats, terrain);','Bounds cityBounds = BuildBuildings(root.transform, buildingMats, terrain);\n        var water = BuildWater(root.transform, terrain, env.waterMaterial);')
    s=s.replace('Assets/EnvironmentAssets/Gobkit/TreeHigh001.fbx','Assets/EnvironmentAssets/UnitySamples/BanyanTree.fbx')
    s=s.replace('float ground=SampleTerrainY(terrain,b.center.x,b.center.z);','float ground=Mathf.Max(SampleTerrainY(terrain,b.center.x,b.center.z),0.42f);')
    if 'StampDryGroundUnderBuildings(terrain, group.transform);' not in s:
        s=s.replace('        Physics.SyncTransforms();\n        Debug.Log($"', '        StampDryGroundUnderBuildings(terrain, group.transform);\n        Physics.SyncTransforms();\n        Debug.Log($"',1)
    if 'static void StampDryGroundUnderBuildings' not in s:
        marker='    static float Deterministic01(int seed)\n'
        method='''    static void StampDryGroundUnderBuildings(Terrain terrain, Transform architectureRoot)\n    {\n        var td=terrain.terrainData; int r=td.heightmapResolution; var hm=td.GetHeights(0,0,r,r);\n        float target=(0.42f-terrain.transform.position.y)/td.size.y;\n        foreach(Transform child in architectureRoot)\n        {\n            if(IsSpecialPlacement(child.name)) continue; var rs=child.GetComponentsInChildren<Renderer>(true); if(rs.Length==0) continue;\n            Bounds b=rs[0].bounds; for(int i=1;i<rs.Length;i++) b.Encapsulate(rs[i].bounds); b.Expand(new Vector3(5f,0f,5f));\n            int x0=Mathf.Clamp(Mathf.FloorToInt((b.min.x-terrain.transform.position.x)/td.size.x*(r-1)),0,r-1);\n            int x1=Mathf.Clamp(Mathf.CeilToInt((b.max.x-terrain.transform.position.x)/td.size.x*(r-1)),0,r-1);\n            int y0=Mathf.Clamp(Mathf.FloorToInt((b.min.z-terrain.transform.position.z)/td.size.z*(r-1)),0,r-1);\n            int y1=Mathf.Clamp(Mathf.CeilToInt((b.max.z-terrain.transform.position.z)/td.size.z*(r-1)),0,r-1);\n            for(int y=y0;y<=y1;y++) for(int x=x0;x<=x1;x++) hm[y,x]=Mathf.Max(hm[y,x],target);\n        }\n        td.SetHeights(0,0,hm); EditorUtility.SetDirty(td);\n    }\n\n'''
        assert marker in s; s=s.replace(marker,method+marker,1)
    p.write_text(s,encoding='utf-8')
    print('patched',fn)
for fn in files:
    p=root/fn; s=p.read_text()
    if 'BuildArchitectureProtectionMask' not in s:
        insert='''    static bool[,] BuildArchitectureProtectionMask(Transform parent,int grid,float step,float origin)\n    {\n        var mask=new bool[grid,grid]; var arch=parent.Find("Architecture - MM6 Original Layout Expanded"); if(!arch) return mask;\n        foreach(Transform child in arch)\n        {\n            if(IsSpecialPlacement(child.name)) continue; var rs=child.GetComponentsInChildren<Renderer>(true); if(rs.Length==0) continue;\n            Bounds b=rs[0].bounds; for(int i=1;i<rs.Length;i++) b.Encapsulate(rs[i].bounds); b.Expand(new Vector3(4f,0f,4f));\n            int x0=Mathf.Clamp(Mathf.FloorToInt((b.min.x-origin)/step),0,grid-1), x1=Mathf.Clamp(Mathf.CeilToInt((b.max.x-origin)/step),0,grid-1);\n            int y0=Mathf.Clamp(Mathf.FloorToInt((b.min.z-origin)/step),0,grid-1), y1=Mathf.Clamp(Mathf.CeilToInt((b.max.z-origin)/step),0,grid-1);\n            for(int y=y0;y<=y1;y++) for(int x=x0;x<=x1;x++) mask[y,x]=true;\n        }\n        return mask;\n    }\n\n'''
        marker='    static GameObject BuildWater(Transform parent, Terrain terrain, Material material)\n'
        assert marker in s; s=s.replace(marker,insert+marker,1)
        s=s.replace('        float origin=-TerrainSize/2f;\n        var verts=', '        float origin=-TerrainSize/2f;\n        bool[,] protectedWater=BuildArchitectureProtectionMask(parent,grid,step,origin);\n        var verts=',1)
        s=s.replace('            Vector2 src=WorldToSource(wx,wz);\n            if (SampleSmoothWaterMask', '            Vector2 src=WorldToSource(wx,wz);\n            if (protectedWater[y,x]) continue;\n            if (SampleSmoothWaterMask',1)
    p.write_text(s)
    print('water protected',fn)