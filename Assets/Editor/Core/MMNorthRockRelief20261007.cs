using System;using System.IO;using System.Linq;using System.Collections.Generic;using UnityEngine;using UnityEditor;using UnityEngine.SceneManagement;using UnityEditor.SceneManagement;
public static class MMNorthRockRelief20261007
{
 public static void Run(int square,bool keep=false)
 {
  var scene=SceneManager.GetActiveScene();if(scene.isDirty||scene.path!="Assets/Scenes/World/Enroth.unity")throw new Exception("Need clean Enroth");string report="Validation/SequentialRepair/rock_relief"+square+"_kept.txt";if(File.Exists(report))throw new Exception("Already kept");
  var terrain=Terrain.activeTerrains.First(t=>t.transform.position.x==-1280+square*512&&t.transform.position.z==768);var original=terrain.terrainData;var clone=UnityEngine.Object.Instantiate(original);clone.hideFlags=HideFlags.HideAndDontSave;var heights=original.GetHeights(0,0,513,513);var root=GameObject.Find("North Alpine Rock Surface "+square+" 20261007");var filter=root.GetComponent<MeshFilter>();var originalMesh=filter.sharedMesh;var mesh=UnityEngine.Object.Instantiate(originalMesh);mesh.hideFlags=HideFlags.HideAndDontSave;
  var water=AssetDatabase.LoadAssetAtPath<UnityEngine.Mesh>("Assets/World/WorldExtensions/Generated/NorthReference20261001/NorthInlandIcyWater20261006.asset").vertices;
  for(int z=0;z<=512;z++)for(int x=0;x<=512;x++)
  {
   float wx=terrain.transform.position.x+x,wz=768+z,y=heights[z,x]*320-24;float slope=1-original.GetInterpolatedNormal(x/512f,z/512f).y;
   float weight=MMNorthAuthoredTools20261007.S(25,65,y)*MMNorthAuthoredTools20261007.S(.10f,.38f,slope)*MMNorthAuthoredTools20261007.S(0,80,z)*MMNorthAuthoredTools20261007.S(0,12,Mathf.Min(x,512-x));
   float distance=999;
   foreach(var range in new[]{new Vector2Int(73,29),new Vector2Int(131,25),new Vector2Int(181,23)})for(int j=0;j<range.y-1;j++){int k=range.x+j*2;var a=(water[k]+water[k+1])*.5f;var b=(water[k+2]+water[k+3])*.5f;var ab=new Vector2(b.x-a.x,b.z-a.z);float q=Mathf.Clamp01(Vector2.Dot(new Vector2(wx-a.x,wz-a.z),ab)/ab.sqrMagnitude);distance=Mathf.Min(distance,Vector2.Distance(new Vector2(wx,wz),new Vector2(a.x+q*ab.x,a.z+q*ab.y)));}
   weight*=MMNorthAuthoredTools20261007.S(22,42,distance);
   float relief=(Mathf.PerlinNoise(wx*.035f+419,wz*.035f+73)-.5f)*5+(Mathf.PerlinNoise(wx*.11f+109,wz*.11f+317)-.5f)*3+(Mathf.PerlinNoise(wx*.29f+51,wz*.29f+10)-.5f)*1.2f;
   heights[z,x]=(y+weight*relief+24)/320;
  }
  var moved=new List<System.Tuple<Transform,Vector3>>();bool saved=false;
  try
  {
   clone.SetHeights(0,0,heights);terrain.terrainData=clone;terrain.GetComponent<TerrainCollider>().terrainData=clone;terrain.Flush();var vertices=mesh.vertices;var normals=mesh.normals;var tangents=mesh.tangents;
   for(int z=0;z<=512;z++)for(int x=0;x<=512;x++){int k=z*513+x;float height=clone.GetHeight(x,z);vertices[k].y=height+Mathf.Lerp(.04f,.65f,MMNorthAuthoredTools20261007.S(3,30,height-24));var n=clone.GetInterpolatedNormal(x/512f,z/512f);normals[k]=n;var t=new Vector3(n.y,-n.x,0).normalized;tangents[k]=new Vector4(t.x,t.y,t.z,-1);}
   mesh.vertices=vertices;mesh.normals=normals;mesh.tangents=tangents;mesh.RecalculateBounds();filter.sharedMesh=mesh;
   foreach(var tr in UnityEngine.Object.FindObjectsByType<Transform>().Where(tr=>tr.name.StartsWith("AuthoredPine_")&&tr.position.x>=terrain.transform.position.x&&tr.position.x<terrain.transform.position.x+512&&tr.position.z>=768&&tr.position.z<=1280)){float u=(tr.position.x-terrain.transform.position.x)/512,v=(tr.position.z-768)/512;float delta=clone.GetInterpolatedHeight(u,v)-original.GetInterpolatedHeight(u,v);if(Mathf.Abs(delta)>.001f){moved.Add(System.Tuple.Create(tr,tr.position));tr.position+=Vector3.up*delta;}}
   string dir="Preview/NorthAuthored20261007/RockRelief"+square+"/";Directory.CreateDirectory(dir);float center=terrain.transform.position.x+256;
   MMNorthAuthoredTools20261007.Capture(dir,"shore",new Vector3(center,26,1475),new Vector3(center,70,1170),false);MMNorthAuthoredTools20261007.Capture(dir,"oblique",new Vector3(center-346,225,740),new Vector3(center+14,55,1050),false);MMNorthAuthoredTools20261007.Capture(dir,"top",new Vector3(center,850,1024),new Vector3(center,0,1024),true);
   if(keep){string backup="Backups/NorthAuthored20261007/RockRelief"+square+"Before";Directory.CreateDirectory(backup);foreach(var path in new[]{AssetDatabase.GetAssetPath(original),AssetDatabase.GetAssetPath(originalMesh),scene.path}){var dst=Path.Combine(backup,Path.GetFileName(path));if(!File.Exists(dst))File.Copy(path,dst);}original.SetHeights(0,0,heights);EditorUtility.SetDirty(original);AssetDatabase.SaveAssetIfDirty(original);originalMesh.vertices=mesh.vertices;originalMesh.normals=mesh.normals;originalMesh.tangents=mesh.tangents;originalMesh.RecalculateBounds();EditorUtility.SetDirty(originalMesh);AssetDatabase.SaveAssetIfDirty(originalMesh);terrain.terrainData=original;terrain.GetComponent<TerrainCollider>().terrainData=original;filter.sharedMesh=originalMesh;EditorSceneManager.MarkSceneDirty(scene);if(!EditorSceneManager.SaveScene(scene))throw new IOException("Save failed");saved=true;File.WriteAllText(report,"Bounded rock relief kept; hydrology and edges protected; no alphamap changes.");}
  }
  finally{terrain.terrainData=original;terrain.GetComponent<TerrainCollider>().terrainData=original;terrain.Flush();filter.sharedMesh=originalMesh;if(!saved)foreach(var p in moved)if(p.Item1)p.Item1.position=p.Item2;UnityEngine.Object.DestroyImmediate(clone);UnityEngine.Object.DestroyImmediate(mesh);}
 }
}

