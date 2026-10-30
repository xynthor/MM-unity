using UnityEngine;
using UnityEngine.SceneManagement;

// Only standalone map scenes need this: the linked world already has a
// persistent New Sorpigal player camera. Do not serialize duplicate cameras.
public static class MMStandaloneSceneCamera {
 [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
 static void Boot(){
  SceneManager.sceneLoaded-=OnLoaded;
  SceneManager.sceneLoaded+=OnLoaded;
  Ensure();
 }
 static void OnLoaded(Scene scene,LoadSceneMode mode){Ensure();}
 static void Ensure(){
  var active=SceneManager.GetActiveScene();
  if(!active.IsValid()||active.name=="Enroth")return;
  if(Camera.allCamerasCount>0)return;
  Terrain terrain=null;
  foreach(var t in Object.FindObjectsByType<Terrain>(
    FindObjectsInactive.Exclude,FindObjectsSortMode.None))
   if(t.gameObject.scene==active){terrain=t;break;}
  if(!terrain)return;
  var data=terrain.terrainData;var pos=terrain.transform.position;
  float cx=pos.x+data.size.x*.5f,cz=pos.z+data.size.z*.5f;
  float floor=terrain.SampleHeight(new Vector3(cx,0,cz))+pos.y;
  Vector3 look=new Vector3(cx,floor+16f,cz);
  var go=new GameObject("Standalone Free-Fly Camera (Play Mode Only)");
  SceneManager.MoveGameObjectToScene(go,active);
  go.tag="MainCamera";
  go.transform.position=look+new Vector3(-37f,30f,-44f);
  go.transform.LookAt(look);
  var cam=go.AddComponent<Camera>();
  cam.fieldOfView=66f;cam.nearClipPlane=.12f;cam.farClipPlane=1800f;
  if(Object.FindObjectsByType<AudioListener>(
      FindObjectsInactive.Exclude,FindObjectsSortMode.None).Length==0)
   go.AddComponent<AudioListener>();
  go.AddComponent<MMStandaloneFreeFly>();
  Debug.Log("STANDALONE_CAMERA_READY "+active.name+
   " | WASD move, Q/E descend/ascend, Shift sprint, RMB look");
 }
}

