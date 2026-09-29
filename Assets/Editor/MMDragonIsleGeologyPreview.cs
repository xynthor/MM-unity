using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
public static class MMDragonIsleGeologyPreview {
    const string Scene="Assets/Scenes/__PREVIEW_DragonIsle_Geology.unity";
    const string TerrainAsset="Assets/TempDragonRework/BASE_DragonIsleTerrain.asset";
    const string Dir="Assets/TempDragonRework";
    const string Group="Dragon Isle - Geology Preview";
    static readonly string[] CliffPaths={
        "Assets/Art/Environment/Cliffs/SmallCliff_01/SmallCliff_A.prefab",
        "Assets/Art/Environment/Cliffs/RockSlussen_01/RockSlussen_01.prefab",
        "Assets/Art/Environment/Cliffs/FlatRock_01/FlatRock_01.prefab",
        "Assets/Art/Environment/Cliffs/Rock_06/Rock_06.prefab"
    };
    const string Shrub="Assets/Environment/PolyHaven/Models/shrub_02/shrub_02_1k.fbx";
    const string ShrubMat="Assets/Materials/RealisticWorld/Shrub02.mat";
    const string Dead="Assets/Art/Environment/Vegetation/Trees/Generic_Stump/Stump_04/Stump_04.prefab";
    static System.Random rng=new System.Random(197811);
    static float R(float a,float b)=>a+(float)rng.NextDouble()*(b-a);
    static float H(Terrain t,float x,float z)=>t.SampleHeight(new Vector3(x,0,z))+t.transform.position.y;
    static float S(Terrain t,float x,float z) {
        var l=new Vector3(x,0,z)-t.transform.position;var s=t.terrainData.size;
        return t.terrainData.GetSteepness(Mathf.Clamp01(l.x/s.x),Mathf.Clamp01(l.z/s.z));
    }
    static Bounds Bounds(GameObject g) {
        var rs=g.GetComponentsInChildren<Renderer>(true).Where(r=>r.enabled&&r.gameObject.activeInHierarchy).ToArray();
        if(rs.Length==0)return new Bounds(g.transform.position,Vector3.zero);
        var b=rs[0].bounds;foreach(var r in rs.Skip(1))b.Encapsulate(r.bounds);return b;
    }
    static Material RockMat(int k) {
        var path=Dir+"/Geology_"+k+".mat";
        var mat=AssetDatabase.LoadAssetAtPath<Material>(path);
        if(mat)return mat;
        var source=AssetDatabase.LoadAssetAtPath<GameObject>(CliffPaths[k]);
        if(!source)throw new Exception("Cliff missing "+CliffPaths[k]);
        var sm=source.GetComponentsInChildren<Renderer>(true).SelectMany(r=>r.sharedMaterials).FirstOrDefault(x=>x);
        var diffuse=sm?(sm.GetTexture("_BaseColorMap")??sm.GetTexture("_MainTex")):null;
        if(!diffuse)throw new Exception("Cliff diffuse missing "+CliffPaths[k]);
        mat=new Material(Shader.Find("Standard")){name="Dragon_Geology_"+k};
        mat.SetTexture("_MainTex",diffuse);
        if(k==3&&sm&&sm.GetTexture("_BumpMap")) {
            mat.SetTexture("_BumpMap",sm.GetTexture("_BumpMap"));mat.EnableKeyword("_NORMALMAP");
        }
        mat.color=new Color(.84f,.82f,.78f,1f);
        mat.SetFloat("_Glossiness",.07f);mat.SetFloat("_Metallic",0f);
        AssetDatabase.CreateAsset(mat,path);return mat;
    }
    static GameObject Spawn(int k,Transform parent,Vector3 pos,float yaw,float maxSize,float sink) {
        var prefab=AssetDatabase.LoadAssetAtPath<GameObject>(CliffPaths[k]);
        var go=(GameObject)PrefabUtility.InstantiatePrefab(prefab);
        if(!go)throw new Exception("Cannot create cliff "+k);
        go.transform.SetParent(parent,false);
        foreach(var r in go.GetComponentsInChildren<Renderer>(true)) {
            int count=Mathf.Max(1,r.sharedMaterials.Length);
            r.sharedMaterials=Enumerable.Repeat(RockMat(k),count).ToArray();
        }
        go.transform.position=pos;go.transform.rotation=Quaternion.Euler(0,yaw,0);
        var b=Bounds(go);float span=Mathf.Max(b.size.x,Mathf.Max(b.size.y,b.size.z));
        if(span<.01f)throw new Exception("Empty cliff prefab");
        go.transform.localScale*=maxSize/span;
        b=Bounds(go);go.transform.position+=Vector3.up*(pos.y-b.min.y-sink);
        return go;
    }
    static bool Near(Bounds[] b,float x,float z,float pad) {
        return b.Any(q=>x>=q.min.x-pad&&x<=q.max.x+pad&&z>=q.min.z-pad&&z<=q.max.z+pad);
    }
    static Vector2 Gradient(Terrain t,float x,float z) {
        return new Vector2(H(t,x+4,z)-H(t,x-4,z),H(t,x,z+4)-H(t,x,z-4)).normalized;
    }
    static List<Vector2> Coast(Terrain t,Bounds[] avoid) {
        var points=new List<(Vector2 p,float score)>();
        var tp=t.transform.position;var ts=t.terrainData.size;
        for(float z=tp.z+12;z<tp.z+ts.z-12;z+=7)
        for(float x=tp.x+12;x<tp.x+ts.x-12;x+=7) {
            float y=H(t,x,z);if(y<1.1f||y>8.2f||S(t,x,z)<6f||Near(avoid,x,z,16f))continue;
            int sea=0;for(int a=0;a<8;a++){
                float ang=a*Mathf.PI/4f;
                if(H(t,x+10*Mathf.Cos(ang),z+10*Mathf.Sin(ang))<.7f)sea++;
            }
            if(sea<2||sea>6)continue;
            points.Add((new Vector2(x,z),sea+S(t,x,z)*.08f+R(0,.2f)));
        }
        var selected=new List<Vector2>();
        foreach(var q in points.OrderByDescending(q=>q.score)) {
            if(selected.All(s=>Vector2.Distance(s,q.p)>42f))selected.Add(q.p);
            if(selected.Count>=11)break;
        }
        return selected;
    }
    static List<Vector2> Ridge(Terrain t,Bounds[] avoid,List<Vector2> coast) {
        var pts=new List<(Vector2 p,float score)>();
        var o=t.transform.position;var size=t.terrainData.size;
        for(float z=o.z+16;z<o.z+size.z-16;z+=12)
        for(float x=o.x+16;x<o.x+size.x-16;x+=12){
            float y=H(t,x,z),s=S(t,x,z);
            if(y<13||y>95||s<15||s>44||Near(avoid,x,z,15))continue;
            var q=new Vector2(x,z);if(coast.Any(c=>Vector2.Distance(q,c)<35))continue;
            pts.Add((q,s+.2f*y+R(0,3)));
        }
        var chosen=new List<Vector2>();
        foreach(var q in pts.OrderByDescending(v=>v.score)){
            if(chosen.All(c=>Vector2.Distance(c,q.p)>55f))chosen.Add(q.p);
            if(chosen.Count>=7)break;
        }
        return chosen;
    }
    static int Outcrop(Terrain t,Transform par,Vector2 c,int id,bool shoreline,List<string> log) {
        var norm=Gradient(t,c.x,c.y);if(norm.sqrMagnitude<.1f)norm=Vector2.up;
        var tangent=new Vector2(-norm.y,norm.x);
        var main=new GameObject((shoreline?"Coastal Cliff ":"Ridge Outcrop ")+id.ToString("00"));
        main.transform.SetParent(par,false);
        float angle=Mathf.Atan2(tangent.x,tangent.y)*Mathf.Rad2Deg;
        int count=0;
        for(int i=0;i<3;i++) {
            float along=(i-1)*R(7.0f,8.8f);
            var q=c+tangent*along+norm*R(1.5f,4.5f);
            float y=H(t,q.x,q.y);
            if(y<.6f)continue;
            var go=Spawn(0,main.transform,new Vector3(q.x,y,q.y),angle+R(-12f,12f),
                         shoreline?R(11f,15f):R(12f,18f),R(1.2f,2.2f));
            go.name="Cliff_Segment_"+i;count++;
        }
        for(int i=0;i<7;i++) {
            float along=R(-13f,13f),inland=R(-5f,5f);
            var q=c+tangent*along+norm*inland;
            float y=H(t,q.x,q.y);if(y<.65f)continue;
            int kind=i%3==0?1:(i%3==1?2:3);
            var go=Spawn(kind,main.transform,new Vector3(q.x,y,q.y),angle+R(-35f,35f),
                         kind==2?R(4f,7f):R(3f,6f),R(.2f,.7f));
            go.name="Talus_"+i;count++;
        }
        log.Add((shoreline?"coast":"ridge")+","+id+","+c.x.ToString("F1")+","+c.y.ToString("F1")+","+count);
        return count;
    }
    static int Understory(Terrain t,Transform group,Transform forest,Bounds[] avoid) {
        if(!forest)return 0;
        var mat=AssetDatabase.LoadAssetAtPath<Material>(ShrubMat);
        if(!mat||!mat.mainTexture)throw new Exception("Shrub texture missing");
        int n=0;
        var centers=forest.Cast<Transform>().Where(c=>c.GetComponentsInChildren<Renderer>(true).Length>0)
            .Select(c=>new Vector2(c.position.x,c.position.z))
            .Where(c=>H(t,c.x,c.y)>3f&&S(t,c.x,c.y)<20f)
            .OrderBy(c=>c.x*13+c.y*7).ToArray();
        foreach(var c in centers.Where((v,i)=>i%5==0).Take(25)) {
            for(int j=0;j<3;j++){
                float ang=R(0,Mathf.PI*2),r=R(3f,8f),x=c.x+r*Mathf.Cos(ang),z=c.y+r*Mathf.Sin(ang);
                if(H(t,x,z)<2.0f||S(t,x,z)>25f||Near(avoid,x,z,7f))continue;
                var src=AssetDatabase.LoadAssetAtPath<GameObject>(Shrub);if(!src)continue;
                var go=(GameObject)PrefabUtility.InstantiatePrefab(src);go.transform.SetParent(group,false);
                go.name="Understory_"+n;go.transform.position=new Vector3(x,H(t,x,z),z);
                go.transform.rotation=Quaternion.Euler(0,R(0,360),0);
                foreach(var rr in go.GetComponentsInChildren<Renderer>(true))
                    rr.sharedMaterials=Enumerable.Repeat(mat,Mathf.Max(1,rr.sharedMaterials.Length)).ToArray();
                var b=Bounds(go);float span=Mathf.Max(b.size.x,Mathf.Max(b.size.y,b.size.z));
                if(span>.01f)go.transform.localScale*=R(.45f,1.1f)/span;
                b=Bounds(go);go.transform.position+=Vector3.up*(H(t,x,z)-b.min.y-.02f);
                n++;
            }
        }
        return n;
    }
    [MenuItem("MMUnity/Dragon Isle/Build Natural Geology PREVIEW")]
    public static void Preview() {
        AssetDatabase.Refresh();
        var sc=EditorSceneManager.OpenScene(Scene,OpenSceneMode.Single);
        var root=sc.GetRootGameObjects().FirstOrDefault(g=>g.GetComponentInChildren<Terrain>(true));
        var t=root?root.GetComponentInChildren<Terrain>(true):null;
        var td=AssetDatabase.LoadAssetAtPath<TerrainData>(TerrainAsset);
        if(!t||!td)throw new Exception("PREVIEW terrain missing");
        t.terrainData=td;var collider=t.GetComponent<TerrainCollider>();if(collider)collider.terrainData=td;
        foreach(var name in new[]{Group,"Dragon Isle - Realism Additions","Dragon Isle - Full Realism"}){
            var old=root.transform.Find(name);if(old)UnityEngine.Object.DestroyImmediate(old.gameObject);
        }
        var landmarks=root.transform.Find("Landmarks - Reference");
        var avoid=landmarks?landmarks.GetComponentsInChildren<Renderer>(true).Where(r=>r.enabled)
            .Select(r=>r.bounds).ToArray():new Bounds[0];
        var group=new GameObject(Group);group.transform.SetParent(root.transform,false);
        var coasts=new GameObject("Coastal rock shelves");coasts.transform.SetParent(group.transform,false);
        var ridges=new GameObject("Connected interior formations");ridges.transform.SetParent(group.transform,false);
        var vegetation=new GameObject("Sheltered understory");vegetation.transform.SetParent(group.transform,false);
        var audit=new List<string>{"type,id,x,z,rocks"};
        rng=new System.Random(197811);
        var cs=Coast(t,avoid);int rockCount=0;
        for(int i=0;i<cs.Count;i++)rockCount+=Outcrop(t,coasts.transform,cs[i],i,true,audit);
        var rs=Ridge(t,avoid,cs);
        for(int i=0;i<rs.Count;i++)rockCount+=Outcrop(t,ridges.transform,rs[i],i,false,audit);
        int shrubs=Understory(t,vegetation.transform,root.transform.Find("Forest - Natural Sparse"),avoid);
        foreach(var r in group.GetComponentsInChildren<Renderer>(true).Where(r=>r.enabled)){
            if(r.sharedMaterials.Any(m=>!m||!m.shader||m.shader.name=="Hidden/InternalErrorShader"||m.mainTexture==null))
                throw new Exception("Textureless/incompatible object in "+r.name);
        }
        EditorSceneManager.MarkSceneDirty(sc);
        if(!EditorSceneManager.SaveScene(sc,Scene))throw new IOException("PREVIEW save failed");
        Directory.CreateDirectory("Validation/CactusDragonAudit");
        audit.Add("SUMMARY,coasts="+cs.Count+",ridges="+rs.Count+",rocks="+rockCount+",shrubs="+shrubs);
        File.WriteAllLines("Validation/CactusDragonAudit/dragon_geology_preview.csv",audit);
        Debug.Log("DRAGON_GEOLOGY_PREVIEW coasts="+cs.Count+" ridges="+rs.Count+" rocks="+rockCount+" shrubs="+shrubs);
    }
}
