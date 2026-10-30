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
public static class MMReferenceDetailPerspectiveQA20260925 {
 const string V="Validation/EdgeGrid20260923/";
 const string P="Preview/";
 static MMReferenceDetailPerspectiveQA20260925(){EditorApplication.delayCall+=Run;}
 static Terrain Find(string name){
  var ts=SceneManager.GetActiveScene().GetRootGameObjects()
   .SelectMany(r=>r.GetComponentsInChildren<Terrain>(true));
  var t=ts.FirstOrDefault(x=>x.terrainData &&
    x.terrainData.name.IndexOf(name,StringComparison.OrdinalIgnoreCase)>=0);
  if(!t)throw new Exception("Missing active linked terrain "+name);
  return t;
 }
 static Vector3 At(Terrain t,float x,float z){
  return t.transform.position+new Vector3(x,
   t.terrainData.GetInterpolatedHeight(x/512f,z/512f),z);
 }
 static void Run(){
  if(!File.Exists(V+"RUN_REFERENCE_PERSPECTIVES.flag"))return;
  if(EditorApplication.isCompiling||EditorApplication.isUpdating){
   EditorApplication.delayCall+=Run;return;
  }
  File.Delete(V+"RUN_REFERENCE_PERSPECTIVES.flag");
  try{CaptureAll();File.WriteAllText(V+"reference_perspectives_runtime.txt",
   "PASS saved 6 visual perspectives from current linked scene\n");}
  catch(Exception e){File.WriteAllText(V+"reference_perspectives_runtime.txt","FAIL "+e+"\n");Debug.LogError(e);}
 }
 public static void CaptureAll(){
  var active=SceneManager.GetActiveScene();
  if(active.path!="Assets/Scenes/World/Enroth.unity"||active.isDirty)
   throw new Exception("Saved linked scene must be active for reference QA");
  var volcano=Find("Kriegspire_NorthTerrain");
  var north=Find("SweetWater_NorthTerrain");
  var paradise=Find("ParadiseValley_WestTerrain");
  var dragon=Find("DragonIsleSouthTerrain");
  var silver=Find("SilverCove_LinkedNorthProfile");
  var sorpigal=Find("NewSorpigalTerrain");
  var originalLights=active.GetRootGameObjects()
   .SelectMany(g=>g.GetComponentsInChildren<Light>(true))
   .Where(l=>l.type==LightType.Directional).ToArray();
  var originalEnabled=originalLights.Select(l=>l.enabled).ToArray();
  var scratch=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Additive);
  var lightGO=new GameObject("TEMP Reference Detail Sun");
  SceneManager.MoveGameObjectToScene(lightGO,scratch);
  lightGO.transform.rotation=Quaternion.Euler(43f,318f,0f);
  var sunlight=lightGO.AddComponent<Light>();
  sunlight.type=LightType.Directional;sunlight.intensity=.7f;
  var cameraGO=new GameObject("TEMP Reference Detail QA Camera");
  SceneManager.MoveGameObjectToScene(cameraGO,scratch);
  var cam=cameraGO.AddComponent<Camera>();cam.orthographic=false;
  cam.fieldOfView=60f;cam.nearClipPlane=.2f;cam.farClipPlane=650f;
  cam.clearFlags=CameraClearFlags.SolidColor;cam.backgroundColor=new Color(.60f,.72f,.8f);
  cam.allowHDR=false;
  cam.depthTextureMode=DepthTextureMode.Depth;
  var rt=new RenderTexture(1280,800,24);rt.antiAliasing=2;
  var prev=RenderTexture.active;cam.targetTexture=rt;
  var prevAmbient=RenderSettings.ambientMode;var prevLight=RenderSettings.ambientLight;
  try {
   foreach(var l in originalLights)if(l)l.enabled=false;
   RenderSettings.ambientMode=AmbientMode.Flat;
   RenderSettings.ambientLight=new Color(.46f,.46f,.46f);
   void Shot(string name,Vector3 origin,Vector3 target){
    cam.transform.position=origin;cam.transform.LookAt(target,Vector3.up);
    cam.Render();RenderTexture.active=rt;
    var tex=new Texture2D(rt.width,rt.height,TextureFormat.RGB24,false);
    tex.ReadPixels(new Rect(0,0,rt.width,rt.height),0,0);tex.Apply();
    File.WriteAllBytes(P+name+".png",tex.EncodeToPNG());
    UnityEngine.Object.DestroyImmediate(tex);
   }
   var c=At(volcano,189f,331f);
   Shot("Reference_KriegspireNorth_Volcano_Oblique",c+new Vector3(-70,65,-75),c+new Vector3(0,5,0));
   c=At(north,384f,8f);
   Shot("Reference_SweetWater_NorthJoin_Oblique",c+new Vector3(25,64,-92),c+new Vector3(0,8,42));
   c=At(paradise,350f,350f);
   Shot("Reference_ParadiseWest_Forest_Oblique",c+new Vector3(-140,75,-70),c+new Vector3(0,5,0));
   c=At(paradise,270f,185f);
   Shot("Reference_ParadiseWest_OchreCape_Oblique",c+new Vector3(-115,85,-110),c+new Vector3(0,7,0));
   c=At(dragon,160f,350f);
   Shot("Reference_DragonSouth_Coast_Oblique",c+new Vector3(-135,65,-85),c+new Vector3(0,6,0));
   c=At(north,462f,263f);
   Shot("Reference_SweetWaterNorth_VolcanicVent_Oblique",
     c+new Vector3(-85f,70f,-83f),c+new Vector3(0f,6f,0f));
   c=At(silver,80f,499f);
   Shot("Reference_SilverCove_NorthJoin_Oblique",
     c+new Vector3(-70f,70f,-93f),c+new Vector3(0f,5f,38f));
   c=At(sorpigal,180f,166f);
   Shot("Reference_NewSorpigal_Architecture_Oblique",
     c+new Vector3(-55f,32f,-60f),c+new Vector3(0f,5f,0f));
  }finally{
   cam.targetTexture=null;RenderTexture.active=prev;
   RenderSettings.ambientMode=prevAmbient;RenderSettings.ambientLight=prevLight;
   for(int i=0;i<originalLights.Length;i++)if(originalLights[i])originalLights[i].enabled=originalEnabled[i];
   UnityEngine.Object.DestroyImmediate(rt);
   EditorSceneManager.CloseScene(scratch,true);
  }
 }
}

// Rerender after north dry-land join

// Visual QA refresh after source-accurate north forest/linked splat restoration.

// rerender after photo reference geology pass

// rerender source-registered north and woodland ground visual QA

// QA pass after 20260926 photo hue + finegrain snow + Paradise forest depth

// recompile to run post-ash six-shot QA 20260927

// PHOTO WEST CAPE DETAIL QA 20260927

// Final western cape reference recovery visual inspection 20260927

// forced rerender post north rebuild 20260927 evening

// refresh perspectives after 20260927 western headland dry-soil adjustment

// post-forest six-angle visual audit, 20260927
