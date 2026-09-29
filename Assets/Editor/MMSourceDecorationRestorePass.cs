using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using System;
using System.IO;
using System.Linq;
using System.Globalization;
using System.Collections.Generic;

public static class MMSourceDecorationRestorePass
{
    sealed class Z{public string key,scene;public Z(string k,string s){key=k;scene=s;}}
    static readonly Z[] Zones={
        new Z("NewSorpigal","Assets/Scenes/Regions/NewSorpigal.unity"),new Z("CastleIronfist","Assets/Scenes/Regions/CastleIronfist.unity"),
        new Z("MireOfTheDamned","Assets/Scenes/Regions/MireOfTheDamned.unity"),new Z("MistyIslands","Assets/Scenes/Regions/MistyIslands.unity"),
        new Z("EelInfestedWaters","Assets/Scenes/Regions/EelInfestedWaters.unity"),new Z("BootlegBay","Assets/Scenes/Regions/BootlegBay.unity"),
        new Z("SilverCove","Assets/Scenes/Regions/SilverCove.unity"),new Z("Dragonsand","Assets/Scenes/Regions/Dragonsand.unity"),
        new Z("HermitsIsle","Assets/Scenes/Regions/HermitsIsle.unity"),new Z("Blackshire","Assets/Scenes/Regions/Blackshire.unity"),
        new Z("ParadiseValley","Assets/Scenes/Regions/ParadiseValley.unity"),new Z("FreeHaven","Assets/Scenes/Regions/FreeHaven.unity"),
        new Z("SweetWater","Assets/Scenes/Regions/SweetWater.unity"),new Z("FrozenHighlands","Assets/Scenes/Regions/FrozenHighlands.unity"),
        new Z("Kriegspire","Assets/Scenes/Regions/Kriegspire.unity")};

    const string SpriteTex="Assets/Textures/MM6SpritesOriginal";
    const string SpriteMat="Assets/Materials/MM6SpritesOriginal";
    const string Shared="Assets/World/Shared";
    static bool P(string s,out float v)=>float.TryParse(s,NumberStyles.Float,CultureInfo.InvariantCulture,out v);
    static bool Tree(string n){n=n.ToLowerInvariant();return n.StartsWith("6tree")||n.StartsWith("tree")||n.StartsWith("swptree")||n.StartsWith("snotre");}
    static bool Rock(string n)=>n.ToLowerInvariant().StartsWith("6rock");
    static bool Start(string n){n=n.Trim().ToLowerInvariant();return n=="party start"||n.EndsWith(" start");}
    static bool NonVisual(string n){n=n.Trim().ToLowerInvariant();return string.IsNullOrEmpty(n)||n.StartsWith("snd_");}

