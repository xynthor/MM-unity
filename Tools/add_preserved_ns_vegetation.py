from pathlib import Path
p=Path(r'C:\MMUnityPort\Assets\Editor\BuildNewSorpigalOpenWorld.cs')
s=p.read_text(encoding='utf-8-sig')
s=s.replace('''        BuildVegetation(root.transform, terrain, env);\n        var player = BuildPlayer(root.transform, terrain);''','''        BuildVegetation(root.transform, terrain, env);\n        RestorePreservedBiomeFill(root.transform, terrain);\n        var player = BuildPlayer(root.transform, terrain);''')
insert='''\n    static void RestorePreservedBiomeFill(Transform parent, Terrain terrain)\n    {\n        const string path="Assets/World/NewSorpigal/Generated/PreservedBiomeFill.prefab";\n        var prefab=AssetDatabase.LoadAssetAtPath<GameObject>(path);\n        if(!prefab) return;\n        var go=(GameObject)PrefabUtility.InstantiatePrefab(prefab);\n        go.name="Vegetation - Preserved User Additions";\n        go.transform.SetParent(parent,false);\n        int grounded=0;\n        foreach(Transform group in go.transform)\n        foreach(Transform item in group)\n        {\n            Vector3 p=item.localPosition;\n            p.x/=3f; p.z/=3f; item.localPosition=p;\n            var rs=item.GetComponentsInChildren<Renderer>(true);\n            if(rs.Length==0) continue;\n            Bounds b=rs[0].bounds; for(int i=1;i<rs.Length;i++) b.Encapsulate(rs[i].bounds);\n            float y=SampleTerrainY(terrain,b.center.x,b.center.z);\n            item.position+=Vector3.up*(y-b.min.y); grounded++;\n        }\n        Debug.Log("NS_PRESERVED_VEGETATION grounded="+grounded);\n    }\n\n'''
marker='    static bool IsSpecialPlacement(string name)'
if marker not in s: raise SystemExit('marker missing')
s=s.replace(marker,insert+marker,1)
p.write_text(s,encoding='utf-8')
print('added preserved NS vegetation restore at 1x1 positions')