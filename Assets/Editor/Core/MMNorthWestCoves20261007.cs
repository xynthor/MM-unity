using System;using System.IO;using System.Linq;using System.Collections.Generic;using UnityEngine;using UnityEditor;using UnityEngine.SceneManagement;using UnityEditor.SceneManagement;
public static class MMNorthWestCoves20261007
{
 public static void Run(int square,bool keep=false)
 {
  var scene=SceneManager.GetActiveScene();if(scene.isDirty||scene.path!="Assets/Scenes/World/Enroth.unity")throw new Exception("Need clean Enroth");string report="Validation/SequentialRepair/west_coves"+square+"_kept.txt";if(File.Exists(report))throw new Exception("Already kept");
  var terrain=Terrain.activeTerrains.First(t=>t.transform.position.x==-1280+square*512&&t.transform.position.z==768);var original=terrain.terrainData;var clone=UnityEngine.Object.Instantiate(original);clone.hideFlags=HideFlags.HideAndDontSave;var heights=original.GetHeights(0,0,513,513);var root=GameObject.Find("North Alpine Rock Surface "+square+" 20261007");var filter=root.GetComponent<MeshFilter>();var originalMesh=filter.sharedMesh;var mesh=UnityEngine.Object.Instantiate(originalMesh);mesh.hideFlags=HideFlags.HideAndDontSave;
  var cuts=new[]{new[]{new Vector4(-1220,930,-1,16),new Vector4(-1182,949,5,16),new Vector4(-1120,970,15,18),new Vector4(-1080,990,11,14)},new[]{new Vector4(-1150,1080,-1,16),new Vector4(-1110,1075,4,22),new Vector4(-1070,1065,10,20)}};
  for(int z=50;z<=512;z++)for(int x=12;x<490;x++)
  {
   float wx=terrain.transform.position.x+x,wz=768+z,y=heights[z,x]*320-24;if(y<.15f)continue;float candidate=y;
   foreach(var cut in cuts){float distance=999,bed=0,width=0;for(int j=0;j<cut.Length-1;j++){var a=cut[j];var b=cut[j+1];var ab=new Vector2(b.x-a.x,b.y-a.y);float q=Mathf.Clamp01(Vector2.Dot(new Vector2(wx-a.x,wz-a.y),ab)/ab.sqrMagnitude);float d=Vector2.Distance(new Vector2(wx,wz),new Vector2(a.x+q*ab.x,a.y+q*ab.y));if(d<distance){distance=d;bed=Mathf.Lerp(a.z,b.z,q);width=Mathf.Lerp(a.w,b.w,q);}}candidate=Mathf.Min(candidate,Mathf.Lerp(bed,candidate,MMNorthAuthoredTools20261007.S(0,width*2.7f,distance)));}
   float weight=MMNorthAuthoredTools20261007.S(12,30,x)*(1-MMNorthAuthoredTools20261007.S(465,490,x));heights[z,x]=(Mathf.Lerp(y,candidate,weight)+24)/320;
  }
  var moved=new List<System.Tuple<Transform,Vector3>>();bool saved=false;GameObject sea=null;
  try
  {
   clone.SetHeights(0,0,heights);terrain.terrainData=clone;terrain.GetComponent<TerrainCollider>().terrainData=clone;terrain.Flush();var vertices=mesh.vertices;var normals=mesh.normals;var tangents=mesh.tangents;
   for(int z=0;z<=512;z++)for(int x=0;x<=512;x++){int k=z*513+x;float height=clone.GetHeight(x,z);vertices[k].y=height+Mathf.Lerp(.04f,.65f,MMNorthAuthoredTools20261007.S(3,30,height-24));var n=clone.GetInterpolatedNormal(x/512f,z/512f);normals[k]=n;var t=new Vector3(n.y,-n.x,0).normalized;tangents[k]=new Vector4(t.x,t.y,t.z,-1);}
   mesh.vertices=vertices;mesh.normals=normals;mesh.tangents=tangents;mesh.RecalculateBounds();filter.sharedMesh=mesh;sea=MMNorthAuthoredTools20261007.SeaPreview(terrain);
   foreach(var tr in UnityEngine.Object.FindObjectsByType<Transform>().Where(tr=>(tr.name.StartsWith("AuthoredPine_")||tr.name.StartsWith("AuthoredBoulder_")||tr.name.StartsWith("Bush_"))&&tr.position.x>=terrain.transform.position.x&&tr.position.x<terrain.transform.position.x+512&&tr.position.z>=768&&tr.position.z<=1280)){float u=(tr.position.x-terrain.transform.position.x)/512,v=(tr.position.z-768)/512;float delta=clone.GetInterpolatedHeight(u,v)-original.GetInterpolatedHeight(u,v);if(Mathf.Abs(delta)>.001f){moved.Add(System.Tuple.Create(tr,tr.position));tr.position+=Vector3.up*delta;}}
   string dir="Preview/NorthAuthored20261007/WestCoves"+square+"/";Directory.CreateDirectory(dir);float center=terrain.transform.position.x+256;
   MMNorthAuthoredTools20261007.Capture(dir,"shore",new Vector3(-1390,25,1030),new Vector3(-1135,45,1040),false);MMNorthAuthoredTools20261007.Capture(dir,"oblique",new Vector3(center-346,225,740),new Vector3(center+14,55,1050),false);MMNorthAuthoredTools20261007.Capture(dir,"top",new Vector3(center,850,1024),new Vector3(center,0,1024),true);
   MMNorthAuthoredTools20261007.Capture(dir,"cross",new Vector3(-900,190,1350),new Vector3(-800,35,950),false);MMNorthAuthoredTools20261007.Capture(dir,"transition",new Vector3(-1024,155,530),new Vector3(-1024,30,950),false);
   if(keep){string backup="Backups/NorthAuthored20261007/WestCoves"+square+"Before";Directory.CreateDirectory(backup);foreach(var path in new[]{AssetDatabase.GetAssetPath(original),AssetDatabase.GetAssetPath(originalMesh),scene.path}){var dst=Path.Combine(backup,Path.GetFileName(path));if(!File.Exists(dst))File.Copy(path,dst);}original.SetHeights(0,0,heights);EditorUtility.SetDirty(original);AssetDatabase.SaveAssetIfDirty(original);originalMesh.vertices=mesh.vertices;originalMesh.normals=mesh.normals;originalMesh.tangents=mesh.tangents;originalMesh.RecalculateBounds();EditorUtility.SetDirty(originalMesh);AssetDatabase.SaveAssetIfDirty(originalMesh);terrain.terrainData=original;terrain.GetComponent<TerrainCollider>().terrainData=original;filter.sharedMesh=originalMesh;EditorSceneManager.MarkSceneDirty(scene);if(!EditorSceneManager.SaveScene(scene))throw new IOException("Save failed");saved=true;File.WriteAllText(report,"Two authored western coastal valleys and coves kept; terrain and conforming rock mesh updated; no alphamap changes.");}
  }
  finally{if(sea){var sm=sea.GetComponent<MeshFilter>().sharedMesh;UnityEngine.Object.DestroyImmediate(sea);UnityEngine.Object.DestroyImmediate(sm);}terrain.terrainData=original;terrain.GetComponent<TerrainCollider>().terrainData=original;terrain.Flush();filter.sharedMesh=originalMesh;if(!saved)foreach(var p in moved)if(p.Item1)p.Item1.position=p.Item2;UnityEngine.Object.DestroyImmediate(clone);UnityEngine.Object.DestroyImmediate(mesh);}
 }
}


