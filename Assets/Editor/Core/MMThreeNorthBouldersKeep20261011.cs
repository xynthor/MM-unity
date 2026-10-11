using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Collections.Generic;
using System.Globalization;
using System.Security.Cryptography;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;

public static class MMThreeNorthBouldersKeep20261011 {
 const string Scene="Assets/Scenes/World/Enroth.unity";
 const string Parent="North Production Boulder Dressing 20261007";
 const string Report="Validation/NorthBoulderGrounding20261011";
 const string Backup="Backups/NorthBouldersBefore20261011";
 const string OriginalSceneSHA="afa386f40d89571e9c8a0e7138064c7910f5c64127d3f8bc5d7645931cbd3d30";
 static readonly string[] Names={"AuthoredBoulder_5","AuthoredBoulder_24","AuthoredBoulder_37"};
 static readonly CultureInfo CI=CultureInfo.InvariantCulture;
 static string SHA(string file){using(var h=SHA256.Create())using(var f=File.OpenRead(file))return BitConverter.ToString(h.ComputeHash(f)).Replace("-","").ToLowerInvariant();}
 static string P(Vector3 v)=>v.x.ToString("R",CI)+","+v.y.ToString("R",CI)+","+v.z.ToString("R",CI);
 static string P(Quaternion v)=>v.x.ToString("R",CI)+","+v.y.ToString("R",CI)+","+v.z.ToString("R",CI)+","+v.w.ToString("R",CI);
 static string Stable(Transform t){var a=new List<string>();for(;t;t=t.parent)a.Add(t.name+"#"+t.GetSiblingIndex());a.Reverse();return string.Join("/",a);}
 static Terrain Ground(Vector3 p){var t=Terrain.activeTerrains.FirstOrDefault(t=>p.x>=t.transform.position.x-.001f&&p.x<=t.transform.position.x+512.001f&&p.z>=t.transform.position.z-.001f&&p.z<=t.transform.position.z+512.001f);if(!t)throw new Exception("Missing saved terrain for boulder "+p);return t;}
 static float Clearance(Transform t){
  var m=t.GetComponent<MeshFilter>().sharedMesh;float min=1e6f;
  foreach(var v in m.vertices){var w=t.TransformPoint(v);var g=Ground(w);min=Mathf.Min(min,w.y-(g.transform.position.y+g.SampleHeight(w)));}
  return min;
 }
 static List<Transform> Targets(bool alreadyGrounded=false){
  var tr=UnityEngine.Object.FindObjectsByType<Transform>(FindObjectsInactive.Include,FindObjectsSortMode.None);
  var list=tr.Where(t=>Names.Contains(t.name)&&t.parent&&t.parent.name==Parent).OrderBy(t=>t.name).ToList();
  if(list.Count!=3||list.Any(t=>!t.gameObject.activeInHierarchy))throw new Exception("Expected 3 active independent boulders");
  foreach(var t in list){
   var f=t.GetComponent<MeshFilter>();var r=t.GetComponent<MeshRenderer>();var c=t.GetComponent<MeshCollider>();
   if(!f||!r||!c||!f.sharedMesh||c.sharedMesh!=f.sharedMesh||!r.enabled||t.GetComponentsInChildren<MeshRenderer>(true).Length!=1||
      r.bounds.size.x>5||r.bounds.size.z>5)throw new Exception("Non-boulder object or unexpectedly altered collision mesh "+t.name);
   if(!r.sharedMaterial||r.sharedMaterial.name.IndexOf("Boulder",StringComparison.OrdinalIgnoreCase)<0)throw new Exception("Unexpected source material "+t.name);
   float min=Clearance(t);
   if(alreadyGrounded){if(Mathf.Abs(min+.12f)>.018f)throw new Exception("Saved boulder no longer grounded "+t.name+" "+min);}
   else if(min<.30f||min>.85f)throw new Exception("Boulder clearance changed since audit "+t.name+" "+min);
  }
  return list;
 }
 static string ProtectedDigest(HashSet<Transform> approved){
  var lines=new List<string>();
  foreach(var t in UnityEngine.Object.FindObjectsByType<Transform>(FindObjectsInactive.Include,FindObjectsSortMode.None)){
   if(approved.Any(a=>t==a||t.IsChildOf(a)))continue;
   lines.Add(Stable(t)+"|"+P(t.localPosition)+"|"+P(t.localRotation)+"|"+P(t.localScale));
  }
  lines.Sort(StringComparer.Ordinal);
  using(var h=SHA256.Create())return BitConverter.ToString(h.ComputeHash(Encoding.UTF8.GetBytes(string.Join("\n",lines)))).Replace("-","").ToLowerInvariant();
 }
 static void Move(List<Transform> list,List<string> report){
  foreach(var t in list){
   float before=Clearance(t);var p=t.position;
   t.position=new Vector3(p.x,p.y-before-.12f,p.z);
   float after=Clearance(t);
   if(Mathf.Abs(after+.12f)>.018f||t.position.x!=p.x||t.position.z!=p.z)throw new Exception("Unsafe boulder move "+t.name+" post="+after);
   report.Add(Stable(t)+"|"+P(p)+"|"+P(t.position)+"|minBefore="+before.ToString("F4",CI)+"|minAfter="+after.ToString("F4",CI));
  }
 }
 static void Shot(string name,string variant,Transform t){
  string dir="Preview/NorthBoulders20261011/"+variant;Directory.CreateDirectory(dir);
  var center=t.GetComponent<MeshRenderer>().bounds.center;var g=Ground(center);
  var look=new Vector3(center.x,g.transform.position.y+g.SampleHeight(center)+1f,center.z);
  var cameraGO=new GameObject("TEMP_BOULDER_PREVIEW");var cam=cameraGO.AddComponent<Camera>();
  cam.transform.position=look+new Vector3(9,6,11);cam.transform.rotation=Quaternion.LookRotation(look-cam.transform.position,Vector3.up);
  cam.fieldOfView=37;cam.nearClipPlane=.05f;cam.farClipPlane=1500;cam.clearFlags=CameraClearFlags.Skybox;
  var previous=RenderTexture.active;var rt=new RenderTexture(1100,800,24);cam.targetTexture=rt;cam.Render();RenderTexture.active=rt;
  var tex=new Texture2D(1100,800,TextureFormat.RGB24,false);tex.ReadPixels(new Rect(0,0,1100,800),0,0);tex.Apply();
  File.WriteAllBytes(dir+"/"+name+".png",tex.EncodeToPNG());
  RenderTexture.active=previous;cam.targetTexture=null;
  UnityEngine.Object.DestroyImmediate(tex);UnityEngine.Object.DestroyImmediate(rt);UnityEngine.Object.DestroyImmediate(cameraGO);
 }
 public static void Preview(){
  var scene=EditorSceneManager.OpenScene(Scene,OpenSceneMode.Single);
  if(scene.isDirty||SHA(Scene)!=OriginalSceneSHA)throw new Exception("Saved source scene has changed");
  var list=Targets();var protectedSet=new HashSet<Transform>(list);
  string digest=ProtectedDigest(protectedSet);
  var savedPos=list.Select(t=>t.position).ToArray();var lines=new List<string>();
  foreach(var t in list)Shot(t.name,"before",t);
  try {
   Move(list,lines);
   if(ProtectedDigest(protectedSet)!=digest)throw new Exception("Non-rock / building moved during preview");
   foreach(var t in list)Shot(t.name,"candidate",t);
  }finally{
   for(int i=0;i<list.Count;i++)list[i].position=savedPos[i];
   if(ProtectedDigest(protectedSet)!=digest||SHA(Scene)!=OriginalSceneSHA)throw new Exception("Preview leaked source change");
  }
  Directory.CreateDirectory(Report);File.WriteAllLines(Report+"/preview.txt",lines);
  Debug.Log("NORTH_THREE_BOULDER_PREVIEW_PASS rocks=3 otherPosesInvariant=True buildingsMoved=0 sceneSaved=False");
 }
 public static void Apply(){
  var scene=EditorSceneManager.OpenScene(Scene,OpenSceneMode.Single);
  if(scene.isDirty||SHA(Scene)!=OriginalSceneSHA||Directory.Exists(Backup)||!File.Exists(Report+"/preview.txt"))throw new Exception("One-time guarded source mismatch");
  var list=Targets();var protectedSet=new HashSet<Transform>(list);string digest=ProtectedDigest(protectedSet);
  Directory.CreateDirectory(Backup);File.Copy(Scene,Backup+"/Enroth.unity",false);
  if(SHA(Backup+"/Enroth.unity")!=OriginalSceneSHA)throw new Exception("Backup SHA mismatch");
  var lines=new List<string>();Move(list,lines);
  if(ProtectedDigest(protectedSet)!=digest)throw new Exception("Any building or non-rock transform changed");
  EditorSceneManager.MarkSceneDirty(scene);if(!EditorSceneManager.SaveScene(scene))throw new Exception("Scene save failed");
  Directory.CreateDirectory(Report);
  File.WriteAllLines(Report+"/kept.txt",new[]{"PASS=True","groundedBoulders=3","buildingPartsMoved=0","otherObjectTransformsMoved=0",
   "protectedPoseSHA="+digest,"originalSceneSHA="+OriginalSceneSHA,"savedSceneSHA="+SHA(Scene)}.Concat(lines));
  Debug.Log("NORTH_THREE_BOULDER_SAVED_PASS grounded=3 buildingsMoved=0 sourceDirty="+scene.isDirty);
 }
 public static void Verify(){
  var scene=EditorSceneManager.OpenScene(Scene,OpenSceneMode.Single);if(scene.isDirty)throw new Exception("Saved scene unexpectedly dirty");
  var rows=File.ReadAllLines(Report+"/kept.txt");var ts=Targets(true);
  var digest=ProtectedDigest(new HashSet<Transform>(ts));
  if(digest!=rows.Single(r=>r.StartsWith("protectedPoseSHA=")).Split('=')[1])throw new Exception("Protected building/non-rock scene pose mutated");
  if(SHA(Scene)!=rows.Single(r=>r.StartsWith("savedSceneSHA=")).Split('=')[1])throw new Exception("Saved source differs from agreed change");
  File.WriteAllText(Report+"/independent_verify.txt","PASS=True groundedBoulders=3 otherObjectTransformsInvariant=True buildingPartsMoved=0 sceneDirty=False");
  Debug.Log("NORTH_THREE_BOULDER_INDEPENDENT_PASS grounded=3 buildingsMoved=0 allOtherPosesInvariant=True");
 }
}
