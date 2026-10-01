using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using UnityEngine.Rendering;

public static class MMGlobalRockRedistribution20261001
{
 const string RootName="Original Regions Rock Scatter 20260930";
 const string WaterMask="Validation/EdgeGrid20260923/WaterQA20260929/CurrentWaterCoverage.png";
 const float X0=-1792f,Z0=-1280f,Step=2f;

 sealed class RockDef { public string fbx,mat; public Mesh mesh; public Material material; }
 static readonly RockDef[] Rocks={
  new RockDef{fbx="Assets/Art/Environment/_ExternalContent/Quixel/Megascans/Rocks/Rock_Granite_rcCwC/Rock_Granite_rcCwC.fbx",mat="Assets/Materials/RealisticWorld/Rocks/Rock_Granite_rcCwC_Standard.mat"},
  new RockDef{fbx="Assets/Art/Environment/_ExternalContent/Quixel/Megascans/Rocks/Rock_Granite_reFto/Rock_Granite_reFto.FBX",mat="Assets/Materials/RealisticWorld/Rocks/Rock_Granite_reFto_Standard.mat"},
  new RockDef{fbx="Assets/Art/Environment/_ExternalContent/Quixel/Megascans/Rocks/Rock_Granite_rgAsy/Aset_rock_granite_M_rgAsy.fbx",mat="Assets/Materials/RealisticWorld/Rocks/Rock_Granite_rgAsy_Standard.mat"}
 };
 struct C { public Terrain t; public float lx,lz,x,z,y,slope,score; public Vector3 normal; public uint h; public int variant,sector; }

 static uint H(int a,int b,int c){uint v=(uint)(a*73856093 ^ b*19349663 ^ c*83492791);v^=v>>16;v*=0x7feb352d;v^=v>>15;v*=0x846ca68b;return v^(v>>16);}
 static bool Wet(Color32[] px,int w,int h,float x,float z){int ix=Mathf.FloorToInt((x-X0)/Step),iz=Mathf.FloorToInt((z-Z0)/Step);if(ix<0||iz<0||ix>=w||iz>=h)return true;var c=px[iz*w+ix];return(c.r==40&&c.g==100&&c.b==135)||(c.r==240&&c.g==75&&c.b==35);}
 static bool Original(Terrain t){var p=t.transform.parent;string n=p?p.name:"";bool src=n.IndexOf("LINKED REFERENCE",StringComparison.OrdinalIgnoreCase)>=0||n.IndexOf("LINKED ADJUSTABLE",StringComparison.OrdinalIgnoreCase)>=0;return src&&n.IndexOf("Dragon Isle",StringComparison.OrdinalIgnoreCase)<0;}
 static bool Architecture(Renderer r){var f=r.GetComponent<MeshFilter>();string p=f&&f.sharedMesh?AssetDatabase.GetAssetPath(f.sharedMesh).ToLowerInvariant():"";string n=(r.name+"|"+p).ToLowerInvariant();return n.Contains("house")||n.Contains("building")||n.Contains("architecture")||n.Contains("castle")||n.Contains("tower")||n.Contains("temple")||n.Contains("bridge")||n.Contains("wall")||n.Contains("dock")||n.Contains("gate")||n.Contains("guild")||n.Contains("tavern");}
 static bool IsRock(Renderer r){var f=r.GetComponent<MeshFilter>();if(!f||!f.sharedMesh)return false;string n=(f.sharedMesh.name+"|"+AssetDatabase.GetAssetPath(f.sharedMesh)+"|"+r.name).ToLowerInvariant();return n.Contains("rock")||n.Contains("boulder")||n.Contains("granite");}
 static float LayerWeight(Terrain t,float lx,float lz,string needle){var td=t.terrainData;int ax=Mathf.Clamp(Mathf.RoundToInt(lx/td.size.x*(td.alphamapWidth-1)),0,td.alphamapWidth-1),az=Mathf.Clamp(Mathf.RoundToInt(lz/td.size.z*(td.alphamapHeight-1)),0,td.alphamapHeight-1);var a=td.GetAlphamaps(ax,az,1,1);float s=0;for(int i=0;i<td.terrainLayers.Length;i++)if(td.terrainLayers[i]&&td.terrainLayers[i].name.IndexOf(needle,StringComparison.OrdinalIgnoreCase)>=0)s+=a[0,0,i];return s;}
 static float Affinity(Terrain t,float lx,float lz){var td=t.terrainData;int ax=Mathf.Clamp(Mathf.RoundToInt(lx/td.size.x*(td.alphamapWidth-1)),0,td.alphamapWidth-1),az=Mathf.Clamp(Mathf.RoundToInt(lz/td.size.z*(td.alphamapHeight-1)),0,td.alphamapHeight-1);var a=td.GetAlphamaps(ax,az,1,1);float s=0;for(int i=0;i<td.terrainLayers.Length;i++){string n=td.terrainLayers[i]?td.terrainLayers[i].name.ToLowerInvariant():"";float w=a[0,0,i];if(n.Contains("rock")||n.Contains("volcan"))s+=w;else if(n.Contains("arid")||n.Contains("desert")||n.Contains("snow"))s+=w*.42f;}return Mathf.Clamp01(s);}
 static float D(Vector3 a,Vector3 b)=>Vector2.Distance(new Vector2(a.x,a.z),new Vector2(b.x,b.z));
 static void Prepare(){foreach(var r in Rocks){r.mesh=AssetDatabase.LoadAllAssetsAtPath(r.fbx).OfType<Mesh>().FirstOrDefault(m=>m.name.IndexOf("LOD2",StringComparison.OrdinalIgnoreCase)>=0)??AssetDatabase.LoadAllAssetsAtPath(r.fbx).OfType<Mesh>().OrderBy(m=>m.triangles.Length).FirstOrDefault();r.material=AssetDatabase.LoadAssetAtPath<Material>(r.mat);if(!r.mesh||!r.material)throw new Exception("Missing rock asset "+r.fbx);}}

