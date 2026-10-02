using System;using System.IO;using System.Linq;using UnityEngine;using UnityEditor;using UnityEditor.SceneManagement;using UnityEngine.SceneManagement;
public static class MMBatchArchipelagoWestEastQA20261001{
 public static void Run(){
  var s=EditorSceneManager.OpenScene("Assets/Scenes/World/Enroth.unity",OpenSceneMode.Single);
  var west=s.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<Transform>(true)).First(t=>t.name=="Archipelago of the Ancients West - LINKED EXTENSION");
  var east=s.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<Transform>(true)).First(t=>t.name=="Archipelago of the Ancients East - LINKED EXTENSION");
  var wt=west.GetComponentInChildren<Terrain>(true);var et=east.GetComponentInChildren<Terrain>(true);
  string dir="Validation/EdgeGrid20260923/ArchipelagoExpansion20261001";Directory.CreateDirectory(dir);
  var sunGO=new GameObject("sun");sunGO.transform.rotation=Quaternion.Euler(43,320,0);var sun=sunGO.AddComponent<Light>();sun.type=LightType.Directional;sun.intensity=.8f;
  var camGO=new GameObject("cam");var cam=camGO.AddComponent<Camera>();cam.fieldOfView=48;cam.nearClipPlane=.2f;cam.farClipPlane=1800;cam.clearFlags=CameraClearFlags.SolidColor;cam.backgroundColor=new Color(.60f,.72f,.8f);cam.allowHDR=false;
  var rt=new RenderTexture(1600,900,24){antiAliasing=2};var prev=RenderTexture.active;
  void Shot(string n,Vector3 pos,Vector3 look){cam.transform.position=pos;cam.transform.LookAt(look);cam.targetTexture=rt;cam.Render();RenderTexture.active=rt;var tex=new Texture2D(1600,900,TextureFormat.RGB24,false);tex.ReadPixels(new Rect(0,0,1600,900),0,0);tex.Apply();File.WriteAllBytes(dir+"/"+n+".png",tex.EncodeToPNG());UnityEngine.Object.DestroyImmediate(tex);}
  var c=new Vector3(1280,7,-1024);
  Shot("ArchipelagoWestEast_Oblique",c+new Vector3(-650,390,-500),c);
  Shot("ArchipelagoWestEast_NorthOblique",c+new Vector3(-120,420,610),c);
  Shot("ArchipelagoWestEast_Overhead",c+new Vector3(0,1050,0),c);
  cam.targetTexture=null;RenderTexture.active=prev;UnityEngine.Object.DestroyImmediate(rt);UnityEngine.Object.DestroyImmediate(camGO);UnityEngine.Object.DestroyImmediate(sunGO);
  File.WriteAllText(dir+"/qa_inventory.txt",$"west={wt.terrainData.name}@{wt.transform.position} east={et.terrainData.name}@{et.transform.position}\n");
  Debug.Log("ARCHIPELAGO_WEST_EAST_QA_DONE");
 }
}