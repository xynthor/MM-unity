using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;

public static class MMDragonSouthInlandOutcrops20260930
{
    const string Root="Assets/World/WorldExtensions/Generated/DragonSouthInlandOutcrops20260930";
    const string Backup="Backups/BeforeDragonSouthOutcrops_20260930/Enroth.unity";
    const string Name="Dragon South Reference Inland Outcrops 20260930";
    const string BiomePath="Validation/EdgeGrid20260923/ReferenceBiomeCandidates_20260925/DragonIsle_South_ReferenceBiomes.png";
    const string LandPath="Validation/EdgeGrid20260923/ReferenceMasks/DragonIsle_South.png";
    const int Target=32;
    struct C { public float x,z,y,yaw; public Vector3 scale,normal; public uint h; public int variant,cell; }

    static uint H(int a,int b){uint v=(uint)(a*73856093 ^ b*19349663);v^=v>>16;v*=0x7feb352d;v^=v>>15;v*=0x846ca68b;v^=v>>16;return v;}
    static Color32[] Load(string p){var t=new Texture2D(2,2,TextureFormat.RGBA32,false,true);if(!File.Exists(p)||!t.LoadImage(File.ReadAllBytes(p))||t.width!=513||t.height!=513)throw new Exception("Bad raster "+p);var a=t.GetPixels32();UnityEngine.Object.DestroyImmediate(t);return a;}
    static Terrain Dragon(Scene s)=>s.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<Terrain>(true)).Single(t=>t.terrainData&&t.terrainData.name.IndexOf("DragonIsleSouthTerrain",StringComparison.OrdinalIgnoreCase)>=0);
    static float Height(Terrain t,float x,float z)=>t.SampleHeight(t.transform.position+new Vector3(x,0,z))+t.transform.position.y;
    static Mesh Pick(string path,string suffix){var m=AssetDatabase.LoadAllAssetsAtPath(path).OfType<Mesh>().FirstOrDefault(x=>x.name.EndsWith(suffix,StringComparison.OrdinalIgnoreCase));if(!m)throw new Exception("Missing "+path+" "+suffix);return m;}

    public static void Apply()
    {
        if(!File.Exists(Backup))throw new Exception("Backup missing");
        var s=SceneManager.GetActiveScene();if(s.path!="Assets/Scenes/World/Enroth.unity"||s.isDirty||EditorApplication.isPlaying)throw new Exception("Saved linked Edit Mode required");
        if(s.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<Transform>(true)).Any(t=>t.name==Name))throw new Exception("Already applied");
        if(AssetDatabase.IsValidFolder(Root))throw new Exception("Generated folder exists");

        var t=Dragon(s);var td=t.terrainData;var biome=Load(BiomePath);var land=Load(LandPath);var cands=new List<C>();
        int maskSkip=0,waterSkip=0,slopeSkip=0;
        for(int z0=230;z0<=490;z0+=8)
        for(int x0=20;x0<=320;x0+=8)
        {
            uint h=H(x0,z0);float x=Mathf.Clamp(x0+((h&1023)/1023f-.5f)*5.5f,14,324),z=Mathf.Clamp(z0+(((h>>10)&1023)/1023f-.5f)*5.5f,224,498);
            int ix=Mathf.Clamp(Mathf.RoundToInt(x),0,512),iz=Mathf.Clamp(Mathf.RoundToInt(z),0,512);var p=biome[iz*513+ix];var lm=land[iz*513+ix];
            float rock=p.r/255f,ochre=p.b/255f;
            if(lm.r<145||Mathf.Max(rock,ochre)<.25f){maskSkip++;continue;}
            float wh=Height(t,x,z);if(wh<1.1f){waterSkip++;continue;}
            float mn=Mathf.Min(Mathf.Min(Height(t,Mathf.Max(0,x-7),z),Height(t,Mathf.Min(512,x+7),z)),Mathf.Min(Height(t,x,Mathf.Max(0,z-7)),Height(t,x,Mathf.Min(512,z+7))));
            if(mn<.35f){waterSkip++;continue;}
            float slope=td.GetSteepness(x/512f,z/512f);if(slope<12f||slope>50f){slopeSkip++;continue;}
            float chance=Mathf.Lerp(.28f,.78f,Mathf.InverseLerp(.25f,.95f,Mathf.Max(rock,ochre)));
            if((h%1000)/1000f>chance)continue;
            float ss=Mathf.Lerp(3.4f,7.2f,((h>>20)&255)/255f),wide=Mathf.Lerp(.8f,1.35f,((h>>12)&255)/255f),tall=Mathf.Lerp(.7f,1.25f,((h>>4)&255)/255f);
            int cell=(x>=170?1:0)+(z>=360?2:0);
            cands.Add(new C{x=x,z=z,y=wh-t.transform.position.y-.22f*ss,yaw=(h>>7)%360,scale=new Vector3(ss*wide,ss*tall,ss),normal=td.GetInterpolatedNormal(x/512f,z/512f),h=h,variant=(int)((h>>18)&1),cell=cell});
        }
        var chosen=cands.OrderBy(c=>c.h^0xc2b2ae35u).Take(Target).ToArray();if(chosen.Length<20)throw new Exception("Too few outcrops "+chosen.Length);

        AssetDatabase.CreateFolder("Assets/World/WorldExtensions/Generated","DragonSouthInlandOutcrops20260930");
        var meshes=new[]{Pick("Assets/EnvironmentAssets/UnitySamples/Rock_A_01.fbx","LOD04"),Pick("Assets/EnvironmentAssets/UnitySamples/Rock_A_02.fbx","LOD03")};
        var src=AssetDatabase.LoadAssetAtPath<Material>("Assets/World/WorldExtensions/Generated/DragonSouthRockDressing20260930/DragonSouthShoreRock.mat");
        if(!src)throw new Exception("Source Dragon rock material missing");
        var mat=new Material(src){name="Dragon South Inland Outcrop"};if(mat.HasProperty("_Color"))mat.SetColor("_Color",new Color(.58f,.56f,.49f,1));mat.enableInstancing=true;AssetDatabase.CreateAsset(mat,Root+"/DragonSouthInlandOutcrop.mat");
        var root=new GameObject(Name);root.transform.SetParent(t.transform.parent,false);root.transform.position=t.transform.position;
        int cells=0;long tris=0;
        for(int cell=0;cell<4;cell++){
            var list=chosen.Where(c=>c.cell==cell).ToArray();if(list.Length==0)continue;var comb=new List<CombineInstance>();
            foreach(var c in list){var up=Vector3.Slerp(Vector3.up,c.normal,.38f).normalized;var rot=Quaternion.FromToRotation(Vector3.up,up)*Quaternion.Euler(0,c.yaw,0);comb.Add(new CombineInstance{mesh=meshes[c.variant],transform=Matrix4x4.TRS(new Vector3(c.x,c.y,c.z),rot,c.scale)});tris+=Enumerable.Range(0,meshes[c.variant].subMeshCount).Sum(i=>(long)meshes[c.variant].GetIndexCount(i)/3);}
            var m=new Mesh{indexFormat=IndexFormat.UInt32,name="Dragon South Inland Outcrops Cell "+cell};m.CombineMeshes(comb.ToArray(),true,true,false);m.RecalculateBounds();AssetDatabase.CreateAsset(m,Root+"/Cell_"+cell+".asset");
            var go=new GameObject("Inland Outcrops Cell "+cell);go.transform.SetParent(root.transform,false);go.AddComponent<MeshFilter>().sharedMesh=m;var mr=go.AddComponent<MeshRenderer>();mr.sharedMaterial=mat;mr.shadowCastingMode=ShadowCastingMode.On;mr.receiveShadows=true;cells++;
        }
        EditorSceneManager.MarkSceneDirty(s);EditorSceneManager.SaveScene(s);AssetDatabase.SaveAssets();
        File.WriteAllText("Validation/EdgeGrid20260923/dragon_south_inland_outcrops_20260930.txt","PASS rocks="+chosen.Length+" candidates="+cands.Count+" cells="+cells+" sourceTriangles="+tris+" maskSkip="+maskSkip+" waterSkip="+waterSkip+" slopeSkip="+slopeSkip+" terrainWrites=0 heightWrites=0 alphamapWrites=0 terrainLayerWrites=0 canonicalWrites=0\n");
        Debug.Log("Dragon South inland outcrops="+chosen.Length+" cells="+cells);
    }
}
