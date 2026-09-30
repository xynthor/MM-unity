using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;

public static class MMParadiseWestShoreRocks20260930
{
    const string Root="Assets/World/WorldExtensions/Generated/ParadiseWestShoreRocks20260930";
    const string Backup="Backups/BeforeParadiseWestShoreRocks_20260930/Enroth.unity";
    const string Name="Paradise West Reference Shore Rocks 20260930";
    const int Target=40;
    struct C { public float x,z,y,yaw; public Vector3 scale,normal; public uint h; public int variant,cell; }

    static uint H(int a,int b){uint v=(uint)(a*73856093 ^ b*19349663);v^=v>>16;v*=0x7feb352d;v^=v>>15;v*=0x846ca68b;v^=v>>16;return v;}
    static Terrain Paradise(Scene s)=>s.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<Terrain>(true)).Single(t=>t.terrainData&&t.terrainData.name.IndexOf("ParadiseValley_WestTerrain",StringComparison.OrdinalIgnoreCase)>=0);
    static float Height(Terrain t,float x,float z)=>t.SampleHeight(t.transform.position+new Vector3(Mathf.Clamp(x,0,512),0,Mathf.Clamp(z,0,512)))+t.transform.position.y;
    static Mesh Pick(string p,string suffix){var m=AssetDatabase.LoadAllAssetsAtPath(p).OfType<Mesh>().FirstOrDefault(x=>x.name.EndsWith(suffix,StringComparison.OrdinalIgnoreCase));if(!m)throw new Exception("Missing "+p+" "+suffix);return m;}

    public static void Apply()
    {
        if(!File.Exists(Backup))throw new Exception("Backup missing");
        var s=SceneManager.GetActiveScene();if(s.path!="Assets/Scenes/World/Enroth.unity"||s.isDirty||EditorApplication.isPlaying)throw new Exception("Saved linked Edit Mode required");
        if(s.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<Transform>(true)).Any(t=>t.name==Name))throw new Exception("Already applied");
        if(AssetDatabase.IsValidFolder(Root))throw new Exception("Generated folder exists");

        var t=Paradise(s);var td=t.terrainData;var cands=new List<C>();
        int waterSkip=0,slopeSkip=0;
        for(int gz=2;gz<126;gz++)
        for(int gx=2;gx<126;gx++)
        {
            uint h=H(gx,gz);float x=Mathf.Clamp(gx*4f+((h&1023)/1023f-.5f)*3f,6,506),z=Mathf.Clamp(gz*4f+(((h>>10)&1023)/1023f-.5f)*3f,6,506);
            float wh=Height(t,x,z);if(wh<.28f||wh>4.4f){waterSkip++;continue;}
            float r=6f;float mn=Mathf.Min(Mathf.Min(Height(t,x+r,z),Height(t,x-r,z)),Mathf.Min(Height(t,x,z+r),Height(t,x,z-r)));
            if(mn>.16f){waterSkip++;continue;}
            float slope=td.GetSteepness(x/512f,z/512f);if(slope<8f){slopeSkip++;continue;}
            float cluster=(H(gx/10,gz/10)%1000)/1000f;if(cluster>.42f)continue;
            if((h%1000)/1000f>Mathf.Lerp(.28f,.55f,Mathf.InverseLerp(8f,35f,slope)))continue;
            float ss=Mathf.Lerp(3.0f,5.8f,((h>>20)&255)/255f),wide=Mathf.Lerp(.8f,1.25f,((h>>12)&255)/255f),tall=Mathf.Lerp(.72f,1.08f,((h>>4)&255)/255f);
            int cell=(x>=256?1:0)+(z>=256?2:0);
            cands.Add(new C{x=x,z=z,y=wh-t.transform.position.y-.15f*ss,yaw=(h>>7)%360,scale=new Vector3(ss*wide,ss*tall,ss),normal=td.GetInterpolatedNormal(x/512f,z/512f),h=h,variant=(int)((h>>18)&1),cell=cell});
        }
        var chosen=cands.OrderBy(c=>c.h^0x27d4eb2du).Take(Target).ToArray();if(chosen.Length<28)throw new Exception("Too few shore rocks "+chosen.Length);

        AssetDatabase.CreateFolder("Assets/World/WorldExtensions/Generated","ParadiseWestShoreRocks20260930");
        var meshes=new[]{Pick("Assets/EnvironmentAssets/UnitySamples/Rock_A_01.fbx","LOD04"),Pick("Assets/EnvironmentAssets/UnitySamples/Rock_A_02.fbx","LOD03")};
        var src=AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/ParadiseValley/Environment/Rock.mat");if(!src)throw new Exception("Paradise rock material missing");
        var mat=new Material(src){name="Paradise West Shore Rock"};if(mat.HasProperty("_Color"))mat.SetColor("_Color",new Color(.62f,.60f,.54f,1));if(mat.HasProperty("_Glossiness"))mat.SetFloat("_Glossiness",.05f);mat.enableInstancing=true;AssetDatabase.CreateAsset(mat,Root+"/ParadiseWestShoreRock.mat");

        var root=new GameObject(Name);root.transform.SetParent(t.transform.parent,false);root.transform.position=t.transform.position;int cells=0;long tris=0;
        for(int cell=0;cell<4;cell++){var list=chosen.Where(c=>c.cell==cell).ToArray();if(list.Length==0)continue;var comb=new List<CombineInstance>();
            foreach(var c in list){var up=Vector3.Slerp(Vector3.up,c.normal,.28f).normalized;var rot=Quaternion.FromToRotation(Vector3.up,up)*Quaternion.Euler(0,c.yaw,0);comb.Add(new CombineInstance{mesh=meshes[c.variant],transform=Matrix4x4.TRS(new Vector3(c.x,c.y,c.z),rot,c.scale)});tris+=Enumerable.Range(0,meshes[c.variant].subMeshCount).Sum(i=>(long)meshes[c.variant].GetIndexCount(i)/3);}
            var m=new Mesh{indexFormat=IndexFormat.UInt32,name="Paradise West Shore Rocks Cell "+cell};m.CombineMeshes(comb.ToArray(),true,true,false);m.RecalculateBounds();AssetDatabase.CreateAsset(m,Root+"/Cell_"+cell+".asset");
            var go=new GameObject("Shore Rocks Cell "+cell);go.transform.SetParent(root.transform,false);go.AddComponent<MeshFilter>().sharedMesh=m;var mr=go.AddComponent<MeshRenderer>();mr.sharedMaterial=mat;mr.shadowCastingMode=ShadowCastingMode.On;mr.receiveShadows=true;cells++;}
        EditorSceneManager.MarkSceneDirty(s);EditorSceneManager.SaveScene(s);AssetDatabase.SaveAssets();
        File.WriteAllText("Validation/EdgeGrid20260923/paradise_west_shore_rocks_20260930.txt","PASS rocks="+chosen.Length+" candidates="+cands.Count+" cells="+cells+" sourceTriangles="+tris+" waterSkip="+waterSkip+" slopeSkip="+slopeSkip+" terrainWrites=0 heightWrites=0 alphamapWrites=0 terrainLayerWrites=0 canonicalWrites=0\n");
        Debug.Log("Paradise West shore rocks="+chosen.Length+" cells="+cells);
    }
}
