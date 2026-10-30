using System;
using System.IO;
using System.Globalization;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
public static class MMNorthSq01WestWaterSafePreview20261011{
 const string Scene="Assets/Scenes/World/Enroth.unity";
 const string Data="Validation/NorthSq01WestCoveRemesh20261011";
 const string Out="Preview/NorthSq01WestWaterSafe20261011";
 static void Shot(string p,Vector3 position,Vector3 focus,bool top,float size){
  var go=new GameObject("TEMP_WATER_PATCH_CAMERA");var cam=go.AddComponent<Camera>();
  cam.transform.position=position;cam.transform.rotation=Quaternion.LookRotation(focus-position,Vector3.up);
  cam.orthographic=top;if(top)cam.orthographicSize=size;else cam.fieldOfView=size;
  cam.nearClipPlane=.1f;cam.farClipPlane=3500f;cam.clearFlags=CameraClearFlags.Skybox;
  var old=RenderTexture.active;var rt=new RenderTexture(1250,850,24);
  cam.targetTexture=rt;cam.Render();RenderTexture.active=rt;
  var tex=new Texture2D(1250,850,TextureFormat.RGB24,false);
  tex.ReadPixels(new Rect(0,0,1250,850),0,0);tex.Apply();File.WriteAllBytes(p,tex.EncodeToPNG());
  RenderTexture.active=old;cam.targetTexture=null;UnityEngine.Object.DestroyImmediate(tex);UnityEngine.Object.DestroyImmediate(rt);UnityEngine.Object.DestroyImmediate(go);
 }
 static void Shots(string name){
  string dir=Out+"/"+name;Directory.CreateDirectory(dir);
  Shot(dir+"/top.png",new Vector3(-900,370,1210),new Vector3(-900,0,1210),true,137);
  Shot(dir+"/sea.png",new Vector3(-900,120,1410),new Vector3(-900,9,1212),false,59);
  Shot(dir+"/low.png",new Vector3(-895,76,1320),new Vector3(-900,9,1215),false,62);
 }
 static Mesh Import(string variant){
  var lines=File.ReadAllLines(Data+"/"+variant+"_triangles.csv");
  if(lines.Length<6||lines.Length>2000)throw new Exception("Invalid polygon source");
  var verts=new List<Vector3>();var tris=new List<int>();var normals=new List<Vector3>();
  var ci=CultureInfo.InvariantCulture;
  for(int i=1;i<lines.Length;i++){
   var cols=lines[i].Split(',');if(cols.Length!=6)throw new Exception("Malformed CSV row "+i);
   Vector3[] p=new Vector3[3];
   for(int k=0;k<3;k++){
    float x=float.Parse(cols[k*2],ci),z=float.Parse(cols[k*2+1],ci);
    p[k]=new Vector3(x,.12f,z);
   }
   int offset=verts.Count;
   foreach(var v in p){verts.Add(v);normals.Add(Vector3.up);}
   bool normalUp=Vector3.Cross(p[1]-p[0],p[2]-p[0]).y>0;
   tris.Add(offset);tris.Add(offset+(normalUp?1:2));tris.Add(offset+(normalUp?2:1));
  }
  var m=new Mesh();m.name="TEMP_OCEAN_CONSTRAINED_GAP_"+variant;m.indexFormat=UnityEngine.Rendering.IndexFormat.UInt32;
  m.SetVertices(verts);m.SetTriangles(tris,0);m.SetNormals(normals);m.RecalculateBounds();m.hideFlags=HideFlags.HideAndDontSave;
  return m;
 }
 public static void Run(){
  var scene=EditorSceneManager.OpenScene(Scene,OpenSceneMode.Single);if(scene.isDirty)throw new Exception("Dirty source scene");
  var o=GameObject.Find("Linked connected sea and existing lowland waters 20260929");
  if(!o||!o.GetComponent<MeshFilter>()||!o.GetComponent<MeshRenderer>())throw new Exception("Ocean missing");
  var original=o.GetComponent<MeshFilter>().sharedMesh;int sourceVert=original.vertexCount;int sourceTri=original.triangles.Length;
  Shots("baseline");
  string report="";
  foreach(string variant in new[]{"W5_iterative_safe"}){
   var mesh=Import(variant);var g=new GameObject("TEMP_CONTIGUOUS_OCEAN_GAP_"+variant);
   var mf=g.AddComponent<MeshFilter>();mf.sharedMesh=mesh;
   var mr=g.AddComponent<MeshRenderer>();mr.sharedMaterial=o.GetComponent<MeshRenderer>().sharedMaterial;
   mr.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;mr.receiveShadows=false;
   try{Shots(variant);report+=variant+" appendedPatchTriangles="+mesh.triangles.Length/3+" appendedPatchVertices="+mesh.vertexCount+" ";
   }finally{UnityEngine.Object.DestroyImmediate(g);UnityEngine.Object.DestroyImmediate(mesh);}
  }
  if(scene.isDirty||o.GetComponent<MeshFilter>().sharedMesh!=original||original.vertexCount!=sourceVert||original.triangles.Length!=sourceTri)throw new Exception("Source scene/mesh invariant violated");
  Debug.Log("SQ01_WEST_SAFE_PATCH_PREVIEW_DONE saved=False "+report+"sceneDirty="+scene.isDirty);
 }
}
