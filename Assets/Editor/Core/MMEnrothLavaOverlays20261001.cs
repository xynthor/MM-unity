using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using UnityEngine.Rendering;

public static class MMEnrothLavaOverlays20261001
{
 const string ScenePath="Assets/Scenes/World/Enroth.unity";
 const string Root="Assets/World/WorldExtensions/Lava/Lava001/";
 const string MeshPath=Root+"Enroth_SourceRegisteredLava.asset";
 const string PrefabPath=Root+"Enroth_SourceRegisteredLava.prefab";
 const string MatPath=Root+"Lava001_Emissive.mat";

 struct Vent { public string terrain; public float x,z,rx,rz; public Vent(string t,float X,float Z,float RX,float RZ){terrain=t;x=X;z=Z;rx=RX;rz=RZ;} }
 static readonly Vent[] Vents={
  new Vent("SweetWater_NorthTerrain",464f,265f,4.8f,4.3f)
 };

 static Terrain FindTerrain(string n){
  return SceneManager.GetActiveScene().GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<Terrain>(true))
    .FirstOrDefault(t=>t.terrainData&&t.terrainData.name.IndexOf(n,StringComparison.OrdinalIgnoreCase)>=0);
 }

 static Mesh BuildMesh(out string report){
  var verts=new List<Vector3>();var uv=new List<Vector2>();var tris=new List<int>();var rows=new List<string>();
  foreach(var v in Vents){
   var t=FindTerrain(v.terrain);if(!t)throw new Exception("Missing lava vent terrain "+v.terrain);
   int n=48,start=verts.Count,accepted=0;
   for(int iz=0;iz<=n;iz++)for(int ix=0;ix<=n;ix++){
    float fx=(ix/(float)n)*2f-1f,fz=(iz/(float)n)*2f-1f;
    float lx=v.x+fx*v.rx,lz=v.z+fz*v.rz;float wx=t.transform.position.x+lx,wz=t.transform.position.z+lz;
    float ground=t.SampleHeight(new Vector3(wx,0,wz))+t.transform.position.y;
    verts.Add(new Vector3(wx,ground+.07f,wz));uv.Add(new Vector2(wx/6.5f,wz/6.5f));
   }
   for(int iz=0;iz<n;iz++)for(int ix=0;ix<n;ix++){
    float fx=((ix+.5f)/n)*2f-1f,fz=((iz+.5f)/n)*2f-1f;
    float noise=Mathf.PerlinNoise(ix*.19f+11.3f,iz*.21f+5.7f);
    float edge=.96f+(noise-.5f)*.08f;
    if(fx*fx+fz*fz>edge*edge)continue;
    int a=start+iz*(n+1)+ix,b=a+1,c=a+(n+1),d=c+1;
    tris.Add(a);tris.Add(b);tris.Add(c);tris.Add(b);tris.Add(d);tris.Add(c);
    tris.Add(a);tris.Add(c);tris.Add(b);tris.Add(b);tris.Add(c);tris.Add(d);accepted+=2;
   }
   rows.Add(v.terrain+" triangles="+accepted+" centerLocal=("+v.x+","+v.z+") radii=("+v.rx+","+v.rz+")");
  }
  var m=new Mesh{name="Enroth_SourceRegisteredLava"};m.indexFormat=UnityEngine.Rendering.IndexFormat.UInt32;m.SetVertices(verts);m.SetUVs(0,uv);m.SetTriangles(tris,0);m.SetNormals(Enumerable.Repeat(Vector3.up,verts.Count).ToList());m.RecalculateBounds();var bb=m.bounds;bb.Expand(new Vector3(0,.5f,0));m.bounds=bb;report=string.Join("\n",rows);return m;
 }

 [MenuItem("MMUnity/World/Build Source-Registered Lava Overlay")]
 public static void BuildAssets(){
  if(SceneManager.GetActiveScene().path!=ScenePath)throw new Exception("Open Enroth before building lava overlay");
  var mat=AssetDatabase.LoadAssetAtPath<Material>(MatPath);if(!mat||!mat.IsKeywordEnabled("_EMISSION"))throw new Exception("Validated emissive lava material missing");
  string report;var generated=BuildMesh(out report);var existing=AssetDatabase.LoadAssetAtPath<Mesh>(MeshPath);Mesh mesh;
  if(existing){EditorUtility.CopySerialized(generated,existing);EditorUtility.SetDirty(existing);UnityEngine.Object.DestroyImmediate(generated);mesh=existing;}
  else{AssetDatabase.CreateAsset(generated,MeshPath);mesh=generated;}
  var go=new GameObject("Enroth Source-Registered Lava 20261001");go.AddComponent<MeshFilter>().sharedMesh=mesh;var mr=go.AddComponent<MeshRenderer>();mr.sharedMaterial=mat;mr.shadowCastingMode=ShadowCastingMode.Off;mr.receiveShadows=false;
  PrefabUtility.SaveAsPrefabAsset(go,PrefabPath);UnityEngine.Object.DestroyImmediate(go);AssetDatabase.SaveAssets();AssetDatabase.Refresh();
  Directory.CreateDirectory("Validation/EdgeGrid20260923/Lava20261001");File.WriteAllText("Validation/EdgeGrid20260923/Lava20261001/overlay_build.txt","PASS terrainWrites=0 heightWrites=0 alphamapWrites=0\n"+report+"\n");
  Debug.Log("LAVA_OVERLAY_ASSETS_READY "+report.Replace("\n"," | "));
 }

 public static GameObject CreatePreviewInstance(){
  var p=AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);if(!p)throw new Exception("Lava overlay prefab missing");
  return (GameObject)PrefabUtility.InstantiatePrefab(p);
 }

 [MenuItem("MMUnity/World/Install Source-Registered Lava Overlay")]
 public static void Install(){
  var s=SceneManager.GetActiveScene();if(s.path!=ScenePath)throw new Exception("Open Enroth before installing lava");
  if(s.isDirty)throw new Exception("Enroth has unsaved edits; lava installer refuses to mix changes");
  if(s.GetRootGameObjects().Any(g=>g.name=="Enroth Source-Registered Lava 20261001"))return;
  var go=CreatePreviewInstance();go.name="Enroth Source-Registered Lava 20261001";EditorSceneManager.MarkSceneDirty(s);
  if(!EditorSceneManager.SaveScene(s))throw new IOException("Could not save lava overlay into Enroth");
 }
}
