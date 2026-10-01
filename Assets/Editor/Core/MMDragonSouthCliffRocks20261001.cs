using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.Rendering;

public static class MMDragonSouthCliffRocks20261001
{
 const string ScenePath="Assets/Scenes/World/Enroth.unity";
 const string RootName="Dragon South Cliff Rocks 20261001";
 const string WaterMask="Validation/EdgeGrid20260923/WaterQA20260929/CurrentWaterCoverage.png";
 const float X0=-1792f,Z0=-1280f,Step=2f;

 sealed class Rock { public string fbx,mat; public Mesh mesh; public Material material; }
 static readonly Rock[] Rocks={
  new Rock{fbx="Assets/Art/Environment/_ExternalContent/Quixel/Megascans/Rocks/Rock_Granite_rcCwC/Rock_Granite_rcCwC.fbx",mat="Assets/Materials/RealisticWorld/Rocks/Rock_Granite_rcCwC_Standard.mat"},
  new Rock{fbx="Assets/Art/Environment/_ExternalContent/Quixel/Megascans/Rocks/Rock_Granite_reFto/Rock_Granite_reFto.FBX",mat="Assets/Materials/RealisticWorld/Rocks/Rock_Granite_reFto_Standard.mat"},
  new Rock{fbx="Assets/Art/Environment/_ExternalContent/Quixel/Megascans/Rocks/Rock_Granite_rgAsy/Aset_rock_granite_M_rgAsy.fbx",mat="Assets/Materials/RealisticWorld/Rocks/Rock_Granite_rgAsy_Standard.mat"}
 };
 struct C { public float x,z,y,slope,rockW,score; public Vector3 normal; public uint h; public int variant; }

 static uint H(int a,int b,int c){
  uint v=(uint)(a*73856093 ^ b*19349663 ^ c*83492791);
  v^=v>>16;v*=0x7feb352d;v^=v>>15;v*=0x846ca68b;return v^(v>>16);
 }
 static bool Wet(Color32[] px,int w,int h,float x,float z){
  int ix=Mathf.FloorToInt((x-X0)/Step),iz=Mathf.FloorToInt((z-Z0)/Step);
  if(ix<0||iz<0||ix>=w||iz>=h)return true;
  var c=px[iz*w+ix];
  return (c.r==40&&c.g==100&&c.b==135)||(c.r==240&&c.g==75&&c.b==35);
 }
 static float LayerWeight(Terrain t,float lx,float lz,string needle){
  var td=t.terrainData;int ax=Mathf.Clamp(Mathf.RoundToInt(lx/td.size.x*(td.alphamapWidth-1)),0,td.alphamapWidth-1);
  int az=Mathf.Clamp(Mathf.RoundToInt(lz/td.size.z*(td.alphamapHeight-1)),0,td.alphamapHeight-1);
  var a=td.GetAlphamaps(ax,az,1,1);float s=0;
  for(int i=0;i<td.terrainLayers.Length;i++) if(td.terrainLayers[i]&&td.terrainLayers[i].name.IndexOf(needle,StringComparison.OrdinalIgnoreCase)>=0) s+=a[0,0,i];
  return s;
 }
 static bool Architecture(Renderer r){
  var f=r.GetComponent<MeshFilter>();string p=f&&f.sharedMesh?AssetDatabase.GetAssetPath(f.sharedMesh).ToLowerInvariant():"";
  string s=(r.name+"|"+p).ToLowerInvariant();
  return s.Contains("house")||s.Contains("building")||s.Contains("castle")||s.Contains("tower")||s.Contains("temple")||s.Contains("bridge")||s.Contains("wall")||s.Contains("dock")||s.Contains("gate")||s.Contains("guild")||s.Contains("tavern");
 }
 static void Prepare(){
  foreach(var r in Rocks){
   r.mesh=AssetDatabase.LoadAllAssetsAtPath(r.fbx).OfType<Mesh>().FirstOrDefault(m=>m.name.IndexOf("LOD2",StringComparison.OrdinalIgnoreCase)>=0)
     ?? AssetDatabase.LoadAllAssetsAtPath(r.fbx).OfType<Mesh>().OrderBy(m=>m.triangles.Length).FirstOrDefault();
   r.material=AssetDatabase.LoadAssetAtPath<Material>(r.mat);
   if(!r.mesh||!r.material||!r.material.mainTexture)throw new Exception("Dragon rock asset unavailable "+r.fbx);
  }
 }

 [MenuItem("MMUnity/World/Dress Dragon South Cliff Rocks")]
 public static void Run(){
  var scene=EditorSceneManager.OpenScene(ScenePath,OpenSceneMode.Single);
  var dragon=scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<Transform>(true)).Single(t=>t.name=="Dragon Isle South - LINKED REFERENCE");
  var terrain=dragon.GetComponentInChildren<Terrain>(true);if(!terrain)throw new Exception("Dragon South terrain missing");
  var td=terrain.terrainData;Prepare();
  if(!File.Exists(WaterMask))throw new FileNotFoundException("Water mask missing",WaterMask);
  var im=new Texture2D(2,2,TextureFormat.RGBA32,false,true);im.LoadImage(File.ReadAllBytes(WaterMask));var px=im.GetPixels32();int mw=im.width,mh=im.height;UnityEngine.Object.DestroyImmediate(im);

