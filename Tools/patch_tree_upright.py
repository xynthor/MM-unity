from pathlib import Path
root=Path(r'C:\MMUnityPort\Assets\Editor')
exclude={'BuildNewSorpigalOpenWorld.cs','BuildCastleIronfistOpenWorld.cs','BuildDragonIsleOpenWorld.cs','BuildEnrothLinkedOpenWorld.cs'}
files=[p for p in root.glob('Build*OpenWorld.cs') if p.name not in exclude]
old='''    static void SpawnTree(GameObject prefab,Transform parent,EnvAssets e,float x,float z,float y,float yaw,float h,string name)\n    { var go=(GameObject)PrefabUtility.InstantiatePrefab(prefab);go.name=name;go.transform.SetParent(parent);go.transform.position=new Vector3(x,y,z);go.transform.rotation=Quaternion.Euler(0,yaw,0);ScaleToHeight(go,h);if(prefab!=e.deadTreePrefab){if(prefab==e.treePrefab2||prefab==e.treePrefab3||prefab==e.treePrefab4){foreach(var r in go.GetComponentsInChildren<Renderer>(true))r.sharedMaterial=e.treeAtlas;}else AssignTreeMaterials(go,e);}var cc=go.AddComponent<CapsuleCollider>();cc.radius=0.35f;cc.height=Mathf.Min(4.5f,h*0.45f);cc.center=new Vector3(0,cc.height*0.5f,0); }\n'''
new='''    static void NormalizeTreeUpright(GameObject go)\n    {\n        Quaternion baseRot=go.transform.rotation,bestRot=baseRot; float best=-999f;\n        Quaternion[] tests={baseRot,baseRot*Quaternion.Euler(90f,0,0),baseRot*Quaternion.Euler(-90f,0,0),baseRot*Quaternion.Euler(0,0,90f),baseRot*Quaternion.Euler(0,0,-90f)};\n        foreach(var q in tests){go.transform.rotation=q;Bounds b=GetRendererBounds(go);float score=b.size.y-.12f*Mathf.Max(b.size.x,b.size.z);if(score>best){best=score;bestRot=q;}}\n        go.transform.rotation=bestRot;\n    }\n\n    static void SpawnTree(GameObject prefab,Transform parent,EnvAssets e,float x,float z,float y,float yaw,float h,string name)\n    {\n        if(!prefab)return; var go=(GameObject)PrefabUtility.InstantiatePrefab(prefab);go.name=name;go.transform.SetParent(parent);go.transform.position=new Vector3(x,y,z);go.transform.rotation=Quaternion.Euler(0,yaw,0);\n        NormalizeTreeUpright(go);ScaleToHeight(go,h);Bounds b=GetRendererBounds(go);go.transform.position+=Vector3.up*(y-b.min.y);\n        var cc=go.AddComponent<CapsuleCollider>();cc.radius=.38f;cc.height=Mathf.Min(5.2f,h*.48f);cc.center=new Vector3(0,cc.height*.5f,0);\n    }\n'''
for p in files:
    s=p.read_text(encoding='utf-8')
    if old not in s:
        print('NO_MATCH',p.name); continue
    s=s.replace(old,new,1)
    p.write_text(s,encoding='utf-8')
    print('patched',p.name)
