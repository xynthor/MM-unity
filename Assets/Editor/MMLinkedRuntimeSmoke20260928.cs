using UnityEngine;
using UnityEditor;
using UnityEngine.SceneManagement;
using UnityEngine.Profiling;
using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;

// Started only by explicit Unity MCP call. No synthetic keyboard/mouse input.
[InitializeOnLoad]
public static class MMLinkedRuntimeSmoke20260928 {
 const string Key="MMLinkedRuntimeSmoke20260928";
 const string Out="Validation/EdgeGrid20260923/TakeoverRuntime/";
 static Camera camera;
 static int step,frames,lastFrame;
 static double began;
 static readonly List<float> timings=new List<float>();
 static readonly List<string> rows=new List<string>();
 static readonly Vector3[] Targets={new Vector3(948,15,-602),new Vector3(-896,50,746),new Vector3(-896,50,790),new Vector3(-1522,15,-71),new Vector3(-1442,16,94),new Vector3(-1632,20,606),new Vector3(-1536,20,786),new Vector3(0,20,0)};
 static MMLinkedRuntimeSmoke20260928(){EditorApplication.playModeStateChanged+=Mode;EditorApplication.update+=Tick;}
 public static void Start(){
  if(EditorApplication.isPlaying||SceneManager.GetActiveScene().path!="Assets/Scenes/Enroth_Linked_OpenWorld.unity"||SceneManager.GetActiveScene().isDirty)throw new Exception("Saved linked world in Edit Mode required.");
  Directory.CreateDirectory(Out);SessionState.SetBool(Key,true);EditorApplication.isPlaying=true;
 }
 static void Mode(PlayModeStateChange mode){
  if(!SessionState.GetBool(Key,false))return;
  if(mode==PlayModeStateChange.EnteredPlayMode){
   try{
    Application.runInBackground=true;
    var player=UnityEngine.Object.FindObjectsByType<MMThirdPersonController>().Single(p=>p.gameObject.activeInHierarchy);
    camera=player.playerCamera;player.enabled=false;
    var character=player.GetComponent<CharacterController>();if(character)character.enabled=false;
    camera.depthTextureMode|=DepthTextureMode.Depth;
    EditorApplication.ExecuteMenuItem("Window/General/Game");
    step=0;frames=0;lastFrame=-1;began=EditorApplication.timeSinceStartup;rows.Clear();timings.Clear();
    rows.Add("Editor Play Mode camera travel; not a standalone performance benchmark.");
    rows.Add("spawn="+player.transform.position.ToString("F3"));
   }catch(Exception e){Finish("FAIL "+e);}
  }
  if(mode==PlayModeStateChange.EnteredEditMode)SessionState.SetBool(Key,false);
 }
 static void Capture(string name){
  var rt=new RenderTexture(960,600,24);var prev=RenderTexture.active;var previousTarget=camera.targetTexture;
  var im=new Texture2D(960,600,TextureFormat.RGB24,false);
  try{camera.targetTexture=rt;camera.Render();RenderTexture.active=rt;im.ReadPixels(new Rect(0,0,960,600),0,0);im.Apply();File.WriteAllBytes(Out+name+".png",im.EncodeToPNG());}
  finally{camera.targetTexture=previousTarget;RenderTexture.active=prev;UnityEngine.Object.DestroyImmediate(im);UnityEngine.Object.DestroyImmediate(rt);}
 }
 static void Tick(){
  if(!SessionState.GetBool(Key,false)||!EditorApplication.isPlaying||!camera)return;
  if(EditorApplication.timeSinceStartup-began>180){Finish("FAIL timed out");return;}
  if(lastFrame==Time.frameCount)return;lastFrame=Time.frameCount;
  try{
   int count=Terrain.activeTerrains.Length;
   if(count!=30||SceneManager.sceneCount!=1)throw new Exception("Linked world replaced or hidden: activeTerrains="+count+" scenes="+SceneManager.sceneCount);
   var target=Targets[step];
   if(step==2||step==6)target=Vector3.Lerp(Targets[step-1],target,Mathf.Clamp01(frames/40f));
   camera.transform.position=target+new Vector3(-45,32,-60);camera.transform.LookAt(target);
   frames++;
   if(frames>10)timings.Add(Time.unscaledDeltaTime*1000);
   if(frames<50)return;
   var ground=Terrain.activeTerrains.FirstOrDefault(t=>Targets[step].x>=t.transform.position.x&&Targets[step].x<=t.transform.position.x+512&&Targets[step].z>=t.transform.position.z&&Targets[step].z<=t.transform.position.z+512);
   if(!ground)throw new Exception("Missing terrain under waypoint "+step);
   float height=ground.SampleHeight(Targets[step])+ground.transform.position.y;
   rows.Add("waypoint="+step+" target="+Targets[step].ToString("F2")+" groundY="+height.ToString("F3")+" activeTerrains="+count+" drawCalls="+UnityStats.drawCalls+" triangles="+UnityStats.triangles+" allocatedMB="+(Profiler.GetTotalAllocatedMemoryLong()/1048576));
   Capture("waypoint_"+step);File.WriteAllLines(Out+"progress.txt",rows);
   step++;frames=0;if(step==Targets.Length)Finish("PASS camera travel across northern and Dragon joins, Paradise and towns; saved world remains active.");
  }catch(Exception e){Finish("FAIL "+e);}
 }
 static void Finish(string status){
  rows.Add(status);
  if(timings.Count>0){timings.Sort();rows.Add("frames="+timings.Count+" meanMs="+timings.Average().ToString("F2")+" p95Ms="+timings[Mathf.Min(timings.Count-1,(int)(timings.Count*.95f))].ToString("F2"));}
  File.WriteAllLines(Out+"result.txt",rows);SessionState.SetBool(Key,false);camera=null;EditorApplication.isPlaying=false;
 }
}
