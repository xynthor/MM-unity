using UnityEngine;
using UnityEditor;
using UnityEngine.SceneManagement;
using System;
using System.Linq;

public static class MMLinkedRuntimeSetup20260928 {
 public static void Apply(Scene scene) {
  if(scene.name!="Enroth")throw new Exception("Linked scene required");
  var roots=scene.GetRootGameObjects();
  var preview=roots.SelectMany(g=>g.GetComponentsInChildren<MMEditorPreviewOnly>(true)).Single();
  var streamer=roots.SelectMany(g=>g.GetComponentsInChildren<MMRegionWorldStreamer>(true)).Single();
  if(!preview.keepActiveInPlayMode){
   if(!streamer.player)throw new Exception("Persistent player missing");
   var offset=new Vector3(1024,0,-512);
   streamer.player.position+=offset;
   var controller=streamer.player.GetComponent<MMThirdPersonController>();
   if(controller&&controller.playerCamera){controller.playerCamera.transform.position+=offset;EditorUtility.SetDirty(controller.playerCamera.transform);}
   EditorUtility.SetDirty(streamer.player);
  }
  preview.keepActiveInPlayMode=true;streamer.enabled=false;
  var hero=streamer.player.GetComponent<MMThirdPersonController>();
  if(hero&&hero.playerCamera){hero.playerCamera.depthTextureMode|=DepthTextureMode.Depth;EditorUtility.SetDirty(hero.playerCamera);}
  EditorUtility.SetDirty(preview);EditorUtility.SetDirty(streamer);
  UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(scene);
  Debug.Log("LINKED_RUNTIME enabled: saved 30-tile reconstruction, continuous world coordinates, New Sorpigal spawn.");
 }
}
