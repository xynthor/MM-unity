using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
public static class MMNorthSq01OceanFootprintAudit20261011 {
 public static void Run() {
  var sc=EditorSceneManager.OpenScene("Assets/Scenes/World/Enroth.unity",OpenSceneMode.Single);
  if(sc.isDirty)throw new Exception("Clean scene required");
  var go=GameObject.Find("Linked connected sea and existing lowland waters 20260929");
  if(!go)throw new Exception("Ocean object missing");
  var mf=go.GetComponent<MeshFilter>();if(!mf||!mf.sharedMesh)throw new Exception("No ocean mesh");
  var m=mf.sharedMesh;var local=m.vertices;var tris=m.triangles;
  var world=local.Select(v=>go.transform.TransformPoint(v)).ToArray();
  string dir="Validation/NorthSq01OceanFootprint20261011";Directory.CreateDirectory(dir);
  var lines=new List<string>{"x0,z0,y0,x1,z1,y1,x2,z2,y2"};
  int matches=0;
  foreach(int index in Enumerable.Range(0,tris.Length/3)){
   int k=index*3;
   Vector3 a=world[tris[k]],b=world[tris[k+1]],c=world[tris[k+2]];
   if(Mathf.Max(a.x,Mathf.Max(b.x,c.x)) < -900 || Mathf.Min(a.x,Mathf.Min(b.x,c.x)) > -630 ||
      Mathf.Max(a.z,Mathf.Max(b.z,c.z)) < 1120 || Mathf.Min(a.z,Mathf.Min(b.z,c.z)) > 1260)continue;
   matches++;
   lines.Add(string.Join(",",new[]{a.x,a.z,a.y,b.x,b.z,b.y,c.x,c.z,c.y}.Select(v=>v.ToString("F4",System.Globalization.CultureInfo.InvariantCulture))));
  }
  File.WriteAllLines(dir+"/triangles.csv",lines);
  string s="sceneDirty="+sc.isDirty+" oceanTotalVertices="+local.Length+" oceanTotalTriangles="+tris.Length/3+" croppedTriangleCandidates="+matches;
  File.WriteAllText(dir+"/audit.txt",s);
  Debug.Log("SQ01_OCEAN_FOOTPRINT "+s);
 }
}
