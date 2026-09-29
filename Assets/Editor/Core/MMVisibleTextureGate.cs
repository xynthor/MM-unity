using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;

public static class MMVisibleTextureGate
{
    sealed class Z { public string key,scene; public Z(string k,string s){key=k;scene=s;} }
    static readonly Z[] Zones={
        new Z("NewSorpigal","Assets/Scenes/Regions/NewSorpigal.unity"),
        new Z("CastleIronfist","Assets/Scenes/Regions/CastleIronfist.unity"),
        new Z("MireOfTheDamned","Assets/Scenes/Regions/MireOfTheDamned.unity"),
        new Z("Dragonsand","Assets/Scenes/Regions/Dragonsand.unity"),
        new Z("HermitsIsle","Assets/Scenes/Regions/HermitsIsle.unity"),
        new Z("MistyIslands","Assets/Scenes/Regions/MistyIslands.unity"),
        new Z("BootlegBay","Assets/Scenes/Regions/BootlegBay.unity"),
        new Z("FreeHaven","Assets/Scenes/Regions/FreeHaven.unity"),
        new Z("Blackshire","Assets/Scenes/Regions/Blackshire.unity"),
        new Z("ParadiseValley","Assets/Scenes/Regions/ParadiseValley.unity"),
        new Z("EelInfestedWaters","Assets/Scenes/Regions/EelInfestedWaters.unity"),
        new Z("SilverCove","Assets/Scenes/Regions/SilverCove.unity"),
        new Z("FrozenHighlands","Assets/Scenes/Regions/FrozenHighlands.unity"),
        new Z("Kriegspire","Assets/Scenes/Regions/Kriegspire.unity"),
        new Z("SweetWater","Assets/Scenes/Regions/SweetWater.unity")};

    static readonly string[] ManagedRoots={
        "Architecture - MM6 Original Layout Expanded",
        "Vegetation - Source Anchored Final",
        "Vegetation - Preserved User Additions",
        "Biome Vegetation - Source Grid",
        "Vegetation - MM Anchors + Natural Groves",
        "Source Objects - OneToOne"
    };

    [MenuItem("MMUnity/Hard Gate - No Visible Missing Textures")]
    public static void ApplyAll()
    {
        Directory.CreateDirectory("Validation");
        var log=new List<string>{"zone,action,object,detail"};
        int fixedN=0,removed=0,fail=0;
        foreach(var z in Zones) Apply(z,log,ref fixedN,ref removed,ref fail);
        File.WriteAllLines("Validation/VisibleTextureGate.csv",log);
        AssetDatabase.SaveAssets();
        if(fail>0) throw new Exception($"VISIBLE_TEXTURE_GATE_FAILED remaining={fail}");
        Debug.Log($"VISIBLE_TEXTURE_GATE_PASS fixed={fixedN} removed={removed} remaining=0");
    }

    static bool HasTexture(Material m)
    {
        if(!m)return false;
        foreach(string p in m.GetTexturePropertyNames()) if(m.GetTexture(p)) return true;
        return false;
    }

    static string AssetPath(GameObject go)
    {
        var src=PrefabUtility.GetCorrespondingObjectFromSource(go) as GameObject;
        return src?AssetDatabase.GetAssetPath(src):"";
    }

    static bool IsVegetationRoot(Transform r)
    {
        string n=r.name.ToLowerInvariant();
        return n.Contains("vegetation")||n.Contains("biome");
    }

