using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;

public static class MMTempDragonTextureAudit
{
    static string Tex(Material m)
    {
        if(!m)return "NULL_MATERIAL";
        var names=m.GetTexturePropertyNames();
        var set=new List<string>();
        foreach(var n in names)
        {
            var t=m.GetTexture(n);
            if(t)set.Add(n+"="+AssetDatabase.GetAssetPath(t));
        }
        return set.Count==0?"NO_TEXTURES":string.Join(";",set);
    }

    [MenuItem("MMUnity/Validation/Temp Dragon Texture Audit")]
    public static void Run()
    {
        Directory.CreateDirectory("Validation/CactusDragonAudit");
        var sc=EditorSceneManager.OpenScene("Assets/Scenes/DragonIsle_Reference.unity",OpenSceneMode.Single);
        var root=sc.GetRootGameObjects().FirstOrDefault(g=>g.GetComponentInChildren<Terrain>(true));
        var t=root.GetComponentInChildren<Terrain>(true);
        var tl=new List<string>{"index,name,asset,diffuse,normal,mask"};
        for(int i=0;i<t.terrainData.terrainLayers.Length;i++)
        {
            var l=t.terrainData.terrainLayers[i];
            tl.Add(string.Join(",",new[]{
                i.ToString(),
                l?l.name:"NULL",
                l?AssetDatabase.GetAssetPath(l):"",
                l&&l.diffuseTexture?AssetDatabase.GetAssetPath(l.diffuseTexture):"MISSING",
                l&&l.normalMapTexture?AssetDatabase.GetAssetPath(l.normalMapTexture):"MISSING",
                l&&l.maskMapTexture?AssetDatabase.GetAssetPath(l.maskMapTexture):"MISSING"
            }));
        }
        File.WriteAllLines("Validation/CactusDragonAudit/dragon_terrain_layers_textures.csv",tl);

        var rows=new List<string>{"scope,object,material,shader,textures"};
        foreach(var r in root.GetComponentsInChildren<Renderer>(true))
        {
            if(!r.enabled||!r.gameObject.activeInHierarchy)continue;
            string scope="OTHER";
            for(var p=r.transform;p&&p!=root.transform;p=p.parent)
                if(p.name=="Dragon Isle - Full Realism"||p.name=="Forest - Natural Sparse"||p.name=="Landmarks - Reference"){scope=p.name;break;}
            foreach(var m in r.sharedMaterials)
                rows.Add(string.Join(",",new[]{
                    scope,
                    r.gameObject.name.Replace(',',';'),
                    m?m.name.Replace(',',';'):"NULL",
                    m&&m.shader?m.shader.name.Replace(',',';'):"NULL",
                    "\""+Tex(m).Replace("\"","'")+"\""
                }));
        }
        File.WriteAllLines("Validation/CactusDragonAudit/dragon_renderer_textures.csv",rows);
        Debug.Log("DRAGON_TEXTURE_AUDIT_DONE renderers="+root.GetComponentsInChildren<Renderer>(true).Length);
    }
}