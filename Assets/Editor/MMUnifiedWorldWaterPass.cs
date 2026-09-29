using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;

public static class MMUnifiedWorldWaterPass
{
    sealed class Z { public string key,scene; public Z(string k,string s){key=k;scene=s;} }
    static readonly Z[] Zones={
        new Z("NewSorpigal","Assets/Scenes/NewSorpigal_OpenWorld.unity"),
        new Z("CastleIronfist","Assets/Scenes/CastleIronfist_SourceGrid.unity"),
        new Z("MireOfTheDamned","Assets/Scenes/MireOfTheDamned_SourceGrid.unity"),
        new Z("Dragonsand","Assets/Scenes/Dragonsand_SourceGrid.unity"),
        new Z("HermitsIsle","Assets/Scenes/HermitsIsle_SourceGrid.unity"),
        new Z("MistyIslands","Assets/Scenes/MistyIslands_SourceGrid.unity"),
        new Z("BootlegBay","Assets/Scenes/BootlegBay_SourceGrid.unity"),
        new Z("FreeHaven","Assets/Scenes/FreeHaven_SourceGrid.unity"),
        new Z("Blackshire","Assets/Scenes/Blackshire_SourceGrid.unity"),
        new Z("ParadiseValley","Assets/Scenes/ParadiseValley_SourceGrid.unity"),        new Z("EelInfestedWaters","Assets/Scenes/EelInfestedWaters_SourceGrid.unity"),
        new Z("SilverCove","Assets/Scenes/SilverCove_SourceGrid.unity"),
        new Z("FrozenHighlands","Assets/Scenes/FrozenHighlands_SourceGrid.unity"),
        new Z("Kriegspire","Assets/Scenes/Kriegspire_SourceGrid.unity"),
        new Z("SweetWater","Assets/Scenes/SweetWater_SourceGrid.unity")};

    public static Material SharedWaterMaterial()
    {
        string path="Assets/Materials/SourceGridTerrain/UnifiedEnrothWater.mat";
        var m=AssetDatabase.LoadAssetAtPath<Material>(path);
        var sh=Shader.Find("MMUnity/EnrothMasterWater") ?? Shader.Find("Standard");
        if(!m){m=new Material(sh);AssetDatabase.CreateAsset(m,path);} else m.shader=sh;
        SetColor(m,"_ShallowColor",new Color(.105f,.29f,.28f,.60f));
        SetColor(m,"_MidColor",new Color(.028f,.13f,.17f,.82f));
        SetColor(m,"_DeepColor",new Color(.006f,.028f,.06f,.98f));
        SetColor(m,"_FoamColor",new Color(.73f,.82f,.76f,.90f));
        SetColor(m,"_ReflectionColor",new Color(.22f,.31f,.35f,1f));
        SetFloat(m,"_DepthRange",18f); SetFloat(m,"_FoamDepth",.80f);
        SetFloat(m,"_WaveAmp",.055f); SetFloat(m,"_WaveAmp2",.025f);
        SetFloat(m,"_WaveScale",.065f); SetFloat(m,"_WaveScale2",.115f);
        if(m.HasProperty("_WaveSpeed"))m.SetVector("_WaveSpeed",new Vector4(.42f,.27f,0,0));        SetFloat(m,"_NormalStrength",.55f);SetFloat(m,"_Distortion",.010f);
        SetFloat(m,"_FresnelPower",4.2f);SetFloat(m,"_ReflectionStrength",.52f);
        SetFloat(m,"_SpecularStrength",.42f);
        if(m.HasProperty("_NormalTex"))
        {
            var n=AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/TerrainDemoScene_HDRP/Prefabs/Water/Textures/Water_Normal.png");
            if(n)m.SetTexture("_NormalTex",n);
        }
        m.renderQueue=3000;EditorUtility.SetDirty(m);return m;
    }