 [MenuItem("MMUnity/World/Rebalance Global Rock Scatter")]
 public static void Run(){
  var s=SceneManager.GetActiveScene();if(s.path!="Assets/Scenes/World/Enroth.unity")throw new Exception("Enroth must be active");
  if(!File.Exists(WaterMask))throw new Exception("Water mask missing");Prepare();
  var im=new Texture2D(2,2,TextureFormat.RGBA32,false,true);im.LoadImage(File.ReadAllBytes(WaterMask));var px=im.GetPixels32();int mw=im.width,mh=im.height;UnityEngine.Object.DestroyImmediate(im);
  var old=s.GetRootGameObjects().FirstOrDefault(g=>g.name==RootName);
  var oldTransforms=old?old.GetComponentsInChildren<Transform>(true):Array.Empty<Transform>();
  var oldSet=new HashSet<Transform>(oldTransforms);
  var arch=s.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<Renderer>(true)).Where(r=>r.enabled&&Architecture(r)).Select(r=>r.bounds).ToArray();
  var authored=s.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<Renderer>(true)).Where(r=>r.enabled&&IsRock(r)&&!oldSet.Contains(r.transform)).Select(r=>r.bounds.center).ToArray();
  var terrains=s.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<Terrain>(true)).Where(Original).OrderBy(t=>t.transform.position.z).ThenBy(t=>t.transform.position.x).ToArray();
  if(terrains.Length!=15)throw new Exception("Expected 15 original terrains, got "+terrains.Length);
  var plans=new List<(Terrain t,List<C> chosen,int candCount)>();
  for(int ti=0;ti<terrains.Length;ti++){
   var t=terrains[ti];var td=t.terrainData;var cand=new List<C>();
   for(int gz=0;gz<24;gz++)for(int gx=0;gx<24;gx++){
    uint h=H(gx,gz,ti+20261001);float lx=12f+gx*21.2f+(((h&1023)/1023f)-.5f)*8f,lz=12f+gz*21.2f+((((h>>10)&1023)/1023f)-.5f)*8f;lx=Mathf.Clamp(lx,12,500);lz=Mathf.Clamp(lz,12,500);
    float wx=t.transform.position.x+lx,wz=t.transform.position.z+lz;if(Wet(px,mw,mh,wx,wz))continue;if(LayerWeight(t,lx,lz,"Road")>.025f)continue;
    float slope=td.GetSteepness(lx/td.size.x,lz/td.size.z);if(slope<5f||slope>50f)continue;float y=t.SampleHeight(new Vector3(wx,0,wz))+t.transform.position.y;if(y<.18f)continue;
    var pos=new Vector3(wx,y,wz);bool nearA=arch.Any(b0=>{var b=b0;b.Expand(new Vector3(14,5,14));return pos.x>=b.min.x&&pos.x<=b.max.x&&pos.z>=b.min.z&&pos.z<=b.max.z;});if(nearA)continue;
    if(authored.Any(a=>D(a,pos)<18f))continue;
    float aff=Affinity(t,lx,lz),score=Mathf.Clamp01((slope-5f)/26f)*.56f+aff*.44f;int sx=Mathf.Clamp((int)(lx/td.size.x*3f),0,2),sz=Mathf.Clamp((int)(lz/td.size.z*2f),0,1);
    cand.Add(new C{t=t,lx=lx,lz=lz,x=wx,z=wz,y=y,slope=slope,score=score,normal=td.GetInterpolatedNormal(lx/td.size.x,lz/td.size.z),h=h,variant=(int)((h>>19)%3),sector=sz*3+sx});
   }
   var chosen=new List<C>();
   for(int sec=0;sec<6;sec++){var secCand=cand.Where(c=>c.sector==sec).OrderByDescending(c=>c.score+.10f*((c.h&255)/255f)).ToArray();foreach(var c in secCand){var p=new Vector3(c.x,c.y,c.z);if(chosen.All(q=>D(new Vector3(q.x,q.y,q.z),p)>=58f)){chosen.Add(c);break;}}}
   foreach(var c in cand.OrderByDescending(c=>c.score+.0025f*(chosen.Count==0?0:chosen.Min(q=>D(new Vector3(q.x,q.y,q.z),new Vector3(c.x,c.y,c.z)))))){if(chosen.Count>=6)break;var p=new Vector3(c.x,c.y,c.z);if(chosen.Any(q=>Mathf.Abs(q.x-c.x)<.05f&&Mathf.Abs(q.z-c.z)<.05f))continue;if(chosen.All(q=>D(new Vector3(q.x,q.y,q.z),p)>=50f))chosen.Add(c);}
   if(chosen.Count<Mathf.Min(6,cand.Count)){foreach(var c in cand.OrderByDescending(c=>c.score)){if(chosen.Count>=Mathf.Min(6,cand.Count))break;var p=new Vector3(c.x,c.y,c.z);if(chosen.Any(q=>Mathf.Abs(q.x-c.x)<.05f&&Mathf.Abs(q.z-c.z)<.05f))continue;if(chosen.All(q=>D(new Vector3(q.x,q.y,q.z),p)>=34f))chosen.Add(c);}}
   plans.Add((t,chosen,cand.Count));
  }
  if(old)UnityEngine.Object.DestroyImmediate(old);var root=new GameObject(RootName);int total=0;long tris=0;var report=new List<string>();
  foreach(var plan in plans){float mind=float.MaxValue;for(int i=0;i<plan.chosen.Count;i++){var c=plan.chosen[i];var rr=Rocks[c.variant];var go=new GameObject($"WorldRock_{total:000}");go.transform.SetParent(root.transform,true);go.AddComponent<MeshFilter>().sharedMesh=rr.mesh;var mr=go.AddComponent<MeshRenderer>();mr.sharedMaterial=rr.material;mr.shadowCastingMode=ShadowCastingMode.On;mr.receiveShadows=true;float maxDim=Mathf.Max(.0001f,Mathf.Max(rr.mesh.bounds.size.x,Mathf.Max(rr.mesh.bounds.size.y,rr.mesh.bounds.size.z)));float desired=Mathf.Lerp(1.35f,3.65f,((c.h>>8)&255)/255f);float sc=desired/maxDim;var align=Quaternion.FromToRotation(Vector3.up,Vector3.Slerp(Vector3.up,c.normal,.28f));go.transform.rotation=align*Quaternion.Euler(-90f,(c.h>>3)%360,0);go.transform.localScale=Vector3.one*sc;go.transform.position=new Vector3(c.x,c.y,c.z);var rb=mr.bounds;go.transform.position+=Vector3.up*(c.y-rb.min.y-.06f);tris+=rr.mesh.triangles.Length/3;total++;for(int j=0;j<i;j++)mind=Mathf.Min(mind,D(new Vector3(c.x,0,c.z),new Vector3(plan.chosen[j].x,0,plan.chosen[j].z)));}
   if(plan.chosen.Count<2)mind=0;report.Add($"{plan.t.terrainData.name}|candidates={plan.candCount}|placed={plan.chosen.Count}|minSpacing={mind:F1}|sectors={string.Join(",",plan.chosen.Select(c=>c.sector).OrderBy(x=>x))}");}
  if(total<70)throw new Exception("Too few redistributed rocks "+total);
  EditorSceneManager.MarkSceneDirty(s);if(!EditorSceneManager.SaveScene(s))throw new IOException("Could not save redistributed rock scene");AssetDatabase.SaveAssets();
  Directory.CreateDirectory("Validation/EdgeGrid20260923/WorldRocks20261001");report.Insert(0,$"PASS rocks={total} terrains={terrains.Length} sourceTriangles={tris} authoredRocksExcluded={authored.Length} terrainWrites=0 heightWrites=0 alphamapWrites=0 terrainLayerWrites=0");File.WriteAllLines("Validation/EdgeGrid20260923/WorldRocks20261001/redistribution.txt",report);Debug.Log(report[0]);
 }
}
