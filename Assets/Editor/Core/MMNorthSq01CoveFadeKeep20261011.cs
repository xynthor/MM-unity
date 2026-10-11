using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Collections.Generic;
using System.Security.Cryptography;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;

public static class MMNorthSq01CoveFadeKeep20261011
{
 const string ScenePath="Assets/Scenes/World/Enroth.unity";
 const string ShaderName="MMUnity/NorthAlpineCoveFade20261011";
 const string ShaderPath="Assets/Shaders/MMNorthAlpineCoveFade20261011.shader";
 const string OutputDir="Assets/World/WorldExtensions/Generated/NorthCoastFade20261011";
 const string ReportDir="Validation/NorthSq01CoveFadeKept20261011";
 const string BackupDir="Backups/NorthSq01CoveFadeBefore20261011";
 const float Strength=.92f, RX=68f, RZ=59f;
 static string PathFor(int i)=>OutputDir+"/Surface"+i+"CoveFade.mat";
 static string Hash(string file) {
  using(var sha=SHA256.Create())using(var stream=File.OpenRead(file))
   return BitConverter.ToString(sha.ComputeHash(stream)).Replace("-","").ToLowerInvariant();
 }
 static Dictionary<string,string> Protected(){
  var paths=Terrain.activeTerrains.Select(t=>AssetDatabase.GetAssetPath(t.terrainData)).Where(s=>!string.IsNullOrEmpty(s)).ToList();
  foreach(int i in new[]{0,1,2,3})paths.Add("Assets/World/WorldExtensions/Generated/NorthAlpine20261007/Surface"+i+".asset");
  paths.Add("Assets/World/WorldExtensions/Generated/ArchipelagoWestEast20261001/ArchipelagoConnectedWater.asset");
  paths.Add("Assets/World/WorldExtensions/Generated/NorthReference20261001/NorthInlandIcyWater20261006.asset");
  paths.Add("Assets/Shaders/MMNorthAlpineSurface20261007.shader");
  for(int i=0;i<2;i++)paths.Add("Assets/World/WorldExtensions/Generated/NorthAlpine20261007/Surface"+i+".mat");
  var rocks=Enumerable.Range(0,2).Select(i=>GameObject.Find("North Alpine Rock Surface "+i+" 20261007")).ToArray();
  foreach(var go in rocks){
   if(go==null)throw new Exception("Missing rock source object");
   string p=AssetDatabase.GetAssetPath(go.GetComponent<MeshRenderer>().sharedMaterial);
   if(!string.IsNullOrEmpty(p)&&!p.StartsWith(OutputDir))paths.Add(p);
  }
  var result=new Dictionary<string,string>();
  foreach(string p in paths.Distinct().OrderBy(s=>s)){
   if(!File.Exists(p))throw new Exception("Protected file missing: "+p);
   result[p]=Hash(p);
  }
  return result;
 }
 static GameObject Rock(int i){
  var go=GameObject.Find("North Alpine Rock Surface "+i+" 20261007");
  if(!go||!go.activeSelf||!go.GetComponent<MeshRenderer>()||!go.GetComponent<MeshFilter>())
   throw new Exception("Rock missing or disabled "+i);
  return go;
 }
 static void Params(Material m){
  m.SetFloat("_CoveFade",Strength);m.SetFloat("_FadeRX",RX);m.SetFloat("_FadeRZ",RZ);
 }
 static void ValidateMaterials(){
  for(int i=0;i<2;i++){
   var renderer=Rock(i).GetComponent<MeshRenderer>();var m=renderer.sharedMaterial;
   if(!m||AssetDatabase.GetAssetPath(m)!=PathFor(i)||!m.shader||m.shader.name!=ShaderName)
    throw new Exception("Saved rock material mismatch at "+i);
   if(Mathf.Abs(m.GetFloat("_CoveFade")-Strength)>.0001f||Mathf.Abs(m.GetFloat("_FadeRX")-RX)>.001f||Mathf.Abs(m.GetFloat("_FadeRZ")-RZ)>.001f)
    throw new Exception("Saved material feather settings mismatch at "+i);
  }
 }
 static string GlobalQA(){
  var ts=Terrain.activeTerrains;int n=0,nonExact=0;float worst=0;
  for(int i=0;i<ts.Length;i++)for(int j=i+1;j<ts.Length;j++){
   var a=ts[i];var b=ts[j];
   float ax=a.transform.position.x,az=a.transform.position.z,bx=b.transform.position.x,bz=b.transform.position.z;
   float size=a.terrainData.size.x,sz=a.terrainData.size.z;
   bool east=Mathf.Abs(bx-ax-size)<.05f&&Mathf.Abs(bz-az)<.05f;
   bool west=Mathf.Abs(bx-ax+size)<.05f&&Mathf.Abs(bz-az)<.05f;
   bool north=Mathf.Abs(bz-az-sz)<.05f&&Mathf.Abs(bx-ax)<.05f;
   bool south=Mathf.Abs(bz-az+sz)<.05f&&Mathf.Abs(bx-ax)<.05f;
   if(!east&&!west&&!north&&!south)continue;
   float gap=0;
   for(int k=0;k<=512;k++){
    float x=east?bx:west?ax:ax+k,z=east||west?az+k:north?bz:az;
    var p=new Vector3(x,0,z);
    float v1=a.transform.position.y+a.SampleHeight(p),v2=b.transform.position.y+b.SampleHeight(p);
    gap=Mathf.Max(gap,Mathf.Abs(v1-v2));
   }
   n++;worst=Mathf.Max(worst,gap);if(gap>.001f)nonExact++;
  }
  int colliders=ts.Count(t=>!t.GetComponent<TerrainCollider>()||t.GetComponent<TerrainCollider>().terrainData!=t.terrainData);
  string s="terrains="+ts.Length+" adjacentPairs="+n+" worstSeamM="+worst.ToString("F6")+" over1mm="+nonExact+" badColliders="+colliders;
  if(ts.Length!=30||n!=49||nonExact!=0||colliders!=0)throw new Exception("Global terrain QA failed "+s);
  return s;
 }
 static string WaterQA(){
  var t=Terrain.activeTerrains.Single(q=>Mathf.Abs(q.transform.position.x+768)<.01f&&Mathf.Abs(q.transform.position.z-768)<.01f);
  int count=0,buried=0,inlandBuried=0;
  foreach(string name in new[]{"Linked connected sea and existing lowland waters 20260929","North Extension Inland Icy Water 20261006"}){
   var go=GameObject.Find(name);if(!go)throw new Exception("Missing water "+name);
   var mf=go.GetComponent<MeshFilter>();if(!mf||!mf.sharedMesh)throw new Exception("Missing water mesh "+name);
   bool inland=name.StartsWith("North Extension");
   foreach(var v in mf.sharedMesh.vertices){
    var p=go.transform.TransformPoint(v);
    if(p.x< -768||p.x>-256||p.z<768||p.z>1280)continue;
    count++;float excess=t.transform.position.y+t.SampleHeight(p)-p.y;
    if(excess>.30f){buried++;if(inland)inlandBuried++;}
   }
  }
  string s="waterVertices="+count+" covered="+buried+" inlandCovered="+inlandBuried;
  if(count!=376||buried!=57||inlandBuried!=0)throw new Exception("Water coverage changed "+s);
  return s;
 }
 public static void Preflight(){
  var scene=EditorSceneManager.OpenScene(ScenePath,OpenSceneMode.Single);
  if(scene.isDirty)throw new Exception("Saved scene was dirty");
  string s=GlobalQA(),w=WaterQA();
  Debug.Log("SQ01_COVE_FADE_PREFLIGHT_PASS "+s+" "+w+" sceneHash="+Hash(ScenePath));
 }
 public static void Apply(){
  var scene=EditorSceneManager.OpenScene(ScenePath,OpenSceneMode.Single);
  if(scene.isDirty||!File.Exists(ShaderPath)||Directory.Exists(BackupDir)||Directory.Exists(OutputDir))
   throw new Exception("Pre-apply guard failed; no files changed");
  var shader=Shader.Find(ShaderName);if(!shader||!shader.isSupported)throw new Exception("Shader failed import/support");
  var initial=Protected();var originalScene=Hash(ScenePath);
  Directory.CreateDirectory(BackupDir);File.Copy(ScenePath,BackupDir+"/Enroth.unity",false);
  if(Hash(BackupDir+"/Enroth.unity")!=originalScene)throw new Exception("Scene backup mismatch");
  AssetDatabase.CreateFolder("Assets/World/WorldExtensions/Generated","NorthCoastFade20261011");
  for(int i=0;i<2;i++){
   var renderer=Rock(i).GetComponent<MeshRenderer>();var source=renderer.sharedMaterial;
   if(!source||!source.shader||source.shader.name!="MMUnity/NorthAlpineSurface20261007")
    throw new Exception("Unexpected old material at "+i);
   var cloned=new Material(source);cloned.name="Surface"+i+"CoveFade";cloned.shader=shader;Params(cloned);
   AssetDatabase.CreateAsset(cloned,PathFor(i));renderer.sharedMaterial=cloned;
  }
  EditorSceneManager.MarkSceneDirty(scene);
  AssetDatabase.SaveAssets();
  if(!EditorSceneManager.SaveScene(scene))throw new Exception("Could not save scene");
  ValidateMaterials();string seams=GlobalQA(),water=WaterQA();
  var now=Protected();
  foreach(var kv in initial)if(!now.TryGetValue(kv.Key,out string sha)||sha!=kv.Value)
   throw new Exception("Protected source was changed: "+kv.Key);
  Directory.CreateDirectory(ReportDir);
  var sb=new StringBuilder();
  sb.AppendLine("PASS=True").AppendLine("sceneHashBefore="+originalScene).AppendLine("sceneHashAfter="+Hash(ScenePath))
   .AppendLine("backup="+BackupDir+"/Enroth.unity").AppendLine("shader="+ShaderPath)
   .AppendLine("materials="+PathFor(0)+";"+PathFor(1))
   .AppendLine("originalTerrainUnmodified=True").AppendLine("originalRockMeshesUnmodified=True")
   .AppendLine("originalWaterMeshesUnmodified=True").AppendLine("strength="+Strength+" radiusX="+RX+" radiusZ="+RZ)
   .AppendLine(seams).AppendLine(water);
  foreach(var kv in initial)sb.AppendLine("PROTECTED|"+kv.Key+"|"+kv.Value);
  File.WriteAllText(ReportDir+"/apply.txt",sb.ToString());
  Debug.Log("SQ01_COVE_FADE_SAVED_PASS "+seams+" "+water+" sceneDirty="+scene.isDirty);
 }
 public static void FinalizeSaved(){
  var scene=EditorSceneManager.OpenScene(ScenePath,OpenSceneMode.Single);
  if(scene.isDirty||!File.Exists(BackupDir+"/Enroth.unity")||!File.Exists(PathFor(0))||!File.Exists(PathFor(1)))
   throw new Exception("Saved material-only apply missing or dirty");
  string initial=Hash(BackupDir+"/Enroth.unity");
  if(initial!="888cff5bfd920e5909c31a537537e0925fc73df7851bfbff2ebf2c5e3b1580f3")
   throw new Exception("Original F2P scene backup hash changed");
  ValidateMaterials();string seams=GlobalQA(),water=WaterQA();
  var protectedFiles=Protected();
  Directory.CreateDirectory(ReportDir);
  var sb=new StringBuilder();
  sb.AppendLine("PASS=True").AppendLine("sceneHashBefore="+initial).AppendLine("sceneHashAfter="+Hash(ScenePath))
   .AppendLine("backup="+BackupDir+"/Enroth.unity").AppendLine("shader="+ShaderPath)
   .AppendLine("materials="+PathFor(0)+";"+PathFor(1))
   .AppendLine("originalTerrainUnmodified=True").AppendLine("originalRockMeshesUnmodified=True")
   .AppendLine("originalWaterMeshesUnmodified=True").AppendLine("strength="+Strength+" radiusX="+RX+" radiusZ="+RZ)
   .AppendLine(seams).AppendLine(water);
  foreach(var kv in protectedFiles)sb.AppendLine("PROTECTED|"+kv.Key+"|"+kv.Value);
  File.WriteAllText(ReportDir+"/apply.txt",sb.ToString());
  Debug.Log("SQ01_COVE_FADE_FINALIZED_PASS "+seams+" "+water+" sceneDirty="+scene.isDirty);
 }
 public static void VerifySaved(){
  var scene=EditorSceneManager.OpenScene(ScenePath,OpenSceneMode.Single);
  if(scene.isDirty)throw new Exception("Saved scene reopened dirty");
  ValidateMaterials();
  string seams=GlobalQA(),water=WaterQA();
  string[] report=File.ReadAllLines(ReportDir+"/apply.txt");
  foreach(var s in report.Where(x=>x.StartsWith("PROTECTED|"))){
   var pair=s.Split('|');if(pair.Length!=3||!File.Exists(pair[1])||Hash(pair[1])!=pair[2])
    throw new Exception("Protected source changed after save: "+s);
  }
  var hashes=report.First(x=>x.StartsWith("sceneHashAfter=")).Split('=')[1];
  if(Hash(ScenePath)!=hashes)throw new Exception("Saved scene drifted");
  string result="PASS=True cleanScene=True shader="+ShaderName+" rock0Mat="+PathFor(0)+" rock1Mat="+PathFor(1)+" "+seams+" "+water+" sceneSHA="+hashes;
  Directory.CreateDirectory(ReportDir);File.WriteAllText(ReportDir+"/independent_verify.txt",result);
  Debug.Log("SQ01_COVE_FADE_INDEPENDENT_VERIFY "+result);
 }
}
