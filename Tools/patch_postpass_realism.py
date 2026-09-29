from pathlib import Path
p=Path(r'C:\MMUnityPort\Assets\Editor\MMSourceGridExactPostPass.cs')
s=p.read_text(encoding='utf-8')
s=s.replace('if(name.StartsWith("swptree")) return CreatePrefabProxy(name,index,parent,"Assets/EnvironmentAssets/Gobkit/TreeHigh003.fbx",6.0f);','if(name.StartsWith("swptree")) return CreatePrefabProxy(name,index,parent,"Assets/Art/Environment/Vegetation/Trees/Pines/Pine_005/Pine_005_01.FBX",6.0f);')
s=s.replace('if(name.StartsWith("snotre")) return CreatePrefabProxy(name,index,parent,"Assets/Environment/PolyHaven/Models/pine_sapling_small/pine_sapling_small_1k.fbx",6.5f);','if(name.StartsWith("snotre")) return CreatePrefabProxy(name,index,parent,"Assets/Art/Environment/Vegetation/Trees/Pines/Pine_004_new/pine_004_4.FBX",6.5f);')
s=s.replace('n.Contains("banyan") || n.Contains("tree_small_02") || n.Contains("treesmall02") || n.Contains("dead_tree_trunk");','n.Contains("banyan") || n.Contains("tree_small_02") || n.Contains("treesmall02") || n.Contains("dead_tree_trunk") ||\n               p.Contains("treehigh001") || p.Contains("treehigh002") || p.Contains("treehigh003") ||\n               n.Contains("treehigh001") || n.Contains("treehigh002") || n.Contains("treehigh003");')
p.write_text(s,encoding='utf-8')
print('basic postpass patched')
marker='    static bool NearArchitecture(Transform arch,float x,float z,float margin)\n'
insert=r'''    static int CactusIndex(string s)
    {
        if(string.IsNullOrEmpty(s)) return 0;
        s=s.ToLowerInvariant();
        for(int i=1;i<=9;i++) if(s.Contains("cactus_"+i)) return i;
        return 0;
    }

    static Material CactusMaterial(int i)
    {
        string dir="Assets/Environment/DesertVegetation/Materials";
        Directory.CreateDirectory(dir);
        string path=$"{dir}/Cactus_{i}.mat";
        var m=AssetDatabase.LoadAssetAtPath<Material>(path);
        var sh=Shader.Find("Standard");
        if(!m){m=new Material(sh);AssetDatabase.CreateAsset(m,path);} else m.shader=sh;
        string td=$"Assets/Environment/DesertVegetation/Textures/cactus_{i}__pbrs2a_diffuse.png";
        string tn=$"Assets/Environment/DesertVegetation/Textures/cactus_{i}__pbrs2a_normal.png";
        var d=AssetDatabase.LoadAssetAtPath<Texture2D>(td); var n=AssetDatabase.LoadAssetAtPath<Texture2D>(tn);
        if(m.HasProperty("_MainTex"))m.SetTexture("_MainTex",d);
        if(n && m.HasProperty("_BumpMap")){m.SetTexture("_BumpMap",n);m.SetFloat("_BumpScale",1f);m.EnableKeyword("_NORMALMAP");}
        if(m.HasProperty("_Glossiness"))m.SetFloat("_Glossiness",.12f);
        EditorUtility.SetDirty(m); return m;
    }

    static void ApplyCactusMaterials(GameObject go)
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

'''
if insert not in s:
    s=s.replace(marker,insert+marker)
s=s.replace('go.transform.localScale=Vector3.one;\n        return go;','go.transform.localScale=Vector3.one;\n        ApplyCactusMaterials(go);\n        return go;',1)
s=s.replace('go.transform.localScale=Vector3.one;\n                changed++;','go.transform.localScale=Vector3.one;\n                ApplyCactusMaterials(go);\n                changed++;')
p.write_text(s,encoding='utf-8')
print('cactus material patch inserted')
