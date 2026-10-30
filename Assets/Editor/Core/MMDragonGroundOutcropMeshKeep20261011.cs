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

public static class MMDragonGroundOutcropMeshKeep20261011 {
 const string Scene="Assets/Scenes/World/Enroth.unity";
 const string Original="Assets/World/WorldExtensions/Generated/DragonSouthInlandOutcrops20260930/Cell_1.asset";
 const string Folder="Assets/World/WorldExtensions/Generated/DragonGroundedOutcrops20261011";
 const string MeshPath=Folder+"/Cell_1_Grounded.asset";
 const string Report="Validation/DragonFloatingRocks20261011";
 const string Backup="Backups/DragonOutcropGroundingBefore20261011";
 const string SourceSceneSHA="3ef15aa4d8690039ff6618ce40db235fb55a95513ea01fa4f6f87af3da0a7bde";
 const string TargetName="Inland Outcrops Cell 1";
 static readonly CultureInfo CI=CultureInfo.InvariantCulture;
 static string Hash(string p){using(var sha=SHA256.Create())using(var fs=File.OpenRead(p))return BitConverter.ToString(sha.ComputeHash(fs)).Replace("-","").ToLowerInvariant();}
 static MeshFilter Target(){
  var f=UnityEngine.Object.FindObjectsByType<MeshFilter>(FindObjectsInactive.Include,FindObjectsSortMode.None).SingleOrDefault(f=>f.name==TargetName&&f.transform.parent&&f.transform.parent.name=="Dragon South Reference Inland Outcrops 20260930");
  if(!f||!f.sharedMesh||f.sharedMesh.vertexCount!=6680)throw new Exception("Target source mesh missing");
  if(f.GetComponent<MeshCollider>())throw new Exception("Cannot change collider without explicit plan");
  return f;
 }
 static Terrain Ground(Vector3 p){
  var t=Terrain.activeTerrains.FirstOrDefault(t=>p.x>=t.transform.position.x&&p.x<=t.transform.position.x+512&&p.z>=t.transform.position.z&&p.z<=t.transform.position.z+512);
  if(!t)throw new Exception("Outcrop outside live terrain "+p);
  return t;
 }
 static int Find(int[] pa,int x){while(pa[x]!=x){pa[x]=pa[pa[x]];x=pa[x];}return x;}
 static void Union(int[] pa,int a,int b){a=Find(pa,a);b=Find(pa,b);if(a!=b)pa[b]=a;}
 static List<List<int>> Components(Mesh m){
  var v=m.vertices;var t=m.triangles;var p=Enumerable.Range(0,v.Length).ToArray();
  for(int i=0;i<t.Length;i+=3){Union(p,t[i],t[i+1]);Union(p,t[i],t[i+2]);}
  var dic=new Dictionary<int,List<int>>();
  for(int i=0;i<v.Length;i++){int root=Find(p,i);if(!dic.ContainsKey(root))dic[root]=new List<int>();dic[root].Add(i);}
  return dic.Values.OrderBy(g=>g.Min()).ToList();
 }
 static List<List<int>> RockInstances(Mesh m){
  var c=Components(m);var groups=new List<List<int>>();
  for(int i=0;i<c.Count;){
   int size=c[i].Count;int howMany=size==91?5:size==94?2:0;
   if(howMany==0||i+howMany>c.Count)throw new Exception("Unexpected rock component grouping "+i+"/"+size);
   int[] expected=size==91?new[]{91,65,79,85,66}:new[]{94,225};
   for(int k=0;k<howMany;k++)if(c[i+k].Count!=expected[k])throw new Exception("Mixed component in one outcrop");
   groups.Add(c.Skip(i).Take(howMany).SelectMany(x=>x).ToList());i+=howMany;
  }
  if(groups.Count!=18)throw new Exception("Unexpected number of original rock instances "+groups.Count);
  return groups;
 }
 static float Gap(MeshFilter f,Vector3 world){
  var t=Ground(world);return world.y-(t.transform.position.y+t.SampleHeight(world));
 }
 static string PoseDigest(){
  var ts=UnityEngine.Object.FindObjectsByType<Transform>(FindObjectsInactive.Include,FindObjectsSortMode.None);
  var rows=new List<string>();
  foreach(var t in ts){
   var hierarchy=new List<string>();for(var p=t;p;p=p.parent)hierarchy.Add(p.name+"#"+p.GetSiblingIndex());
   hierarchy.Reverse();
   Vector3 v=t.localPosition,s=t.localScale;Quaternion q=t.localRotation;
   rows.Add(string.Join("/",hierarchy)+"|"+v.x.ToString("R",CI)+","+v.y.ToString("R",CI)+","+v.z.ToString("R",CI)+
    "|"+q.x.ToString("R",CI)+","+q.y.ToString("R",CI)+","+q.z.ToString("R",CI)+","+q.w.ToString("R",CI)+
    "|"+s.x.ToString("R",CI)+","+s.y.ToString("R",CI)+","+s.z.ToString("R",CI));
  }
  rows.Sort(StringComparer.Ordinal);
  using(var sha=SHA256.Create())return BitConverter.ToString(sha.ComputeHash(Encoding.UTF8.GetBytes(string.Join("\n",rows)))).Replace("-","").ToLowerInvariant();
 }
 static Mesh Build(MeshFilter f,List<string> details){
  var original=f.sharedMesh;
  if(AssetDatabase.GetAssetPath(original)!=Original)throw new Exception("Unexpected source mesh asset "+AssetDatabase.GetAssetPath(original));
  var parts=RockInstances(original);var v=original.vertices;
  int count=0,shiftedVertices=0;float max=0;int idx=0;
  foreach(var part in parts){
   float lowest=99999;
   foreach(int i in part)lowest=Mathf.Min(lowest,Gap(f,f.transform.TransformPoint(v[i])));
   if(lowest>1f){
    float shift=-(lowest+.18f);
    foreach(int i in part){var w=f.transform.TransformPoint(v[i]);w.y+=shift;v[i]=f.transform.InverseTransformPoint(w);}
    count++;shiftedVertices+=part.Count;max=Mathf.Max(max,-shift);
    details.Add("rock="+idx+"|vertices="+part.Count+"|originalMinGap="+lowest.ToString("F4",CI)+"|downM="+(-shift).ToString("F4",CI));
   }
   idx++;
  }
  if(count!=10||shiftedVertices!=3726||max>32||max<30)throw new Exception("Unsafe outcrop set count="+count+" vertices="+shiftedVertices+" max="+max);
  var m=UnityEngine.Object.Instantiate(original);m.name="DragonOutcropCell1Grounded";m.vertices=v;m.RecalculateBounds();
  if(m.triangles.Length!=original.triangles.Length||m.vertexCount!=original.vertexCount)throw new Exception("Unexpected topology mutation");
  details.Insert(0,"PASS=True movedWholeNaturalRocks="+count+" movedMeshVertices="+shiftedVertices+" maxDownM="+max.ToString("F4",CI));
  return m;
 }
 static void Capture(string sub){
  string dir="Preview/DragonFloatingRocks20261011/outcrop_"+sub;Directory.CreateDirectory(dir);
  Shot(dir+"/south_oblique.png",new Vector3(-1530,77,674),new Vector3(-1605,19,596),false,57);
  Shot(dir+"/south_top.png",new Vector3(-1600,220,591),new Vector3(-1600,0,591),true,115);
  Shot(dir+"/south_side.png",new Vector3(-1550,39,600),new Vector3(-1605,20,598),false,53);
 }
 static void Shot(string path,Vector3 pos,Vector3 focus,bool top,float size){
  var g=new GameObject("TEMP_DRAGON_OUTCROP_CAMERA");var c=g.AddComponent<Camera>();
  c.transform.position=pos;c.transform.rotation=Quaternion.LookRotation(focus-pos,Vector3.up);
  c.orthographic=top;if(top)c.orthographicSize=size;else c.fieldOfView=size;c.nearClipPlane=.1f;c.farClipPlane=2200;c.clearFlags=CameraClearFlags.Skybox;
  var old=RenderTexture.active;var rt=new RenderTexture(1400,920,24);c.targetTexture=rt;c.Render();RenderTexture.active=rt;
  var img=new Texture2D(1400,920,TextureFormat.RGB24,false);img.ReadPixels(new Rect(0,0,1400,920),0,0);img.Apply();
  File.WriteAllBytes(path,img.EncodeToPNG());RenderTexture.active=old;c.targetTexture=null;
  UnityEngine.Object.DestroyImmediate(img);UnityEngine.Object.DestroyImmediate(rt);UnityEngine.Object.DestroyImmediate(g);
 }
 static void QA(MeshFilter f,Mesh orig){
  var m=f.sharedMesh;if(m.vertexCount!=6680||m.triangles.Length!=orig.triangles.Length)throw new Exception("Outcrop mesh geometry count mismatch");
  var originalGroups=RockInstances(orig);var currentGroups=RockInstances(m);
  var old=orig.vertices;var cur=m.vertices;int shifted=0,rocks=0;float longest=0;
  for(int k=0;k<originalGroups.Count;k++){
   var group=originalGroups[k];float min=999;
   foreach(var i in group)min=Mathf.Min(min,Gap(f,f.transform.TransformPoint(old[i])));
   bool moving=min>1f;if(moving)rocks++;
   float adjusted=999;
   foreach(var i in group){
    var p=cur[i];adjusted=Mathf.Min(adjusted,Gap(f,f.transform.TransformPoint(p)));
    float diff=(p-old[i]).magnitude;
    if(moving){if(diff>.01)shifted++;longest=Mathf.Max(longest,diff);}
    else if(diff>.00001f)throw new Exception("Original grounded rock mutated group="+k);
   }
   if(moving&&Mathf.Abs(adjusted+.18f)>.035f)throw new Exception("Grounding gap group "+k+" "+adjusted);
  }
  if(rocks!=10||shifted!=3726)throw new Exception("QA count failed rocks="+rocks+" vertices="+shifted);
 }
 public static void Preview(){
  var scene=EditorSceneManager.OpenScene(Scene,OpenSceneMode.Single);if(scene.isDirty)throw new Exception("Scene dirty");
  var f=Target();var original=f.sharedMesh;string hash=Hash(Scene),sha=Hash(Original),digest=PoseDigest();
  var details=new List<string>();var temp=Build(f,details);Capture("before");
  try{
   f.sharedMesh=temp;QA(f,original);if(PoseDigest()!=digest)throw new Exception("A building or transform was touched");
   Capture("candidate");Directory.CreateDirectory(Report);File.WriteAllLines(Report+"/outcrop_preview.txt",details);
  }finally{f.sharedMesh=original;UnityEngine.Object.DestroyImmediate(temp);
   if(Hash(Scene)!=hash||Hash(Original)!=sha||PoseDigest()!=digest)throw new Exception("Preview altered source or transforms");
  }
  Debug.Log("DRAGON_OUTCROP_PREVIEW_PASS rocks=10 buildingPartsMoved=0 sourceSaved=False");
 }
 public static void Apply(){
  var scene=EditorSceneManager.OpenScene(Scene,OpenSceneMode.Single);
  if(scene.isDirty||Hash(Scene)!=SourceSceneSHA||Directory.Exists(Backup)||Directory.Exists(Folder)||!File.Exists(Report+"/outcrop_preview.txt"))
   throw new Exception("One-time source and preview guards");
  var f=Target();var original=f.sharedMesh;string oldHash=Hash(Original),digest=PoseDigest();
  var details=new List<string>();var result=Build(f,details);
  Directory.CreateDirectory(Backup);File.Copy(Scene,Backup+"/Enroth.unity",false);
  if(Hash(Backup+"/Enroth.unity")!=SourceSceneSHA){UnityEngine.Object.DestroyImmediate(result);throw new Exception("Backup SHA not trusted");}
  AssetDatabase.CreateFolder("Assets/World/WorldExtensions/Generated","DragonGroundedOutcrops20261011");
  AssetDatabase.CreateAsset(result,MeshPath);f.sharedMesh=result;
  QA(f,original);if(PoseDigest()!=digest||Hash(Original)!=oldHash)throw new Exception("Original natural mesh or transforms modified");
  EditorSceneManager.MarkSceneDirty(scene);AssetDatabase.SaveAssets();if(!EditorSceneManager.SaveScene(scene))throw new Exception("Scene failed save");
  Directory.CreateDirectory(Report);
  File.WriteAllLines(Report+"/outcrop_kept.txt",new[]{"PASS=True","poseSHA="+digest,"originalMeshSHA="+oldHash,"sourceSceneSHA="+SourceSceneSHA,"savedSceneSHA="+Hash(Scene),"buildingPartsMoved=0","allTransformsMoved=0","oldRockMeshUnmodified=True"}.Concat(details));
  Debug.Log("DRAGON_OUTCROP_SAVED_PASS wholeRocksGrounded=10 originalRockMeshUnchanged=True buildingsMoved=0");
 }
 public static void Verify(){
  var scene=EditorSceneManager.OpenScene(Scene,OpenSceneMode.Single);if(scene.isDirty)throw new Exception("Dirty scene reopen");
  var details=File.ReadAllLines(Report+"/outcrop_kept.txt");
  var f=TargetReadOnly();var src=AssetDatabase.LoadAssetAtPath<Mesh>(Original);
  QA(f,src);
  if(PoseDigest()!=details.Single(x=>x.StartsWith("poseSHA=")).Split('=')[1])throw new Exception("Any building or transform changed");
  if(Hash(Original)!=details.Single(x=>x.StartsWith("originalMeshSHA=")).Split('=')[1])throw new Exception("Original source rock asset hash mismatch");
  if(Hash(Scene)!=details.Single(x=>x.StartsWith("savedSceneSHA=")).Split('=')[1])throw new Exception("Saved scene SHA mismatch");
  File.WriteAllText(Report+"/outcrop_independent_verify.txt","PASS=True wholeNaturalOutcropsGrounded=10 otherOutcropsPreserved=8 buildingPartsMoved=0 allTransformsInvariant=True originalMeshUnchanged=True cleanScene=True");
  Debug.Log("DRAGON_OUTCROP_INDEPENDENT_PASS movedWholeNaturalRocks=10 buildingsMoved=0 allTransformPosesInvariant=True");
 }
 static MeshFilter TargetReadOnly(){
  var f=UnityEngine.Object.FindObjectsByType<MeshFilter>(FindObjectsInactive.Include,FindObjectsSortMode.None).SingleOrDefault(f=>f.name==TargetName&&f.transform.parent&&f.transform.parent.name=="Dragon South Reference Inland Outcrops 20260930");
  if(!f||!f.sharedMesh||AssetDatabase.GetAssetPath(f.sharedMesh)!=MeshPath)throw new Exception("Linked grounded outcrop mesh missing");
  return f;
 }
}
