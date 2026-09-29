using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;

public static class MMGlobalOceanPass
{
    const string ScenePath="Assets/Scenes/Enroth_Linked_OpenWorld.unity";
    const float InnerX=1280f, InnerZ=768f;
    const float OuterX=2304f, OuterZ=1792f;
    const float Step=32f;

    [MenuItem("MMUnity/Rebuild Global Ocean + Bathymetry")]
    public static void Apply()
    {
        var sc=EditorSceneManager.OpenScene(ScenePath,OpenSceneMode.Single);
        var root=sc.GetRootGameObjects().FirstOrDefault(g=>g.name.StartsWith("ENROTH - LINKED"));
        if(!root) throw new Exception("Linked Enroth root missing");
        var old=root.transform.Find("GLOBAL OCEAN + BATHYMETRY");
        if(old) UnityEngine.Object.DestroyImmediate(old.gameObject);
        var g=new GameObject("GLOBAL OCEAN + BATHYMETRY");
        g.transform.SetParent(root.transform,false);
        BuildObject(g.transform,"Global Ocean Surface",false,OceanMaterial());
        BuildObject(g.transform,"Global Bathymetric Seafloor",true,SeabedMaterial());
        EditorSceneManager.MarkSceneDirty(sc);
        if(!EditorSceneManager.SaveScene(sc,ScenePath)) throw new IOException("Could not save linked ocean scene");
        AssetDatabase.SaveAssets();
        Debug.Log("GLOBAL_OCEAN_DONE continuous=true depth=0.65..36m step=32m strips=false");
    }

    static void BuildObject(Transform parent,string name,bool bathy,Material mat)
    {
        var mesh=BuildMesh(bathy);
        var go=new GameObject(name); go.transform.SetParent(parent,false);
        go.AddComponent<MeshFilter>().sharedMesh=mesh;
        var mr=go.AddComponent<MeshRenderer>(); mr.sharedMaterial=mat;
        mr.shadowCastingMode=bathy?UnityEngine.Rendering.ShadowCastingMode.On:UnityEngine.Rendering.ShadowCastingMode.Off;
        mr.receiveShadows=bathy;
    }

    static Mesh BuildMesh(bool bathy)
    {
        int nx=Mathf.RoundToInt((OuterX*2f)/Step);
        int nz=Mathf.RoundToInt((OuterZ*2f)/Step);
        var v=new List<Vector3>(nx*nz*4);
        var uv=new List<Vector2>(nx*nz*4);
        var tr=new List<int>(nx*nz*6);
        for(int iz=0;iz<nz;iz++) for(int ix=0;ix<nx;ix++)
        {
            float x0=-OuterX+ix*Step, x1=x0+Step;
            float z0=-OuterZ+iz*Step, z1=z0+Step;
            float cx=(x0+x1)*.5f, cz=(z0+z1)*.5f;
            if(Mathf.Abs(cx)<InnerX && Mathf.Abs(cz)<InnerZ) continue;
            int q=v.Count;
            v.Add(new Vector3(x0,Y(x0,z0,bathy),z0));
            v.Add(new Vector3(x1,Y(x1,z0,bathy),z0));
            v.Add(new Vector3(x1,Y(x1,z1,bathy),z1));
            v.Add(new Vector3(x0,Y(x0,z1,bathy),z1));

            uv.Add(new Vector2(x0/20f,z0/20f)); uv.Add(new Vector2(x1/20f,z0/20f));
            uv.Add(new Vector2(x1/20f,z1/20f)); uv.Add(new Vector2(x0/20f,z1/20f));
            tr.Add(q);tr.Add(q+2);tr.Add(q+1);tr.Add(q);tr.Add(q+3);tr.Add(q+2);
        }
        Directory.CreateDirectory("Assets/World/GlobalOcean");
        string path=bathy?"Assets/World/GlobalOcean/GlobalBathymetry.asset":"Assets/World/GlobalOcean/GlobalOceanSurface.asset";
        var m=AssetDatabase.LoadAssetAtPath<Mesh>(path);
        if(!m){m=new Mesh();AssetDatabase.CreateAsset(m,path);} else m.Clear();
        m.indexFormat=UnityEngine.Rendering.IndexFormat.UInt32;
        m.SetVertices(v);m.SetTriangles(tr,0);m.SetUVs(0,uv);m.RecalculateNormals();m.RecalculateBounds();
        EditorUtility.SetDirty(m);return m;
    }

    static float Y(float x,float z,bool bathy)
    {
        if(!bathy)return .12f;
        float dx=Mathf.Max(0f,Mathf.Abs(x)-InnerX), dz=Mathf.Max(0f,Mathf.Abs(z)-InnerZ);
        float d=Mathf.Sqrt(dx*dx+dz*dz);
        float t=Mathf.SmoothStep(0f,1f,Mathf.Clamp01(d/900f));
        float n=(Mathf.PerlinNoise(x*.0017f+13.7f,z*.0017f+4.2f)-.5f)*2.6f*t;
        return Mathf.Lerp(-.65f,-36f,t)+n;
    }

    static Material OceanMaterial()
    {
        return MMUnifiedWorldWaterPass.SharedWaterMaterial();
    }

    static Material SeabedMaterial()
    {
        return MMUnifiedSeabedPass.SharedMaterial();
    }

}