    static Material Wood()
    {
        string path="Assets/Materials/Terrain/Gate_CC0_Wood.mat";
        var m=AssetDatabase.LoadAssetAtPath<Material>(path);
        if(!m){m=new Material(Shader.Find("Standard"));AssetDatabase.CreateAsset(m,path);}
        m.SetTexture("_MainTex",AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Environment/PolyHaven/Textures/wood_planks/wood_planks_diff_1k.jpg"));
        var n=AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Environment/PolyHaven/Textures/wood_planks/wood_planks_nor_gl_1k.jpg");
        if(n){m.SetTexture("_BumpMap",n);m.EnableKeyword("_NORMALMAP");}
        m.SetFloat("_Glossiness",.05f);EditorUtility.SetDirty(m);return m;
    }

    static Material PlayerFallback(string role)
    {
        string safe=new string(role.Where(char.IsLetterOrDigit).ToArray());
        string path=$"Assets/Materials/Terrain/CC0_Player_{safe}.mat";
        var m=AssetDatabase.LoadAssetAtPath<Material>(path);
        if(!m){m=new Material(Shader.Find("Standard"));AssetDatabase.CreateAsset(m,path);}
        bool metal=role=="Metal";
        string dir=metal?"Assets/Environment/PolyHaven/Textures/metal_plate/":"Assets/Environment/PolyHaven/Textures/fabric_pattern_05/";
        string diff=metal?dir+"metal_plate_diff_1k.jpg":dir+"fabric_pattern_05_col_01_1k.jpg";
        string nor=metal?dir+"metal_plate_nor_gl_1k.jpg":dir+"fabric_pattern_05_nor_gl_1k.jpg";
        m.SetTexture("_MainTex",AssetDatabase.LoadAssetAtPath<Texture2D>(diff));
        var n=AssetDatabase.LoadAssetAtPath<Texture2D>(nor);if(n){m.SetTexture("_BumpMap",n);m.EnableKeyword("_NORMALMAP");}
        if(role=="Hair")m.color=new Color(.10f,.07f,.045f);
        else if(role=="Skin")m.color=new Color(.78f,.55f,.42f);
        else if(role=="ClothDark")m.color=new Color(.12f,.16f,.19f);
        else if(role=="Cloth")m.color=new Color(.30f,.34f,.37f);
        else m.color=Color.white;
        if(m.HasProperty("_Glossiness"))m.SetFloat("_Glossiness",metal?.22f:.05f);
        EditorUtility.SetDirty(m);return m;
    }

    static int FixVisiblePlayerTextures(Transform root)
    {
        int n=0;
        foreach(var r in root.GetComponentsInChildren<Renderer>(true))
        {
            if(!r||!r.enabled||!r.gameObject.activeInHierarchy)continue;
            string ap=AssetPath(r.gameObject).ToLowerInvariant();if(!ap.Contains("/player/"))continue;
            if(r.sharedMaterials.Length>0&&!r.sharedMaterials.Any(m=>!HasTexture(m)))continue;
            string rn=r.gameObject.name.ToLowerInvariant();
            string role=(rn.Contains("sword")||rn.Contains("weapon"))?"Metal":
                        (rn.Contains("hair")||rn.Contains("eyelash"))?"Hair":
                        (rn.Contains("face")||rn.Contains("body"))?"Skin":
                        (rn.Contains("shirt")||rn.Contains("short")||rn.Contains("sneaker"))?"ClothDark":"Cloth";
            var mat=PlayerFallback(role);var ms=r.sharedMaterials;if(ms==null||ms.Length==0)ms=new Material[1];
            for(int i=0;i<ms.Length;i++)ms[i]=mat;r.sharedMaterials=ms;n++;
        }
        return n;
    }

    static bool TryFixCactus(Renderer r)
    {
        string n=r.gameObject.name.ToLowerInvariant();
        if(!n.StartsWith("cactus_"))return false;
        string digits=new string(n.Where(char.IsDigit).ToArray());
        if(!int.TryParse(digits,out int id))return false;
        id=Mathf.Clamp(id,1,9);
        var m=AssetDatabase.LoadAssetAtPath<Material>($"Assets/Environment/DesertVegetation/Materials/Cactus_{id}.mat");
        if(!m||!HasTexture(m))return false;
        r.sharedMaterial=m;return true;
    }

    static Transform PlacedVegetationRoot(Transform managed,Renderer r)
    {
        Transform t=r.transform,last=t;
        while(t&&t!=managed){last=t;t=t.parent;}
        return t==managed?last:null;
    }

    static void Apply(Z z,List<string> log,ref int fixedN,ref int removed,ref int fail)
    {
        var sc=EditorSceneManager.OpenScene(z.scene,OpenSceneMode.Single);
        var root=sc.GetRootGameObjects().FirstOrDefault(g=>g.name.IndexOf("Open World",StringComparison.OrdinalIgnoreCase)>=0)??sc.GetRootGameObjects().First();
        var doomed=new HashSet<GameObject>();
        foreach(string rn in ManagedRoots)
        {
            var mr=root.transform.Find(rn);if(!mr)continue;
            foreach(var r in mr.GetComponentsInChildren<Renderer>(true))
            {
                if(!r||!r.enabled||!r.gameObject.activeInHierarchy)continue;
                bool bad=r.sharedMaterials.Length==0||r.sharedMaterials.Any(m=>!HasTexture(m));
                if(!bad)continue;
                if(TryFixCactus(r)){fixedN++;log.Add($"{z.key},FIX_CACTUS,{r.name},{AssetPath(r.gameObject)}");continue;}
                string ap=AssetPath(r.gameObject).ToLowerInvariant();
                if(rn.StartsWith("Architecture")&&z.key=="Blackshire"&&ap.Contains("bench"))
                {var wm=Wood();var ms=r.sharedMaterials;if(ms.Length==0)ms=new Material[1];for(int mi=0;mi<ms.Length;mi++)ms[mi]=wm;r.sharedMaterials=ms;fixedN++;log.Add($"{z.key},FIX_WOOD,{r.name},{ap}");continue;}
                if(rn.StartsWith("Architecture")&&z.key=="MistyIslands"&&ap.Contains("pond"))
                {r.enabled=false;removed++;log.Add($"{z.key},HIDE_REPLACED_WATER,{r.name},{ap}");continue;}
                if(IsVegetationRoot(mr))
                {
                    var top=PlacedVegetationRoot(mr,r);
                    if(top){doomed.Add(top.gameObject);log.Add($"{z.key},REMOVE_BAD_VEGETATION,{top.name},{ap}");continue;}
                }
                r.enabled=false;removed++;log.Add($"{z.key},DISABLE_UNTEXTURED,{r.name},{ap}");
            }
        }
        foreach(var go in doomed)if(go){UnityEngine.Object.DestroyImmediate(go);removed++;}
        int playerFixed=FixVisiblePlayerTextures(root.transform);fixedN+=playerFixed;
        if(playerFixed>0)log.Add($"{z.key},FIX_PLAYER_TEXTURES,Player,{playerFixed}");
        EditorSceneManager.MarkSceneDirty(sc);EditorSceneManager.SaveScene(sc,z.scene);
        fail+=CountRemaining(root.transform);
    }

    static int CountRemaining(Transform root)
    {
        int bad=0;
        foreach(string rn in ManagedRoots)
        {
            var mr=root.Find(rn);if(!mr)continue;
            foreach(var r in mr.GetComponentsInChildren<Renderer>(true))
                if(r&&r.enabled&&r.gameObject.activeInHierarchy&&(r.sharedMaterials.Length==0||r.sharedMaterials.Any(m=>!HasTexture(m))))bad++;
        }
        return bad;
    }
}
