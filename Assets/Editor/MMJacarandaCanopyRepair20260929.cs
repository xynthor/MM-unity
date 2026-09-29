using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using UnityMeshSimplifier;

public static class MMJacarandaCanopyRepair20260929
{
 const string Root="Assets/World/WorldExtensions/Generated/LinkedJacarandaRepairV2";
 static uint Hash(int n){uint v=(uint)n;v^=v>>16;v*=0x7feb352d;v^=v>>15;v*=0x846ca68b;return v^(v>>16);}
 static Dictionary<int,List<int>> Components(Mesh source,int slot){var indices=source.GetTriangles(slot);var parent=new int[source.vertexCount];for(int i=0;i<parent.Length;i++)parent[i]=i;int Find(int v){while(parent[v]!=v){parent[v]=parent[parent[v]];v=parent[v];}return v;}for(int i=0;i<indices.Length;i+=3){int r=Find(indices[i]);parent[Find(indices[i+1])]=r;parent[Find(indices[i+2])]=r;}var groups=new Dictionary<int,List<int>>();for(int i=0;i<indices.Length;i+=3){int r=Find(indices[i]);if(!groups.TryGetValue(r,out var list)){list=new List<int>();groups[r]=list;}list.Add(indices[i]);list.Add(indices[i+1]);list.Add(indices[i+2]);}return groups;}
 static Mesh Compact(Vector3[] vertices,Vector3[] normals,Vector2[] uv,IEnumerable<int> indices){var map=new Dictionary<int,int>();var vs=new List<Vector3>();var ns=new List<Vector3>();var us=new List<Vector2>();var tris=new List<int>();foreach(int index in indices){if(!map.TryGetValue(index,out int next)){next=vs.Count;map[index]=next;vs.Add(vertices[index]*100);ns.Add(normals[index]);us.Add(uv[index]);}tris.Add(next);}var mesh=new Mesh{indexFormat=IndexFormat.UInt32};mesh.SetVertices(vs);mesh.SetNormals(ns);mesh.SetUVs(0,us);mesh.SetTriangles(tris,0);return mesh;}
 static Mesh Reduce(Mesh source,float quality){var simplifier=new MeshSimplifier();simplifier.Initialize(source);simplifier.SimplifyMesh(quality);var mesh=simplifier.ToMesh();mesh.vertices=mesh.vertices.Select(v=>v/100).ToArray();mesh.RecalculateBounds();UnityEngine.Object.DestroyImmediate(source);return mesh;}
 static Mesh Leaves(Dictionary<int,List<int>> groups,Vector3[] vertices,Vector2[] uv,int stride){
  var vs=new List<Vector3>();var us=new List<Vector2>();var ns=new List<Vector3>();var tris=new List<int>();
  foreach(var group in groups){if(Hash(group.Key)%(uint)stride!=0)continue;var ids=group.Value.Distinct().ToArray();Vector3 center=Vector3.zero;Vector2 mean=Vector2.zero,min=Vector2.one*float.MaxValue,max=Vector2.one*float.MinValue;foreach(int id in ids){center+=vertices[id];mean+=uv[id];min=Vector2.Min(min,uv[id]);max=Vector2.Max(max,uv[id]);}center/=ids.Length;mean/=ids.Length;float uu=0,vv=0,uvsum=0;Vector3 pu=Vector3.zero,pv=Vector3.zero;foreach(int id in ids){var d=uv[id]-mean;var p=vertices[id]-center;uu+=d.x*d.x;vv+=d.y*d.y;uvsum+=d.x*d.y;pu+=p*d.x;pv+=p*d.y;}float determinant=uu*vv-uvsum*uvsum;if(Mathf.Abs(determinant)<1e-15f)throw new Exception("Degenerate leaf UV component "+group.Key);Vector3 axisU=(pu*vv-pv*uvsum)/determinant,axisV=(pv*uu-pu*uvsum)/determinant;float expansion=Mathf.Sqrt(stride);int first=vs.Count;var corners=new[]{min,new Vector2(max.x,min.y),max,new Vector2(min.x,max.y)};var normal=Vector3.Cross(axisU,axisV).normalized;foreach(var corner in corners){vs.Add(center+(axisU*(corner.x-mean.x)+axisV*(corner.y-mean.y))*expansion);us.Add(corner);ns.Add(normal);}tris.AddRange(new[]{first,first+1,first+2,first,first+2,first+3});}
  var mesh=new Mesh{indexFormat=IndexFormat.UInt32};mesh.SetVertices(vs);mesh.SetNormals(ns);mesh.SetUVs(0,us);mesh.SetTriangles(tris,0);mesh.RecalculateTangents();return mesh;
 }
 public static void Generate(){
  if(!AssetDatabase.IsValidFolder(Root))AssetDatabase.CreateFolder("Assets/World/WorldExtensions/Generated","LinkedJacarandaRepairV2");
  if(AssetDatabase.LoadAssetAtPath<Mesh>(Root+"/JacarandaNear.asset"))throw new Exception("Candidate exists; inspect before regeneration");
  var source=AssetDatabase.LoadAllAssetsAtPath("Assets/Environment/PolyHaven/Downloaded/jacaranda_tree/jacaranda_tree_1k.fbx").OfType<Mesh>().Single();var vertices=source.vertices;var normals=source.normals;var uv=source.uv;var branches=Components(source,0);var trunk=source.GetTriangles(1);var leaves=Components(source,2);var report=new List<string>();
  foreach(bool far in new[]{false,true}){
   // Keep structural branches; thin only the tiny disconnected petiole meshes.
   var branchIndices=branches.Where(p=>p.Value.Count>192||Hash(p.Key)%(far?32u:4u)==0).SelectMany(p=>p.Value);
   var branch=Reduce(Compact(vertices,normals,uv,branchIndices),far?.025f:.08f);var stem=Reduce(Compact(vertices,normals,uv,trunk),far?.015f:.04f);var canopy=Leaves(leaves,vertices,uv,far?8:1);
   var mesh=new Mesh{indexFormat=IndexFormat.UInt32};mesh.CombineMeshes(new[]{new CombineInstance{mesh=branch},new CombineInstance{mesh=stem},new CombineInstance{mesh=canopy}},false,false);mesh.RecalculateBounds();mesh.name=far?"Jacaranda faithful mid canopy":"Jacaranda faithful near canopy";
   if(mesh.subMeshCount!=3||Enumerable.Range(0,3).Any(i=>mesh.GetIndexCount(i)==0))throw new Exception("Missing material geometry");
   report.Add(mesh.name+" triangles="+string.Join(",",Enumerable.Range(0,3).Select(i=>mesh.GetIndexCount(i)/3))+" bounds="+mesh.bounds+" leavesStride="+(far?8:1));AssetDatabase.CreateAsset(mesh,Root+(far?"/JacarandaMid.asset":"/JacarandaNear.asset"));UnityEngine.Object.DestroyImmediate(branch);UnityEngine.Object.DestroyImmediate(stem);UnityEngine.Object.DestroyImmediate(canopy);
  }
  AssetDatabase.SaveAssets();File.WriteAllLines("Validation/EdgeGrid20260923/Resume_JacarandaCandidates.txt",report);Debug.Log(string.Join("\n",report));
 }
 public static void Apply(){
  var scene=SceneManager.GetActiveScene();if(scene.path!="Assets/Scenes/Enroth_Linked_OpenWorld.unity"||!File.Exists("Backups/BeforeJacarandaRepair_20260929/manifest.json"))throw new Exception("Linked scene and backup required");
  var near=AssetDatabase.LoadAssetAtPath<Mesh>(Root+"/JacarandaNear.asset");var mid=AssetDatabase.LoadAssetAtPath<Mesh>(Root+"/JacarandaMid.asset");if(!near||!mid)throw new Exception("Missing validated candidates");int count=0;
  foreach(var group in scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<LODGroup>(true))){var f=group.GetComponent<MeshFilter>();if(!f||!f.sharedMesh||AssetDatabase.GetAssetPath(f.sharedMesh)!="Assets/Optimization/GeneratedMeshes/ac43f7d2_jacaranda_tree_LOD0_q50.asset")continue;var child=group.transform.Find("Linked distance LOD 1");if(!child)throw new Exception("Expected linked tree stages");f.sharedMesh=near;child.GetComponent<MeshFilter>().sharedMesh=mid;group.RecalculateBounds();count++;}
  if(count==0)throw new Exception("No Jacaranda instances matched");EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);File.WriteAllText("Validation/EdgeGrid20260923/Resume_JacarandaApplied.txt","groups="+count+" transformWrites=0 canonicalWrites=0 materialSlotsPreserved=3");Debug.Log("Repaired Jacaranda groups="+count);
 }
}
