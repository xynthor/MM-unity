using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Globalization;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
public static class MMWorldThreeFloatingRocksKeep20261011 {
 const string Scene="Assets/Scenes/World/Enroth.unity";
 const string Parent="Original Regions Rock Scatter 20260930";
 const string Report="Validation/DragonFloatingRocks20261011";
 const string Backup="Backups/ThreeNaturalWorldRocksBefore20261011";
 const string BaselineSHA="c9a1f8c4d41a15f63f13e6bc216f6363b2122ffbc4e78b0e6969fe7a1251a904";
 static readonly string[] Names={"WorldRock_064","WorldRock_069","WorldRock_075"};
 static readonly CultureInfo CI=CultureInfo.InvariantCulture;
 static string SHA(string file){using(var h=SHA256.Create())using(var stream=File.OpenRead(file))return BitConverter.ToString(h.ComputeHash(stream)).Replace("-","").ToLowerInvariant();}
 static Terrain TerrainAt(Vector3 p)=>Terrain.activeTerrains.FirstOrDefault(t=>p.x>=t.transform.position.x&&p.x<=t.transform.position.x+512&&p.z>=t.transform.position.z&&p.z<=t.transform.position.z+512);
 static string Path(Transform t){var a=new List<string>();for(;t;t=t.parent)a.Add(t.name+"#"+t.GetSiblingIndex());a.Reverse();return string.Join("/",a);}
 static string P(Vector3 v)=>v.x.ToString("R",CI)+","+v.y.ToString("R",CI)+","+v.z.ToString("R",CI);
 static string P(Quaternion q)=>q.x.ToString("R",CI)+","+q.y.ToString("R",CI)+","+q.z.ToString("R",CI)+","+q.w.ToString("R",CI);
 static List<Transform> Targets(){
  var all=UnityEngine.Object.FindObjectsByType<Transform>(FindObjectsInactive.Include,FindObjectsSortMode.None);
  var list=all.Where(t=>Names.Contains(t.name)&&t.parent&&t.parent.name==Parent).OrderBy(t=>t.name).ToList();
  if(list.Count!=3||list.Any(t=>!t.gameObject.activeInHierarchy))throw new Exception("Exactly 3 active source rocks required");
  foreach(var t in list){
   var render=t.GetComponentsInChildren<MeshRenderer>(true);
   if(render.Length!=1||!render[0].enabled||!render[0].GetComponent<MeshFilter>()||
      render[0].bounds.size.x>5||render[0].bounds.size.z>5||t.GetComponentInChildren<Collider>(true))
      throw new Exception("Only isolated small natural render props may be moved "+t.name);
   float g=Gap(t);
   if(g<1.8f||g>3.3f)throw new Exception("Unexpected clearance for "+t.name+" "+g);
  }
  return list;
 }
 static float Gap(Transform tr){var b=tr.GetComponentInChildren<MeshRenderer>(true).bounds;var terr=TerrainAt(b.center);
  if(!terr)throw new Exception("No terrain under "+tr.name);
  return b.min.y-(terr.transform.position.y+terr.SampleHeight(b.center));
 }
 static string ProtectedDigest(HashSet<Transform> exclude) {
  var lines=new List<string>();
  foreach(var t in UnityEngine.Object.FindObjectsByType<Transform>(FindObjectsInactive.Include,FindObjectsSortMode.None)){
   if(exclude.Any(e=>t==e||t.IsChildOf(e)))continue;
   lines.Add(Path(t)+"|"+P(t.localPosition)+"|"+P(t.localRotation)+"|"+P(t.localScale));
  }
  lines.Sort(StringComparer.Ordinal);
  using(var h=SHA256.Create())return BitConverter.ToString(h.ComputeHash(Encoding.UTF8.GetBytes(string.Join("\n",lines)))).Replace("-","").ToLowerInvariant();
 }
 static void Move(List<Transform> ts,List<string> report){
  foreach(var t in ts){
   float gap=Gap(t);var original=t.position;
   t.position=original+Vector3.down*(gap+.12f);
   float post=Gap(t);if(Mathf.Abs(post+.12f)>.02f)throw new Exception("Grounding target not reached "+t.name+" "+post);
   if((original-t.position).x!=0||(original-t.position).z!=0)throw new Exception("Horizontal rock position changed");
   report.Add(Path(t)+"|"+P(original)+"|"+P(t.position)+"|oldGap="+gap.ToString("F4",CI)+"|newGap="+post.ToString("F4",CI));
  }
 }
 static void Shot(string dir,string tag,Vector3 focus){
  Directory.CreateDirectory(dir);var g=new GameObject("TEMP_WORLD_ROCK_QA_CAMERA");var c=g.AddComponent<Camera>();
  c.transform.position=focus+new Vector3(38,56,67);c.transform.rotation=Quaternion.LookRotation(focus-c.transform.position,Vector3.up);
  c.fieldOfView=52;c.nearClipPlane=.1f;c.farClipPlane=2500;c.clearFlags=CameraClearFlags.Skybox;
  var old=RenderTexture.active;var rt=new RenderTexture(950,670,24);c.targetTexture=rt;c.Render();RenderTexture.active=rt;
  var tx=new Texture2D(950,670,TextureFormat.RGB24,false);tx.ReadPixels(new Rect(0,0,950,670),0,0);tx.Apply();
  File.WriteAllBytes(dir+"/"+tag+".png",tx.EncodeToPNG());RenderTexture.active=old;c.targetTexture=null;
  UnityEngine.Object.DestroyImmediate(tx);UnityEngine.Object.DestroyImmediate(rt);UnityEngine.Object.DestroyImmediate(g);
 }
 static void Pictures(List<Transform> ts,string sub){
  string dir="Preview/WorldThreeFloatingRocks20261011/"+sub;
  foreach(var t in ts){
   var ground=TerrainAt(t.position);
   var target=new Vector3(t.position.x,ground.transform.position.y+ground.SampleHeight(t.position)+5,t.position.z);
   Shot(dir,t.name,target);
  }
 }
 public static void Preview(){
  var scene=EditorSceneManager.OpenScene(Scene,OpenSceneMode.Single);
  if(scene.isDirty||SHA(Scene)!=BaselineSHA)throw new Exception("Approved source scene changed");
  var ts=Targets();var protect=new HashSet<Transform>(ts);
  var before=ts.Select(t=>t.position).ToArray();string digest=ProtectedDigest(protect),fileHash=SHA(Scene);
  var lines=new List<string>();
  Pictures(ts,"before");
  try {
   Move(ts,lines);
   if(ProtectedDigest(protect)!=digest)throw new Exception("Any other object including buildings moved");
   Pictures(ts,"candidate");
  } finally {
   for(int i=0;i<ts.Count;i++)ts[i].position=before[i];
   if(ProtectedDigest(protect)!=digest||SHA(Scene)!=fileHash)throw new Exception("Preview leaked source changes");
  }
  Directory.CreateDirectory(Report);File.WriteAllLines(Report+"/three_world_rock_preview.txt",lines);
  Debug.Log("THREE_WORLD_ROCKS_PREVIEW_PASS rocks=3 buildingsMoved=0 sceneSaved=False");
 }
 public static void Apply(){
  var scene=EditorSceneManager.OpenScene(Scene,OpenSceneMode.Single);
  if(scene.isDirty||SHA(Scene)!=BaselineSHA||Directory.Exists(Backup)||!File.Exists(Report+"/three_world_rock_preview.txt"))
   throw new Exception("One-time approval and backup guard failed");
  var ts=Targets();var protect=new HashSet<Transform>(ts);string digest=ProtectedDigest(protect);
  Directory.CreateDirectory(Backup);File.Copy(Scene,Backup+"/Enroth.unity",false);
  if(SHA(Backup+"/Enroth.unity")!=BaselineSHA)throw new Exception("Original scene backup mismatch");
  var lines=new List<string>();Move(ts,lines);
  if(ProtectedDigest(protect)!=digest||lines.Count!=3)throw new Exception("Protected objects changed");
  EditorSceneManager.MarkSceneDirty(scene);
  if(!EditorSceneManager.SaveScene(scene))throw new Exception("Scene save failed");
  var data=new[]{"PASS=True","buildingPartsMoved=0","otherTransformsMoved=0","protectedPoseSHA="+digest,
   "originalSceneSHA="+BaselineSHA,"savedSceneSHA="+SHA(Scene)}.Concat(lines);
  Directory.CreateDirectory(Report);File.WriteAllLines(Report+"/three_world_rock_kept.txt",data);
  Debug.Log("THREE_WORLD_ROCKS_SAVED_PASS rockedGrounded=3 buildingsMoved=0 sceneDirty="+scene.isDirty);
 }
 public static void Verify(){
  var scene=EditorSceneManager.OpenScene(Scene,OpenSceneMode.Single);if(scene.isDirty)throw new Exception("Dirty scene on reload");
  var rows=File.ReadAllLines(Report+"/three_world_rock_kept.txt");var ids=rows.Where(r=>r.Contains("|oldGap=")).Select(r=>r.Split('|')[0]).ToHashSet();
  var ts=UnityEngine.Object.FindObjectsByType<Transform>(FindObjectsInactive.Include,FindObjectsSortMode.None).Where(t=>ids.Contains(Path(t))).ToList();
  if(ts.Count!=3)throw new Exception("Grounded rock identities missing");
  if(ProtectedDigest(new HashSet<Transform>(ts))!=rows.Single(r=>r.StartsWith("protectedPoseSHA=")).Split('=')[1])throw new Exception("A building or other object transform changed");
  foreach(var t in ts)if(Mathf.Abs(Gap(t)+.12f)>.025f)throw new Exception("Rock still floating "+t.name);
  if(SHA(Scene)!=rows.Single(r=>r.StartsWith("savedSceneSHA=")).Split('=')[1])throw new Exception("Saved source scene changed");
  string text="PASS=True threeWorldRocksGrounded=3 allOtherTransformPosesIdentical=True buildingPartsMoved=0 sceneDirty=False";
  File.WriteAllText(Report+"/three_world_rock_independent_verify.txt",text);
  Debug.Log("THREE_WORLD_ROCKS_INDEPENDENT_PASS "+text);
 }
}
