using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using System;
using System.IO;
using System.Linq;
using System.Globalization;
using System.Collections.Generic;

public static class MMTempSourceDecoration3DTest
{
    const string BlackScene="Assets/Scenes/__TEST_Blackshire_3D.unity";
    const string DragonScene="Assets/Scenes/__TEST_Dragonsand_3D.unity";
    const string Cactus="Assets/Environment/DesertVegetation/Cactus.fbx";
    const string ShrubA="Assets/Environment/PolyHaven/Downloaded/shrub_03/shrub_03_1k.fbx";
    const string ShrubB="Assets/Environment/PolyHaven/Downloaded/wild_rooibos_bush/wild_rooibos_bush_1k.fbx";

    static bool F(string s,out float v)=>float.TryParse(s,NumberStyles.Float,CultureInfo.InvariantCulture,out v);
    static bool Start(string n){n=n.Trim().ToLowerInvariant();return n=="party start"||n.EndsWith(" start");}
    static bool Existing3D(string n){n=n.ToLowerInvariant();return n.StartsWith("6tree")||n.StartsWith("tree")||n.StartsWith("6rock");}

    static Bounds ActiveBounds(GameObject go)
    {
        var rs=go.GetComponentsInChildren<Renderer>(true).Where(r=>r.enabled&&r.gameObject.activeInHierarchy).ToArray();
        if(rs.Length==0)return new Bounds(go.transform.position,Vector3.zero);
        var b=rs[0].bounds;for(int i=1;i<rs.Length;i++)b.Encapsulate(rs[i].bounds);return b;
    }

    static bool ShaderOK(GameObject go)
    {
        foreach(var r in go.GetComponentsInChildren<Renderer>(true))
        foreach(var m in r.sharedMaterials)
            if(m&&(!m.shader||m.shader.name=="Hidden/InternalErrorShader"))return false;
        return true;
    }

    static string Dominant(Terrain t,float x,float z)
    {
        var td=t.terrainData;Vector3 l=new Vector3(x,0,z)-t.transform.position;
        int ax=Mathf.Clamp(Mathf.FloorToInt(l.x/td.size.x*td.alphamapWidth),0,td.alphamapWidth-1);
        int ay=Mathf.Clamp(Mathf.FloorToInt(l.z/td.size.z*td.alphamapHeight),0,td.alphamapHeight-1);
        var a=td.GetAlphamaps(ax,ay,1,1);int best=0;float v=a[0,0,0];
        for(int k=1;k<td.alphamapLayers;k++)if(a[0,0,k]>v){v=a[0,0,k];best=k;}
        return td.terrainLayers[best]?td.terrainLayers[best].name:"NULL";
    }

    static void Ground(GameObject go,Terrain t,float x,float z,float sourceY)
    {
        float gy=t.SampleHeight(new Vector3(x,0,z))+t.transform.position.y;
        float target=Mathf.Max(gy,sourceY);
        var b=ActiveBounds(go);
        go.transform.position+=Vector3.up*(target-b.min.y);
    }
    static GameObject MakeCactus(Transform parent,int idx)
    {
        var src=AssetDatabase.LoadAssetAtPath<GameObject>(Cactus);if(!src)return null;
        var go=(GameObject)PrefabUtility.InstantiatePrefab(src);
        if(!go)go=UnityEngine.Object.Instantiate(src);
        go.transform.SetParent(parent,false);
        var vars=go.transform.Cast<Transform>().Where(t=>t.name.StartsWith("cactus_",StringComparison.OrdinalIgnoreCase)).OrderBy(t=>t.name).ToArray();
        if(vars.Length==0){UnityEngine.Object.DestroyImmediate(go);return null;}
        int keep=Math.Abs(idx)%vars.Length;
        for(int i=0;i<vars.Length;i++)
        {
            vars[i].gameObject.SetActive(i==keep);
            if(i==keep)vars[i].localPosition=Vector3.zero;
        }
        return go;
    }

    static GameObject MakeAsset(string path,Transform parent)
    {
        var src=AssetDatabase.LoadAssetAtPath<GameObject>(path);if(!src)return null;
        var go=(GameObject)PrefabUtility.InstantiatePrefab(src);
        if(!go)go=UnityEngine.Object.Instantiate(src);
        go.transform.SetParent(parent,false);return go;
    }

    static bool SizeOK(string family,Bounds b)
    {
        float xz=Mathf.Max(b.size.x,b.size.z);
        if(family=="CACTUS")return b.size.y>=1.5f&&b.size.y<=4.5f&&xz<=3.0f;
        if(family=="SHRUB_A")return b.size.y<=0.75f&&xz<=1.7f;
        if(family=="SHRUB_B")return b.size.y<=0.85f&&xz<=2.7f;
        return false;
    }

