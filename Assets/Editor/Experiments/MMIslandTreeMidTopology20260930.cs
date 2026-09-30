using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;

public static class MMIslandTreeMidTopology20260930
{
    const string Root="Assets/World/WorldExtensions/Generated/LinkedIslandTreeMidTopology20260930";
    const string Backup="Backups/BeforeIslandTreeMidReduction_20260930/manifest.json";
    static readonly string[] OldMid={
        "Assets/World/WorldExtensions/Generated/LinkedTreeLOD/ed685f268ccd6ae48a6511c98bb3bde5_4300000_150.asset",
        "Assets/World/WorldExtensions/Generated/LinkedTreeLOD/1852c3ef7d28336498e5559ecf3c1c5f_4300000_150.asset",
        "Assets/World/WorldExtensions/Generated/LinkedTreeLOD/5b6bf80a26085b049830e90137c07b49_4300000_150.asset"};
    static readonly string[] Source={
        "Assets/Optimization/GeneratedMeshes/bd0d6a6b_island_tree_01_LOD0_q50.asset",
        "Assets/Optimization/GeneratedMeshes/cc6eeadc_island_tree_02_LOD0_q50.asset",
        "Assets/Optimization/GeneratedMeshes/d37345cb_island_tree_03_LOD0_q50.asset"};

    static uint Hash(int n){uint v=(uint)n;v^=v>>16;v*=0x7feb352d;v^=v>>15;v*=0x846ca68b;return v^(v>>16);}
    static Dictionary<int,List<int>> Components(Mesh source,int slot)
    {
        var indices=source.GetTriangles(slot);var parent=new int[source.vertexCount];for(int i=0;i<parent.Length;i++)parent[i]=i;
        int Find(int v){while(parent[v]!=v){parent[v]=parent[parent[v]];v=parent[v];}return v;}
        for(int i=0;i<indices.Length;i+=3){int r=Find(indices[i]);parent[Find(indices[i+1])]=r;parent[Find(indices[i+2])]=r;}
        var groups=new Dictionary<int,List<int>>();
        for(int i=0;i<indices.Length;i+=3){int r=Find(indices[i]);if(!groups.TryGetValue(r,out var list)){list=new List<int>();groups[r]=list;}list.Add(indices[i]);list.Add(indices[i+1]);list.Add(indices[i+2]);}
        return groups;
    }
    static Mesh Compact(Mesh source,IEnumerable<int> indices)
    {
        var vs0=source.vertices;var ns0=source.normals;var uv0=source.uv;
        var map=new Dictionary<int,int>();var vs=new List<Vector3>();var ns=new List<Vector3>();var us=new List<Vector2>();var tris=new List<int>();
        foreach(int index in indices){if(!map.TryGetValue(index,out int next)){next=vs.Count;map[index]=next;vs.Add(vs0[index]);ns.Add(ns0[index]);us.Add(uv0[index]);}tris.Add(next);}
        var mesh=new Mesh{indexFormat=IndexFormat.UInt32};mesh.SetVertices(vs);mesh.SetNormals(ns);mesh.SetUVs(0,us);mesh.SetTriangles(tris,0);mesh.RecalculateBounds();return mesh;
    }
    static Mesh LeafCards(Mesh source,Dictionary<int,List<int>> groups,int stride)
    {
        var vertices=source.vertices;var uv=source.uv;
        var vs=new List<Vector3>();var us=new List<Vector2>();var ns=new List<Vector3>();var tris=new List<int>();
        foreach(var group in groups)
        {
            if(Hash(group.Key)%(uint)stride!=0)continue;
            var ids=group.Value.Distinct().ToArray();Vector3 center=Vector3.zero;Vector2 mean=Vector2.zero,min=Vector2.one*float.MaxValue,max=Vector2.one*float.MinValue;
            foreach(int id in ids){center+=vertices[id];mean+=uv[id];min=Vector2.Min(min,uv[id]);max=Vector2.Max(max,uv[id]);}
            center/=ids.Length;mean/=ids.Length;float uu=0,vv=0,uvsum=0;Vector3 pu=Vector3.zero,pv=Vector3.zero;
            foreach(int id in ids){var duv=uv[id]-mean;var p=vertices[id]-center;uu+=duv.x*duv.x;vv+=duv.y*duv.y;uvsum+=duv.x*duv.y;pu+=p*duv.x;pv+=p*duv.y;}
            float determinant=uu*vv-uvsum*uvsum;if(Mathf.Abs(determinant)<1e-12f)continue;
            Vector3 axisU=(pu*vv-pv*uvsum)/determinant,axisV=(pv*uu-pu*uvsum)/determinant;float expansion=Mathf.Sqrt(stride);
            int first=vs.Count;var corners=new[]{min,new Vector2(max.x,min.y),max,new Vector2(min.x,max.y)};var normal=Vector3.Cross(axisU,axisV).normalized;
            foreach(var corner in corners){vs.Add(center+(axisU*(corner.x-mean.x)+axisV*(corner.y-mean.y))*expansion);us.Add(corner);ns.Add(normal);}
            tris.AddRange(new[]{first,first+1,first+2,first,first+2,first+3});
        }
        var mesh=new Mesh{indexFormat=IndexFormat.UInt32};mesh.SetVertices(vs);mesh.SetNormals(ns);mesh.SetUVs(0,us);mesh.SetTriangles(tris,0);mesh.RecalculateTangents();mesh.RecalculateBounds();return mesh;
    }
    static long Total(Mesh m)=>Enumerable.Range(0,m.subMeshCount).Sum(i=>(long)m.GetIndexCount(i)/3);