  var old=dragon.Find(RootName);if(old)UnityEngine.Object.DestroyImmediate(old.gameObject);
  var arch=scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<Renderer>(true)).Where(r=>r.enabled&&Architecture(r)).Select(r=>r.bounds).ToArray();
  var existingRocks=scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<MeshRenderer>(true))
   .Where(r=>{var f=r.GetComponent<MeshFilter>();var p=f&&f.sharedMesh?AssetDatabase.GetAssetPath(f.sharedMesh):"";return p.IndexOf("/Rocks/",StringComparison.OrdinalIgnoreCase)>=0;})
   .Select(r=>r.bounds.center).ToArray();

  var cand=new List<C>();
  for(int gz=0;gz<30;gz++)for(int gx=0;gx<30;gx++){
   uint h=H(gx,gz,20261001);float lx=10f+gx*16.9f+(((h&1023)/1023f)-.5f)*7f,lz=10f+gz*16.9f+((((h>>10)&1023)/1023f)-.5f)*7f;
   lx=Mathf.Clamp(lx,8,504);lz=Mathf.Clamp(lz,8,504);float wx=terrain.transform.position.x+lx,wz=terrain.transform.position.z+lz;
   if(Wet(px,mw,mh,wx,wz))continue;
   float y=terrain.SampleHeight(new Vector3(wx,0,wz))+terrain.transform.position.y;if(y<1.2f)continue;
   float slope=td.GetSteepness(lx/512f,lz/512f);if(slope<26f||slope>72f)continue;
   float road=LayerWeight(terrain,lx,lz,"Road");if(road>.03f)continue;
   float rw=LayerWeight(terrain,lx,lz,"Rock");if(rw<.20f)continue;
   var p=new Vector3(wx,y,wz);bool nearArch=arch.Any(b=>{var q=b;q.Expand(new Vector3(16,6,16));return q.Contains(new Vector3(p.x,q.center.y,p.z));});if(nearArch)continue;
   if(existingRocks.Any(q=>Vector2.Distance(new Vector2(q.x,q.z),new Vector2(wx,wz))<11f))continue;
   float score=Mathf.Clamp01((slope-26f)/34f)*.62f+Mathf.Clamp01(rw)*.38f;
   cand.Add(new C{x=wx,z=wz,y=y,slope=slope,rockW=rw,score=score,normal=td.GetInterpolatedNormal(lx/512f,lz/512f),h=h,variant=(int)((h>>20)%3)});
  }

  var chosen=new List<C>();foreach(var c in cand.OrderByDescending(c=>c.score+.12f*((c.h&255)/255f))){
   if(chosen.Any(q=>Vector2.Distance(new Vector2(q.x,q.z),new Vector2(c.x,c.z))<18f))continue;
   chosen.Add(c);if(chosen.Count>=28)break;
  }
  if(chosen.Count<18)throw new Exception("Too few safe Dragon cliff-rock candidates "+chosen.Count);
  var root=new GameObject(RootName);root.transform.SetParent(dragon,false);long tris=0;
  for(int i=0;i<chosen.Count;i++){
   var c=chosen[i];var rr=Rocks[c.variant];var go=new GameObject($"DragonCliffRock_{i:00}");go.transform.SetParent(root.transform,true);
   go.AddComponent<MeshFilter>().sharedMesh=rr.mesh;var mr=go.AddComponent<MeshRenderer>();mr.sharedMaterial=rr.material;mr.shadowCastingMode=ShadowCastingMode.On;mr.receiveShadows=true;
   float maxDim=Mathf.Max(.001f,Mathf.Max(rr.mesh.bounds.size.x,Mathf.Max(rr.mesh.bounds.size.y,rr.mesh.bounds.size.z)));
   float desired=Mathf.Lerp(2.0f,4.8f,((c.h>>8)&255)/255f);float sc=desired/maxDim;
   var align=Quaternion.FromToRotation(Vector3.up,Vector3.Slerp(Vector3.up,c.normal,.45f));
   go.transform.rotation=align*Quaternion.Euler(-90f,(c.h>>4)%360,0);go.transform.localScale=Vector3.one*sc;go.transform.position=new Vector3(c.x,c.y-.10f*desired,c.z);
   tris+=rr.mesh.triangles.Length/3;
  }
  EditorSceneManager.MarkSceneDirty(scene);if(!EditorSceneManager.SaveScene(scene))throw new IOException("Dragon South scene save failed");AssetDatabase.SaveAssets();
  Directory.CreateDirectory("Validation/EdgeGrid20260923/DragonSouthCliffRocks20261001");
  string report=$"PASS candidates={cand.Count} placed={chosen.Count} tris={tris} terrainWrites=0 heightWrites=0 alphamapWrites=0 terrainLayerWrites=0";
  File.WriteAllText("Validation/EdgeGrid20260923/DragonSouthCliffRocks20261001/report.txt",report+"\n");
  Debug.Log(report);
 }
}
