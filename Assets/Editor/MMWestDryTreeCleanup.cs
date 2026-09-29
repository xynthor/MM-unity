using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Globalization;

public static class MMWestDryTreeCleanup
{
    const string Paradise="Assets/Scenes/ParadiseValley_SourceGrid.unity";
    const string Bootleg="Assets/Scenes/BootlegBay_SourceGrid.unity";
    const string Linked="Assets/Scenes/Enroth_Linked_OpenWorld.unity";
    const string Report="Validation/EnvironmentRealism/paradise_dead_tree_cleanup.csv";
    const string BootlegReport="Validation/EnvironmentRealism/bootleg_dead_tree_cleanup.csv";

    [MenuItem("MMUnity/Environment/Cleanup Paradise Old Dead Trees")]
    public static void Run()
    {
        Directory.CreateDirectory("Validation/EnvironmentRealism");
        var rows=new List<string>{"scope,name,old_asset,new_asset,x_before,z_before,x_after,z_after,gap_after,status"};
        int a=0,b=0;

        var sc=EditorSceneManager.OpenScene(Paradise,OpenSceneMode.Single);
        var root=sc.GetRootGameObjects().FirstOrDefault(g=>g.GetComponentInChildren<Terrain>(true));
        var terrain=root?root.GetComponentInChildren<Terrain>(true):null;
        if(!root||!terrain)throw new Exception("Paradise root/terrain missing");
        a=ReplaceIn(root.transform,terrain,"standalone",rows);
        if(sc.isDirty)EditorSceneManager.SaveScene(sc);

        var lsc=EditorSceneManager.OpenScene(Linked,OpenSceneMode.Single);
        var world=lsc.GetRootGameObjects().FirstOrDefault();
        var rr=Find(world.transform,"Paradise Valley - LINKED REFERENCE");
        var lt=rr?rr.GetComponentInChildren<Terrain>(true):null;
        if(!rr||!lt)throw new Exception("Linked Paradise root/terrain missing");
        b=ReplaceIn(rr,lt,"linked",rows);
        if(lsc.isDirty)EditorSceneManager.SaveScene(lsc);

        File.WriteAllLines(Report,rows);
        AssetDatabase.SaveAssets();
        Debug.Log($"MM_PARADISE_DEAD_TREE_CLEANUP standalone={a} linked={b}");
    }

    [MenuItem("MMUnity/Environment/Cleanup Bootleg Old Dead Trees")]
    public static void RunBootleg()
    {
        Directory.CreateDirectory("Validation/EnvironmentRealism");
        var rows=new List<string>{"scope,name,old_asset,new_asset,x_before,z_before,x_after,z_after,gap_after,status"};
        var sc=EditorSceneManager.OpenScene(Bootleg,OpenSceneMode.Single);
        var root=sc.GetRootGameObjects().FirstOrDefault(g=>g.GetComponentInChildren<Terrain>(true));
        var terrain=root?root.GetComponentInChildren<Terrain>(true):null;
        if(!root||!terrain)throw new Exception("Bootleg root/terrain missing");
        int n=ReplaceIn(root.transform,terrain,"standalone",rows);
        if(sc.isDirty)EditorSceneManager.SaveScene(sc);
        File.WriteAllLines(BootlegReport,rows);
        AssetDatabase.SaveAssets();
        Debug.Log($"MM_BOOTLEG_DEAD_TREE_CLEANUP standalone={n}");
    }

    [MenuItem("MMUnity/Validation/Verify Blackshire Linked Terrain")]
    public static void VerifyBlackshireLinked()
    {
        var sc=EditorSceneManager.OpenScene(Linked,OpenSceneMode.Single);
        var world=sc.GetRootGameObjects().FirstOrDefault();
        var rr=Find(world.transform,"Blackshire - LINKED REFERENCE");
        var t=rr?rr.GetComponentInChildren<Terrain>(true):null;
        if(!t)throw new Exception("Linked Blackshire terrain missing");
        string p=AssetDatabase.GetAssetPath(t.terrainData);
        int hard=HardCount(t.terrainData.GetAlphamaps(0,0,t.terrainData.alphamapWidth,t.terrainData.alphamapHeight));
        Debug.Log("MM_BLACK_LINKED_TERRAIN asset="+p+" hard="+hard+" layers="+t.terrainData.alphamapLayers);
    }

