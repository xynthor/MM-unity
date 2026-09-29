using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;

public static class MMUnifiedSeabedPass
{
    const int N=128;
    sealed class Z{public string key,scene;public Z(string k,string s){key=k;scene=s;}}
    static readonly Z[] Zones={
        new Z("NewSorpigal","Assets/Scenes/Regions/NewSorpigal.unity"),
        new Z("CastleIronfist","Assets/Scenes/Regions/CastleIronfist.unity"),
        new Z("MireOfTheDamned","Assets/Scenes/Regions/MireOfTheDamned.unity"),
        new Z("MistyIslands","Assets/Scenes/Regions/MistyIslands.unity"),
        new Z("EelInfestedWaters","Assets/Scenes/Regions/EelInfestedWaters.unity"),
        new Z("BootlegBay","Assets/Scenes/Regions/BootlegBay.unity"),
        new Z("SilverCove","Assets/Scenes/Regions/SilverCove.unity"),
        new Z("Dragonsand","Assets/Scenes/Regions/Dragonsand.unity"),
        new Z("HermitsIsle","Assets/Scenes/Regions/HermitsIsle.unity"),
        new Z("Blackshire","Assets/Scenes/Regions/Blackshire.unity"),
        new Z("ParadiseValley","Assets/Scenes/Regions/ParadiseValley.unity"),
        new Z("FreeHaven","Assets/Scenes/Regions/FreeHaven.unity"),
        new Z("SweetWater","Assets/Scenes/Regions/SweetWater.unity"),
        new Z("FrozenHighlands","Assets/Scenes/Regions/FrozenHighlands.unity"),
        new Z("Kriegspire","Assets/Scenes/Regions/Kriegspire.unity")};

