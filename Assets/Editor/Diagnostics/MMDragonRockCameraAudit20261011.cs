using System;using System.IO;using System.Linq;using System.Collections.Generic;using UnityEngine;using UnityEditor;using UnityEditor.SceneManagement;
public static class MMDragonRockCameraAudit20261011 {
 static void Shot(string dest,Vector3 p,Vector3 target,bool ortho,float size){
  var obj=new GameObject("TEMP_DRAGON_ROCK_CAMERA");var c=obj.AddComponent<Camera>();
  c.transform.position=p;c.transform.rotation=Quaternion.LookRotation(target-p,Vector3.up);
  c.orthographic=ortho;if(ortho)c.orthographicSize=size;else c.fieldOfView=size;
  c.nearClipPlane=.1f;c.farClipPlane=2200;c.clearFlags=CameraClearFlags.Skybox;
  var original=RenderTexture.active;var rt=new RenderTexture(1400,920,24);
  c.targetTexture=rt;c.Render();RenderTexture.active=rt;
  var t=new Texture2D(1400,920,TextureFormat.RGB24,false);t.ReadPixels(new Rect(0,0,1400,920),0,0);t.Apply();
  File.WriteAllBytes(dest,t.EncodeToPNG());RenderTexture.active=original;c.targetTexture=null;
  UnityEngine.Object.DestroyImmediate(t);UnityEngine.Object.DestroyImmediate(rt);UnityEngine.Object.DestroyImmediate(obj);
 }
 public static void Run(){
  var sc=EditorSceneManager.OpenScene("Assets/Scenes/World/Enroth.unity",OpenSceneMode.Single);
  if(sc.isDirty)throw new Exception("Scene dirty");
  string d="Preview/DragonFloatingRocks20261011/baseline";Directory.CreateDirectory(d);
  Shot(d+"/lair_oblique.png",new Vector3(-1481,74,875),new Vector3(-1526,22,825),false,61);
  Shot(d+"/lair_top.png",new Vector3(-1528,170,826),new Vector3(-1528,0,826),true,55);
  Shot(d+"/ruins_oblique.png",new Vector3(-1519,122,745),new Vector3(-1570,44,657),false,57);
  Shot(d+"/ruins_top.png",new Vector3(-1568,245,654),new Vector3(-1568,0,654),true,86);
  Shot(d+"/shore_south.png",new Vector3(-1515,130,460),new Vector3(-1515,10,480),false,61);
  Debug.Log("DRAGON_ROCK_CAMERA_AUDIT_PASS saved=False sceneDirty="+sc.isDirty);
 }
}
