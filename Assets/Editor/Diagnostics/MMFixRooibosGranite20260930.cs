using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;

public static class MMFixRooibosGranite20260930
{
    const string ScenePath="Assets/Scenes/World/Enroth.unity";
    const string WaterMask="Validation/EdgeGrid20260923/WaterQA20260929/CurrentWaterCoverage.png";
    const float X0=-1792f,Z0=-1280f,Step=2f;
    const string GraniteMatPath="Assets/Materials/RealisticWorld/Rocks/Rock_Granite_rcCwC_Standard.mat";
    static bool IsWater(Color32[] px,int w,int h,float x,float z)
    {
        int ix=Mathf.FloorToInt((x-X0)/Step), iz=Mathf.FloorToInt((z-Z0)/Step);
        if(ix<0||iz<0||ix>=w||iz>=h)return true;
        var c=px[iz*w+ix];
        return (c.r==40&&c.g==100&&c.b==135)||(c.r==240&&c.g==75&&c.b==35);
    }
    static GameObject RooibosInstanceRoot(MeshFilter mf)
    {
        Transform t=mf.transform;
        while(t.parent)
        {
            string n=t.name.ToLowerInvariant();
            string pn=t.parent.name.ToLowerInvariant();
            if(n.StartsWith("src3d_")||n.StartsWith("desert_")||n.Contains("rooibos")) {
                if(pn.Contains("source decorations")||pn.Contains("vegetation")||pn.Contains("ecosystem")||pn.Contains("supplement")) return t.gameObject;
            }
            if(pn.Contains("source decorations")||pn.Contains("vegetation")||pn.Contains("ecosystem")||pn.Contains("supplement")||t.parent.GetComponent<Terrain>()) return t.gameObject;
            t=t.parent;
        }
        return mf.gameObject;
    }
    static Material BuildGraniteMaterial()
    {
        Directory.CreateDirectory("Assets/Materials/RealisticWorld/Rocks");
        var m=AssetDatabase.LoadAssetAtPath<Material>(GraniteMatPath);
        if(!m){m=new Material(Shader.Find("Standard")){name="Rock_Granite_rcCwC_Standard"};AssetDatabase.CreateAsset(m,GraniteMatPath);}
        m.shader=Shader.Find("Standard");
        string b="Assets/Art/Environment/_ExternalContent/Quixel/Megascans/Rocks/Rock_Granite_rcCwC/Materials/";
        var albedo=AssetDatabase.LoadAssetAtPath<Texture2D>(b+"Rock_Granite_rcCwC_albedo.tif");
        var normal=AssetDatabase.LoadAssetAtPath<Texture2D>(b+"Rock_Granite_rcCwC_Normal.tif");
        var mask=AssetDatabase.LoadAssetAtPath<Texture2D>(b+"Rock_Granite_rcCwC_MaskMap.tif");
        if(!albedo||!normal||!mask)throw new Exception("Granite texture set incomplete");
        m.SetTexture("_MainTex",albedo);
        m.SetTexture("_BumpMap",normal);m.EnableKeyword("_NORMALMAP");
        m.SetTexture("_MetallicGlossMap",mask);m.EnableKeyword("_METALLICGLOSSMAP");
        m.SetTexture("_OcclusionMap",mask);
        m.SetFloat("_Metallic",0f);
        m.SetFloat("_GlossMapScale",.28f);
        m.SetFloat("_Glossiness",.18f);
        m.SetFloat("_OcclusionStrength",1f);
        m.SetColor("_Color",new Color(.94f,.94f,.94f,1f));
        m.enableInstancing=true;
        EditorUtility.SetDirty(m);
        return m;
    }
    public static void Run()
    {
        var scene=EditorSceneManager.OpenScene(ScenePath,OpenSceneMode.Single);
        if(scene.path!=ScenePath)throw new Exception("Wrong scene");
        if(!File.Exists(WaterMask))throw new Exception("Water mask missing");
        var maskTex=new Texture2D(2,2,TextureFormat.RGBA32,false,true);
        if(!maskTex.LoadImage(File.ReadAllBytes(WaterMask)))throw new Exception("Cannot load water mask");
        var px=maskTex.GetPixels32();int mw=maskTex.width,mh=maskTex.height;UnityEngine.Object.DestroyImmediate(maskTex);

        var rows=new List<string>();
        var rooibos=scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<MeshFilter>(true))
          .Where(f=>f.sharedMesh&&AssetDatabase.GetAssetPath(f.sharedMesh).IndexOf("wild_rooibos_bush",StringComparison.OrdinalIgnoreCase)>=0).ToArray();
        var roots=new HashSet<GameObject>();int wetMeshes=0;
        foreach(var f in rooibos)
        {
            var r=f.GetComponent<Renderer>();Vector3 p=r?r.bounds.center:f.transform.position;
            bool wet=IsWater(px,mw,mh,p.x,p.z);
            if(wet){wetMeshes++;roots.Add(RooibosInstanceRoot(f));}
            rows.Add("rooibos mesh="+f.name+" pos="+p+" wet="+wet+" root="+RooibosInstanceRoot(f).name);
        }
        int removed=0;
        foreach(var go in roots.Where(x=>x).ToArray()){rows.Add("REMOVE "+go.name+" pos="+go.transform.position);UnityEngine.Object.DestroyImmediate(go);removed++;}

        var granite=BuildGraniteMaterial();
        int graniteRenderers=0;
        foreach(var f in scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<MeshFilter>(true)))
        {
            if(!f.sharedMesh||f.sharedMesh.name.IndexOf("Rock_Granite_rcCwC_LOD",StringComparison.OrdinalIgnoreCase)<0)continue;
            var r=f.GetComponent<MeshRenderer>();if(!r)continue;
            var a=r.sharedMaterials;
            if(a==null||a.Length==0)a=new[]{granite};
            else for(int i=0;i<a.Length;i++)a[i]=granite;
            r.sharedMaterials=a;EditorUtility.SetDirty(r);graniteRenderers++;
            rows.Add("GRANITE "+f.sharedMesh.name+" obj="+f.name+" mat="+AssetDatabase.GetAssetPath(granite));
        }

        EditorSceneManager.MarkSceneDirty(scene);
        if(!EditorSceneManager.SaveScene(scene))throw new Exception("Scene save failed");
        AssetDatabase.SaveAssets();
        string head="PASS rooibosMeshes="+rooibos.Length+" wetMeshes="+wetMeshes+" removedRoots="+removed+" graniteRenderersFixed="+graniteRenderers+" terrainWrites=0 heightWrites=0 alphamapWrites=0 terrainLayerWrites=0";
        rows.Insert(0,head);
        File.WriteAllLines("Validation/EdgeGrid20260923/rooibos_granite_fix_20260930.txt",rows);
        Debug.Log(head);
    }
}
