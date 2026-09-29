from pathlib import Path
p=Path(r'C:\MMUnityPort\Assets\Editor\MMSourceGridExactPostPass.cs')
s=p.read_text(encoding='utf-8')
s=s.replace('int biomeCount=ApplyBiomeVegetation(z,root.transform,terrain,tilemap,groups);',
'''int biomeCount=ApplyBiomeVegetation(z,root.transform,terrain,tilemap,groups);
        int treeMats=ApplyRealisticTreeMaterials(root.transform);''')
s=s.replace('biomePlants={biomeCount}");','biomePlants={biomeCount} treeMaterials={treeMats}");')
marker='    static Material SurfaceMaterial(string key,string texturePath,bool water=false)'
insert=r'''    static Material RealTreeMaterial(bool foliage)
    {
        string key=foliage?"RealPineFoliage":"RealPineBark";
        string path=$"{LayerFolder}/{key}.mat";
        var m=AssetDatabase.LoadAssetAtPath<Material>(path);
        var sh=Shader.Find("Standard");
        if(!m){m=new Material(sh);AssetDatabase.CreateAsset(m,path);} else m.shader=sh;
        string baseDir="Assets/Environment/PolyHaven/Models/pine_sapling_small/";
        string d=baseDir+(foliage?"pine_sapling_small_twig_diff_1k.png":"pine_sapling_small_bark_diff_1k.png");
        string n=baseDir+(foliage?"pine_sapling_small_twig_nor_gl_1k.png":"pine_sapling_small_bark_nor_gl_1k.png");
        m.SetTexture("_MainTex",AssetDatabase.LoadAssetAtPath<Texture2D>(d));
        m.SetTexture("_BumpMap",AssetDatabase.LoadAssetAtPath<Texture2D>(n)); m.EnableKeyword("_NORMALMAP");
        m.SetFloat("_Glossiness",foliage?.08f:.12f);
        if(foliage){m.SetFloat("_Mode",1f);m.SetFloat("_Cutoff",.35f);m.EnableKeyword("_ALPHATEST_ON");m.renderQueue=2450;}
        EditorUtility.SetDirty(m);return m;
    }
'''
insert+=r'''    static int ApplyRealisticTreeMaterials(Transform root)
    {
        var bark=RealTreeMaterial(false);var foliage=RealTreeMaterial(true);int changed=0;
        foreach(var r in root.GetComponentsInChildren<Renderer>(true))
        {
            string ap=SourceAssetPath(r.gameObject).ToLowerInvariant();
            if(!ap.Contains("/vegetation/trees/"))continue;
            var a=r.sharedMaterials;bool dirty=false;
            for(int i=0;i<a.Length;i++)
            {
                var m=a[i];if(!m||m.mainTexture)continue;
                string n=m.name.ToLowerInvariant();
                bool leaf=n.Contains("leaf")||n.Contains("twig")||n.Contains("needle")||n.Contains("billboard")||n.Contains("branch");
                bool wood=n.Contains("bark")||n.Contains("trunk")||n.Contains("wood");
                if(leaf){a[i]=foliage;dirty=true;changed++;}
                else if(wood){a[i]=bark;dirty=true;changed++;}
            }
            if(dirty)r.sharedMaterials=a;
        }
        return changed;
    }

'''
if marker not in s: raise SystemExit('marker missing')
s=s.replace(marker,insert+marker)
p.write_text(s,encoding='utf-8')
print('patched realistic tree materials')
