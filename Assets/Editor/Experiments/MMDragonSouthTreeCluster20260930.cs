using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;

public static class MMDragonSouthTreeCluster20260930
{
    const string Backup="Backups/BeforeDragonSouthTreeCluster_20260930/Enroth.unity";
    const string Name="Dragon South Reference Tree Cluster 20260930";
    const string BiomePath="Validation/EdgeGrid20260923/ReferenceBiomeCandidates_20260925/DragonIsle_South_ReferenceBiomes.png";
    const string LandPath="Validation/EdgeGrid20260923/ReferenceMasks/DragonIsle_South.png";
    const int Target=32;

    struct C { public float x,z,y,yaw,scale; public uint h; public int variant; }

    static uint H(int a,int b)
    {
        uint v=(uint)(a*73856093 ^ b*19349663);
        v^=v>>16;v*=0x7feb352d;v^=v>>15;v*=0x846ca68b;v^=v>>16;
        return v;
    }

    static Color32[] Load(string p)
    {
        var t=new Texture2D(2,2,TextureFormat.RGBA32,false,true);
        if(!File.Exists(p)||!t.LoadImage(File.ReadAllBytes(p))||t.width!=513||t.height!=513)
            throw new Exception("Bad reference raster "+p);
        var a=t.GetPixels32();UnityEngine.Object.DestroyImmediate(t);return a;
    }

    static Terrain DragonSouth(Scene scene)
    {
        return scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<Terrain>(true))
            .Single(t=>t.terrainData&&t.terrainData.name.IndexOf("DragonIsleSouthTerrain",StringComparison.OrdinalIgnoreCase)>=0);
    }

    static float Height(Terrain t,float x,float z)
    {
        return t.SampleHeight(t.transform.position+new Vector3(x,0,z))+t.transform.position.y;
    }

    public static void Apply()
    {
        if(!File.Exists(Backup))throw new Exception("Backup missing");
        var scene=SceneManager.GetActiveScene();
        if(scene.path!="Assets/Scenes/World/Enroth.unity"||scene.isDirty||EditorApplication.isPlaying)
            throw new Exception("Saved linked Edit Mode required");
        if(scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<Transform>(true)).Any(t=>t.name==Name))
            throw new Exception("Already applied");

        var terrain=DragonSouth(scene);var region=terrain.transform.parent;var td=terrain.terrainData;
        var biome=Load(BiomePath);var land=Load(LandPath);

        var templates=region.GetComponentsInChildren<LODGroup>(true)
            .Where(g=>g.enabled&&g.gameObject.activeInHierarchy)
            .Where(g=>g.GetComponentsInChildren<MeshFilter>(true).Any(f=>f.sharedMesh&&AssetDatabase.GetAssetPath(f.sharedMesh).IndexOf("searsia_lucida",StringComparison.OrdinalIgnoreCase)>=0))
            .GroupBy(g=>{
                var f=g.GetComponentsInChildren<MeshFilter>(true).FirstOrDefault(x=>x.sharedMesh);
                return f&&f.sharedMesh?f.sharedMesh.name:g.name;
            })
            .Select(g=>g.First()).Take(7).ToArray();
        if(templates.Length<3)throw new Exception("Too few Searsia templates "+templates.Length);

        var existing=region.GetComponentsInChildren<LODGroup>(true)
            .Where(g=>g.enabled&&g.gameObject.activeInHierarchy)
            .Where(g=>g.GetComponentsInChildren<MeshFilter>(true).Any(f=>f.sharedMesh&&AssetDatabase.GetAssetPath(f.sharedMesh).IndexOf("searsia_lucida",StringComparison.OrdinalIgnoreCase)>=0))
            .Select(g=>g.transform.position).ToArray();

        var candidates=new List<C>();
        int maskSkip=0,waterSkip=0,slopeSkip=0,nearSkip=0;
        for(int z0=230;z0<=490;z0+=10)
        for(int x0=22;x0<=316;x0+=10)
        {
            uint h=H(x0,z0);
            float x=Mathf.Clamp(x0+((h&1023)/1023f-.5f)*6.5f,16,322);
            float z=Mathf.Clamp(z0+(((h>>10)&1023)/1023f-.5f)*6.5f,224,496);
            int ix=Mathf.Clamp(Mathf.RoundToInt(x),0,512),iz=Mathf.Clamp(Mathf.RoundToInt(z),0,512);
            var p=biome[iz*513+ix];var lm=land[iz*513+ix];
            float pf=p.g/255f, dry=Mathf.Max(p.r,p.b)/255f;
            if(lm.r<145||pf<.34f||pf<dry*.72f){maskSkip++;continue;}

            float wh=Height(terrain,x,z);
            if(wh<1.0f){waterSkip++;continue;}
            float mn=Mathf.Min(Mathf.Min(Height(terrain,Mathf.Max(0,x-8),z),Height(terrain,Mathf.Min(512,x+8),z)),
                               Mathf.Min(Height(terrain,x,Mathf.Max(0,z-8)),Height(terrain,x,Mathf.Min(512,z+8))));
            if(mn<.35f){waterSkip++;continue;}
            float slope=td.GetSteepness(x/512f,z/512f);
            if(slope>22f){slopeSkip++;continue;}

            var world=terrain.transform.position+new Vector3(x,0,z);
            if(existing.Any(e=>(new Vector2(e.x-world.x,e.z-world.z)).sqrMagnitude<81f)){nearSkip++;continue;}

            float chance=Mathf.Lerp(.28f,.82f,Mathf.InverseLerp(.34f,.86f,pf));
            if((h%1000)/1000f>chance)continue;
            candidates.Add(new C{x=x,z=z,y=wh,yaw=(h>>7)%360,scale=Mathf.Lerp(.82f,1.18f,((h>>20)&255)/255f),h=h,variant=(int)((h>>17)%(uint)templates.Length)});
        }

        var chosen=candidates.OrderBy(c=>c.h^0x9e3779b9u).Take(Target).ToArray();
        if(chosen.Length<20)throw new Exception("Too few tree candidates "+chosen.Length);

        var root=new GameObject(Name);root.transform.SetParent(region,false);
        int added=0;
        foreach(var c in chosen)
        {
            var src=templates[c.variant].gameObject;
            var go=UnityEngine.Object.Instantiate(src);
            go.name="Reference Searsia "+added.ToString("00");
            go.transform.SetParent(root.transform,true);
            go.transform.position=new Vector3(terrain.transform.position.x+c.x,c.y,terrain.transform.position.z+c.z);
            go.transform.rotation=Quaternion.Euler(0,c.yaw,0);
            float baseScale=src.transform.lossyScale.x;
            go.transform.localScale=Vector3.one*(baseScale*c.scale);
            added++;
        }

        EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);AssetDatabase.SaveAssets();
        File.WriteAllText("Validation/EdgeGrid20260923/dragon_south_tree_cluster_20260930.txt",
            "PASS trees="+added+" candidates="+candidates.Count+" templates="+templates.Length+
            " maskSkip="+maskSkip+" waterSkip="+waterSkip+" slopeSkip="+slopeSkip+" nearSkip="+nearSkip+
            " terrainWrites=0 heightWrites=0 alphamapWrites=0 terrainLayerWrites=0 canonicalWrites=0\n");
        Debug.Log("Dragon South reference trees="+added+" candidates="+candidates.Count);
    }
}
