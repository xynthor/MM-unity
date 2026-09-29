using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;

public static class MMDragonIsleTextureRepair
{
    const string GroupName="Dragon Isle - Full Realism";
    const string RockMatPath="Assets/Materials/Source3D/Stone.mat";

    static Transform FindRegion(UnityEngine.SceneManagement.Scene sc,bool linked)
    {
        if(!linked)return sc.GetRootGameObjects().FirstOrDefault(g=>g.GetComponentInChildren<Terrain>(true))?.transform;
        return sc.GetRootGameObjects()
            .SelectMany(g=>g.GetComponentsInChildren<Transform>(true))
            .FirstOrDefault(t=>t.name.StartsWith("Dragon Isle",StringComparison.OrdinalIgnoreCase)&&
                               t.name.IndexOf("LINKED",StringComparison.OrdinalIgnoreCase)>=0);
    }

    static bool HasTexture(Material m)
    {
        if(!m)return false;
        foreach(var n in m.GetTexturePropertyNames())if(m.GetTexture(n))return true;
        return false;
    }

    static int ApplyToScene(string path,bool linked,List<string> rows)
    {
        var sc=EditorSceneManager.OpenScene(path,OpenSceneMode.Single);
        var region=FindRegion(sc,linked);if(!region)throw new Exception("Dragon Isle region missing in "+path);
        var group=region.Find(GroupName);if(!group)throw new Exception("Full realism group missing in "+path);
        var rockMat=AssetDatabase.LoadAssetAtPath<Material>(RockMatPath);
        if(!rockMat||!HasTexture(rockMat))throw new Exception("Textured rock material missing/broken");

        int objects=0,renderers=0;
        foreach(string childName in new[]{"Shoreline Boulder Clusters","Interior Rock Fields"})
        {
            var c=group.Find(childName);if(!c)continue;
            foreach(Transform obj in c)
            {
                objects++;
                foreach(var r in obj.GetComponentsInChildren<Renderer>(true))
                {
                    if(!r.enabled)continue;
                    int n=(r.sharedMaterials==null||r.sharedMaterials.Length==0)?1:r.sharedMaterials.Length;
                    var a=new Material[n];for(int i=0;i<n;i++)a[i]=rockMat;
                    r.sharedMaterials=a;renderers++;
                }
            }
        }

        EditorSceneManager.MarkSceneDirty(sc);
        if(!EditorSceneManager.SaveScene(sc,path))throw new IOException("Save failed "+path);

        int textureless=0,total=0;
        foreach(var r in group.GetComponentsInChildren<Renderer>(true))
        {
            if(!r.enabled||!r.gameObject.activeInHierarchy)continue;
            total++;
            if(r.sharedMaterials==null||r.sharedMaterials.Length==0||r.sharedMaterials.Any(m=>!HasTexture(m)))textureless++;
        }
        rows.Add($"{(linked?"linked":"standalone")},{objects},{renderers},{total},{textureless}");
        return textureless;
    }

    [MenuItem("MMUnity/Recovery/Fix Dragon Isle Missing Textures")]
    public static void Run()
    {
        Directory.CreateDirectory("Validation/CactusDragonAudit");
        var rows=new List<string>{"scope,rock_objects,rock_renderers,total_active_renderers,textureless_active_renderers"};
        int a=ApplyToScene("Assets/Scenes/Regions/DragonIsle.unity",false,rows);
        int b=ApplyToScene("Assets/Scenes/World/Enroth.unity",true,rows);
        File.WriteAllLines("Validation/CactusDragonAudit/dragon_texture_repair.csv",rows);
        AssetDatabase.SaveAssets();
        Debug.Log($"DRAGON_TEXTURE_REPAIR_DONE standaloneBad={a} linkedBad={b}");
    }
}