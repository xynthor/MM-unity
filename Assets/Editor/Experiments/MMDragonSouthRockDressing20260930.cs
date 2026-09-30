using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;

public static class MMDragonSouthRockDressing20260930
{
    const string Root="Assets/World/WorldExtensions/Generated/DragonSouthRockDressing20260930";
    const string Backup="Backups/BeforeDragonSouthRockDressing_20260930/Enroth.unity";
    const string Name="Dragon South Reference Shore Rocks 20260930";
    const int Target=72;

    struct C { public float x,z,y,yaw; public Vector3 scale,normal; public uint h; public int variant,cell; }

    static uint H(int a,int b)
    {
        uint v=(uint)(a*73856093 ^ b*19349663);
        v^=v>>16;v*=0x7feb352d;v^=v>>15;v*=0x846ca68b;v^=v>>16;
        return v;
    }

    static Terrain DragonSouth(Scene scene)
    {
        return scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<Terrain>(true))
            .Single(t=>t.terrainData && t.terrainData.name.IndexOf("DragonIsleSouthTerrain",StringComparison.OrdinalIgnoreCase)>=0);
    }

    static float WorldHeight(Terrain t,float x,float z)
    {
        var wp=t.transform.position+new Vector3(Mathf.Clamp(x,0,512),0,Mathf.Clamp(z,0,512));
        return t.SampleHeight(wp)+t.transform.position.y;
    }

    static Mesh Pick(string p,string suffix)
    {
        var m=AssetDatabase.LoadAllAssetsAtPath(p).OfType<Mesh>().FirstOrDefault(x=>x.name.EndsWith(suffix,StringComparison.OrdinalIgnoreCase));
        if(!m)throw new Exception("Missing rock mesh "+p+" "+suffix);
        return m;
    }

    static Material MaterialFrom(string p)
    {
        var go=AssetDatabase.LoadAssetAtPath<GameObject>(p);
        var r=go?go.GetComponentInChildren<Renderer>(true):null;
        return r&&r.sharedMaterial?r.sharedMaterial:null;
    }

    public static void Apply()
    {
        if(!File.Exists(Backup))throw new Exception("Backup missing");
        var scene=SceneManager.GetActiveScene();
        if(scene.path!="Assets/Scenes/World/Enroth.unity"||scene.isDirty||EditorApplication.isPlaying)
            throw new Exception("Saved linked Edit Mode scene required");
        if(scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<Transform>(true)).Any(t=>t.name==Name))
            throw new Exception("Already applied");
        if(AssetDatabase.IsValidFolder(Root))throw new Exception("Generated folder already exists");

        var t=DragonSouth(scene);
        var td=t.terrainData;
        var candidates=new List<C>();
        const float step=4f, water=.10f;

        for(int gz=2;gz<126;gz++)
        for(int gx=2;gx<126;gx++)
        {
            uint h=H(gx,gz);
            float x=Mathf.Clamp(gx*step+((h&1023)/1023f-.5f)*3.0f,6,506);
            float z=Mathf.Clamp(gz*step+(((h>>10)&1023)/1023f-.5f)*3.0f,6,506);
            float wh=WorldHeight(t,x,z);
            if(wh<.32f||wh>3.5f)continue;

            float r=6.0f;
            float mn=Mathf.Min(Mathf.Min(WorldHeight(t,x+r,z),WorldHeight(t,x-r,z)),
                               Mathf.Min(WorldHeight(t,x,z+r),WorldHeight(t,x,z-r)));
            if(mn>water+.045f)continue;

            float slope=td.GetSteepness(x/512f,z/512f);
            if(slope<10f)continue;

            // Sparse deterministic rock punctuation, not a continuous seawall.
            if((h%1000)/1000f > Mathf.Lerp(.18f,.45f,Mathf.InverseLerp(10f,38f,slope)))continue;

            float s=Mathf.Lerp(2.2f,4.2f,((h>>20)&255)/255f);
            float wide=Mathf.Lerp(.82f,1.22f,((h>>12)&255)/255f);
            float tall=Mathf.Lerp(.75f,1.08f,((h>>4)&255)/255f);
            int cell=(x>=256?1:0)+(z>=256?2:0);
            candidates.Add(new C{
                x=x,z=z,y=wh-t.transform.position.y-.14f*s,
                scale=new Vector3(s*wide,s*tall,s),
                yaw=(h>>7)%360,normal=td.GetInterpolatedNormal(x/512f,z/512f),
                h=h,variant=(int)((h>>18)&1),cell=cell
            });
        }

        var chosen=candidates.OrderBy(c=>c.h^0x9e3779b9u).Take(Target).ToArray();
        if(chosen.Length<48)throw new Exception("Too few shore candidates "+chosen.Length);

        AssetDatabase.CreateFolder("Assets/World/WorldExtensions/Generated","DragonSouthRockDressing20260930");
        string a="Assets/EnvironmentAssets/UnitySamples/Rock_A_01.fbx";
        string b="Assets/EnvironmentAssets/UnitySamples/Rock_A_02.fbx";
        var meshes=new[]{Pick(a,"LOD04"),Pick(b,"LOD03")};
        var srcMat=AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/ParadiseValley/Environment/Rock.mat");
        if(!srcMat)throw new Exception("Rock material missing");

        string matPath=Root+"/DragonSouthShoreRock.mat";
        var mat=new Material(srcMat){name="Dragon South Shore Rock"};
        if(mat.HasProperty("_Color"))mat.SetColor("_Color",new Color(.68f,.64f,.56f,1));
        if(mat.HasProperty("_Glossiness"))mat.SetFloat("_Glossiness",.06f);
        mat.enableInstancing=true;
        AssetDatabase.CreateAsset(mat,matPath);

        var root=new GameObject(Name);
        root.transform.SetParent(t.transform.parent,false);
        root.transform.position=t.transform.position;

        int built=0; long tris=0;
        for(int cell=0;cell<4;cell++)
        {
            var list=chosen.Where(c=>c.cell==cell).ToArray();
            if(list.Length==0)continue;
            var combines=new List<CombineInstance>();
            foreach(var c in list)
            {
                var up=Vector3.Slerp(Vector3.up,c.normal,.28f).normalized;
                var rot=Quaternion.FromToRotation(Vector3.up,up)*Quaternion.Euler(0,c.yaw,0);
                combines.Add(new CombineInstance{mesh=meshes[c.variant],transform=Matrix4x4.TRS(new Vector3(c.x,c.y,c.z),rot,c.scale)});
                tris+=Enumerable.Range(0,meshes[c.variant].subMeshCount).Sum(i=>(long)meshes[c.variant].GetIndexCount(i)/3);
            }
            var m=new Mesh{indexFormat=IndexFormat.UInt32,name="Dragon South Shore Rocks Cell "+cell};
            m.CombineMeshes(combines.ToArray(),true,true,false);m.RecalculateBounds();
            AssetDatabase.CreateAsset(m,Root+"/Cell_"+cell+".asset");
            var go=new GameObject("Shore Rocks Cell "+cell);go.transform.SetParent(root.transform,false);
            go.AddComponent<MeshFilter>().sharedMesh=m;
            var mr=go.AddComponent<MeshRenderer>();mr.sharedMaterial=mat;mr.shadowCastingMode=ShadowCastingMode.On;mr.receiveShadows=true;
            built++;
        }

        EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);AssetDatabase.SaveAssets();
        File.WriteAllText("Validation/EdgeGrid20260923/dragon_south_rock_dressing_20260930.txt",
            "PASS rocks="+chosen.Length+" candidates="+candidates.Count+" cells="+built+" sourceTriangles="+tris+
            " terrainWrites=0 heightWrites=0 alphamapWrites=0 terrainLayerWrites=0 canonicalWrites=0\n");
        Debug.Log("Dragon South shore rocks="+chosen.Length+" cells="+built+" sourceTris="+tris);
    }
}