    static void Run(string zone,string scenePath,string tag)
    {
        Directory.CreateDirectory("Validation/SourceDecoration3D/Test");
        var sc=EditorSceneManager.OpenScene(scenePath,OpenSceneMode.Single);
        var root=sc.GetRootGameObjects().FirstOrDefault(g=>g.GetComponentInChildren<Terrain>(true));
        if(!root)throw new Exception("Root missing "+zone);
        var terrain=root.GetComponentInChildren<Terrain>(true);if(!terrain)throw new Exception("Terrain missing "+zone);

        var old=root.transform.Find("Source Sprites - OneToOne");
        int oldBillboards=old?old.GetComponentsInChildren<MMBillboardToCamera>(true).Length:0;
        if(old)UnityEngine.Object.DestroyImmediate(old.gameObject);
        var old3=root.transform.Find("Source Decorations 3D - TEST");
        if(old3)UnityEngine.Object.DestroyImmediate(old3.gameObject);
        var group=new GameObject("Source Decorations 3D - TEST");group.transform.SetParent(root.transform,false);

        int mapped=0,omitted=0,rejected=0;
        var rows=new List<string>{"zone,index,name,family,status,terrain,x,z,size_x,size_y,size_z"};
        string[] lines=File.ReadAllLines($"Assets/World/{zone}/Data/decorations.csv");
        for(int i=1;i<lines.Length;i++)
        {
            var q=lines[i].Split(',');if(q.Length<10)continue;
            string n=q[1].Trim().ToLowerInvariant();
            if(string.IsNullOrEmpty(n)||Start(n)||Existing3D(n)||n.StartsWith("snd_"))continue;
            if(!F(q[4],out float x)||!F(q[5],out float sy)||!F(q[6],out float oz))continue;
            F(q[7],out float yaw);float z=-oz;int idx=i-1;int.TryParse(q[0],out idx);
            string dom=Dominant(terrain,x,z);
            GameObject go=null;string family="UNMAPPED";

            if(n.StartsWith("cac"))
            {
                family="CACTUS";
                if(dom.IndexOf("Desert",StringComparison.OrdinalIgnoreCase)>=0)go=MakeCactus(group.transform,idx);
            }
            else if(n.StartsWith("searka")){family="SHRUB_A";go=MakeAsset(ShrubA,group.transform);}
            else if(n.StartsWith("searkb")){family="SHRUB_B";go=MakeAsset(ShrubB,group.transform);}

            if(!go){omitted++;rows.Add($"{zone},{idx},{n},{family},OMITTED,{dom},{x:F3},{z:F3},,,");continue;}

            go.name=$"SRC3D_TEST_{idx:0000}_{n}_{family}";
            go.transform.position=new Vector3(x,0,z);
            go.transform.rotation=Quaternion.Euler(0f,-yaw,0f);
            Ground(go,terrain,x,z,sy);
            var b=ActiveBounds(go);

            if(!ShaderOK(go)||!SizeOK(family,b))
            {
                rejected++;rows.Add($"{zone},{idx},{n},{family},REJECT,{dom},{x:F3},{z:F3},{b.size.x:F3},{b.size.y:F3},{b.size.z:F3}");
                UnityEngine.Object.DestroyImmediate(go);continue;
            }

            mapped++;rows.Add($"{zone},{idx},{n},{family},MAPPED,{dom},{x:F3},{z:F3},{b.size.x:F3},{b.size.y:F3},{b.size.z:F3}");
        }

        EditorSceneManager.MarkSceneDirty(sc);
        if(!EditorSceneManager.SaveScene(sc,scenePath))throw new IOException("Save failed "+scenePath);
        File.WriteAllLines($"Validation/SourceDecoration3D/Test/{tag}_objects.csv",rows);
        File.WriteAllText($"Validation/SourceDecoration3D/Test/{tag}_summary.txt",
            $"zone={zone}\nmapped={mapped}\nomitted={omitted}\nrejected={rejected}\nremoved_billboards={oldBillboards}\nremaining_billboards={root.GetComponentsInChildren<MMBillboardToCamera>(true).Length}\n");
        AssetDatabase.SaveAssets();
    }

    [MenuItem("MMUnity/Source Decorations 3D TEST/Blackshire")] public static void Black()=>Run("Blackshire",BlackScene,"blackshire");
    [MenuItem("MMUnity/Source Decorations 3D TEST/Dragonsand")] public static void Dragon()=>Run("Dragonsand",DragonScene,"dragonsand");
}
