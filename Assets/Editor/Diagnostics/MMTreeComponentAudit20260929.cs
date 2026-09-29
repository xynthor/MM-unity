using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
public static class MMTreeComponentAudit20260929
{
 public static void Run(){
  var m=AssetDatabase.LoadAllAssetsAtPath("Assets/Environment/PolyHaven/Downloaded/jacaranda_tree/jacaranda_tree_1k.fbx").OfType<Mesh>().Single();
  var rows=new List<string>();
  for(int slot=0;slot<m.subMeshCount;slot++){
   var indices=m.GetTriangles(slot);var parent=new int[m.vertexCount];for(int i=0;i<parent.Length;i++)parent[i]=i;
   int Root(int v){while(parent[v]!=v){parent[v]=parent[parent[v]];v=parent[v];}return v;}
   for(int i=0;i<indices.Length;i+=3){int r=Root(indices[i]);parent[Root(indices[i+1])]=r;parent[Root(indices[i+2])]=r;}
   var counts=new Dictionary<int,int>();for(int i=0;i<indices.Length;i+=3){int r=Root(indices[i]);if(!counts.ContainsKey(r))counts[r]=0;counts[r]++;}
   rows.Add("slot="+slot+" triangles="+indices.Length/3+" connectedComponents="+counts.Count+" componentTriangleHistogram="+string.Join(",",counts.Values.GroupBy(n=>n).OrderByDescending(g=>g.Count()).Take(12).Select(g=>g.Key+":"+g.Count()))+" max="+counts.Values.Max());
  }
  File.WriteAllLines("Validation/EdgeGrid20260923/Resume_JacarandaComponents.txt",rows);Debug.Log(string.Join("\n",rows));
 }
}
