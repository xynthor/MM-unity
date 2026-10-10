using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Security.Cryptography;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using UnityEngine.Rendering;
public static class MMNorthSq1F2PKeep20261010
{
 const string ScenePath="Assets/Scenes/World/Enroth.unity";
 const string TerrainPath="Assets/World/WorldExtensions/Generated/NorthReference20261001/Kriegspire_NorthTerrain.asset";
 const string RockPath="Assets/World/WorldExtensions/Generated/NorthAlpine20261007/Surface1.asset";
 const string Input="Tools/NorthSq1F2P20261010/F2P_rebuild.f32";
 const string Baseline="Tools/NorthSq1F2P20261010/height.f32";
 const string Backup="Backups/NorthSq1F2PBefore20261010";
 const string Report="Validation/NorthSq1F2PKept20261010";
 static readonly string[] Protected={
  "Assets/World/WorldExtensions/Generated/LinkedSurfaceInputs/Terrain/b2adcca80c21fb542bd822ac5a7b75cf.asset",
  "Assets/World/WorldExtensions/Generated/LinkedSurfaceInputs/Terrain/9a29f5601247e3f4ba63f7800b155bea.asset",
  "Assets/World/WorldExtensions/Generated/LinkedSurfaceInputs/Terrain/d85d07a7edcfde747bfd4436baa76b47.asset",
  "Assets/World/WorldExtensions/Generated/LinkedSurfaceInputs/Terrain/1a659482bbf7f2e4bb722f6f5a3bbf78.asset",
  "Assets/World/WorldExtensions/Generated/NorthReference20261001/SweetWater_NorthTerrain.asset",
  "Assets/World/WorldExtensions/Generated/NorthReference20261001/FrozenHighlands_NorthTerrain.asset",
  "Assets/World/WorldExtensions/Generated/NorthReference20261001/SilverCove_NorthTerrain.asset",
  "Assets/World/WorldExtensions/Generated/NorthReference20261001/NorthInlandIcyWater20261006.asset",
  "Assets/World/WorldExtensions/Generated/ArchipelagoWestEast20261001/ArchipelagoConnectedWater.asset",
  "Assets/World/WorldExtensions/Generated/NorthAlpine20261007/Surface0.asset",
  "Assets/World/WorldExtensions/Generated/NorthAlpine20261007/Surface2.asset",
  "Assets/World/WorldExtensions/Generated/NorthAlpine20261007/Surface3.asset"
 };
 static string Hash(string path){using(var fs=File.OpenRead(path))using(var sha=SHA256.Create())return BitConverter.ToString(sha.ComputeHash(fs)).Replace("-","").ToLowerInvariant();}
 static float Smooth(float a,float b,float v){return Mathf.SmoothStep(0,1,Mathf.InverseLerp(a,b,v));}
 static float Sample(float[,] m,float x,float z){
  x=Mathf.Clamp(x,0,512);z=Mathf.Clamp(z,0,512);int ix=Mathf.Min(511,(int)x),iz=Mathf.Min(511,(int)z);float u=x-ix,v=z-iz;
  return Mathf.Lerp(Mathf.Lerp(m[iz,ix],m[iz,ix+1],u),Mathf.Lerp(m[iz+1,ix],m[iz+1,ix+1],u),v);
 }
 static bool Dress(Transform t)=>t.name.Contains("AuthoredPine_")||t.name.StartsWith("TransitionPine_")||t.name.StartsWith("ReferenceTallPine_")||t.name.StartsWith("SourceTree_")||t.name.StartsWith("Eco_")||t.name.StartsWith("AuthoredBoulder_")||t.name.StartsWith("Bush_")||t.name.StartsWith("Pine_");
 static float Ground(Terrain t,float x,float z)=>t.transform.position.y+t.SampleHeight(new Vector3(x,0,z));
 static float[,] ReadFloat(string path){
  byte[] b=File.ReadAllBytes(path);if(b.Length!=513*513*4)throw new Exception("Bad height size "+path);
  var a=new float[513,513];Buffer.BlockCopy(b,0,a,0,b.Length);return a;
 }
 static void Rock(Mesh mesh,TerrainData td){
  var vertices=new Vector3[513*513];var normals=new Vector3[vertices.Length];var tangent=new Vector4[vertices.Length];var tris=new List<int>(200000);
  for(int z=0;z<=512;z++)for(int x=0;x<=512;x++){
   int i=z*513+x;float h=td.GetHeight(x,z);
   vertices[i]=new Vector3(x,h+Mathf.Lerp(.1f,.60f,Smooth(4,28,h-24)),z);
   normals[i]=td.GetInterpolatedNormal(x/512f,z/512f);
   var tv=new Vector3(normals[i].y,-normals[i].x,0).normalized;tangent[i]=new Vector4(tv.x,tv.y,tv.z,-1);
  }
  for(int z=12;z<512;z++)for(int x=0;x<512;x++){
   int a=z*513+x,b=a+1,c=a+513,d=c+1;
   float min=Mathf.Min(vertices[a].y,Mathf.Min(vertices[b].y,Mathf.Min(vertices[c].y,vertices[d].y)));
   float slope=(Vector3.Angle(normals[a],Vector3.up)+Vector3.Angle(normals[b],Vector3.up)+Vector3.Angle(normals[c],Vector3.up)+Vector3.Angle(normals[d],Vector3.up))*.25f;
   if(min<25f||slope<27f)continue;
   tris.Add(a);tris.Add(c);tris.Add(d);tris.Add(a);tris.Add(d);tris.Add(b);
  }
  mesh.Clear();mesh.indexFormat=IndexFormat.UInt32;
  mesh.vertices=vertices;mesh.normals=normals;mesh.tangents=tangent;
  mesh.SetTriangles(tris,0);mesh.RecalculateBounds();EditorUtility.SetDirty(mesh);
 }
 static int Buried(float[,] meters,out int n,out int worsened,float[,] baseline){
  int count=0;n=0;worsened=0;
  foreach(var mr in UnityEngine.Object.FindObjectsByType<MeshRenderer>()){
   if(!mr.enabled||!mr.gameObject.activeInHierarchy||!mr.sharedMaterial||!mr.sharedMaterial.shader.name.Contains("Water"))continue;
   if(mr.bounds.max.x< -768||mr.bounds.min.x> -256||mr.bounds.max.z<768||mr.bounds.min.z>1280)continue;
   var mf=mr.GetComponent<MeshFilter>();if(!mf||!mf.sharedMesh)continue;
   foreach(var lv in mf.sharedMesh.vertices){
    var p=mf.transform.TransformPoint(lv);if(p.x< -768||p.x> -256||p.z<768||p.z>1280)continue;
    float d=Sample(meters,p.x+768,p.z-768)-p.y,prior=Sample(baseline,p.x+768,p.z-768)-p.y;
    n++;if(d>.3f)count++;
    if(d>prior+.10f&&d>.3f)worsened++;
   }
  }
  return count;
 }
 static Dictionary<string,string> ProtectedHashes(){
  var d=new Dictionary<string,string>();
  foreach(string p in Protected)if(File.Exists(p))d[p]=Hash(p);
  return d;
 }
 static float[,] TerrainMeters(Terrain t){
  var td=t.terrainData;var raw=td.GetHeights(0,0,513,513);
  for(int z=0;z<=512;z++)for(int x=0;x<=512;x++)raw[z,x]=raw[z,x]*td.size.y+t.transform.position.y;return raw;
 }
 static float AlphaChecksum(TerrainData d){
  var a=d.GetAlphamaps(0,0,d.alphamapWidth,d.alphamapHeight);double s=0;
  for(int z=0;z<a.GetLength(0);z++)for(int x=0;x<a.GetLength(1);x++)for(int k=0;k<a.GetLength(2);k++)s+=a[z,x,k]*(1+k)*(.5+x*.00001+z*.00002);
  return (float)s;
 }
 static string BackupOne(string p){
  var target=Path.Combine(Backup,Path.GetFileName(p));
  if(File.Exists(target))throw new IOException("Backup exists: "+target);
  File.Copy(p,target,false);
  if(Hash(p)!=Hash(target))throw new IOException("Backup verification mismatch: "+p);
  return target;
 }
 static void Shot(string path,Vector3 pos,Vector3 target,bool top,float size){
  var go=new GameObject("F2P_KEPT_QA_CAMERA");var c=go.AddComponent<Camera>();
  c.transform.position=pos;c.transform.rotation=Quaternion.LookRotation(target-pos,Vector3.up);
  c.orthographic=top;if(top)c.orthographicSize=size;else c.fieldOfView=size;c.nearClipPlane=.1f;c.farClipPlane=5000;c.clearFlags=CameraClearFlags.Skybox;
  var rt=new RenderTexture(1200,800,24);var prev=RenderTexture.active;c.targetTexture=rt;c.Render();RenderTexture.active=rt;
  var im=new Texture2D(1200,800,TextureFormat.RGB24,false);im.ReadPixels(new Rect(0,0,1200,800),0,0);im.Apply();File.WriteAllBytes(path,im.EncodeToPNG());
  RenderTexture.active=prev;c.targetTexture=null;UnityEngine.Object.DestroyImmediate(im);UnityEngine.Object.DestroyImmediate(rt);UnityEngine.Object.DestroyImmediate(go);
 }
 public static void Run(){
  if(File.Exists(Report+"/apply.txt"))throw new IOException("Already applied; refusing duplicate");
  if(Directory.Exists(Backup))throw new IOException("F2P backup directory already exists; refusing duplicate");
  if(!File.Exists(Input)||!File.Exists(Baseline))throw new FileNotFoundException("F2P candidate or source baseline missing");
  var scene=EditorSceneManager.OpenScene(ScenePath,OpenSceneMode.Single);
  if(scene.isDirty)throw new Exception("Scene dirty before operation");
  var t=Terrain.activeTerrains.Single(q=>Mathf.Abs(q.transform.position.x+768)<.1f&&Mathf.Abs(q.transform.position.z-768)<.1f);
  if(AssetDatabase.GetAssetPath(t.terrainData)!=TerrainPath)throw new Exception("Unexpected source TerrainData");
  var td=t.terrainData;var coll=t.GetComponent<TerrainCollider>();if(!coll||coll.terrainData!=td)throw new Exception("Collider mismatch");
  var rockObj=GameObject.Find("North Alpine Rock Surface 1 20261007");
  if(!rockObj||!rockObj.activeSelf)throw new Exception("Expected live rock surface missing or inactive");
  var rockFilter=rockObj.GetComponent<MeshFilter>();
  if(!rockFilter||!rockFilter.sharedMesh||AssetDatabase.GetAssetPath(rockFilter.sharedMesh)!=RockPath)throw new Exception("Unexpected rock mesh asset "+(rockFilter?AssetDatabase.GetAssetPath(rockFilter.sharedMesh):"null"));
  var mesh=rockFilter.sharedMesh;
  var old=TerrainMeters(t);var expected=ReadFloat(Baseline);var next=ReadFloat(Input);
  var attrs=ProtectedHashes();float alpha=AlphaChecksum(td);
  float maxBaseline=0,maxBorder=0,maxDelta=0;int changed=0;
  var nh=new float[513,513];
  for(int z=0;z<=512;z++)for(int x=0;x<=512;x++){
   float before=old[z,x],wanted=next[z,x];
   if(float.IsNaN(wanted)||float.IsInfinity(wanted))throw new Exception("Nonfinite height");
   maxBaseline=Mathf.Max(maxBaseline,Mathf.Abs(before-expected[z,x]));
   float norm=(wanted-t.transform.position.y)/td.size.y;
   if(norm<0||norm>1)throw new Exception("Normalized height out of range at "+x+","+z);
   nh[z,x]=norm;
   float d=Mathf.Abs(wanted-before);
   if(d>.01f){changed++;maxDelta=Mathf.Max(maxDelta,d);}
   if(x==0||x==512||z==0||z==512)maxBorder=Mathf.Max(maxBorder,d);
  }
  if(maxBaseline>.003f)throw new Exception("Saved terrain diverged from F2P source baseline: "+maxBaseline);
  if(maxBorder>.001f)throw new Exception("F2P violates existing border: "+maxBorder);
  int wn,bw;int waterBefore=Buried(old,out wn,out bw,old);int nn,worse;int waterAfter=Buried(next,out nn,out worse,old);
  if(wn!=nn||nn!=376||worse!=0||waterAfter>waterBefore)throw new Exception("Water preflight failed "+wn+","+nn+" buried "+waterBefore+","+waterAfter+" worse "+worse);
  string[] backups={TerrainPath,RockPath,ScenePath};
  if(new DriveInfo(Path.GetPathRoot(Path.GetFullPath(ScenePath))).AvailableFreeSpace<2L*1024*1024*1024)throw new IOException("Disk space too low");
  Directory.CreateDirectory(Backup);
  foreach(var path in backups)BackupOne(path);
  Directory.CreateDirectory(Report);
  File.WriteAllLines(Report+"/baseline.txt",attrs.Select(kv=>kv.Key+" "+kv.Value).Concat(backups.Select(p=>p+" "+Hash(p))).Concat(new[]{"F2Pinput "+Hash(Input),"F2Pbaseline "+Hash(Baseline),"sceneBefore "+Hash(ScenePath)}));
  td.SetHeights(0,0,nh);td.SetBaseMapDirty();EditorUtility.SetDirty(td);t.Flush();
  Rock(mesh,td);
  int moved=0,rootCount=0,over35=0,over45=0;
  foreach(var tr in UnityEngine.Object.FindObjectsByType<Transform>()){
   if(!Dress(tr)||tr.GetComponentsInParent<Transform>().Skip(1).Any(Dress))continue;
   var p=tr.position;if(p.x< -768||p.x>= -256||p.z<768||p.z>=1280)continue;
   rootCount++;
   float lx=p.x+768,lz=p.z-768,dy=Sample(next,lx,lz)-Sample(old,lx,lz);
   float angle=Vector3.Angle(td.GetInterpolatedNormal(lx/512f,lz/512f),Vector3.up);
   if(angle>35)over35++;if(angle>45)over45++;
   if(Mathf.Abs(dy)<.01f)continue;
   tr.position=p+Vector3.up*dy;moved++;
  }
  EditorSceneManager.MarkSceneDirty(scene);
  if(!EditorSceneManager.SaveScene(scene))throw new IOException("Scene failed to save");
  AssetDatabase.SaveAssets();
  if(scene.isDirty)throw new Exception("Scene still dirty after save");
  if(Mathf.Abs(AlphaChecksum(td)-alpha)>0.015f)throw new Exception("Alphamaps unexpectedly changed");
  foreach(var kv in attrs)if(Hash(kv.Key)!=kv.Value)throw new Exception("Protected asset changed: "+kv.Key);
  var post=TerrainMeters(t);float maxError=0;
  for(int z=0;z<=512;z++)for(int x=0;x<=512;x++)maxError=Mathf.Max(maxError,Mathf.Abs(post[z,x]-next[z,x]));
  if(maxError>.009f)throw new Exception("Saved terrain mismatch: "+maxError);
  var west=Terrain.activeTerrains.Single(q=>Mathf.Abs(q.transform.position.x+1280)<.1f&&Mathf.Abs(q.transform.position.z-768)<.1f);
  var east=Terrain.activeTerrains.Single(q=>Mathf.Abs(q.transform.position.x+256)<.1f&&Mathf.Abs(q.transform.position.z-768)<.1f);
  var south=Terrain.activeTerrains.Single(q=>Mathf.Abs(q.transform.position.x+768)<.1f&&Mathf.Abs(q.transform.position.z-256)<.1f);
  float gapW=0,gapE=0,gapS=0;
  for(int k=0;k<=512;k++){gapW=Mathf.Max(gapW,Mathf.Abs(Ground(west,-768,768+k)-Ground(t,-768,768+k)));gapE=Mathf.Max(gapE,Mathf.Abs(Ground(t,-256,768+k)-Ground(east,-256,768+k)));gapS=Mathf.Max(gapS,Mathf.Abs(Ground(south,-768+k,768)-Ground(t,-768+k,768)));}
  var preview="Preview/NorthSq1F2PKept20261010";Directory.CreateDirectory(preview);
  Shot(preview+"/top.png",new Vector3(-512,900,1024),new Vector3(-512,0,1024),true,255);
  Shot(preview+"/oblique.png",new Vector3(-520,235,610),new Vector3(-520,28,1050),false,54);
  Shot(preview+"/joint.png",new Vector3(-510,420,350),new Vector3(-510,38,1050),false,59);
  var lines=new[]{
   "PASS=TRUE","candidate=F2P","target="+TerrainPath,"sourceHEADExpected=482a3e0","backups="+Backup,
   "baselineMaxError="+maxBaseline.ToString("F6"),"changedSamples="+changed,"maxDeltaM="+maxDelta.ToString("F3"),
   "outerBorderMaxChangeM="+maxBorder.ToString("F6"),"savedHeightMaxErrorM="+maxError.ToString("F6"),
   "dressingRoots="+rootCount,"dressingMoved="+moved,"treesOn35deg="+over35,"treesOn45deg="+over45,
   "waterVertices="+nn,"waterBuriedBefore="+waterBefore,"waterBuriedAfter="+waterAfter,"waterNewWorse="+worse,
   "westSeamM="+gapW.ToString("F6"),"eastSeamM="+gapE.ToString("F6"),"southSeamM="+gapS.ToString("F6"),
   "alphamapsModified=false","protectedAssetsInvariant=true","sceneDirtyAfterSave="+scene.isDirty,
   "sceneHashAfter="+Hash(ScenePath),"terrainHashAfter="+Hash(TerrainPath),"rockHashAfter="+Hash(RockPath)
  };
  File.WriteAllLines(Report+"/apply.txt",lines);
  Debug.Log("SQ1_F2P_KEPT_PASS changed="+changed+" moved="+moved+" water="+waterBefore+"->"+waterAfter+" seams="+gapW+","+gapE+","+gapS);
 }
 public static void VerifySaved(){
  var scene=EditorSceneManager.OpenScene(ScenePath,OpenSceneMode.Single);
  if(scene.isDirty)throw new Exception("Scene dirty on clean saved verification");
  if(!File.Exists(Report+"/apply.txt"))throw new Exception("No apply report");
  var t=Terrain.activeTerrains.Single(q=>Mathf.Abs(q.transform.position.x+768)<.1f&&Mathf.Abs(q.transform.position.z-768)<.1f);
  if(AssetDatabase.GetAssetPath(t.terrainData)!=TerrainPath)throw new Exception("Wrong saved terrain");
  var desired=ReadFloat(Input);var old=ReadFloat(Baseline);var now=TerrainMeters(t);
  float worst=0,border=0;for(int z=0;z<=512;z++)for(int x=0;x<=512;x++){
   worst=Mathf.Max(worst,Mathf.Abs(now[z,x]-desired[z,x]));
   if(x==0||x==512||z==0||z==512)border=Mathf.Max(border,Mathf.Abs(old[z,x]-now[z,x]));
  }
  int n,w;int buried=Buried(now,out n,out w,old);
  var rock=GameObject.Find("North Alpine Rock Surface 1 20261007");
  if(!rock||!rock.GetComponent<MeshFilter>()||AssetDatabase.GetAssetPath(rock.GetComponent<MeshFilter>().sharedMesh)!=RockPath)throw new Exception("Rock binding broken");
  var mesh=rock.GetComponent<MeshFilter>().sharedMesh;
  var prot=File.ReadAllLines(Report+"/baseline.txt");
  int checkedHashes=0;foreach(var l in prot){int at=l.LastIndexOf(' ');if(at<=0)continue;string p=l.Substring(0,at),sha=l.Substring(at+1);if(!Protected.Contains(p))continue;if(Hash(p)!=sha)throw new Exception("Protected SHA changed "+p);checkedHashes++;}
  bool ok=worst<.009f&&border<.001f&&n==376&&buried==57&&w==0&&mesh.vertexCount==263169&&checkedHashes>=10;
  string report="PASS="+ok+" savedHeightMaxError="+worst.ToString("F6")+" borderDelta="+border.ToString("F6")+" waterVertices="+n+" buried="+buried+" newlyWorse="+w+" rockVertices="+mesh.vertexCount+" protectedHashesChecked="+checkedHashes+" sceneDirty="+scene.isDirty;
  Directory.CreateDirectory(Report);
  File.WriteAllText(Report+"/independent_verify.txt",report);
  Debug.Log("SQ1_F2P_INDEPENDENT_VERIFY "+report);
  if(!ok)throw new Exception("Saved QA failed: "+report);
 }
}
