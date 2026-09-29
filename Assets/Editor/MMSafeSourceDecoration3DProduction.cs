using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using System;
using System.IO;
using System.Linq;
using System.Globalization;
using System.Collections.Generic;

public static class MMSafeSourceDecoration3DProduction
{
    sealed class Z{public string key,scene;public Z(string k,string s){key=k;scene=s;}}
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
        new Z("ParadiseValley","Assets/Scenes/ParadiseValley_SourceGrid.unity"),
        new Z("EelInfestedWaters","Assets/Scenes/EelInfestedWaters_SourceGrid.unity"),
        new Z("SilverCove","Assets/Scenes/SilverCove_SourceGrid.unity"),
        new Z("FrozenHighlands","Assets/Scenes/FrozenHighlands_SourceGrid.unity"),
        new Z("Kriegspire","Assets/Scenes/Kriegspire_SourceGrid.unity"),
        new Z("SweetWater","Assets/Scenes/SweetWater_SourceGrid.unity")
    };

    const string Cactus="Assets/Environment/DesertVegetation/Cactus.fbx";
    const string ShrubA="Assets/Environment/PolyHaven/Downloaded/shrub_03/shrub_03_1k.fbx";
    const string ShrubB="Assets/Environment/PolyHaven/Downloaded/wild_rooibos_bush/wild_rooibos_bush_1k.fbx";
    const string Fern="Assets/Environment/PolyHaven/Models/fern_02/fern_02_1k.fbx";
    const string Pine="Assets/Art/Environment/Vegetation/Trees/Pines/Pine_004_new/pine_004_4.prefab";
    const string Stump="Assets/Art/Environment/Vegetation/Trees/Generic_Stump/Stump_04/Stump_04.prefab";
    const string Barrel="Assets/Environment/PolyHaven/Models/wine_barrel_01/wine_barrel_01_1k.fbx";
    const string Rock="Assets/EnvironmentAssets/Gobkit/Rock002.fbx";

    static bool F(string s,out float v)=>float.TryParse(s,NumberStyles.Float,CultureInfo.InvariantCulture,out v);
    static bool Start(string n){n=n.Trim().ToLowerInvariant();return n=="party start"||n.EndsWith(" start");}
    static bool Already3D(string n){n=n.ToLowerInvariant();return n.StartsWith("6tree")||n.StartsWith("tree")||n.StartsWith("6rock");}
    static Bounds B(GameObject go)
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
        var b=B(go);
        go.transform.position+=Vector3.up*(target-b.min.y);
    }
    static GameObject MakeAsset(string path,Transform parent,float scale)
    {
        var src=AssetDatabase.LoadAssetAtPath<GameObject>(path);if(!src)return null;
        var go=(GameObject)PrefabUtility.InstantiatePrefab(src);
        if(!go)go=UnityEngine.Object.Instantiate(src);
        go.transform.SetParent(parent,false);
        go.transform.localScale*=scale;
        return go;
    }
    static GameObject MakeCactus(Transform parent,int idx)
    {
        var src=AssetDatabase.LoadAssetAtPath<GameObject>(Cactus);if(!src)return null;
        var go=(GameObject)PrefabUtility.InstantiatePrefab(src);
        if(!go)go=UnityEngine.Object.Instantiate(src);
        go.transform.SetParent(parent,false);
        var vars=go.transform.Cast<Transform>()
            .Where(t=>t.name.StartsWith("cactus_",StringComparison.OrdinalIgnoreCase))
            .OrderBy(t=>t.name).ToArray();
        if(vars.Length==0){UnityEngine.Object.DestroyImmediate(go);return null;}
        int keep=Math.Abs(idx)%vars.Length;
        for(int i=0;i<vars.Length;i++){
            vars[i].gameObject.SetActive(i==keep);
            if(i==keep)vars[i].localPosition=Vector3.zero;
        }
        return go;
    }
    sealed class Map
    {
        public string family,path,status; public float scale,minY,maxY,maxXZ; public bool cactus;
        public Map(string f,string p,float s,float min,float max,float xz,bool c=false,string st="MAPPED")
        {family=f;path=p;scale=s;minY=min;maxY=max;maxXZ=xz;cactus=c;status=st;}
    }
    static Map Pick(string n,string dominant)
    {
        n=n.ToLowerInvariant();
        if(n.StartsWith("cac")){
            if(dominant.IndexOf("Desert",StringComparison.OrdinalIgnoreCase)<0)
                return new Map("CACTUS",null,1,0,0,0,false,"OMIT_NON_DESERT_CACTUS");
            return new Map("CACTUS",Cactus,1f,1.5f,4.5f,3.0f,true);
        }
        if(n.StartsWith("searka"))return new Map("SHRUB_A",ShrubA,1f,.15f,.75f,1.7f);
        if(n.StartsWith("searkb"))return new Map("SHRUB_B",ShrubB,1f,.20f,.85f,2.7f);
        if(n.StartsWith("6flower"))return new Map("LOW_PLANT",Fern,.35f,.08f,.40f,.85f);
        if(n.StartsWith("snoweed"))return new Map("LOW_PLANT",Fern,.35f,.08f,.40f,.85f);
        if(n.StartsWith("swptree"))return new Map("SOURCE_TREE",Pine,0.2180f,5.3f,6.7f,2.2f);
        if(n.StartsWith("snotre"))return new Map("SOURCE_SNOW_TREE",Pine,0.2361f,5.8f,7.2f,2.4f);
        if(n.StartsWith("stump"))return new Map("STUMP",Stump,1f,.7f,1.6f,2.5f);
        if(n=="brnrock0")return new Map("BROWN_ROCK",Rock,.0040f,.7f,1.6f,1.6f);
        if(n.Contains("barel")||n.Contains("barrel")){
            float s=n.Contains("big")?1.25f:(n.Contains("sml")?.82f:1f);
            return new Map("BARREL",Barrel,s,.55f,1.25f,1.25f);
        }
        return new Map("UNMAPPED",null,1,0,0,0,false,"OMITTED_UNMAPPED");
    }
    static bool SizeOK(Map m,Bounds b)
    {
        if(b.size==Vector3.zero)return false;
        float xz=Mathf.Max(b.size.x,b.size.z);
        return b.size.y>=m.minY-.001f&&b.size.y<=m.maxY+.001f&&xz<=m.maxXZ+.001f;
    }
    static bool SaveSafe(Scene sc,string scenePath,string key,out string mode)
    {
        mode="DIRECT";
        EditorSceneManager.MarkSceneDirty(sc);
        if(EditorSceneManager.SaveScene(sc,scenePath))return true;

        mode="STAGING";
        string staging=$"Assets/Scenes/__SAFE3D_STAGING_{key}.unity";
        if(!EditorSceneManager.SaveScene(sc,staging))return false;
        AssetDatabase.SaveAssets();
        EditorSceneManager.CloseScene(sc,true);
        AssetDatabase.Refresh();
        string src=Path.GetFullPath(staging),dst=Path.GetFullPath(scenePath);
        File.Copy(src,dst,true);
        File.Delete(src);
        string meta=src+".meta";if(File.Exists(meta))File.Delete(meta);
        AssetDatabase.Refresh();
        return true;
    }
    sealed class R{public int mapped,omitted,rejected,removed;public string save;}
    static R Convert(Z z,List<string> audit)
    {
        var sc=EditorSceneManager.OpenScene(z.scene,OpenSceneMode.Single);
        var root=sc.GetRootGameObjects().FirstOrDefault(g=>g.GetComponentInChildren<Terrain>(true));
        if(!root)throw new Exception("Root missing "+z.key);
        var terrain=root.GetComponentInChildren<Terrain>(true);
        if(!terrain)throw new Exception("Terrain missing "+z.key);

        int removed=0;
        var old=root.transform.Find("Source Sprites - OneToOne");
        if(old){removed=old.GetComponentsInChildren<MMBillboardToCamera>(true).Length;UnityEngine.Object.DestroyImmediate(old.gameObject);}
        var old3=root.transform.Find("Source Decorations 3D - OneToOne");
        if(old3)UnityEngine.Object.DestroyImmediate(old3.gameObject);

        var group=new GameObject("Source Decorations 3D - OneToOne");
        group.transform.SetParent(root.transform,false);
        int mapped=0,omitted=0,rejected=0;
        string[] lines=File.ReadAllLines($"Assets/World/{z.key}/Data/decorations.csv");

        for(int i=1;i<lines.Length;i++)
        {
            var q=lines[i].Split(',');if(q.Length<10)continue;
            string n=q[1].Trim().ToLowerInvariant();
            if(string.IsNullOrEmpty(n)||Start(n)||Already3D(n)||n.StartsWith("snd_"))continue;
            if(!F(q[4],out float x)||!F(q[5],out float sy)||!F(q[6],out float oz))continue;
            F(q[7],out float yaw);float zz=-oz;
            int idx=i-1;int.TryParse(q[0],out idx);
            string dom=Dominant(terrain,x,zz);
            var m=Pick(n,dom);

            if(m.path==null){
                omitted++;
                audit.Add($"{z.key},{idx},{n},{m.family},{m.status},,{dom},{x:F3},{zz:F3},,,,");
                continue;
            }

            GameObject go=m.cactus?MakeCactus(group.transform,idx):MakeAsset(m.path,group.transform,m.scale);
            if(!go){
                omitted++;
                audit.Add($"{z.key},{idx},{n},{m.family},ASSET_MISSING,{m.path},{dom},{x:F3},{zz:F3},,,,");
                continue;
            }

            go.name=$"SRC3D_{idx:0000}_{n}_{m.family}";
            go.transform.position=new Vector3(x,0f,zz);
            go.transform.rotation=Quaternion.Euler(0f,-yaw,0f);
            Ground(go,terrain,x,zz,sy);
            var b=B(go);

            if(!ShaderOK(go)||!SizeOK(m,b)){
                rejected++;
                audit.Add($"{z.key},{idx},{n},{m.family},REJECT,{m.path},{dom},{x:F3},{zz:F3},{b.size.x:F3},{b.size.y:F3},{b.size.z:F3}");
                UnityEngine.Object.DestroyImmediate(go);
                continue;
            }
            if(Mathf.Abs(go.transform.position.x-x)>.001f||Mathf.Abs(go.transform.position.z-zz)>.001f)
                throw new Exception($"XZ moved {z.key} idx={idx}");

            mapped++;
            audit.Add($"{z.key},{idx},{n},{m.family},MAPPED,{m.path},{dom},{x:F3},{zz:F3},{b.size.x:F3},{b.size.y:F3},{b.size.z:F3}");
        }

        if(!SaveSafe(sc,z.scene,z.key,out string saveMode))throw new IOException("Save failed "+z.scene);
        return new R{mapped=mapped,omitted=omitted,rejected=rejected,removed=removed,save=saveMode};
    }
    static void RunRange(int start,int count,string tag)
    {
        Directory.CreateDirectory("Validation/SourceDecoration3D/Production");
        var audit=new List<string>{"zone,index,source_name,family,status,asset,dominant_terrain,x,z,size_x,size_y,size_z"};
        var sum=new List<string>{"zone,mapped3d,omitted,rejected,removed_billboards,save_mode"};
        int end=Mathf.Min(Zones.Length,start+count);
        for(int i=start;i<end;i++){
            var z=Zones[i];var r=Convert(z,audit);
            sum.Add($"{z.key},{r.mapped},{r.omitted},{r.rejected},{r.removed},{r.save}");
            Debug.Log($"SAFE_SOURCE_3D {z.key} mapped={r.mapped} omitted={r.omitted} rejected={r.rejected} removedBB={r.removed} save={r.save}");
        }
        File.WriteAllLines($"Validation/SourceDecoration3D/Production/audit_{tag}.csv",audit);
        File.WriteAllLines($"Validation/SourceDecoration3D/Production/summary_{tag}.csv",sum);
        AssetDatabase.SaveAssets();
    }

    [MenuItem("MMUnity/Source Decorations 3D SAFE/Production 01-03")] public static void A()=>RunRange(0,3,"01_03");
    [MenuItem("MMUnity/Source Decorations 3D SAFE/Production 04-06")] public static void B()=>RunRange(3,3,"04_06");
    [MenuItem("MMUnity/Source Decorations 3D SAFE/Production 07-09")] public static void C()=>RunRange(6,3,"07_09");
    [MenuItem("MMUnity/Source Decorations 3D SAFE/Production 10-12")] public static void D()=>RunRange(9,3,"10_12");
    [MenuItem("MMUnity/Source Decorations 3D SAFE/Production 13-15")] public static void E()=>RunRange(12,3,"13_15");

    [MenuItem("MMUnity/Source Decorations 3D SAFE/Audit Standalones")]
    public static void Audit()
    {
        Directory.CreateDirectory("Validation/SourceDecoration3D/Production");
        var rows=new List<string>{"zone,billboards,mm6_quad_meshes,source_sprite_roots,source3d_roots,error_shader_renderers,status"};
        foreach(var z in Zones)
        {
            var sc=EditorSceneManager.OpenScene(z.scene,OpenSceneMode.Single);
            var root=sc.GetRootGameObjects().FirstOrDefault(g=>g.GetComponentInChildren<Terrain>(true));
            int bb=root?root.GetComponentsInChildren<MMBillboardToCamera>(true).Length:999;
            int quad=0,err=0;
            if(root){
                foreach(var mf in root.GetComponentsInChildren<MeshFilter>(true))
                    if(mf.sharedMesh&&mf.sharedMesh.name=="MM6_SourceSpriteQuad")quad++;
                foreach(var rr in root.GetComponentsInChildren<Renderer>(true))
                    if(rr.sharedMaterials.Any(m=>m&&(!m.shader||m.shader.name=="Hidden/InternalErrorShader")))err++;
            }
            int sr=root?root.GetComponentsInChildren<Transform>(true).Count(t=>t.name=="Source Sprites - OneToOne"):999;
            int d3=root?root.GetComponentsInChildren<Transform>(true).Count(t=>t.name=="Source Decorations 3D - OneToOne"):0;
            string ok=(bb==0&&quad==0&&sr==0&&d3==1&&err==0)?"PASS":"FAIL";
            rows.Add($"{z.key},{bb},{quad},{sr},{d3},{err},{ok}");
        }
        File.WriteAllLines("Validation/SourceDecoration3D/Production/standalone_audit.csv",rows);
    }
}
