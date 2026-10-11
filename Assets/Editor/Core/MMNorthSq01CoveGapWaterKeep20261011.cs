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

public static class MMNorthSq01CoveGapWaterKeep20261011 {
 const string Scene="Assets/Scenes/World/Enroth.unity";
 const string Input="Tools/NorthSq1F2P20261010/Accepted_CoveGap_V2Safe_triangles.csv";
 const string Folder="Assets/World/WorldExtensions/Generated/NorthCoastWater20261011";
 const string MeshPath=Folder+"/CoveGapV2.asset";
 const string ObjectName="North Cove Seamless Water Extension 20261011";
 const string SeaName="Linked connected sea and existing lowland waters 20260929";
 const string Backup="Backups/NorthSq01CoveWaterBefore20261011";
 const string Report="Validation/NorthSq01CoveWaterKept20261011";
 const string SceneSHA="5140f9748b7f6a9fc54886e9ab44cd17485927b88a3f03205d2d331e7534b5e2";
 static string Hash(string file) { using(var sha=SHA256.Create())using(var s=File.OpenRead(file))
  return BitConverter.ToString(sha.ComputeHash(s)).Replace("-","").ToLowerInvariant();}
 static GameObject Ocean(){
  var g=GameObject.Find(SeaName);
  if(!g||!g.GetComponent<MeshRenderer>()||!g.GetComponent<MeshFilter>())throw new Exception("Original sea renderer missing");
  if(AssetDatabase.GetAssetPath(g.GetComponent<MeshFilter>().sharedMesh)!="Assets/World/WorldExtensions/Generated/ArchipelagoWestEast20261001/ArchipelagoConnectedWater.asset")
   throw new Exception("Unexpected original ocean asset");
  return g;
 }
 static GameObject Patch(){
  var all=UnityEngine.Object.FindObjectsByType<Transform>(FindObjectsSortMode.None).Where(t=>t.name==ObjectName).ToArray();
  if(all.Length!=1)throw new Exception("Expected exactly 1 saved water patch, found "+all.Length);
  return all[0].gameObject;
 }
 static Dictionary<string,string> Protected(){
  var paths=Terrain.activeTerrains.Select(t=>AssetDatabase.GetAssetPath(t.terrainData)).Where(p=>!string.IsNullOrEmpty(p)).ToList();
  foreach(int i in Enumerable.Range(0,4))paths.Add("Assets/World/WorldExtensions/Generated/NorthAlpine20261007/Surface"+i+".asset");
  foreach(int i in Enumerable.Range(0,2))paths.Add("Assets/World/WorldExtensions/Generated/NorthCoveRock20261011/Surface"+i+"CoveSlope.asset");
  foreach(int i in Enumerable.Range(0,2))paths.Add("Assets/World/WorldExtensions/Generated/NorthCoastFade20261011/Surface"+i+"CoveFade.mat");
  paths.Add("Assets/World/WorldExtensions/Generated/ArchipelagoWestEast20261001/ArchipelagoConnectedWater.asset");
  paths.Add("Assets/World/WorldExtensions/Generated/NorthReference20261001/NorthInlandIcyWater20261006.asset");
  paths.Add("Assets/Shaders/MMNorthAlpineCoveFade20261011.shader");
  var result=new Dictionary<string,string>();
  foreach(var p in paths.Distinct().OrderBy(p=>p)){if(!File.Exists(p))throw new Exception("Missing protected asset "+p);result[p]=Hash(p);}
  return result;
 }
 static Mesh GenerateMesh(){
  var lines=File.ReadAllLines(Input);if(lines.Length!=45)throw new Exception("Unexpected V2 triangle list size "+lines.Length);
  var vertices=new List<Vector3>();var normals=new List<Vector3>();var tris=new List<int>();
  var ci=CultureInfo.InvariantCulture;float area=0,maxLen=0;float minx=99999,maxx=-99999,minz=99999,maxz=-99999;
  for(int row=1;row<lines.Length;row++){
   var c=lines[row].Split(',');if(c.Length!=6)throw new Exception("Malformed triangle "+row);
   var v=new Vector3[3];
   for(int i=0;i<3;i++){
    float x=float.Parse(c[2*i],ci),z=float.Parse(c[2*i+1],ci);
    if(x< -810||x> -730||z<1192||z>1213)throw new Exception("Input triangle outside approved coast");
    v[i]=new Vector3(x,.12f,z);
    minx=Mathf.Min(minx,x);maxx=Mathf.Max(maxx,x);minz=Mathf.Min(minz,z);maxz=Mathf.Max(maxz,z);
   }
   float triArea=Mathf.Abs(Vector3.Cross(v[1]-v[0],v[2]-v[0]).y)/2f;
   area+=triArea;if(triArea<.035f)throw new Exception("Degenerate or too-small triangle "+row+" "+triArea);
   maxLen=Mathf.Max(maxLen,Vector3.Distance(v[0],v[1]),Vector3.Distance(v[1],v[2]),Vector3.Distance(v[2],v[0]));
   int k=vertices.Count;foreach(var p in v){vertices.Add(p);normals.Add(Vector3.up);}
   bool up=Vector3.Cross(v[1]-v[0],v[2]-v[0]).y>0;
   tris.Add(k);tris.Add(k+(up?1:2));tris.Add(k+(up?2:1));
  }
  if(Mathf.Abs(area-203.8242f)>.025f||maxLen>25||vertices.Count!=132||Mathf.Abs(minx+803.9f)>3||maxz>1211)
   throw new Exception("Patch geometry mismatch area="+area+" longest="+maxLen+" v="+vertices.Count+" bounds="+minx+","+maxx+","+minz+","+maxz);
  var m=new Mesh();m.name="CoveGapV2";m.SetVertices(vertices);m.SetTriangles(tris,0);m.SetNormals(normals);m.RecalculateBounds();return m;
 }
 static string QA(GameObject originalSea=null){
  if(originalSea==null)originalSea=Ocean();
  var go=Patch();var mesh=go.GetComponent<MeshFilter>().sharedMesh;var render=go.GetComponent<MeshRenderer>();
  if(go.transform.parent!=originalSea.transform||AssetDatabase.GetAssetPath(mesh)!=MeshPath||
     render.sharedMaterial!=originalSea.GetComponent<MeshRenderer>().sharedMaterial)
   throw new Exception("Patch mesh/renderer/hierarchy mismatch");
  if(mesh.vertexCount!=132||mesh.triangles.Length!=132)throw new Exception("Patch face count changed");
  var origMesh=originalSea.GetComponent<MeshFilter>().sharedMesh;
  if(origMesh.vertexCount!=28142||origMesh.triangles.Length!=8701*3)throw new Exception("Original sea geometry changed");
  var terrains=Terrain.activeTerrains;if(terrains.Length!=30)throw new Exception("Expected 30 saved terrains");
  int pairs=0,bad=0,badColliders=0;float gap=0;
  foreach(var a in terrains){
   if(!a.GetComponent<TerrainCollider>()||a.GetComponent<TerrainCollider>().terrainData!=a.terrainData)badColliders++;
   foreach(var b in terrains){
    float dx=b.transform.position.x-a.transform.position.x,dz=b.transform.position.z-a.transform.position.z;
    bool east=Mathf.Abs(dx-512)<.01f&&Mathf.Abs(dz)<.01f,north=Mathf.Abs(dz-512)<.01f&&Mathf.Abs(dx)<.01f;
    if(!east&&!north)continue;
    pairs++;
    for(int k=0;k<=512;k++){
     var p=new Vector3(east?b.transform.position.x:a.transform.position.x+k,0,east?a.transform.position.z+k:b.transform.position.z);
     float d=Mathf.Abs(a.transform.position.y+a.SampleHeight(p)-b.transform.position.y-b.SampleHeight(p));
     gap=Mathf.Max(gap,d);if(d>.001f)bad++;
    }
   }
  }
  if(pairs!=49||bad!=0||badColliders!=0)throw new Exception("Global terrain seams invalid pairs="+pairs+" bad="+bad+" colliders="+badColliders);
  var verts=mesh.vertices;int newBuried=0,samples=0;float tallest=-999,lowest=999;
  var tris=mesh.triangles;
  for(int t=0;t<tris.Length;t+=3){
   var p0=go.transform.TransformPoint(verts[tris[t]]);
   var p1=go.transform.TransformPoint(verts[tris[t+1]]);
   var p2=go.transform.TransformPoint(verts[tris[t+2]]);
   foreach(var frac in new[]{new Vector3(1,0,0),new Vector3(0,1,0),new Vector3(0,0,1),new Vector3(1f/3,1f/3,1f/3),new Vector3(.5f,.5f,0),new Vector3(0,.5f,.5f),new Vector3(.5f,0,.5f)}){
    var p=p0*frac.x+p1*frac.y+p2*frac.z;
    var tt=terrains.FirstOrDefault(q=>p.x>=q.transform.position.x-.001f&&p.x<=q.transform.position.x+512.001f&&p.z>=q.transform.position.z-.001f&&p.z<=q.transform.position.z+512.001f);
    if(!tt)throw new Exception("Patch test point outside terrain: "+p);
    float h=tt.transform.position.y+tt.SampleHeight(p);
    tallest=Mathf.Max(tallest,h);lowest=Mathf.Min(lowest,h);
    if(h>p.y)newBuried++;
    samples++;
   }
  }
  if(newBuried!=0||tallest>=.12f)throw new Exception("New coast water buried samples="+newBuried+" tallest="+tallest);
  return "PASS=True terrains=30 pairs="+pairs+" worstJoinM="+gap.ToString("F6")+" badColliders="+badColliders+
   " oldOceanVertices="+origMesh.vertexCount+" appendedTriangles="+mesh.triangles.Length/3+" appendedVertices="+mesh.vertexCount+
   " waterSamples="+samples+" newlyBuried="+newBuried+" highestTerrainY="+tallest.ToString("F4")+" lowestTerrainY="+lowest.ToString("F4");
 }
 public static void Preflight(){
  var sc=EditorSceneManager.OpenScene(Scene,OpenSceneMode.Single);
  if(sc.isDirty||Hash(Scene)!=SceneSHA||Directory.Exists(Backup)||Directory.Exists(Folder)||GameObject.Find(ObjectName))
   throw new Exception("World not at approved clean dbacab4 state or patch already exists");
  var protectedAssets=Protected();var candidate=GenerateMesh();
  Debug.Log("SQ01_COVE_GAP_PREFLIGHT_PASS preparedVertices="+candidate.vertexCount+" protectedAssets="+protectedAssets.Count);
  UnityEngine.Object.DestroyImmediate(candidate);
 }
 public static void Apply(){
  Preflight();
  var sc=EditorSceneManager.GetActiveScene();
  var oldHash=Hash(Scene);var before=Protected();
  Directory.CreateDirectory(Backup);File.Copy(Scene,Backup+"/Enroth.unity",false);
  if(Hash(Backup+"/Enroth.unity")!=oldHash)throw new Exception("Scene backup mismatch");
  AssetDatabase.CreateFolder("Assets/World/WorldExtensions/Generated","NorthCoastWater20261011");
  var newMesh=GenerateMesh();AssetDatabase.CreateAsset(newMesh,MeshPath);
  var sea=Ocean();var o=new GameObject(ObjectName);
  o.transform.position=Vector3.zero;o.transform.rotation=Quaternion.identity;o.transform.localScale=Vector3.one;
  o.transform.SetParent(sea.transform,true);o.layer=sea.layer;
  var filter=o.AddComponent<MeshFilter>();filter.sharedMesh=newMesh;
  var renderer=o.AddComponent<MeshRenderer>();renderer.sharedMaterial=sea.GetComponent<MeshRenderer>().sharedMaterial;
  renderer.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;renderer.receiveShadows=false;
  EditorSceneManager.MarkSceneDirty(sc);AssetDatabase.SaveAssets();
  if(!EditorSceneManager.SaveScene(sc))throw new Exception("Scene save failed");
  string qa=QA(sea);
  foreach(var p in before)if(Hash(p.Key)!=p.Value)throw new Exception("Protected source mutated: "+p.Key);
  Directory.CreateDirectory(Report);
  var lines=new List<string>{qa,"savedSceneSHA="+Hash(Scene),"previousSceneSHA="+oldHash,"backup="+Backup+"/Enroth.unity",
    "source="+Input,"newMesh="+MeshPath,"oldOceanMeshUnchanged=True","existingTerrainsUnchanged=True"};
  foreach(var p in before)lines.Add("PROTECTED|"+p.Key+"|"+p.Value);
  File.WriteAllLines(Report+"/apply.txt",lines);
  Debug.Log("SQ01_COVE_GAP_SAVED_PASS "+qa+" sceneDirty="+sc.isDirty);
 }
 public static void RepairSaved(){
  var sc=EditorSceneManager.OpenScene(Scene,OpenSceneMode.Single);
  if(sc.isDirty||Hash(Backup+"/Enroth.unity")!=SceneSHA||Hash(Scene)==SceneSHA||File.Exists(Report+"/apply.txt"))
   throw new Exception("Recovery guard failed; refusing to replace mesh");
  var go=Patch();var existing=go.GetComponent<MeshFilter>().sharedMesh;
  if(!existing||AssetDatabase.GetAssetPath(existing)!=MeshPath||existing.vertexCount!=126||existing.triangles.Length!=126)
   throw new Exception("Not the narrowly known first-pass shoreline mesh");
  var protectedFiles=Protected();
  File.Copy(MeshPath,Backup+"/RejectedV2WaterMesh.asset",false);
  var replacement=GenerateMesh();
  existing.Clear();existing.vertices=replacement.vertices;existing.triangles=replacement.triangles;
  existing.normals=replacement.normals;existing.RecalculateBounds();
  EditorUtility.SetDirty(existing);AssetDatabase.SaveAssets();
  UnityEngine.Object.DestroyImmediate(replacement);
  string qa=QA();
  foreach(var p in protectedFiles)if(Hash(p.Key)!=p.Value)throw new Exception("Protected asset modified "+p.Key);
  if(sc.isDirty)throw new Exception("Scene was modified while repairing mesh");
  Directory.CreateDirectory(Report);
  var lines=new List<string>{qa,"savedSceneSHA="+Hash(Scene),"previousSceneSHA="+SceneSHA,
   "backup="+Backup+"/Enroth.unity","originalFailedMeshBackup="+Backup+"/RejectedV2WaterMesh.asset",
   "source="+Input,"newMesh="+MeshPath,"oldOceanMeshUnchanged=True","existingTerrainsUnchanged=True"};
  foreach(var p in protectedFiles)lines.Add("PROTECTED|"+p.Key+"|"+p.Value);
  File.WriteAllLines(Report+"/apply.txt",lines);
  Debug.Log("SQ01_COVE_GAP_SAVED_PASS afterBoundaryCorrection=True "+qa+" sceneDirty="+sc.isDirty);
 }
 public static void Verify(){
  var sc=EditorSceneManager.OpenScene(Scene,OpenSceneMode.Single);
  if(sc.isDirty)throw new Exception("Reopened scene dirty");
  var result=QA();
  var report=File.ReadAllLines(Report+"/apply.txt");
  foreach(string s in report.Where(s=>s.StartsWith("PROTECTED|"))){
   var p=s.Split('|');if(p.Length!=3||!File.Exists(p[1])||Hash(p[1])!=p[2])throw new Exception("Protected source changed "+s);
  }
  var last=report.First(s=>s.StartsWith("savedSceneSHA=")).Split('=')[1];
  if(Hash(Scene)!=last)throw new Exception("Scene SHA mismatch after reload");
  File.WriteAllText(Report+"/independent_verify.txt",result+" sceneSHA="+last);
  Debug.Log("SQ01_COVE_GAP_INDEPENDENT_PASS "+result+" clean="+!sc.isDirty);
 }
}
