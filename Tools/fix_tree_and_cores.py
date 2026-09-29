from pathlib import Path
ED=Path(r'C:\MMUnityPort\Assets\Editor')
files=[p for p in sorted(ED.glob('Build*OpenWorld.cs')) if p.name not in {'BuildEnrothOpenWorld.cs','BuildEnrothFullWorld.cs'}]
for p in files:
    s=p.read_text(encoding='utf-8')
    old='''            if(b.size.x<3f||b.size.z<3f||b.size.x>48f||b.size.z>48f||b.size.y<2f||b.size.y>22f) continue;
            var core=GameObject.CreatePrimitive(PrimitiveType.Cube); core.name="SolidCore_"+(n++); core.transform.SetParent(root,true);
            core.transform.position=new Vector3(b.center.x,b.min.y+b.size.y*.43f,b.center.z);
            core.transform.localScale=new Vector3(Mathf.Max(1f,b.size.x-1.4f),Mathf.Max(.8f,b.size.y*.76f),Mathf.Max(1f,b.size.z-1.4f));'''
    new='''            if(b.size.x<4f||b.size.z<4f||b.size.x>32f||b.size.z>32f||b.size.y<2.2f||b.size.y>18f) continue;
            var core=GameObject.CreatePrimitive(PrimitiveType.Cube); core.name="SolidCore_"+(n++); core.transform.SetParent(root,true);
            core.transform.position=new Vector3(b.center.x,b.min.y+b.size.y*.34f,b.center.z);
            core.transform.localScale=new Vector3(Mathf.Max(1f,b.size.x*.55f),Mathf.Max(.8f,b.size.y*.56f),Mathf.Max(1f,b.size.z*.55f));'''
    if old in s: s=s.replace(old,new)
    oldspawn='''    { var go=(GameObject)PrefabUtility.InstantiatePrefab(prefab);go.name=name;go.transform.SetParent(parent);go.transform.position=new Vector3(x,y,z);go.transform.rotation=Quaternion.Euler(0,yaw,0);ScaleToHeight(go,h);var cc=go.AddComponent<CapsuleCollider>();cc.radius=0.35f;cc.height=Mathf.Min(4.5f,h*0.45f);cc.center=new Vector3(0,cc.height*0.5f,0); }'''
    newspawn='''    { var go=(GameObject)PrefabUtility.InstantiatePrefab(prefab);go.name=name;go.transform.SetParent(parent);go.transform.position=new Vector3(x,y,z);go.transform.rotation=Quaternion.Euler(0,yaw,0);ScaleToHeight(go,h);if(prefab!=e.deadTreePrefab){if(prefab==e.treePrefab2||prefab==e.treePrefab3||prefab==e.treePrefab4){foreach(var r in go.GetComponentsInChildren<Renderer>(true))r.sharedMaterial=e.treeAtlas;}else AssignTreeMaterials(go,e);}var cc=go.AddComponent<CapsuleCollider>();cc.radius=0.35f;cc.height=Mathf.Min(4.5f,h*0.45f);cc.center=new Vector3(0,cc.height*0.5f,0); }'''
    if oldspawn in s: s=s.replace(oldspawn,newspawn)
    p.write_text(s,encoding='utf-8')
print('patched',len(files))
