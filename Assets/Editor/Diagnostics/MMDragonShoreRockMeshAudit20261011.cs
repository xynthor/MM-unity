using System;using System.IO;using System.Linq;using System.Collections.Generic;using System.Globalization;using UnityEngine;using UnityEditor.SceneManagement;
public static class MMDragonShoreRockMeshAudit20261011{
 const string Report="Validation/DragonFloatingRocks20261011";
 static Terrain Find(Vector3 p)=>Terrain.activeTerrains.FirstOrDefault(t=>p.x>=t.transform.position.x&&p.x<=t.transform.position.x+512&&p.z>=t.transform.position.z&&p.z<=t.transform.position.z+512);
 public static void Run(){
  var sc=EditorSceneManager.OpenScene("Assets/Scenes/World/Enroth.unity",OpenSceneMode.Single);if(sc.isDirty)throw new Exception("Audit requires saved scene");
  var report=new List<string>{"name,mesh,vertexCount,sampled,naturalGapAbove1m,above3m,above8m,maxGap,meanGap,xMax,zMax"};
  foreach(var mesh in UnityEngine.Object.FindObjectsByType<MeshFilter>(FindObjectsInactive.Include,FindObjectsSortMode.None)){
   if(!mesh.sharedMesh)continue;
   var n=mesh.gameObject.name;
   if(!(n.StartsWith("Shore Rocks Cell ")||n.StartsWith("Inland Outcrops Cell ")||n.StartsWith("DragonRockSurface")))continue;
   if(mesh.transform.position.x< -1800||mesh.transform.position.x> -1200)continue;
   var v=mesh.sharedMesh.vertices;
   if(v.Length==0)continue;
   int stride=Math.Max(1,v.Length/15000),total=0,above1=0,above3=0,above8=0;float sum=0,max=-999;Vector3 maxPosition=new Vector3();
   for(int j=0;j<v.Length;j+=stride){
    var p=mesh.transform.TransformPoint(v[j]);var t=Find(p);if(!t)continue;
    float g=p.y-(t.transform.position.y+t.SampleHeight(p));
    if(g>1)above1++;if(g>3)above3++;if(g>8)above8++;
    if(g>max){max=g;maxPosition=p;}sum+=g;total++;
   }
   if(total==0)continue;
   var ci=CultureInfo.InvariantCulture;
   report.Add(string.Join(",",new[]{n,mesh.sharedMesh.name,v.Length.ToString(),total.ToString(),above1.ToString(),above3.ToString(),above8.ToString(),
    max.ToString("F3",ci),(sum/total).ToString("F3",ci),maxPosition.x.ToString("F2",ci),maxPosition.z.ToString("F2",ci)}));
  }
  Directory.CreateDirectory(Report);File.WriteAllLines(Report+"/shore_mesh_vertex_audit.csv",report);
  Debug.Log("DRAGON_SHORE_ROCK_VERTEX_AUDIT_PASS meshes="+(report.Count-1)+" sceneDirty="+sc.isDirty);
 }
}
