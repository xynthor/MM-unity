from pathlib import Path
p=Path(r'C:\MMUnityPort\Assets\Editor\MMSourceGridExactPostPass.cs')
s=p.read_text(encoding='utf-8')

s=s.replace('''    static void ApplyCactusMaterials(GameObject go)
    {
        if(!go) return;
        foreach(var r in go.GetComponentsInChildren<Renderer>(true))
        {
            var a=r.sharedMaterials;
            for(int k=0;k<a.Length;k++)
            {
                int i=CactusIndex(a[k]?a[k].name:r.gameObject.name);
                if(i==0)i=CactusIndex(r.gameObject.name);
                if(i>0)a[k]=CactusMaterial(i);
            }
            r.sharedMaterials=a;
        }
    }
''','''    static void ApplyCactusMaterials(GameObject go)
    {
        if(!go) return;
        foreach(var r in go.GetComponentsInChildren<Renderer>(true))
        {
            var a=r.sharedMaterials;
            for(int k=0;k<a.Length;k++)
            {
                int i=CactusIndex(a[k]?a[k].name:r.gameObject.name);
                if(i==0)i=CactusIndex(r.gameObject.name);
                if(i>0)a[k]=CactusMaterial(i);
            }
            r.sharedMaterials=a;
        }
    }

    static void KeepOneCactusVariant(GameObject go,int seed)
    {
        if(!go) return;
        var rs=go.GetComponentsInChildren<Renderer>(true)
            .Where(r=>CactusIndex(r.gameObject.name)>0)
            .OrderBy(r=>CactusIndex(r.gameObject.name)).ToArray();
        if(rs.Length==0) return;
        int keep=Mathf.Abs(seed)%rs.Length;
        for(int i=0;i<rs.Length;i++) rs[i].gameObject.SetActive(i==keep);
    }
''')
s=s.replace('''        Quaternion q=old.transform.rotation;
        string n=old.name;
        UnityEngine.Object.DestroyImmediate(old);
        var go=(GameObject)PrefabUtility.InstantiatePrefab(prefab);
        go.name=n;
        go.transform.SetParent(parent,true);
        float y=terrain.SampleHeight(new Vector3(p.x,0f,p.z))+terrain.transform.position.y;
        go.transform.position=new Vector3(p.x,y,p.z);
        go.transform.rotation=q;
        go.transform.localScale=Vector3.one;
        ApplyCactusMaterials(go);
        return go;
''','''        Quaternion q=old.transform.rotation;
        string n=old.name;
        UnityEngine.Object.DestroyImmediate(old);
        var go=(GameObject)PrefabUtility.InstantiatePrefab(prefab);
        go.name=n;
        go.transform.SetParent(parent,true);
        float y=terrain.SampleHeight(new Vector3(p.x,0f,p.z))+terrain.transform.position.y;
        go.transform.position=new Vector3(p.x,y,p.z);
        go.transform.rotation=Quaternion.Euler(0f,q.eulerAngles.y,0f);
        go.transform.localScale=Vector3.one;
        KeepOneCactusVariant(go,n.GetHashCode());
        ApplyCactusMaterials(go);
        return go;
''')

s=s.replace('var desertShrub=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Environment/DesertVegetation/desert_shrubs.fbx");','GameObject desertShrub=null; // source pack has no shrub/yucca textures; disabled until supplied')
s=s.replace('var pine=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Art/Environment/Vegetation/Trees/Pines/Pine_004_new/pine_004_4.FBX");','var pine=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Art/Environment/Vegetation/Trees/Pines/Pine_004_new/pine_004_4.prefab");')
s=s.replace('''               n.Contains("treehigh001") || n.Contains("treehigh002") || n.Contains("treehigh003");''','''               n.Contains("treehigh001") || n.Contains("treehigh002") || n.Contains("treehigh003") ||
               p.Contains("desertvegetation/desert_shrubs") || p.Contains("wüstenstrauch") || p.Contains("w�stenstrauch");''')
s=s.replace('''                go.transform.rotation=Quaternion.Euler(0f,(h&1023)*.3519f,0f);
                go.transform.localScale=Vector3.one;
                ApplyCactusMaterials(go);
                changed++;''','''                go.transform.rotation=Quaternion.Euler(0f,(h&1023)*.3519f,0f);
                go.transform.localScale=Vector3.one;
                KeepOneCactusVariant(go,h);
                ApplyCactusMaterials(go);
                changed++;''')

anchor='''        if(trees)
        {
            foreach(var t in trees.Cast<Transform>().ToList())
            {
'''
replacement='''        if(trees)
        {
            var pineA=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Art/Environment/Vegetation/Trees/Pines/Pine_004_new/pine_004_4.prefab");
            var pineB=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Art/Environment/Vegetation/Trees/Pines/Pine_005/Pine_005_01.prefab");
            var pineC=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Art/Environment/Vegetation/Trees/Pines/Pine_006/pine_006_02.prefab");
            var pineD=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Art/Environment/Vegetation/Trees/Pines/Pine_007/pine_007_01.prefab");
            var realTrees=new[]{pineA,pineB,pineC,pineD}.Where(x=>x).ToArray();
            foreach(var t in trees.Cast<Transform>().ToList())
            {
'''
s=s.replace(anchor,replacement)
old='''                var go=t.gameObject;
                byte g=GroupAtWorld(tilemap,groups,t.position.x,t.position.z);
                if(BadVegetationAsset(go))
                {
                    if(g==2) ReplacePlant(go,((changed&1)==0?cactus:desertShrub),trees,terrain);
                    else ReplacePlant(go,pine,trees,terrain);
                    changed++;
                    continue;
                }
                if(g==2)
                {
                    ReplacePlant(go,((changed&1)==0?cactus:desertShrub),trees,terrain);
                    changed++;
                }
                else if(g==1 && SourceAssetPath(go).IndexOf("/Pines/",StringComparison.OrdinalIgnoreCase)<0)
                {
                    ReplacePlant(go,pine,trees,terrain);
                    changed++;
                }
                else if((g==3||g==6) && go.name.StartsWith("GroveTree_",StringComparison.OrdinalIgnoreCase))
                {
                    UnityEngine.Object.DestroyImmediate(go);
                    changed++;
                }
'''
new='''                var go=t.gameObject;
                byte g=GroupAtWorld(tilemap,groups,t.position.x,t.position.z);
                int h=Mathf.RoundToInt(t.position.x*31f+t.position.z*17f) ^ z.key.GetHashCode();
                if(g==2)
                {
                    var repl=ReplacePlant(go,cactus,trees,terrain);
                    if(repl) KeepOneCactusVariant(repl,h);
                    changed++; continue;
                }
                if(g==3||g==6)
                {
                    UnityEngine.Object.DestroyImmediate(go);
                    changed++; continue;
                }
                if(realTrees.Length>0)
                {
                    var repl=ReplacePlant(go,realTrees[Mathf.Abs(h)%realTrees.Length],trees,terrain);
                    if(repl) ScaleToHeight(repl,Mathf.Lerp(4.8f,9.2f,(Mathf.Abs(h>>8)%1000)/999f));
                    changed++;
                }
'''
s=s.replace(old,new)
p.write_text(s,encoding='utf-8')
print('patched postpass vegetation')