    public static Material SharedMaterial()
    {
        string dir="Assets/Materials/RealisticWorld";Directory.CreateDirectory(dir);
        string path=dir+"/UnifiedWaterBottom.mat";
        var m=AssetDatabase.LoadAssetAtPath<Material>(path);
        var sh=Shader.Find("Standard");
        if(!m){m=new Material(sh);AssetDatabase.CreateAsset(m,path);}else m.shader=sh;
        var diff=AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Environment/PolyHaven/Textures/damp_sand/damp_sand_diff_1k.jpg");
        var nor=AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Environment/PolyHaven/Textures/damp_sand/damp_sand_nor_gl_1k.jpg");
        if(diff)m.SetTexture("_MainTex",diff);
        if(nor){m.SetTexture("_BumpMap",nor);m.EnableKeyword("_NORMALMAP");}
        m.color=new Color(.58f,.61f,.54f,1f);
        if(m.HasProperty("_Glossiness"))m.SetFloat("_Glossiness",.025f);
        if(m.HasProperty("_Metallic"))m.SetFloat("_Metallic",0f);
        EditorUtility.SetDirty(m);return m;
    }
    static bool WaterCell(byte[] tile,byte[] sem,int sx,int sy)
    {
        sx=Mathf.Clamp(sx,0,N-1);sy=Mathf.Clamp(sy,0,N-1);
        byte raw=tile[sy*N+sx],f=sem[raw];
        return (f&1)!=0||(f&2)!=0;
    }
    static float Y(Terrain t,float x,float z)
    {
        return t.SampleHeight(new Vector3(x,0,z))+t.transform.position.y+.025f;
    }
    static void AddQuad(List<Vector3> v,List<int> tr,List<Vector2> uv,
                        Vector3 a,Vector3 b,Vector3 c,Vector3 d)
    {
        int q=v.Count;v.Add(a);v.Add(b);v.Add(c);v.Add(d);
        tr.Add(q);tr.Add(q+2);tr.Add(q+1);tr.Add(q);tr.Add(q+3);tr.Add(q+2);
        uv.Add(new Vector2(a.x,a.z)*.08f);uv.Add(new Vector2(b.x,b.z)*.08f);
        uv.Add(new Vector2(c.x,c.z)*.08f);uv.Add(new Vector2(d.x,d.z)*.08f);
    }
    static Mesh BuildMesh(Terrain t,byte[] tile,byte[] sem,string zone)
    {
        var v=new List<Vector3>();var tr=new List<int>();var uv=new List<Vector2>();
        for(int sy=0;sy<N;sy++)for(int sx=0;sx<N;sx++)
        {
            if(!WaterCell(tile,sem,sx,sy))continue;
            float x0=(sx-64f)*4f,x1=x0+4f;
            float z1=(64f-sy)*4f,z0=z1-4f;
            var a=new Vector3(x0,Y(t,x0,z0),z0);
            var b=new Vector3(x1,Y(t,x1,z0),z0);
            var c=new Vector3(x1,Y(t,x1,z1),z1);
            var d=new Vector3(x0,Y(t,x0,z1),z1);
            AddQuad(v,tr,uv,a,b,c,d);
        }
        var m=new Mesh{name="UnifiedWaterBottom_"+zone};
        m.indexFormat=v.Count>65000?UnityEngine.Rendering.IndexFormat.UInt32:UnityEngine.Rendering.IndexFormat.UInt16;
        m.SetVertices(v);m.SetTriangles(tr,0);m.SetUVs(0,uv);m.RecalculateNormals();m.RecalculateBounds();
        return m;
    }
    static int ApplyZone(Z z,Material mat)
    {
        var sc=EditorSceneManager.OpenScene(z.scene,OpenSceneMode.Single);
        var roots=sc.GetRootGameObjects();
        var t=roots.SelectMany(g=>g.GetComponentsInChildren<Terrain>(true)).FirstOrDefault();
        if(!t)throw new Exception(z.key+" terrain missing");
        var regionRoot=roots.FirstOrDefault(g=>g.GetComponentInChildren<Terrain>(true)==t)??roots.First();
        var old=regionRoot.transform.Find("Unified Water Bottom");if(old)UnityEngine.Object.DestroyImmediate(old.gameObject);

        string d=$"Assets/World/{z.key}/Data";
        var tile=File.ReadAllBytes(d+"/tilemap_u8.bin");var sem=File.ReadAllBytes(d+"/tile_semantics_u8.bin");
        var mesh=BuildMesh(t,tile,sem,z.key);
        string mdir=$"Assets/World/{z.key}/Generated";Directory.CreateDirectory(mdir);
        string mp=$"{mdir}/UnifiedWaterBottom.asset";
        var prior=AssetDatabase.LoadAssetAtPath<Mesh>(mp);
        if(prior)AssetDatabase.DeleteAsset(mp);
        AssetDatabase.CreateAsset(mesh,mp);

        var go=new GameObject("Unified Water Bottom");go.transform.SetParent(regionRoot.transform,false);
        var mf=go.AddComponent<MeshFilter>();mf.sharedMesh=AssetDatabase.LoadAssetAtPath<Mesh>(mp);
        var mr=go.AddComponent<MeshRenderer>();mr.sharedMaterial=mat;mr.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;mr.receiveShadows=true;

        // Any existing seabed/bathymetry renderer uses the exact same material too.
        foreach(var r in roots.SelectMany(g=>g.GetComponentsInChildren<Renderer>(true)))
        {
            string n=r.gameObject.name.ToLowerInvariant();
            if(n.Contains("seabed")||n.Contains("bathymetry")||n.Contains("water bottom"))
                r.sharedMaterial=mat;
        }

        EditorSceneManager.MarkSceneDirty(sc);EditorSceneManager.SaveScene(sc,z.scene);
        Debug.Log($"UNIFIED_SEABED {z.key} quads={mesh.triangles.Length/6}");
        return mesh.triangles.Length/6;
    }
    [MenuItem("MMUnity/Realistic World/4 Unified Water Bottom")]
    public static void ApplyAll()
    {
        var mat=SharedMaterial();int total=0;
        foreach(var z in Zones)total+=ApplyZone(z,mat);

        const string linked="Assets/Scenes/World/Enroth.unity";
        if(File.Exists(linked))
        {
            var sc=EditorSceneManager.OpenScene(linked,OpenSceneMode.Single);
            foreach(var r in sc.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<Renderer>(true)))
            {
                string n=r.gameObject.name.ToLowerInvariant();
                if(n.Contains("seabed")||n.Contains("bathymetry")||n.Contains("water bottom"))r.sharedMaterial=mat;
            }
            EditorSceneManager.MarkSceneDirty(sc);EditorSceneManager.SaveScene(sc,linked);
        }
        AssetDatabase.SaveAssets();
        Debug.Log("UNIFIED_SEABED_ALL_DONE quads="+total+" sameMaterial=true");
    }
}
