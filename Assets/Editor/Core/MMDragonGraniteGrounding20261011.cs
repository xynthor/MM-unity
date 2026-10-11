using System;
using System.IO;
using System.Text;
using System.Linq;
using System.Collections.Generic;
using System.Globalization;
using System.Security.Cryptography;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;

public static class MMDragonGraniteGrounding20261011 {
 const string Scene="Assets/Scenes/World/Enroth.unity";
 const string Report="Validation/DragonFloatingRocks20261011";
 const string Backup="Backups/DragonGraniteGroundingBefore20261011";
 const string Group="Rock_Granite_rcCwC";
 static readonly CultureInfo CI=CultureInfo.InvariantCulture;
 static float Height(Vector3 p){
  var t=Terrain.activeTerrains.FirstOrDefault(t=>p.x>=t.transform.position.x&&p.x<=t.transform.position.x+512&&
                                              p.z>=t.transform.position.z&&p.z<=t.transform.position.z+512);
  if(!t)throw new Exception("Ground terrain missing under "+p);
  return t.transform.position.y+t.SampleHeight(p);
 }
 static string NamePath(Transform t){var list=new List<string>();while(t){list.Add(t.name);t=t.parent;}list.Reverse();return string.Join("/",list);}
 static string StablePath(Transform t){var list=new List<string>();while(t){list.Add(t.name+"#"+t.GetSiblingIndex());t=t.parent;}list.Reverse();return string.Join("/",list);}
 static Bounds BoundsOf(Transform t){
  var rr=t.GetComponentsInChildren<MeshRenderer>(true);
  if(rr.Length!=5)throw new Exception("Expected exactly five natural-rock mesh LODs at "+NamePath(t)+" got "+rr.Length);
  var b=rr[0].bounds;foreach(var r in rr.Skip(1))b.Encapsulate(r.bounds);return b;
 }
 static List<Transform> Rocks(){
  var ts=UnityEngine.Object.FindObjectsByType<Transform>(FindObjectsInactive.Include,FindObjectsSortMode.None);
  var list=new List<Transform>();
  foreach(var t in ts){
   if(t.name!=Group||!t.parent)continue;
   string parent=t.parent.name;
   if(parent!="Dragon Lair - Eastern Ridge"&&parent!="Vampire Ruins - SW Reference")continue;
   var b=BoundsOf(t);
   if(b.center.x< -1792||b.center.x> -1280||b.center.z<256||b.center.z>1280)continue;
   if(b.size.x>4||b.size.z>4)throw new Exception("Potential building-scale rock cannot be moved");
   float gap=b.min.y-Height(b.center);
   if(gap>.75f)list.Add(t);
  }
  if(list.Count!=7)throw new Exception("Expected 7 discrete suspended natural granites, found "+list.Count);
  return list.OrderBy(t=>StablePath(t)).ToList();
 }
 static string H(string path){using(var sha=SHA256.Create())using(var s=File.OpenRead(path))return BitConverter.ToString(sha.ComputeHash(s)).Replace("-","").ToLowerInvariant();}
 static string Pose(Vector3 p)=>p.x.ToString("R",CI)+","+p.y.ToString("R",CI)+","+p.z.ToString("R",CI);
 static string Pose(Quaternion q)=>q.x.ToString("R",CI)+","+q.y.ToString("R",CI)+","+q.z.ToString("R",CI)+","+q.w.ToString("R",CI);
 static string ProtectedDigest(HashSet<Transform> approved){
  using(var sha=SHA256.Create()){
   var lines=new List<string>();
   foreach(var t in UnityEngine.Object.FindObjectsByType<Transform>(FindObjectsInactive.Include,FindObjectsSortMode.None)){
    if(approved.Any(a=>t==a||t.IsChildOf(a)))continue;
    // Include ALL other transforms, not merely buildings. No non-rock positions may change.
    lines.Add(StablePath(t)+"|"+Pose(t.localPosition)+"|"+Pose(t.localRotation)+"|"+Pose(t.localScale));
   }
   lines.Sort(StringComparer.Ordinal);
   return BitConverter.ToString(sha.ComputeHash(Encoding.UTF8.GetBytes(string.Join("\n",lines)))).Replace("-","").ToLowerInvariant();
  }
 }
 static void Move(List<Transform> list, List<string> rows){
  foreach(var t in list){
   var b=BoundsOf(t);float ground=Height(b.center);float before=b.min.y-ground;
   if(before<.75f||before>90)throw new Exception("Unexpected clearance "+before);
   Vector3 world=t.position;float delta=ground-.12f-b.min.y;
   t.position=world+Vector3.up*delta;
   var after=BoundsOf(t);float post=after.min.y-Height(after.center);
   if(Mathf.Abs(post+.12f)>.025f)throw new Exception("Rock does not rest on ground "+NamePath(t)+" post "+post);
   rows.Add(StablePath(t)+"|"+Pose(world)+"|"+Pose(t.position)+"|beforeGap="+before.ToString("F4",CI)+"|afterGap="+post.ToString("F4",CI));
  }
 }
 static void Screenshot(string path,Vector3 pos,Vector3 target,bool top,float size){
  var g=new GameObject("TEMP_DRAGON_GROUNDING_CAMERA");var c=g.AddComponent<Camera>();
  c.transform.position=pos;c.transform.rotation=Quaternion.LookRotation(target-pos,Vector3.up);
  c.orthographic=top;if(top)c.orthographicSize=size;else c.fieldOfView=size;
  c.nearClipPlane=.1f;c.farClipPlane=2200;c.clearFlags=CameraClearFlags.Skybox;
  var old=RenderTexture.active;var rt=new RenderTexture(1400,920,24);c.targetTexture=rt;c.Render();RenderTexture.active=rt;
  var im=new Texture2D(1400,920,TextureFormat.RGB24,false);im.ReadPixels(new Rect(0,0,1400,920),0,0);im.Apply();
  File.WriteAllBytes(path,im.EncodeToPNG());RenderTexture.active=old;c.targetTexture=null;
  UnityEngine.Object.DestroyImmediate(im);UnityEngine.Object.DestroyImmediate(rt);UnityEngine.Object.DestroyImmediate(g);
 }
 static void Photos(string tag){
  string dir="Preview/DragonFloatingRocks20261011/"+tag;Directory.CreateDirectory(dir);
  Screenshot(dir+"/lair_top.png",new Vector3(-1528,170,826),new Vector3(-1528,0,826),true,55);
  Screenshot(dir+"/lair_side.png",new Vector3(-1468,40,820),new Vector3(-1527,14,824),false,49);
  Screenshot(dir+"/ruins_top.png",new Vector3(-1568,245,654),new Vector3(-1568,0,654),true,86);
  Screenshot(dir+"/ruins_side.png",new Vector3(-1523,50,650),new Vector3(-1576,20,650),false,55);
 }
 public static void Preview(){
  var scene=EditorSceneManager.OpenScene(Scene,OpenSceneMode.Single);if(scene.isDirty)throw new Exception("Preview scene dirty on reload");
  var list=Rocks();var protectedSet=new HashSet<Transform>(list);string hash=H(Scene),protectedHash=ProtectedDigest(protectedSet);
  var old=list.Select(t=>t.position).ToArray();var rows=new List<string>();
  Photos("before");
  try{
   Move(list,rows);
   if(ProtectedDigest(protectedSet)!=protectedHash)throw new Exception("Protected poses changed during preview");
   Photos("candidate");
   Directory.CreateDirectory(Report);
   File.WriteAllLines(Report+"/preview_only_moves.txt",rows);
  }finally{
   for(int i=0;i<list.Count;i++)if(list[i])list[i].position=old[i];
   if(ProtectedDigest(protectedSet)!=protectedHash)throw new Exception("Protected poses changed on preview revert");
   if(H(Scene)!=hash)throw new Exception("Disk scene changed in preview");
  }
  Debug.Log("DRAGON_ROCK_PREVIEW_PASS count="+rows.Count+" buildingsMoved=0 sourceSaved=False");
 }
 public static void Apply(){
  var scene=EditorSceneManager.OpenScene(Scene,OpenSceneMode.Single);
  if(scene.isDirty||Directory.Exists(Backup)||File.Exists(Report+"/kept.txt"))throw new Exception("One-time backup or scene guard");
  var list=Rocks();var approved=new HashSet<Transform>(list);
  string beforeHash=H(Scene),beforeProtected=ProtectedDigest(approved);
  if(!File.Exists(Report+"/preview_only_moves.txt"))throw new Exception("Preview evidence missing");
  Directory.CreateDirectory(Backup);File.Copy(Scene,Backup+"/Enroth.unity",false);
  if(H(Backup+"/Enroth.unity")!=beforeHash)throw new Exception("Scene backup hash invalid");
  var lines=new List<string>();Move(list,lines);
  if(ProtectedDigest(approved)!=beforeProtected)throw new Exception("Non-rock or building transforms changed");
  if(lines.Count!=7)throw new Exception("Not seven discrete natural rocks");
  EditorSceneManager.MarkSceneDirty(scene);if(!EditorSceneManager.SaveScene(scene))throw new Exception("Scene save failed");
  Directory.CreateDirectory(Report);
  File.WriteAllLines(Report+"/kept.txt",new[]{"PASS=True","rockRootsMoved=7","nonRockPoseSHA="+beforeProtected,"buildingPartsMoved=0","otherTransformsMoved=0","sourceSceneSHA="+beforeHash,"savedSceneSHA="+H(Scene),"backup="+Backup+"/Enroth.unity"}.Concat(lines));
  Debug.Log("DRAGON_ROCK_SAVED_PASS moved=7 buildingPartsMoved=0 unaffectedTransformHash="+beforeProtected);
 }
 public static void Verify(){
  var scene=EditorSceneManager.OpenScene(Scene,OpenSceneMode.Single);if(scene.isDirty)throw new Exception("Reopen not clean");
  var report=File.ReadAllLines(Report+"/kept.txt");
  string oldHash=report.Single(s=>s.StartsWith("nonRockPoseSHA=")).Split('=')[1];
  // Approved rocks no longer float, identify the exact seven by previously saved stable paths.
  var keys=report.Where(s=>s.Contains("|beforeGap=")).Select(s=>s.Split('|')[0]).ToHashSet();
  var all=UnityEngine.Object.FindObjectsByType<Transform>(FindObjectsInactive.Include,FindObjectsSortMode.None);
  var targets=all.Where(t=>keys.Contains(StablePath(t))).ToList();
  if(targets.Count!=7)throw new Exception("Reopened target cardinality changed "+targets.Count);
  if(ProtectedDigest(new HashSet<Transform>(targets))!=oldHash)throw new Exception("Protected non-rock/building poses changed");
  foreach(var t in targets){
   var b=BoundsOf(t);float gap=b.min.y-Height(b.center);
   if(Mathf.Abs(gap+.12f)>.03f)throw new Exception("Still floating "+NamePath(t)+" "+gap);
  }
  if(H(Scene)!=report.Single(s=>s.StartsWith("savedSceneSHA=")).Split('=')[1])throw new Exception("Saved hash drift");
  File.WriteAllText(Report+"/independent_verify.txt","PASS=True rocksGrounded=7 allOtherTransformPosesInvariant=True buildingPartsMoved=0 sceneDirty=False");
  Debug.Log("DRAGON_ROCK_INDEPENDENT_PASS sevenGrounded=True buildingsMoved=0 allOtherPosesInvariant=True");
 }
}