    static int ReplaceIn(Transform root,Terrain terrain,string scope,List<string> rows)
    {
        var c=root.Find("Realistic Ecosystem Supplementary");
        if(!c)return 0;int n=0;
        foreach(var w in c.Cast<Transform>().ToList())
        {
            string old=OldDeadAsset(w.gameObject);if(string.IsNullOrEmpty(old))continue;
            Vector3 p0=w.position;Quaternion r0=w.localRotation;Vector3 s0=w.localScale;
            float oldH=BoundsOf(w.gameObject).size.y;
            string np=((Stable(w.name)&1)==0)?
              "Assets/Art/Environment/Vegetation/Trees/Pines/PineDead_001/PineDead_02.prefab":
              "Assets/Art/Environment/Vegetation/Trees/Pines/PineDead_001/PineDead_03.prefab";
            var prefab=AssetDatabase.LoadAssetAtPath<GameObject>(np);if(!prefab)throw new Exception("Missing "+np);
            foreach(Transform ch in w.Cast<Transform>().ToList())UnityEngine.Object.DestroyImmediate(ch.gameObject);
            var v=(GameObject)PrefabUtility.InstantiatePrefab(prefab);
            v.transform.SetParent(w,false);v.transform.localPosition=Vector3.zero;v.transform.localRotation=Quaternion.identity;v.transform.localScale=Vector3.one;
            float nh=BoundsOf(w.gameObject).size.y;if(nh>.05f&&oldH>.05f)v.transform.localScale*=Mathf.Clamp(oldH/nh,.65f,1.6f);
            Bounds bb=BoundsOf(w.gameObject);float gy=terrain.SampleHeight(new Vector3(p0.x,0,p0.z))+terrain.transform.position.y;
            w.position+=Vector3.up*(gy-bb.min.y);w.position=new Vector3(p0.x,w.position.y,p0.z);w.localRotation=r0;w.localScale=s0;
            bb=BoundsOf(w.gameObject);gy=terrain.SampleHeight(new Vector3(p0.x,0,p0.z))+terrain.transform.position.y;float gap=bb.min.y-gy;
            if(Mathf.Abs(w.position.x-p0.x)>.0001f||Mathf.Abs(w.position.z-p0.z)>.0001f||Quaternion.Angle(w.localRotation,r0)>.0001f||(w.localScale-s0).sqrMagnitude>1e-10f||Mathf.Abs(gap)>.04f)
                throw new Exception("Dead tree replacement verification failed "+w.name);
            rows.Add(string.Join(",",new[]{scope,w.name.Replace(',',';'),old.Replace(',',';'),np,F(p0.x),F(p0.z),F(w.position.x),F(w.position.z),F(gap),"PASS"}));
            EditorUtility.SetDirty(w);n++;
        }
        return n;
    }
    static string OldDeadAsset(GameObject go)
    {
        foreach(var mf in go.GetComponentsInChildren<MeshFilter>(true))
        {
            if(!mf.sharedMesh)continue;string p=AssetDatabase.GetAssetPath(mf.sharedMesh).ToLowerInvariant();
            if(p.Contains("tree_dead_001")||p.Contains("dead_tree_trunk_02"))return p;
        }
        return "";
    }
    static Bounds BoundsOf(GameObject go){var rs=go.GetComponentsInChildren<Renderer>(true).Where(r=>r.enabled&&r.gameObject.activeInHierarchy).ToArray();if(rs.Length==0)return new Bounds(go.transform.position,Vector3.zero);var b=rs[0].bounds;for(int i=1;i<rs.Length;i++)b.Encapsulate(rs[i].bounds);return b;}
    static Transform Find(Transform root,string n){if(!root)return null;if(root.name==n)return root;foreach(var t in root.GetComponentsInChildren<Transform>(true))if(t.name==n)return t;return null;}
    static int Stable(string s){unchecked{int h=17;foreach(char c in s)h=h*31+c;return h;}}
    static string F(float v)=>v.ToString("0.######",CultureInfo.InvariantCulture);
    static int HardCount(float[,,] a){int h=a.GetLength(0),w=a.GetLength(1),l=a.GetLength(2),n=0;for(int y=0;y<h-1;y+=2)for(int x=0;x<w-1;x+=2){float dx=0,dz=0;for(int k=0;k<l;k++){dx+=Mathf.Abs(a[y,x,k]-a[y,x+1,k]);dz+=Mathf.Abs(a[y,x,k]-a[y+1,x,k]);}if(dx>1.55f)n++;if(dz>1.55f)n++;}return n;}
}
