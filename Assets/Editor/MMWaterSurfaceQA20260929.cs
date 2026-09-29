using System;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;

public static class MMWaterSurfaceQA20260929
{
 public static void Capture(string tag)
 {
  if(tag.Any(c=>!char.IsLetterOrDigit(c)&&c!='_'))throw new Exception("Invalid capture tag");
  var scene=SceneManager.GetActiveScene();
  if(scene.path!="Assets/Scenes/World/Enroth.unity")throw new Exception("Linked scene required");
  string folder="Validation/EdgeGrid20260923/WaterQA20260929/"+tag;
  Directory.CreateDirectory(folder);
  var lights=scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<Light>(true)).Where(l=>l.type==LightType.Directional).ToArray();
  var enabled=lights.Select(l=>l.enabled).ToArray();
  var ambient=RenderSettings.ambientMode;var ambientColor=RenderSettings.ambientLight;bool fog=RenderSettings.fog;
  var scratch=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Additive);
  var cameraObject=new GameObject("TEMP water QA camera");SceneManager.MoveGameObjectToScene(cameraObject,scratch);var camera=cameraObject.AddComponent<Camera>();
  var sunObject=new GameObject("TEMP water QA daylight");SceneManager.MoveGameObjectToScene(sunObject,scratch);var sun=sunObject.AddComponent<Light>();sun.type=LightType.Directional;sun.intensity=.7f;sunObject.transform.rotation=Quaternion.Euler(55,326,0);
  try{
   foreach(var light in lights)light.enabled=false;
   RenderSettings.ambientMode=AmbientMode.Flat;RenderSettings.ambientLight=new Color(.46f,.46f,.46f);RenderSettings.fog=false;
   camera.fieldOfView=55;camera.nearClipPlane=.1f;camera.farClipPlane=6000;camera.depthTextureMode=DepthTextureMode.Depth;camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=new Color(.34f,.49f,.57f);camera.allowHDR=false;
   View("BootlegBay_Top",new Vector3(512,750,0),new Vector3(512,0,0),280);
   View("FreeHaven_Top",new Vector3(0,750,0),Vector3.zero,280);
   View("FrozenHighlands_Top",new Vector3(0,750,512),new Vector3(0,0,512),280);
   View("CastleIronfist_Top",new Vector3(512,750,-512),new Vector3(512,0,-512),280);
   View("BootlegBay_Islands",new Vector3(425,92,45),new Vector3(540,0,146),0);
   View("BootlegBay_Coast",new Vector3(325,55,-175),new Vector3(405,0,-80),0);
   View("FreeHaven_Harbor",new Vector3(300,70,-75),new Vector3(210,0,30),0);
   View("FrozenHighlands_Watercourse",new Vector3(-70,100,430),new Vector3(-115,10,500),0);
   View("Dragon_SweetWater_Channel",new Vector3(-1470,95,510),new Vector3(-1350,0,625),0);
   View("NewSorpigal_Coast",new Vector3(990,50,-680),new Vector3(1080,0,-590),0);
   View("SweetWater_Pond",new Vector3(-990,75,470),new Vector3(-915,0,555),0);
   void View(string name,Vector3 eye,Vector3 target,float size){
    camera.transform.position=eye;camera.orthographic=size>0;
    if(size>0){camera.orthographicSize=size;camera.transform.rotation=Quaternion.Euler(90,0,0);}else camera.transform.LookAt(target);
    var rt=new RenderTexture(1280,960,24){antiAliasing=4};var image=new Texture2D(1280,960,TextureFormat.RGB24,false);var previous=RenderTexture.active;
    try{camera.targetTexture=rt;camera.Render();RenderTexture.active=rt;image.ReadPixels(new Rect(0,0,1280,960),0,0);image.Apply();File.WriteAllBytes(folder+"/"+name+".png",image.EncodeToPNG());}
    finally{camera.targetTexture=null;RenderTexture.active=previous;UnityEngine.Object.DestroyImmediate(image);UnityEngine.Object.DestroyImmediate(rt);}
   }
  }finally{for(int i=0;i<lights.Length;i++)lights[i].enabled=enabled[i];RenderSettings.ambientMode=ambient;RenderSettings.ambientLight=ambientColor;RenderSettings.fog=fog;EditorSceneManager.CloseScene(scratch,true);SceneManager.SetActiveScene(scene);}
  Debug.Log("Water fixed views saved: "+folder);
 }
}