    static Bounds B(GameObject go){
        var rs=go.GetComponentsInChildren<Renderer>(true).Where(r=>r.enabled).ToArray();
        if(rs.Length==0)return new Bounds(go.transform.position,Vector3.zero);
        var b=rs[0].bounds;for(int i=1;i<rs.Length;i++)b.Encapsulate(rs[i].bounds);return b;
    }
    static void Ground(GameObject go,Terrain t,float x,float z){
        float gy=t.SampleHeight(new Vector3(x,0,z))+t.transform.position.y;var b=B(go);
        go.transform.position+=Vector3.up*(gy-b.min.y);
    }
    static void ScaleHeight(GameObject go,float target){
        var b=B(go);if(b.size.y>.001f)go.transform.localScale*=target/b.size.y;
    }
    static Mesh Quad(){
        string p=Shared+"/MM6_SourceSpriteQuad.asset";var m=AssetDatabase.LoadAssetAtPath<Mesh>(p);if(m)return m;
        m=new Mesh{name="MM6_SourceSpriteQuad"};
        m.vertices=new[]{new Vector3(-.5f,0,0),new Vector3(.5f,0,0),new Vector3(.5f,1,0),new Vector3(-.5f,1,0)};
        m.uv=new[]{new Vector2(0,0),new Vector2(1,0),new Vector2(1,1),new Vector2(0,1)};
        m.triangles=new[]{0,2,1,0,3,2};m.RecalculateNormals();m.RecalculateBounds();AssetDatabase.CreateAsset(m,p);return m;
    }
    static Material SpriteMaterial(string name,Texture2D tex){
        string p=SpriteMat+"/"+name+".mat";var m=AssetDatabase.LoadAssetAtPath<Material>(p);
        var sh=Shader.Find("Unlit/Transparent Cutout");if(!sh)sh=Shader.Find("Standard");
        if(!m){m=new Material(sh);AssetDatabase.CreateAsset(m,p);}else m.shader=sh;
        if(m.HasProperty("_MainTex"))m.SetTexture("_MainTex",tex);
        if(m.HasProperty("_Cutoff"))m.SetFloat("_Cutoff",.05f);
        if(m.HasProperty("_Cull"))m.SetInt("_Cull",0);
        EditorUtility.SetDirty(m);return m;
    }
    static Material Wood(){
        string p="Assets/Materials/RealisticWorld/SourceBoatWood.mat";Directory.CreateDirectory("Assets/Materials/RealisticWorld");
        var m=AssetDatabase.LoadAssetAtPath<Material>(p);var sh=Shader.Find("Standard");
        if(!m){m=new Material(sh);AssetDatabase.CreateAsset(m,p);}else m.shader=sh;
        var tex=AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Environment/PolyHaven/Textures/wood_planks/wood_planks_diff_1k.jpg");
        if(m.HasProperty("_MainTex"))m.SetTexture("_MainTex",tex);if(m.HasProperty("_Glossiness"))m.SetFloat("_Glossiness",.18f);
        EditorUtility.SetDirty(m);return m;
    }
    static GameObject Billboard(string name,int lineIndex,Transform parent,Texture2D tex){
        var go=new GameObject($"SRC_{lineIndex:000}_{name}");go.transform.SetParent(parent,false);
        var mf=go.AddComponent<MeshFilter>();mf.sharedMesh=Quad();
        var mr=go.AddComponent<MeshRenderer>();mr.sharedMaterial=SpriteMaterial(name,tex);mr.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;mr.receiveShadows=false;
        float h=Mathf.Clamp(tex.height/32f,.35f,11f),w=h*Mathf.Clamp(tex.width/(float)Mathf.Max(1,tex.height),.18f,2.5f);
        go.transform.localScale=new Vector3(w,h,1);go.AddComponent<MMBillboardToCamera>();return go;
    }
    static GameObject Boat(string name,int lineIndex,Transform parent){
        var root=new GameObject($"SRC_{lineIndex:000}_{name}");root.transform.SetParent(parent,false);var mat=Wood();
        GameObject hull=GameObject.CreatePrimitive(PrimitiveType.Cube);hull.name="Hull";hull.transform.SetParent(root.transform,false);hull.transform.localPosition=new Vector3(0,.35f,0);hull.transform.localScale=new Vector3(4.8f,.55f,1.45f);hull.GetComponent<Renderer>().sharedMaterial=mat;UnityEngine.Object.DestroyImmediate(hull.GetComponent<Collider>());
        GameObject deck=GameObject.CreatePrimitive(PrimitiveType.Cube);deck.name="Deck";deck.transform.SetParent(root.transform,false);deck.transform.localPosition=new Vector3(-.25f,.78f,0);deck.transform.localScale=new Vector3(2.4f,.25f,1.15f);deck.GetComponent<Renderer>().sharedMaterial=mat;UnityEngine.Object.DestroyImmediate(deck.GetComponent<Collider>());
        return root;
    }
    static GameObject GenericProp(string name,int lineIndex,Transform parent){
        var root=new GameObject($"SRC_{lineIndex:000}_{name}");root.transform.SetParent(parent,false);var mat=Wood();
        var p=GameObject.CreatePrimitive(PrimitiveType.Cube);p.name="SourceProp";p.transform.SetParent(root.transform,false);p.transform.localPosition=new Vector3(0,.65f,0);p.transform.localScale=new Vector3(.7f,1.3f,.7f);p.GetComponent<Renderer>().sharedMaterial=mat;UnityEngine.Object.DestroyImmediate(p.GetComponent<Collider>());return root;
    }
    static GameObject RockGO(string name,int idx,Transform parent){
        string p=(idx%2==0)?"Assets/EnvironmentAssets/Gobkit/Rock001.fbx":"Assets/EnvironmentAssets/Gobkit/Rock002.fbx";
        var prefab=AssetDatabase.LoadAssetAtPath<GameObject>(p);if(!prefab)return null;
        var go=(GameObject)PrefabUtility.InstantiatePrefab(prefab);go.name=$"SourceRock_{idx:0000}_{name}";go.transform.SetParent(parent,false);return go;
    }