    public static void Generate()
    {
        if(!File.Exists(Backup))throw new Exception("Backup required");
        if(!AssetDatabase.IsValidFolder(Root))AssetDatabase.CreateFolder("Assets/World/WorldExtensions/Generated","LinkedIslandTreeMidTopology20260930");
        var rows=new List<string>();
        for(int i=0;i<Source.Length;i++)
        {
            string outPath=Root+"/IslandTree0"+(i+1)+"_MidTopology.asset";if(AssetDatabase.LoadAssetAtPath<Mesh>(outPath))continue;
            var src=AssetDatabase.LoadAssetAtPath<Mesh>(Source[i]);if(!src)throw new Exception("Missing "+Source[i]);
            var leaves=Components(src,1);var branches=Components(src,2);
            var trunk=Compact(src,src.GetTriangles(0));
            var leafCards=LeafCards(src,leaves,4);
            var branchIdx=branches.Where(p=>p.Value.Count/3>=20 || Hash(p.Key)%4u==0).SelectMany(p=>p.Value);
            var branch=Compact(src,branchIdx);
            var m=new Mesh{indexFormat=IndexFormat.UInt32};
            m.CombineMeshes(new[]{new CombineInstance{mesh=trunk},new CombineInstance{mesh=leafCards},new CombineInstance{mesh=branch}},false,false);
            m.name="IslandTree0"+(i+1)+" topology mid";m.RecalculateBounds();m.RecalculateTangents();
            if(m.subMeshCount!=3||Enumerable.Range(0,3).Any(s=>m.GetIndexCount(s)==0))throw new Exception("Missing material geometry "+i);
            AssetDatabase.CreateAsset(m,outPath);
            rows.Add("tree"+(i+1)+" oldMid="+Total(AssetDatabase.LoadAssetAtPath<Mesh>(OldMid[i]))+" candidate="+Total(m)+" slots="+string.Join(",",Enumerable.Range(0,3).Select(s=>m.GetIndexCount(s)/3))+" bounds="+m.bounds);
            UnityEngine.Object.DestroyImmediate(trunk);UnityEngine.Object.DestroyImmediate(leafCards);UnityEngine.Object.DestroyImmediate(branch);
        }
        AssetDatabase.SaveAssets();File.WriteAllLines("Validation/EdgeGrid20260923/island_tree_mid_topology_candidates_20260930.txt",rows);Debug.Log(string.Join("\n",rows));
    }

    public static void Apply()
    {
        if(!File.Exists(Backup))throw new Exception("Backup required");
        var scene=SceneManager.GetActiveScene();if(scene.path!="Assets/Scenes/World/Enroth.unity"||EditorApplication.isPlaying)throw new Exception("Linked Edit Mode required");
        var map=new Dictionary<Mesh,Mesh>();
        for(int i=0;i<3;i++){var old=AssetDatabase.LoadAssetAtPath<Mesh>(OldMid[i]);var next=AssetDatabase.LoadAssetAtPath<Mesh>(Root+"/IslandTree0"+(i+1)+"_MidTopology.asset");if(!old||!next)throw new Exception("Missing mesh "+i);map[old]=next;}
        int changed=0;foreach(var root in scene.GetRootGameObjects())foreach(var f in root.GetComponentsInChildren<MeshFilter>(true))if(f.sharedMesh&&map.TryGetValue(f.sharedMesh,out var next)&&f.transform.name=="Linked distance LOD 1"){f.sharedMesh=next;EditorUtility.SetDirty(f);changed++;}
        if(changed<100)throw new Exception("Too few replacements "+changed);
        EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);AssetDatabase.SaveAssets();
        File.WriteAllText("Validation/EdgeGrid20260923/island_tree_mid_topology_applied_20260930.txt","PASS changed="+changed+" terrainWrites=0 transformWrites=0 materialWrites=0 canonicalWrites=0\n");Debug.Log("Island topology mid replacements="+changed);
    }

    public static void Revert()
    {
        var scene=SceneManager.GetActiveScene();if(scene.path!="Assets/Scenes/World/Enroth.unity")throw new Exception("Linked scene required");
        var map=new Dictionary<Mesh,Mesh>();for(int i=0;i<3;i++){var old=AssetDatabase.LoadAssetAtPath<Mesh>(OldMid[i]);var cur=AssetDatabase.LoadAssetAtPath<Mesh>(Root+"/IslandTree0"+(i+1)+"_MidTopology.asset");if(old&&cur)map[cur]=old;}
        int n=0;foreach(var root in scene.GetRootGameObjects())foreach(var f in root.GetComponentsInChildren<MeshFilter>(true))if(f.sharedMesh&&map.TryGetValue(f.sharedMesh,out var old)){f.sharedMesh=old;EditorUtility.SetDirty(f);n++;}
        EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);Debug.Log("Reverted island topology mids="+n);
    }
}
