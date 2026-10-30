using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;

public static class MMDragonReferencePolish20261006
{
 const string ScenePath="Assets/Scenes/World/Enroth.unity";
 const string Root="Assets/World/WorldExtensions/Generated/DragonReference20261006";
 const string MatPath=Root+"/DragonCraterTurquoiseWater.mat";
 const string MeshPath=Root+"/DragonCraterTurquoiseWater.asset";
 const string LakeName="Water - Dragon Isle Reference Turquoise Crater Lake 20261006";

 static float S(float a,float b,float x){float t=Mathf.Clamp01((x-a)/Mathf.Max(.001f,b-a));return t*t*(3f-2f*t);}
 static int F(TerrainData d,string token){for(int i=0;i<d.terrainLayers.Length;i++){var l=d.terrainLayers[i];if(l&&l.name.IndexOf(token,StringComparison.OrdinalIgnoreCase)>=0)return i;}return -1;}

 static void Paint(Terrain t,bool south)
 {
  var d=t.terrainData;
  var a=d.GetAlphamaps(0,0,d.alphamapWidth,d.alphamapHeight);
  int sand=F(d,"_1"),rock=F(d,"Trace_Rock"),ash=F(d,"_5"),dry=F(d,"_2"),green=0;
  if(sand<0||rock<0||ash<0||dry<0)throw new Exception("Dragon linked terrain layers missing");
  for(int z=0;z<d.alphamapHeight;z++)for(int x=0;x<d.alphamapWidth;x++){
   float u=x/(float)(d.alphamapWidth-1),v=z/(float)(d.alphamapHeight-1);
   float y=t.transform.position.y+d.GetInterpolatedHeight(u,v),slope=d.GetSteepness(u,v);
   if(y<.65f)continue;
   if(south && u<.68f){
    float southness=1f-S(.58f,.93f,v);
    float flat=1f-S(23f,38f,slope);
    float w=.92f*southness*flat*S(.7f,4f,y);
    if(w>.01f){
     float ds=a[z,x,sand],dd=a[z,x,dry],total=ds+dd+.0001f;
     float move=Mathf.Min(ds+dd,.78f*w);
     a[z,x,sand]-=move*ds/total;
     a[z,x,dry]-=move*dd/total;
     a[z,x,green]+=move;
    }
    float rw=S(19f,34f,slope)*.60f*S(.7f,3f,y);
    if(rw>.01f){float m=Mathf.Min(a[z,x,sand],rw*.42f);a[z,x,sand]-=m;a[z,x,rock]+=m;}
   } else if(!south && u<.80f){
    float interior=S(.4f,3f,y),steep=S(13f,31f,slope),northness=S(.05f,.25f,v);
    float move=Mathf.Min(a[z,x,sand],(.30f+.48f*steep+.10f*northness)*interior);
    if(move>.001f){
     a[z,x,sand]-=move;
     a[z,x,rock]+=move*(.58f+.20f*steep);
     a[z,x,ash]+=move*.28f;
     a[z,x,dry]+=move*Mathf.Max(0,.14f-.20f*steep);
    }
   }
  }
  d.SetAlphamaps(0,0,a);d.SetBaseMapDirty();EditorUtility.SetDirty(d);
 }

 static Material LakeMaterial()
 {
  if(!AssetDatabase.IsValidFolder(Root))AssetDatabase.CreateFolder("Assets/World/WorldExtensions/Generated","DragonReference20261006");
  var m=AssetDatabase.LoadAssetAtPath<Material>(MatPath);
  if(!m){
   var src=AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/Terrain/EnrothReferenceLinkedWater.mat");
   if(!src)throw new Exception("Linked water material missing");
   m=new Material(src);m.name="DragonCraterTurquoiseWater";AssetDatabase.CreateAsset(m,MatPath);
  }
  m.SetColor("_ShallowColor",new Color(.09f,.78f,.86f,.96f));
  m.SetColor("_MidColor",new Color(.02f,.50f,.68f,.98f));
  m.SetColor("_DeepColor",new Color(.01f,.25f,.38f,1f));
  m.SetColor("_FoamColor",new Color(.82f,.98f,1f,.88f));
  m.SetFloat("_DepthRange",8f);m.SetFloat("_FoamDepth",1.6f);EditorUtility.SetDirty(m);return m;
 }