    [MenuItem("MMUnity/Locked/Restore Exact Source Rocks And Sprites")]
    public static void ApplyAll(){
        Directory.CreateDirectory(SpriteMat);Directory.CreateDirectory(Shared);AssetDatabase.Refresh();
        var log=new List<string>{"zone,rocks,sprites,boats,generic,nonvisual"};
        foreach(var z in Zones)Apply(z,log);
        File.WriteAllLines("Validation/SourceDecorationRestore.csv",log);AssetDatabase.SaveAssets();
        Debug.Log("SOURCE_DECORATION_RESTORE_DONE zones="+Zones.Length);
    }
    static void Apply(Z z,List<string> log){
        var sc=EditorSceneManager.OpenScene(z.scene,OpenSceneMode.Single);
        var root=sc.GetRootGameObjects().FirstOrDefault(g=>g.name.IndexOf("Open World",StringComparison.OrdinalIgnoreCase)>=0)??sc.GetRootGameObjects().First();
        var terrain=root.GetComponentInChildren<Terrain>(true);if(!terrain)throw new Exception(z.key+" terrain missing");
        var oldS=root.transform.Find("Source Sprites - OneToOne");if(oldS)UnityEngine.Object.DestroyImmediate(oldS.gameObject);
        var oldR=root.transform.Find("Source Rocks - OneToOne");if(oldR)UnityEngine.Object.DestroyImmediate(oldR.gameObject);
        var sr=new GameObject("Source Sprites - OneToOne");sr.transform.SetParent(root.transform,false);
        var rr=new GameObject("Source Rocks - OneToOne");rr.transform.SetParent(root.transform,false);
        int rocks=0,sprites=0,boats=0,generic=0,nonvisual=0;string[] lines=File.ReadAllLines($"Assets/World/{z.key}/Data/decorations.csv");
        for(int i=1;i<lines.Length;i++){
            var q=lines[i].Split(',');if(q.Length<10)continue;string n=q[1].Trim().ToLowerInvariant();if(Start(n)||Tree(n))continue;
            if(!P(q[4],out float x)||!P(q[5],out float sy)||!P(q[6],out float oz))continue;P(q[7],out float yaw);float zz=-oz;int idx=i-1;int.TryParse(q[0],out idx);
            if(Rock(n)){
                var go=RockGO(n,idx,rr.transform);if(!go)continue;go.transform.position=new Vector3(x,0,zz);go.transform.rotation=Quaternion.Euler(0,-yaw,0);ScaleHeight(go,Mathf.Lerp(.75f,1.8f,(Mathf.Abs(idx*37)%100)/99f));Ground(go,terrain,x,zz);rocks++;continue;
            }
            if(NonVisual(n)){nonvisual++;continue;}
            GameObject sgo=null;var tex=AssetDatabase.LoadAssetAtPath<Texture2D>($"{SpriteTex}/{n}.png");
            if(tex){sgo=Billboard(n,i,sr.transform,tex);sprites++;}
            else if(n=="shp"){sgo=Boat(n,i,sr.transform);boats++;}
            else{sgo=GenericProp(n,i,sr.transform);generic++;}
            float ground=terrain.SampleHeight(new Vector3(x,0,zz))+terrain.transform.position.y;
            float y=n=="shp"?.05f:Mathf.Max(ground,sy);
            sgo.transform.position=new Vector3(x,y,zz);
            if(!sgo.GetComponent<MMBillboardToCamera>())sgo.transform.rotation=Quaternion.Euler(0,-yaw,0);
        }
        EditorSceneManager.MarkSceneDirty(sc);EditorSceneManager.SaveScene(sc);
        log.Add($"{z.key},{rocks},{sprites},{boats},{generic},{nonvisual}");
        Debug.Log($"SOURCE_DECORATION_RESTORE {z.key} rocks={rocks} sprites={sprites} boats={boats} generic={generic} nonvisual={nonvisual}");
    }
    public static void AuditAll(){
        Directory.CreateDirectory("Validation/SourceDecorationAudit");
        var sum=new List<string>{"zone,rows,trees,rocks,sprites,nonvisual,missing,xz_bad,extra_source_visuals"};
        foreach(var z in Zones){
            var sc=EditorSceneManager.OpenScene(z.scene,OpenSceneMode.Single);
            var root=sc.GetRootGameObjects().FirstOrDefault(g=>g.name.IndexOf("Open World",StringComparison.OrdinalIgnoreCase)>=0)??sc.GetRootGameObjects().First();
            var all=root.GetComponentsInChildren<Transform>(true).ToArray();
            string[] lines=File.ReadAllLines($"Assets/World/{z.key}/Data/decorations.csv");
            int rows=0,trees=0,rocks=0,sprites=0,nonvisual=0,missing=0,bad=0,extra=0;
            var expected=new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var detail=new List<string>{"index,name,category,expected_x,expected_z,scene_x,scene_z,dx,dz,status"};
            for(int i=1;i<lines.Length;i++){
                var q=lines[i].Split(',');if(q.Length<10)continue;string n=q[1].Trim().ToLowerInvariant();if(Start(n))continue;
                if(!P(q[4],out float x)||!P(q[6],out float oz))continue;float zz=-oz;int idx=i-1;int.TryParse(q[0],out idx);
                string cat,sceneName;
                if(Tree(n)){cat="TREE";sceneName=$"SourceTree_{idx:0000}_{n}";trees++;}
                else if(Rock(n)){cat="ROCK";sceneName=$"SourceRock_{idx:0000}_{n}";rocks++;}
                else if(NonVisual(n)){cat="NONVISUAL";sceneName="";nonvisual++;rows++;continue;}
                else{cat="SPRITE";sceneName=$"SRC_{i:000}_{n}";sprites++;}
                rows++;expected.Add(sceneName);
                var t=all.FirstOrDefault(a=>a.name.Equals(sceneName,StringComparison.OrdinalIgnoreCase));
                if(!t){missing++;detail.Add($"{idx},{n},{cat},{x:F4},{zz:F4},,,,,MISSING");continue;}
                float dx=t.position.x-x,dz=t.position.z-zz;bool xb=Mathf.Abs(dx)>.01f||Mathf.Abs(dz)>.01f;if(xb)bad++;
                detail.Add($"{idx},{n},{cat},{x:F4},{zz:F4},{t.position.x:F4},{t.position.z:F4},{dx:F4},{dz:F4},{(xb?"XZ_BAD":"OK")}");
            }
            foreach(var t in all){
                string n=t.name;
                if((n.StartsWith("SRC_",StringComparison.OrdinalIgnoreCase)||n.StartsWith("SourceTree_",StringComparison.OrdinalIgnoreCase)||n.StartsWith("SourceRock_",StringComparison.OrdinalIgnoreCase))&&!expected.Contains(n))extra++;
            }
            File.WriteAllLines($"Validation/SourceDecorationAudit/{z.key}.csv",detail);
            sum.Add($"{z.key},{rows},{trees},{rocks},{sprites},{nonvisual},{missing},{bad},{extra}");
            Debug.Log($"SOURCE_DECOR_AUDIT {z.key} rows={rows} missing={missing} xzBad={bad} extra={extra}");
        }
        File.WriteAllLines("Validation/SourceDecorationAudit/summary.csv",sum);
        Debug.Log("SOURCE_DECOR_AUDIT_DONE zones="+Zones.Length);
    }

}