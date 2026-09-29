using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using UnityEngine.Rendering;
using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
[InitializeOnLoad]
public static class MMReference20260925VisualQAOnce {
 const string V="Validation/EdgeGrid20260923/";
 const string P="Preview/";
 static MMReference20260925VisualQAOnce(){EditorApplication.delayCall+=Run;}
 static void Run(){
  if(!File.Exists(V+"RUN_REFERENCE_20260925_QA.flag"))return;
  File.Delete(V+"RUN_REFERENCE_20260925_QA.flag");
  try {
   if(SceneManager.GetActiveScene().path!="Assets/Scenes/World/Enroth.unity" ||
      SceneManager.GetActiveScene().isDirty)
    throw new Exception("Saved linked world must be the active scene");
   Audit();
   Render();
   File.WriteAllText(V+"reference_reconstruction_20260925_QA_runtime.txt","PASS 30/30 grid and perspective renders saved\n");
  }catch(Exception ex){
   File.WriteAllText(V+"reference_reconstruction_20260925_QA_runtime.txt","FAIL "+ex+"\n");
   Debug.LogError(ex);
  }
 }
 public static void RunFinalQA(){
  if(SceneManager.GetActiveScene().path!="Assets/Scenes/World/Enroth.unity" ||
     SceneManager.GetActiveScene().isDirty)
   throw new Exception("Saved linked world required for final render QA");
  Audit();Render();
  File.WriteAllText(V+"reference_reconstruction_20260925_QA_runtime.txt",
   "PASS 30/30 grid and perspective renders saved\n");
 }
 static void Audit(){
  var s=SceneManager.GetActiveScene();
  var t=s.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<Terrain>(true)).ToArray();
  var grid=new Terrain[6,5];
  int bad=0,duplicate=0;float worst=0;int seams=0;
  foreach(var a in t){
   var p=a.transform.position;var size=a.terrainData.size;
   int col=Mathf.RoundToInt((p.x+size.x*.5f)/512f)+2;
   int row=Mathf.RoundToInt((p.z+size.z*.5f)/512f)+1;
   if(col< -1||col>4||row< -1||row>3||Mathf.Abs(size.x-512)>.01f||
      Mathf.Abs(size.z-512)>.01f){bad++;continue;}
   if(grid[col+1,row+1])duplicate++;else grid[col+1,row+1]=a;
  }
  int holes=0;
  for(int x=0;x<6;x++)for(int z=0;z<5;z++){
   var a=grid[x,z];if(!a){holes++;continue;}
   if(x<5&&grid[x+1,z]){
    var b=grid[x+1,z];
    var ah=a.terrainData.GetHeights(512,0,1,513);
    var bh=b.terrainData.GetHeights(0,0,1,513);
    for(int i=0;i<513;i++)
     worst=Mathf.Max(worst,Mathf.Abs((a.transform.position.y+ah[i,0]*a.terrainData.size.y)-
      (b.transform.position.y+bh[i,0]*b.terrainData.size.y)));
    seams++;
   }
   if(z<4&&grid[x,z+1]){
    var b=grid[x,z+1];
    var ah=a.terrainData.GetHeights(0,512,513,1);
    var bh=b.terrainData.GetHeights(0,0,513,1);
    for(int i=0;i<513;i++)
     worst=Mathf.Max(worst,Mathf.Abs((a.transform.position.y+ah[0,i]*a.terrainData.size.y)-
      (b.transform.position.y+bh[0,i]*b.terrainData.size.y)));
    seams++;
   }
  }
  var title="grid="+t.Length+"/30 holes="+holes+" duplicate="+duplicate+
   " bad="+bad+" seams="+seams+"/49 max_gap_m="+worst.ToString("F5");
  if(t.Length!=30||holes>0||duplicate>0||bad>0||seams!=49||worst>.1f)
   throw new Exception("Linked terrain QA FAIL "+title);
  File.WriteAllText(V+"reference_reconstruction_20260925_grid.txt",title+"\n");
  Debug.Log("REFERENCE_RECONSTRUCTION_GRID_PASS "+title);
 }
 static void Render(){
  var linked=SceneManager.GetActiveScene();
  var lights=linked.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<Light>(true))
   .Where(l=>l.type==LightType.Directional).ToArray();
  var enabled=lights.Select(l=>l.enabled).ToArray();
  var ambient=RenderSettings.ambientMode;var ambientLight=RenderSettings.ambientLight;
  bool fog=RenderSettings.fog;var prior=RenderTexture.active;
  var scratch=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Additive);
  GameObject sun=null,go=null;Camera cam=null;
  try{
   foreach(var l in lights)l.enabled=false;
   RenderSettings.fog=false;RenderSettings.ambientMode=AmbientMode.Flat;
   RenderSettings.ambientLight=new Color(.53f,.53f,.53f);
   sun=new GameObject("TEMP Reference20260925 QA Sun");
   SceneManager.MoveGameObjectToScene(sun,scratch);
   sun.transform.rotation=Quaternion.Euler(55f,326f,0f);
   var lit=sun.AddComponent<Light>();lit.type=LightType.Directional;lit.intensity=.7f;
   go=new GameObject("TEMP Reference20260925 QA Camera");
   SceneManager.MoveGameObjectToScene(go,scratch);
   cam=go.AddComponent<Camera>();cam.orthographic=true;cam.depthTextureMode=DepthTextureMode.Depth;
   cam.nearClipPlane=.1f;cam.farClipPlane=7000f;
   cam.allowHDR=false;cam.clearFlags=CameraClearFlags.SolidColor;
   cam.backgroundColor=new Color(.12f,.16f,.19f);
   Directory.CreateDirectory(P);
   Capture("Enroth_Reference_After20260925_Top",new Vector3(-256f,2300f,0f),
     new Vector3(-256f,0f,0f),1450f);
   Capture("Enroth_Reference_After20260925_North",new Vector3(-350f,1500f,780f),
     new Vector3(-350f,0f,780f),715f);
   Capture("Enroth_Reference_After20260925_West",new Vector3(-1320f,1000f,120f),
     new Vector3(-1200f,0f,300f),600f);
   Capture("Enroth_Reference_After20260925_Dragon",new Vector3(-1380f,1150f,720f),
     new Vector3(-1310f,0f,760f),550f);
   void Capture(string name,Vector3 from,Vector3 target,float size){
    cam.transform.position=from;
    if(name=="Enroth_Reference_After20260925_Top"||
       name=="Enroth_Reference_After20260925_North")
     cam.transform.rotation=Quaternion.Euler(90f,0f,0f); // +Z (north) is image UP
    else cam.transform.LookAt(target,Vector3.up);
    cam.orthographicSize=size;
    var rt=new RenderTexture(1400,1050,24);rt.antiAliasing=2;
    Texture2D tex=null;
    try{
     cam.targetTexture=rt;cam.Render();RenderTexture.active=rt;
     tex=new Texture2D(1400,1050,TextureFormat.RGB24,false);
     tex.ReadPixels(new Rect(0,0,1400,1050),0,0);tex.Apply();
     File.WriteAllBytes(P+name+".png",tex.EncodeToPNG());
    }finally{
     RenderTexture.active=prior;cam.targetTexture=null;
     if(tex)UnityEngine.Object.DestroyImmediate(tex);
     UnityEngine.Object.DestroyImmediate(rt);
    }
   }
  }finally{
   RenderTexture.active=prior;
   for(int k=0;k<lights.Length;k++)if(lights[k])lights[k].enabled=enabled[k];
   RenderSettings.ambientMode=ambient;RenderSettings.ambientLight=ambientLight;RenderSettings.fog=fog;
   if(go)UnityEngine.Object.DestroyImmediate(go);
   if(sun)UnityEngine.Object.DestroyImmediate(sun);
   EditorSceneManager.CloseScene(scratch,true);
  }
 }
}

// QA_RELOAD_AFTER_POND_RESTORE_20260925

// Refresh and render imported bright linked-only snow 20260927