    static void SetFloat(Material m,string n,float v){if(m.HasProperty(n))m.SetFloat(n,v);}
    static void SetColor(Material m,string n,Color v){if(m.HasProperty(n))m.SetColor(n,v);}

    [MenuItem("MMUnity/Witcher World - Unified Water")]
    public static void ApplyAll()
    {
        var mat=SharedWaterMaterial();int renderers=0;
        foreach(var z in Zones)renderers+=ApplyZone(z,mat);
        renderers+=ApplyLinked(mat);
        AssetDatabase.SaveAssets();
        Debug.Log("UNIFIED_WORLD_WATER_DONE renderers="+renderers+" sameMaterial=true depthDriven=true");
    }

    static GameObject WorldRoot(UnityEngine.SceneManagement.Scene sc)
    {
        return sc.GetRootGameObjects().FirstOrDefault(g=>g.name.IndexOf("Open World",StringComparison.OrdinalIgnoreCase)>=0)
            ??sc.GetRootGameObjects().FirstOrDefault(g=>g.name.StartsWith("ENROTH - LINKED",StringComparison.OrdinalIgnoreCase))
            ??sc.GetRootGameObjects().FirstOrDefault();
    }    static bool WaterRenderer(Renderer r,Transform root)
    {
        if(!r||!r.enabled)return false;
        Transform t=r.transform;
        while(t&&t!=root.parent)
        {
            string n=t.name.ToLowerInvariant();
            if(n.Contains("seabed")||n.Contains("bathymetric")||n.Contains("drybeach")||
               n.Contains("wetbeach")||n.Contains("rockyshore"))return false;
            if(n=="water"||n.Contains("internal water")||n.StartsWith("curved ocean")||
               n.StartsWith("outer ocean")||n=="global ocean surface"||n.Contains("water surface"))return true;
            if(t==root)break;t=t.parent;
        }
        return false;
    }

    static int ApplyZone(Z z,Material mat)
    {
        var sc=EditorSceneManager.OpenScene(z.scene,OpenSceneMode.Single);var root=WorldRoot(sc);if(!root)return 0;
        int n=0;
        foreach(var r in root.GetComponentsInChildren<Renderer>(true))
        {
            if(!WaterRenderer(r,root.transform))continue;
            var a=r.sharedMaterials;if(a==null||a.Length==0)a=new Material[1];
            for(int i=0;i<a.Length;i++)a[i]=mat;r.sharedMaterials=a;
            r.shadowCastingMode=ShadowCastingMode.Off;r.receiveShadows=false;n++;
        }
        EnsureDepth(sc);EditorSceneManager.MarkSceneDirty(sc);EditorSceneManager.SaveScene(sc,z.scene);
        Debug.Log($"UNIFIED_WATER {z.key} renderers={n}");return n;
    }

    static int ApplyLinked(Material mat)
    {
        const string path="Assets/Scenes/Enroth_Linked_OpenWorld.unity";
        if(!File.Exists(path))return 0;
        var sc=EditorSceneManager.OpenScene(path,OpenSceneMode.Single);var root=WorldRoot(sc);if(!root)return 0;int n=0;        foreach(var r in root.GetComponentsInChildren<Renderer>(true))
        {
            if(!WaterRenderer(r,root.transform))continue;
            var a=r.sharedMaterials;if(a==null||a.Length==0)a=new Material[1];
            for(int i=0;i<a.Length;i++)a[i]=mat;r.sharedMaterials=a;
            r.shadowCastingMode=ShadowCastingMode.Off;r.receiveShadows=false;n++;
        }
        EnsureDepth(sc);EditorSceneManager.MarkSceneDirty(sc);EditorSceneManager.SaveScene(sc,path);
        return n;
    }

    static void EnsureDepth(UnityEngine.SceneManagement.Scene sc)
    {
        foreach(var g in sc.GetRootGameObjects())
            foreach(var c in g.GetComponentsInChildren<Camera>(true))
                c.depthTextureMode|=DepthTextureMode.Depth;
    }
}