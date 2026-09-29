using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;

// Reads the current terrain and current water only. Never calls a world generator.
public static class MMLinkedWaterCoverage20260929
{
 const int W=1536,H=1280; const float Step=2, X0=-1792,Z0=-1280,Level=.10f;
 const string Folder="Validation/EdgeGrid20260923/WaterQA20260929";
 const string MeshPath="Assets/World/WorldExtensions/Generated/LinkedConnectedWater20260929.asset";
 const string ObjectName="Linked connected sea and existing lowland waters 20260929";
 static bool[] wet,existing,reached;static MeshRenderer[] oldWater;
 static float Cross(Vector2 a,Vector2 b,Vector2 c){return (b.x-a.x)*(c.y-a.y)-(b.y-a.y)*(c.x-a.x);}
 static void Read()
 {
  var scene=SceneManager.GetActiveScene();if(scene.path!="Assets/Scenes/Enroth_Linked_OpenWorld.unity"||EditorApplication.isPlaying)throw new Exception("Linked Edit Mode scene required");
  if(scene.GetRootGameObjects().Any(g=>g.name==ObjectName))throw new Exception("Connected water already applied; do not regenerate over new state");
  Directory.CreateDirectory(Folder);wet=new bool[W*H];existing=new bool[W*H];reached=new bool[W*H];
  foreach(var t in Terrain.activeTerrains){var p=t.transform.position;var d=t.terrainData;if(d.heightmapResolution!=513||d.size.x!=512||d.size.z!=512)throw new Exception("Unexpected terrain resolution");var heights=d.GetHeights(0,0,513,513);int ox=Mathf.RoundToInt((p.x-X0)/Step),oz=Mathf.RoundToInt((p.z-Z0)/Step);for(int z=0;z<256;z++)for(int x=0;x<256;x++)wet[(oz+z)*W+ox+x]=p.y+heights[z*2+1,x*2+1]*d.size.y<Level;}
  oldWater=scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<MeshRenderer>(false)).Where(r=>r.enabled&&r.sharedMaterials.Any(m=>m&&m.shader.name=="MMUnity/Linked Natural Water")&&r.bounds.center.y>=.09f&&r.bounds.center.y<=.13f&&r.bounds.size.y<.01f).ToArray();
  foreach(var renderer in oldWater){var mesh=renderer.GetComponent<MeshFilter>().sharedMesh;var vertices=mesh.vertices.Select(v=>renderer.transform.TransformPoint(v)).Select(v=>new Vector2((v.x-X0)/Step,(v.z-Z0)/Step)).ToArray();var triangles=mesh.triangles;
   for(int i=0;i<triangles.Length;i+=3){var a=vertices[triangles[i]];var b=vertices[triangles[i+1]];var c=vertices[triangles[i+2]];float area=Cross(a,b,c);if(Mathf.Abs(area)<.000001f)continue;int xa=Mathf.Max(0,Mathf.CeilToInt(Mathf.Min(a.x,Mathf.Min(b.x,c.x))-.5f)),xb=Mathf.Min(W-1,Mathf.FloorToInt(Mathf.Max(a.x,Mathf.Max(b.x,c.x))-.5f));int za=Mathf.Max(0,Mathf.CeilToInt(Mathf.Min(a.y,Mathf.Min(b.y,c.y))-.5f)),zb=Mathf.Min(H-1,Mathf.FloorToInt(Mathf.Max(a.y,Mathf.Max(b.y,c.y))-.5f));
    for(int z=za;z<=zb;z++)for(int x=xa;x<=xb;x++){int k=z*W+x;if(existing[k])continue;var p=new Vector2(x+.5f,z+.5f);if(Cross(a,b,p)*area>=-.00001f&&Cross(b,c,p)*area>=-.00001f&&Cross(c,a,p)*area>=-.00001f)existing[k]=true;}
   }
  }
  var queue=new Queue<int>();for(int z=0;z<H;z++)for(int x=0;x<W;x++){int k=z*W+x;if(wet[k]&&(existing[k]||x==0||z==0||x==W-1||z==H-1)){reached[k]=true;queue.Enqueue(k);}}
  while(queue.Count>0){int k=queue.Dequeue(),x=k%W,z=k/W;Visit(x-1,z);Visit(x+1,z);Visit(x,z-1);Visit(x,z+1);}
  void Visit(int x,int z){if(x<0||z<0||x>=W||z>=H)return;int k=z*W+x;if(wet[k]&&!reached[k]){reached[k]=true;queue.Enqueue(k);}}
 }
 public static void Audit()
 {
  Read();var pixels=new Color32[W*H];int missing=0,isolated=0,total=0;var rows=new List<string>();
  foreach(var t in Terrain.activeTerrains){var p=t.transform.position;int ox=Mathf.RoundToInt((p.x-X0)/Step),oz=Mathf.RoundToInt((p.z-Z0)/Step),newCells=0;for(int z=0;z<256;z++)for(int x=0;x<256;x++){int k=(oz+z)*W+ox+x;if(reached[k]&&!existing[k])newCells++;}rows.Add(t.terrainData.name+" newlyConnectedWaterAreaM2="+(newCells*4));}
  for(int k=0;k<pixels.Length;k++){if(reached[k]){total++;if(!existing[k])missing++;}else if(wet[k])isolated++;pixels[k]=reached[k]?(existing[k]?new Color32(40,100,135,255):new Color32(240,75,35,255)):(wet[k]?new Color32(210,180,50,255):new Color32(58,68,53,255));}
  var image=new Texture2D(W,H,TextureFormat.RGBA32,false);image.SetPixels32(pixels);image.Apply();File.WriteAllBytes(Folder+"/CurrentWaterCoverage.png",image.EncodeToPNG());UnityEngine.Object.DestroyImmediate(image);
  rows.Insert(0,"Sea-level surfaces="+oldWater.Length+" existingTriangles="+oldWater.Sum(r=>(long)r.GetComponent<MeshFilter>().sharedMesh.triangles.Length/3)+" connectedWetAreaM2="+(total*4)+" missingConnectedAreaM2="+(missing*4)+" isolatedBelowSeaAreaPreservedDryM2="+(isolated*4)+" seaLevel="+Level);
  File.WriteAllLines(Folder+"/CoverageAudit.txt",rows);Debug.Log(string.Join("\n",rows));
 }
 public static void Apply()
 {
  if(!File.Exists("Backups/BeforeConnectedWater_20260929/manifest.json"))throw new Exception("Verified backup required");
  using(var sha=System.Security.Cryptography.SHA256.Create()){
   var saved=sha.ComputeHash(File.ReadAllBytes("Assets/Scenes/Enroth_Linked_OpenWorld.unity"));
   var backedUp=sha.ComputeHash(File.ReadAllBytes("Backups/BeforeConnectedWater_20260929/Enroth_Linked_OpenWorld.unity"));
   if(!saved.SequenceEqual(backedUp))throw new Exception("Scene changed after backup; inspect and deliberately create a new baseline");
  }
  Read();if(AssetDatabase.LoadAssetAtPath<Mesh>(MeshPath))throw new Exception("Candidate asset already exists");
  // One-cell overlap beneath banks hides the raster boundary; terrain occludes
  // it. Unconnected below-sea depressions are never seeded as new lakes.
  var draw=(bool[])reached.Clone();for(int z=0;z<H;z++)for(int x=0;x<W;x++)if(reached[z*W+x])for(int dz=-1;dz<=1;dz++)for(int dx=-1;dx<=1;dx++){int xx=x+dx,zz=z+dz;if(xx>=0&&zz>=0&&xx<W&&zz<H&&!wet[zz*W+xx])draw[zz*W+xx]=true;}
  var vertices=new List<Vector3>();var triangles=new List<int>();var active=new Dictionary<long,int>();var rectangles=new List<Vector4>();
  for(int z=0;z<H;z++){var next=new Dictionary<long,int>();for(int x=0;x<W;){if(!draw[z*W+x]){x++;continue;}int start=x;while(x<W&&draw[z*W+x])x++;long key=((long)start<<32)|(uint)x;if(active.TryGetValue(key,out int index)){var rect=rectangles[index];rect.w=z+1;rectangles[index]=rect;next[key]=index;}else{next[key]=rectangles.Count;rectangles.Add(new Vector4(start,z,x,z+1));}}active=next;}
  foreach(var rect in rectangles)Quad(X0+rect.x*Step,Z0+rect.y*Step,X0+rect.z*Step,Z0+rect.w*Step);
  // Ocean continues beyond the authored tile rectangle, beyond camera range.
  Quad(-12000,-12000,12000,Z0);Quad(-12000,Z0+H*Step,12000,12000);Quad(-12000,Z0,X0,Z0+H*Step);Quad(X0+W*Step,Z0,12000,Z0+H*Step);
  void Quad(float x1,float z1,float x2,float z2){int n=vertices.Count;vertices.Add(new Vector3(x1,Level,z1));vertices.Add(new Vector3(x1,Level,z2));vertices.Add(new Vector3(x2,Level,z2));vertices.Add(new Vector3(x2,Level,z1));triangles.AddRange(new[]{n,n+1,n+2,n,n+2,n+3});}
  var mesh=new Mesh{indexFormat=IndexFormat.UInt32,name="Connected sea surface from current wet terrain"};mesh.SetVertices(vertices);mesh.SetTriangles(triangles,0);mesh.RecalculateNormals();mesh.RecalculateBounds();AssetDatabase.CreateAsset(mesh,MeshPath);
  var go=new GameObject(ObjectName);go.AddComponent<MeshFilter>().sharedMesh=mesh;var renderer=go.AddComponent<MeshRenderer>();renderer.sharedMaterial=AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/SourceGridTerrain/EnrothReferenceLinkedWater.mat");renderer.shadowCastingMode=ShadowCastingMode.Off;renderer.receiveShadows=false;
  foreach(var old in oldWater)old.enabled=false;
  EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());AssetDatabase.SaveAssets();EditorSceneManager.SaveScene(SceneManager.GetActiveScene());
  File.WriteAllText(Folder+"/Applied.txt","Replaced linked sea-level surfaces="+oldWater.Length+" newTriangles="+(triangles.Count/3)+" rectangles="+rectangles.Count+" seaLevel="+Level+" canonicalWrites=0 terrainWrites=0 elevatedPondsUnchanged=true\n");Debug.Log(File.ReadAllText(Folder+"/Applied.txt"));
 }
 public static void Verify()
 {
  var go=SceneManager.GetActiveScene().GetRootGameObjects().Single(g=>g.name==ObjectName);
  var image=new Texture2D(2,2,TextureFormat.RGBA32,false,true);image.LoadImage(File.ReadAllBytes(Folder+"/CurrentWaterCoverage.png"));var pixels=image.GetPixels32();UnityEngine.Object.DestroyImmediate(image);
  var probe=new GameObject("TEMP connected water coverage probe");var collider=probe.AddComponent<MeshCollider>();collider.sharedMesh=go.GetComponent<MeshFilter>().sharedMesh;
  int expected=0,missing=0;float worstLevel=0;
  try{for(int z=0;z<H;z++)for(int x=0;x<W;x++){var p=pixels[z*W+x];if(!((p.r==40&&p.b==135)||(p.r==240&&p.g==75)))continue;expected++;if(!collider.Raycast(new Ray(new Vector3(X0+(x+.5f)*Step,100,Z0+(z+.5f)*Step),Vector3.down),out var hit,200))missing++;else worstLevel=Mathf.Max(worstLevel,Mathf.Abs(hit.point.y-Level));}}
  finally{UnityEngine.Object.DestroyImmediate(probe);}
  int surfaces=UnityEngine.Object.FindObjectsByType<MeshRenderer>(FindObjectsSortMode.None).Count(r=>r.enabled&&r.sharedMaterials.Any(m=>m&&m.shader.name=="MMUnity/Linked Natural Water")&&Mathf.Abs(r.bounds.center.y-Level)<.03f);
  string report="Expected connected samples="+expected+" missing="+missing+" worstLevelErrorM="+worstLevel+" activeSeaLevelSurfaces="+surfaces+" testedGridSpacingM="+Step;
  File.WriteAllText(Folder+"/CoverageVerification.txt",report+"\n");if(missing>0||surfaces!=1||worstLevel>.001f)throw new Exception(report);Debug.Log(report);
 }
}
