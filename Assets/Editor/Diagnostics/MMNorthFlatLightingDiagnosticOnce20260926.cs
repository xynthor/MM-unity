using UnityEngine;
using UnityEditor;
using UnityEngine.SceneManagement;
using UnityEngine.Rendering;
using System;
using System.IO;
using System.Linq;
[InitializeOnLoad]
internal static class MMNorthFlatLightingDiagnosticOnce20260926 {
 const string V="Validation/EdgeGrid20260923/";
 static MMNorthFlatLightingDiagnosticOnce20260926(){EditorApplication.delayCall+=Run;}
 static void Run(){
  string flag=V+"RUN_NORTH_FLAT_LIGHT_DIAG.flag";
  if(!File.Exists(flag))return;File.Delete(flag);
  var sc=SceneManager.GetActiveScene();
  if(sc.path!="Assets/Scenes/World/Enroth.unity"||sc.isDirty)return;
  var lights=sc.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<Light>(true)).ToArray();
  var prior=lights.Select(l=>l.enabled).ToArray();
  var mode=RenderSettings.ambientMode;var amb=RenderSettings.ambientLight;bool fog=RenderSettings.fog;
  var scratch=UnityEditor.SceneManagement.EditorSceneManager.NewScene(
    UnityEditor.SceneManagement.NewSceneSetup.EmptyScene,
    UnityEditor.SceneManagement.NewSceneMode.Additive);
  GameObject go=null;RenderTexture oldRT=RenderTexture.active;
  try{
   foreach(var l in lights)l.enabled=false;
   RenderSettings.fog=false;RenderSettings.ambientMode=AmbientMode.Flat;RenderSettings.ambientLight=Color.white;
   go=new GameObject("TEMP flat north camera");SceneManager.MoveGameObjectToScene(go,scratch);
   var cam=go.AddComponent<Camera>();cam.orthographic=true;cam.nearClipPlane=.1f;cam.farClipPlane=7000f;
   cam.allowHDR=false;cam.clearFlags=CameraClearFlags.SolidColor;cam.backgroundColor=new Color(.12f,.16f,.19f);
   cam.transform.position=new Vector3(-350f,1500f,1070f);cam.transform.LookAt(new Vector3(-350f,0f,800f));
   cam.orthographicSize=780f;
   var rt=new RenderTexture(1400,1050,24);cam.targetTexture=rt;cam.Render();RenderTexture.active=rt;
   var tex=new Texture2D(1400,1050,TextureFormat.RGB24,false);tex.ReadPixels(new Rect(0,0,1400,1050),0,0);tex.Apply();
   File.WriteAllBytes("Preview/Enroth_North_FlatLightingDiag_20260926.png",tex.EncodeToPNG());
   UnityEngine.Object.DestroyImmediate(tex);UnityEngine.Object.DestroyImmediate(rt);
   File.WriteAllText(V+"north_flat_lighting_diag_20260926.txt","PASS ambientWhite=true directionalLights=off\n");
  }finally{
   RenderTexture.active=oldRT;for(int i=0;i<lights.Length;i++)if(lights[i])lights[i].enabled=prior[i];
   RenderSettings.ambientMode=mode;RenderSettings.ambientLight=amb;RenderSettings.fog=fog;
   if(go)UnityEngine.Object.DestroyImmediate(go);UnityEditor.SceneManagement.EditorSceneManager.CloseScene(scratch,true);
  }
 }
}

// exact present-scene illumination QA 20260927 20h
