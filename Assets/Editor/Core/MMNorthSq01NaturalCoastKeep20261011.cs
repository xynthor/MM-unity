using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Collections.Generic;
using System.Security.Cryptography;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
public static class MMNorthSq01NaturalCoastKeep20261011 {
 const string Scene="Assets/Scenes/World/Enroth.unity";
 const string Input="Validation/NorthSq01CoastSeam20261011";
 const string Backup="Backups/NorthSq01NaturalCoastBefore20261011";
 const string Folder="Assets/World/WorldExtensions/Generated/NorthCoveRock20261011";
 const string Report="Validation/NorthSq01NaturalCoastKept20261011";
 static string Asset(int k)=>"Assets/World/WorldExtensions/Generated/NorthReference20261001/"+(k==0?"SweetWater_NorthTerrain.asset":"Kriegspire_NorthTerrain.asset");
 static string MeshPath(int k)=>Folder+"/Surface"+k+"CoveSlope.asset";
 static string Hash(string path) {using(var h=SHA256.Create())using(var stream=File.OpenRead(path))return BitConverter.ToString(h.ComputeHash(stream)).Replace("-","").ToLowerInvariant();}
 static float[,] Load(string name) {byte[] bytes=File.ReadAllBytes(Input+"/"+name);if(bytes.Length!=513*513*4)throw new Exception("Invalid 513x513 candidate "+name);var h=new float[513,513];Buffer.BlockCopy(bytes,0,h,0,bytes.Length);return h;}
 static Terrain Get(int k){float px=k==0?-1280:-768;return Terrain.activeTerrains.Single(t=>Mathf.Abs(t.transform.position.x-px)<.01f&&Mathf.Abs(t.transform.position.z-768)<.01f);}
 static MeshFilter Filter(int k){
  var o=GameObject.Find("North Alpine Rock Surface "+k+" 20261007");
  if(!o||!o.activeSelf)throw new Exception("Missing rock overlay "+k);
  var f=o.GetComponent<MeshFilter>();var r=o.GetComponent<MeshRenderer>();
  if(!f||!f.sharedMesh||f.sharedMesh.vertexCount!=263169||!r||!r.sharedMaterial||r.sharedMaterial.shader.name!="MMUnity/NorthAlpineCoveFade20261011")
   throw new Exception("Unexpected rock setup at "+k);
  return f;
 }
 static bool Dress(Transform t)=>t.name.Contains("AuthoredPine_")||t.name.StartsWith("TransitionPine_")||t.name.StartsWith("ReferenceTallPine_")||t.name.StartsWith("SourceTree_")||t.name.StartsWith("Eco_")||t.name.StartsWith("AuthoredBoulder_")||t.name.StartsWith("Bush_")||t.name.StartsWith("Pine_");
 static float S(float a,float b,float v)=>Mathf.SmoothStep(0,1,Mathf.InverseLerp(a,b,v));
 static float Sample(float[,] h,float x,float z) {
  x=Mathf.Clamp(x,0,512);z=Mathf.Clamp(z,0,512);int i=Mathf.Min(511,(int)x),j=Mathf.Min(511,(int)z);float a=x-i,b=z-j;
  return Mathf.Lerp(Mathf.Lerp(h[j,i],h[j,i+1],a),Mathf.Lerp(h[j+1,i],h[j+1,i+1],a),b);
 }
 static Dictionary<string,string> Protected(){
  var paths=Terrain.activeTerrains.Select(t=>AssetDatabase.GetAssetPath(t.terrainData)).Where(p=>p!=Asset(0)&&p!=Asset(1)).ToList();
  paths.AddRange(Enumerable.Range(0,4).Select(k=>"Assets/World/WorldExtensions/Generated/NorthAlpine20261007/Surface"+k+".asset"));
  paths.Add("Assets/World/WorldExtensions/Generated/ArchipelagoWestEast20261001/ArchipelagoConnectedWater.asset");
  paths.Add("Assets/World/WorldExtensions/Generated/NorthReference20261001/NorthInlandIcyWater20261006.asset");
  paths.Add("Assets/Shaders/MMNorthAlpineCoveFade20261011.shader");
  paths.Add("Assets/World/WorldExtensions/Generated/NorthCoastFade20261011/Surface0CoveFade.mat");
  paths.Add("Assets/World/WorldExtensions/Generated/NorthCoastFade20261011/Surface1CoveFade.mat");
  var dict=new Dictionary<string,string>();
  foreach(string p in paths.Distinct().OrderBy(s=>s)){if(!File.Exists(p))throw new Exception("Missing protected "+p);dict[p]=Hash(p);}
  return dict;
 }
 static string SeamQA(){
  var ts=Terrain.activeTerrains;int pairs=0,invalid=0;float max=0;
  foreach(var a in ts)foreach(var b in ts){
   float dx=b.transform.position.x-a.transform.position.x,dz=b.transform.position.z-a.transform.position.z;
   bool east=Mathf.Abs(dx-512)<.01f&&Mathf.Abs(dz)<.01f;
   bool north=Mathf.Abs(dz-512)<.01f&&Mathf.Abs(dx)<.01f;
   if(!east&&!north)continue;
   pairs++;
   for(int t=0;t<=512;t++){
    float x=east?b.transform.position.x:a.transform.position.x+t;
    float z=east?a.transform.position.z+t:b.transform.position.z;
    var p=new Vector3(x,0,z);
    float diff=Mathf.Abs(a.transform.position.y+a.SampleHeight(p)-b.transform.position.y-b.SampleHeight(p));
    max=Mathf.Max(max,diff);if(diff>.001f)invalid++;
   }
  }
  int mismatchedColliders=ts.Count(t=>!t.GetComponent<TerrainCollider>()||t.GetComponent<TerrainCollider>().terrainData!=t.terrainData);
  string s="terrains="+ts.Length+" adjacentPairs="+pairs+" badSeamSamples="+invalid+" maxSeamM="+max.ToString("F6")+" colliderMismatch="+mismatchedColliders;
  if(ts.Length!=30||pairs!=49||invalid>0||mismatchedColliders>0)throw new Exception("SeamQA failed "+s);
  return s;
 }
 static string WaterQA(){
  int checkedVerts=0,newBuried=0,nowBuried=0,oldBuried=0,inlandBuried=0;
  var old=new[]{Load("sq0_current.f32"),Load("sq1_current.f32")};
  foreach(string label in new[]{"Linked connected sea and existing lowland waters 20260929","North Extension Inland Icy Water 20261006"}){
   var o=GameObject.Find(label);if(!o)throw new Exception("Water object absent "+label);
   var mesh=o.GetComponent<MeshFilter>().sharedMesh;if(!mesh)throw new Exception("Water asset missing "+label);
   bool inland=label.StartsWith("North Extension");
   foreach(var v in mesh.vertices){
    var p=o.transform.TransformPoint(v);
    if(p.x< -1280||p.x>=-256||p.z<768||p.z>1280)continue;
    int k=p.x< -768?0:1;checkedVerts++;
    float before=Sample(old[k],p.x-Get(k).transform.position.x,p.z-768)-p.y;
    float after=Get(k).transform.position.y+Get(k).SampleHeight(p)-p.y;
    if(before>.3f)oldBuried++;if(after>.3f){nowBuried++;if(inland)inlandBuried++;}
    if(before<=.3f&&after>.3f+.001f)newBuried++;
   }
  }
  string s="waterVertices="+checkedVerts+" oldCovered="+oldBuried+" currentCovered="+nowBuried+" newlyCovered="+newBuried+" inlandCovered="+inlandBuried;
  if(newBuried!=0||inlandBuried!=0||checkedVerts<200||nowBuried>oldBuried)throw new Exception("WaterQA failed "+s);
  return s;
 }
 static void CheckHeight(){
  for(int k=0;k<2;k++){
   var t=Get(k);var target=Load("U2_sq"+k+".f32");var td=t.terrainData;
   if(AssetDatabase.GetAssetPath(td)!=Asset(k))throw new Exception("Terrain path drift "+k);
   var h=td.GetHeights(0,0,513,513);float worst=0;
   for(int z=0;z<=512;z++)for(int x=0;x<=512;x++)worst=Mathf.Max(worst,Mathf.Abs(t.transform.position.y+h[z,x]*td.size.y-target[z,x]));
   if(worst>.015f)throw new Exception("Saved height mismatch k="+k+" maxM="+worst);
   if(AssetDatabase.GetAssetPath(Filter(k).sharedMesh)!=MeshPath(k))throw new Exception("Rock mesh reference mismatch k="+k);
  }
 }
 public static void Preflight(){
  var sc=EditorSceneManager.OpenScene(Scene,OpenSceneMode.Single);
  if(sc.isDirty||Directory.Exists(Backup)||Directory.Exists(Folder))throw new Exception("Preflight clean-scene / one-time guard failed");
  var old=new[]{Load("sq0_current.f32"),Load("sq1_current.f32")};var next=new[]{Load("U2_sq0.f32"),Load("U2_sq1.f32")};
  int changed=0;float worst=0;
  for(int k=0;k<2;k++){
   var t=Get(k);var raw=t.terrainData.GetHeights(0,0,513,513);
   for(int z=0;z<=512;z++)for(int x=0;x<=512;x++){
    float current=t.transform.position.y+raw[z,x]*t.terrainData.size.y;
    if(Mathf.Abs(current-old[k][z,x])>.015f)throw new Exception("Source F2P terrain mismatch k="+k+" x="+x+" z="+z);
    float delta=Mathf.Abs(next[k][z,x]-old[k][z,x]);if(delta>.01f){changed++;worst=Mathf.Max(worst,delta);}
   }
  }
  var p=Protected();string seams=SeamQA(),water=WaterQA();
  Debug.Log("SQ01_NATURAL_COAST_PREFLIGHT_PASS changed="+changed+" max="+worst+" protected="+p.Count+" "+seams+" "+water);
 }
 public static void Apply(){
  var sc=EditorSceneManager.OpenScene(Scene,OpenSceneMode.Single);
  if(sc.isDirty||Directory.Exists(Backup)||Directory.Exists(Folder))throw new Exception("One-time apply guard failed");
  var before=Protected();
  var initialHash=Hash(Scene);var t=new[]{Get(0),Get(1)};
  var originals=new[]{Load("sq0_current.f32"),Load("sq1_current.f32")};
  var next=new[]{Load("U2_sq0.f32"),Load("U2_sq1.f32")};
  var oldMesh=new[]{Filter(0).sharedMesh,Filter(1).sharedMesh};
  int changed=0;float largest=0,outer=0,seam=0;
  for(int z=0;z<=512;z++)seam=Mathf.Max(seam,Mathf.Abs(next[0][z,512]-next[1][z,0]));
  for(int k=0;k<2;k++){
   var td=t[k].terrainData;
   var height=td.GetHeights(0,0,513,513);
   for(int z=0;z<=512;z++)for(int x=0;x<=512;x++){
    float v=t[k].transform.position.y+height[z,x]*td.size.y;
    if(Mathf.Abs(v-originals[k][z,x])>.015f)throw new Exception("Baseline mismatch "+k+" "+x+","+z);
    float delta=next[k][z,x]-originals[k][z,x];if(Mathf.Abs(delta)>.01f){changed++;largest=Mathf.Max(largest,Mathf.Abs(delta));}
    if(z==0||z==512||(k==0&&x==0)||(k==1&&x==512))outer=Mathf.Max(outer,Mathf.Abs(delta));
    float normalized=(next[k][z,x]-t[k].transform.position.y)/td.size.y;
    if(normalized<0||normalized>1)throw new Exception("New height outside allowed terrain range");
   }
  }
  if(changed<12000||changed>14000||largest>8.2f||outer>.0001f||seam>.00001f)throw new Exception("Candidate checks failed "+changed+"/"+largest+"/"+outer+"/"+seam);
  Directory.CreateDirectory(Backup);
  File.Copy(Scene,Backup+"/Enroth.unity");
  for(int k=0;k<2;k++)File.Copy(Asset(k),Backup+"/Terrain"+k+".asset");
  if(Hash(Backup+"/Enroth.unity")!=initialHash)throw new Exception("Scene backup failed");
  AssetDatabase.CreateFolder("Assets/World/WorldExtensions/Generated","NorthCoveRock20261011");
  for(int k=0;k<2;k++){
   var td=t[k].terrainData;var h=new float[513,513];
   for(int z=0;z<=512;z++)for(int x=0;x<=512;x++)h[z,x]=(next[k][z,x]-t[k].transform.position.y)/td.size.y;
   td.SetHeights(0,0,h);td.SetBaseMapDirty();EditorUtility.SetDirty(td);t[k].Flush();
   var m=UnityEngine.Object.Instantiate(oldMesh[k]);m.name="Surface"+k+"CoveSlope";var vertices=m.vertices;var normals=m.normals;
   for(int z=0;z<=512;z++)for(int x=0;x<=512;x++){
    int i=z*513+x;float delta=next[k][z,x]-originals[k][z,x];
    if(Mathf.Abs(delta)<.01f)continue;
    float oldLocal=originals[k][z,x]-t[k].transform.position.y,newLocal=next[k][z,x]-t[k].transform.position.y;
    float oldOffset=Mathf.Lerp(.1f,.6f,S(4,28,oldLocal-24)),newOffset=Mathf.Lerp(.1f,.6f,S(4,28,newLocal-24));
    vertices[i].y+=delta+newOffset-oldOffset;normals[i]=td.GetInterpolatedNormal(x/512f,z/512f);
   }
   m.vertices=vertices;m.normals=normals;m.RecalculateBounds();
   AssetDatabase.CreateAsset(m,MeshPath(k));Filter(k).sharedMesh=m;
  }
  int moved=0;
  foreach(var tr in UnityEngine.Object.FindObjectsByType<Transform>(FindObjectsSortMode.None)){
   if(!Dress(tr)||tr.GetComponentsInParent<Transform>().Skip(1).Any(Dress))continue;
   var p=tr.position;if(p.x< -1280||p.x>=-256||p.z<768||p.z>=1280)continue;
   int k=p.x< -768?0:1;
   float x=p.x-t[k].transform.position.x,z=p.z-768;
   float delta=Sample(next[k],x,z)-Sample(originals[k],x,z);
   if(Mathf.Abs(delta)>.02f){tr.position=p+Vector3.up*delta;moved++;}
  }
  if(moved>0)throw new Exception("Unexpected moved vegetation; requires review count="+moved);
  EditorSceneManager.MarkSceneDirty(sc);
  AssetDatabase.SaveAssets();
  if(!EditorSceneManager.SaveScene(sc))throw new Exception("Scene save failed");
  CheckHeight();string seamQA=SeamQA(),waterQA=WaterQA();
  foreach(var p in before)if(!File.Exists(p.Key)||Hash(p.Key)!=p.Value)throw new Exception("Protected source changed "+p.Key);
  Directory.CreateDirectory(Report);
  var lines=new List<string>{"PASS=True","sceneBeforeSHA="+initialHash,"sceneAfterSHA="+Hash(Scene),"changed="+changed,"maxDeltaM="+largest.ToString("F4"),"outerBorderM="+outer.ToString("F6"),"sq01JoinM="+seam.ToString("F6"),"dressingMoved="+moved,seamQA,waterQA};
  foreach(var p in before)lines.Add("PROTECTED|"+p.Key+"|"+p.Value);
  File.WriteAllLines(Report+"/apply.txt",lines);
  Debug.Log("SQ01_NATURAL_COAST_SAVED_PASS "+seamQA+" "+waterQA+" changed="+changed+" sceneDirty="+sc.isDirty);
 }
 public static void Verify(){
  var sc=EditorSceneManager.OpenScene(Scene,OpenSceneMode.Single);if(sc.isDirty)throw new Exception("Reopened scene dirty");
  CheckHeight();var seam=SeamQA();var water=WaterQA();
  var report=File.ReadAllLines(Report+"/apply.txt");
  foreach(string s in report.Where(v=>v.StartsWith("PROTECTED|"))){
   var p=s.Split('|');if(p.Length!=3||!File.Exists(p[1])||Hash(p[1])!=p[2])throw new Exception("Protected hash changed "+s);
  }
  string prev=report.Single(v=>v.StartsWith("sceneAfterSHA=")).Split('=')[1];
  if(Hash(Scene)!=prev)throw new Exception("Saved scene hash drift");
  string result="PASS=True cleanScene=True "+seam+" "+water+" sceneSHA="+prev;
  File.WriteAllText(Report+"/independent_verify.txt",result);
  Debug.Log("SQ01_NATURAL_COAST_INDEPENDENT_PASS "+result);
 }
}