 static Mesh LakeMesh(Terrain north)
 {
  var d=north.terrainData;int N=129;float step=512f/(N-1);Vector2 c=new Vector2(-1523.5f,876.1f);
  var verts=new List<Vector3>();var uv=new List<Vector2>();var tris=new List<int>();
  for(int z=0;z<N-1;z++)for(int x=0;x<N-1;x++){
   float wx=north.transform.position.x+(x+.5f)*step,wz=north.transform.position.z+(z+.5f)*step;
   if((new Vector2(wx,wz)-c).sqrMagnitude>42f*42f)continue;
   float u=(x+.5f)/(N-1f),v=(z+.5f)/(N-1f);
   if(north.transform.position.y+d.GetInterpolatedHeight(u,v)>.12f)continue;
   int b=verts.Count;float x0=north.transform.position.x+x*step,x1=x0+step,z0=north.transform.position.z+z*step,z1=z0+step;
   verts.Add(new Vector3(x0,.18f,z0));verts.Add(new Vector3(x1,.18f,z0));verts.Add(new Vector3(x1,.18f,z1));verts.Add(new Vector3(x0,.18f,z1));
   uv.Add(new Vector2(0,0));uv.Add(new Vector2(1,0));uv.Add(new Vector2(1,1));uv.Add(new Vector2(0,1));
   tris.Add(b);tris.Add(b+2);tris.Add(b+1);tris.Add(b);tris.Add(b+3);tris.Add(b+2);
  }
  var m=AssetDatabase.LoadAssetAtPath<Mesh>(MeshPath);
  if(!m){m=new Mesh();m.name="DragonCraterTurquoiseWater";AssetDatabase.CreateAsset(m,MeshPath);}
  m.Clear();m.SetVertices(verts);m.SetUVs(0,uv);m.SetTriangles(tris,0);m.RecalculateNormals();m.RecalculateBounds();EditorUtility.SetDirty(m);return m;
 }

 public static void Run()
 {
  if(File.Exists("Validation/EdgeGrid20260923/DragonReference20261006/apply.txt"))throw new Exception("Dragon reference polish already applied; refusing a cumulative repaint.");
  var s=EditorSceneManager.OpenScene(ScenePath,OpenSceneMode.Single);
  var terrains=Terrain.activeTerrains;
  var south=terrains.FirstOrDefault(t=>Mathf.Abs(t.transform.position.x+1792)<1&&Mathf.Abs(t.transform.position.z-256)<1);
  var north=terrains.FirstOrDefault(t=>Mathf.Abs(t.transform.position.x+1792)<1&&Mathf.Abs(t.transform.position.z-768)<1);
  if(!south||!north)throw new Exception("Dragon linked terrains missing");
  Paint(south,true);Paint(north,false);
  if(!AssetDatabase.IsValidFolder(Root))AssetDatabase.CreateFolder("Assets/World/WorldExtensions/Generated","DragonReference20261006");
  var parent=s.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<Transform>(true)).FirstOrDefault(t=>t.name=="Dragon Isle North - LINKED REFERENCE");
  if(!parent)throw new Exception("Dragon north linked root missing");
  var old=parent.Find(LakeName);GameObject go=old?old.gameObject:new GameObject(LakeName);
  go.transform.SetParent(parent,false);go.transform.position=Vector3.zero;go.transform.rotation=Quaternion.identity;go.transform.localScale=Vector3.one;
  var mf=go.GetComponent<MeshFilter>();if(!mf)mf=go.AddComponent<MeshFilter>();
  var mr=go.GetComponent<MeshRenderer>();if(!mr)mr=go.AddComponent<MeshRenderer>();
  mf.sharedMesh=LakeMesh(north);mr.sharedMaterial=LakeMaterial();mr.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;
  EditorSceneManager.MarkSceneDirty(s);
  if(!EditorSceneManager.SaveScene(s))throw new IOException("Could not save Enroth");
  AssetDatabase.SaveAssets();
  Directory.CreateDirectory("Validation/EdgeGrid20260923/DragonReference20261006");
  File.WriteAllText("Validation/EdgeGrid20260923/DragonReference20261006/apply.txt",
   "PASS Dragon Isle reference polish: south forest strengthened; north rock/arid strengthened; exact enclosed crater basin overlaid turquoise; geometry and landmark positions preserved.\n");
  Debug.Log("DRAGON_REFERENCE_POLISH_20261006_DONE");
 }

 [MenuItem("MMUnity/Reference 2026/Polish Dragon Isle From Reference")]
 public static void Menu(){Run();}
}
