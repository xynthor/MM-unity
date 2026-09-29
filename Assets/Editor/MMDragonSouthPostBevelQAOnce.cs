using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using System;
using System.Linq;
using System.IO;
[InitializeOnLoad]
internal static class MMDragonSouthPostBevelQAOnce {
 const string Dir="Validation/EdgeGrid20260923";
 const string Done=Dir+"/postbevel_qa.txt";
 static MMDragonSouthPostBevelQAOnce(){EditorApplication.delayCall+=Run;}
 static void Run(){
  if(File.Exists(Done)||EditorApplication.isPlayingOrWillChangePlaymode||EditorApplication.isCompiling)return;
  Directory.CreateDirectory(Dir);
  var scene=SceneManager.GetActiveScene();
  if(scene.path!="Assets/Scenes/Enroth_Linked_OpenWorld.unity"){File.WriteAllText(Done,"SKIPPED active_scene="+scene.path);return;}
  try{
   var d=AssetDatabase.LoadAssetAtPath<TerrainData>("Assets/World/DragonIsle/Generated/DragonIsleSouthTerrain.asset");
   var s=AssetDatabase.LoadAssetAtPath<TerrainData>("Assets/World/SweetWater/Generated/SweetWaterTerrain.asset");
   var n=AssetDatabase.LoadAssetAtPath<TerrainData>("Assets/World/DragonIsle/Generated/DragonIsleNorthTerrain.asset");
   var p=AssetDatabase.LoadAssetAtPath<TerrainData>("Assets/World/WorldExtensions/Generated/ParadiseValley_WestTerrain.asset");
   if(!d||!s||!n||!p)throw new Exception("Terrain missing");
   var dh=d.GetHeights(0,0,513,513);var sh=s.GetHeights(0,0,513,513);
   var nh=n.GetHeights(0,0,513,513);var ph=p.GetHeights(0,0,513,513);
   float edge=0,slope=0,curve=0,north=0,paradise=0;
   for(int z=0;z<513;z++){
    edge=Mathf.Max(edge,Mathf.Abs(dh[z,512]-sh[z,0])*320f);
    slope=Mathf.Max(slope,Mathf.Abs((dh[z,512]-dh[z,511])-(sh[z,1]-sh[z,0]))*320f);
    curve=Mathf.Max(curve,Mathf.Abs((dh[z,512]-2*dh[z,511]+dh[z,510])-(sh[z,0]-2*sh[z,1]+sh[z,2]))*320f);
    north=Mathf.Max(north,Mathf.Abs(dh[512,z]-nh[0,z])*320f);
    paradise=Mathf.Max(paradise,Mathf.Abs(dh[0,z]-ph[512,z])*320f);
   }
   int pondWater=0;
   for(int z=185;z<=230;z++)for(int x=458;x<=495;x++)if(-24f+dh[z,x]*320f<=.1f)pondWater++;
   var grove=scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<Transform>(true))
      .FirstOrDefault(t=>t.name=="Sweet Water Western Shore - Source Forest Continuation");
   int trees=grove?grove.childCount:0;
   var woodland=grove?grove.parent.Find("Reference Woodland - Dragon Isle"):null;
   int woodlandCount=woodland?woodland.childCount:0;
   string report="postbevel edge_m="+edge.ToString("F5")+" slope="+slope.ToString("F5")+
      " curvature="+curve.ToString("F5")+" north="+north.ToString("F5")+" paradise="+paradise.ToString("F5")+
      " pond_water_samples="+pondWater+" source_trees="+trees+" woodland="+woodlandCount;
   if(edge>.02f||slope>.03f||curve>.03f||north>.10f||paradise>.10f||pondWater<145||trees!=220||woodlandCount!=248)
     throw new Exception("POSTBEVEL_AUDIT_FAIL "+report);
   Capture(scene,report);
   File.WriteAllText(Done,"PASS "+report+"\noblique_postbevel_images=2\n");
   Debug.Log("POSTBEVEL_QA_PASS "+report);
  }catch(Exception e){File.WriteAllText(Done,"FAIL "+e+"\n");Debug.LogError(e);}
 }
 static void Capture(Scene main,string report){
  var scratch=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Additive);
  var cg=new GameObject("TEMP PostBevel QA Camera");
  SceneManager.MoveGameObjectToScene(cg,scratch);
  var cam=cg.AddComponent<Camera>();cam.orthographic=false;cam.fieldOfView=58f;
  cam.nearClipPlane=.3f;cam.farClipPlane=850f;
  cam.clearFlags=CameraClearFlags.SolidColor;cam.backgroundColor=new Color(.63f,.75f,.82f);cam.allowHDR=false;
  var rt=new RenderTexture(1280,800,24);rt.antiAliasing=2;
  var previous=RenderTexture.active;cam.targetTexture=rt;Directory.CreateDirectory("Preview");
  try{
   Shot("SweetWaterWest_Oblique_InlandView_postbevel",new Vector3(-1270f,43f,630f),new Vector3(-1420f,5f,505f));
   Shot("SweetWaterWest_Oblique_OffshoreView_postbevel",new Vector3(-1450f,30f,520f),new Vector3(-1310f,7f,510f));
  }finally{
   RenderTexture.active=previous;cam.targetTexture=null;
   UnityEngine.Object.DestroyImmediate(rt);EditorSceneManager.CloseScene(scratch,true);
  }
  void Shot(string name,Vector3 from,Vector3 look){
   cam.transform.position=from;cam.transform.LookAt(look,Vector3.up);cam.Render();
   RenderTexture.active=rt;var tex=new Texture2D(rt.width,rt.height,TextureFormat.RGB24,false);
   tex.ReadPixels(new Rect(0,0,rt.width,rt.height),0,0);tex.Apply();
   File.WriteAllBytes("Preview/"+name+".png",tex.EncodeToPNG());UnityEngine.Object.DestroyImmediate(tex);
  }
 }
}
