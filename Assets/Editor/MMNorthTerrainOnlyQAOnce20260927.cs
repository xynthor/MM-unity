using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using UnityEngine.Rendering;
using System;
using System.IO;
using System.Linq;
[InitializeOnLoad]
internal static class MMNorthTerrainOnlyQAOnce20260927 {
 const string Flag="Validation/EdgeGrid20260923/RUN_NORTH_TERRAIN_ONLY_20260927.flag";
 static MMNorthTerrainOnlyQAOnce20260927(){EditorApplication.delayCall+=Run;}
 static void Run(){
  if(!File.Exists(Flag))return;File.Delete(Flag);
  var sc=SceneManager.GetActiveScene();
  if(sc.path!="Assets/Scenes/Enroth_Linked_OpenWorld.unity"||sc.isDirty){
   Debug.LogError("North terrain-only QA requires saved linked scene");return;
  }
  var rs=sc.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<Renderer>(true)).ToArray();
  var enabled=rs.Select(r=>r.enabled).ToArray();
  var lights=sc.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<Light>(true)).ToArray();
  var prior=lights.Select(l=>l.enabled).ToArray();
  var ambient=RenderSettings.ambientLight;var mode=RenderSettings.ambientMode;var fog=RenderSettings.fog;
  RenderTexture old=RenderTexture.active;GameObject camObj=null;
  var scratch=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Additive);
  try{
   foreach(var r in rs)r.enabled=false;
   foreach(var l in lights)l.enabled=false;
   RenderSettings.ambientMode=AmbientMode.Flat;RenderSettings.ambientLight=Color.white;RenderSettings.fog=false;
   camObj=new GameObject("TEMP TerrainOnly Diagnostic Camera");SceneManager.MoveGameObjectToScene(camObj,scratch);
   var cam=camObj.AddComponent<Camera>();cam.orthographic=true;cam.allowHDR=false;cam.nearClipPlane=.1f;
   cam.farClipPlane=4000f;cam.backgroundColor=new Color(.11f,.16f,.2f);cam.clearFlags=CameraClearFlags.SolidColor;
   cam.transform.position=new Vector3(-350f,1500f,780f);cam.transform.rotation=Quaternion.Euler(90f,0f,0f);
   cam.orthographicSize=715f;var rt=new RenderTexture(1400,1050,24);
   cam.targetTexture=rt;cam.Render();RenderTexture.active=rt;
   var tex=new Texture2D(1400,1050,TextureFormat.RGB24,false);
   tex.ReadPixels(new Rect(0,0,1400,1050),0,0);tex.Apply();
   File.WriteAllBytes("Preview/Enroth_North_TerrainOnly_Flat_20260927.png",tex.EncodeToPNG());
   UnityEngine.Object.DestroyImmediate(tex);cam.targetTexture=null;UnityEngine.Object.DestroyImmediate(rt);
   File.WriteAllText("Validation/EdgeGrid20260923/north_terrain_only_QA_20260927.txt",
    "PASS terrain-only screenshot (no other renderers/lights); scene not saved; canonical unaffected\n");
  }catch(Exception e){Debug.LogError("NORTH_TERRAIN_ONLY "+e);}
  finally{
   RenderTexture.active=old;
   for(int i=0;i<rs.Length;i++)if(rs[i])rs[i].enabled=enabled[i];
   for(int i=0;i<lights.Length;i++)if(lights[i])lights[i].enabled=prior[i];
   RenderSettings.ambientLight=ambient;RenderSettings.ambientMode=mode;RenderSettings.fog=fog;
   if(camObj)UnityEngine.Object.DestroyImmediate(camObj);
   EditorSceneManager.CloseScene(scratch,true);
  }
 }
}
