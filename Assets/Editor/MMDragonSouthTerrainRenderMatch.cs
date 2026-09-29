using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using System;
using System.IO;
using System.Linq;
public static class MMDragonSouthTerrainRenderMatch {
 [MenuItem("MMUnity/World/Match Dragon South Renderer To Sweet Water")]
 public static void Apply(){
  var target=EditorSceneManager.OpenScene("Assets/Scenes/DragonIsle_South.unity",OpenSceneMode.Single);
  var tr=target.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<Terrain>(true)).FirstOrDefault();
  if(!tr)throw new Exception("Dragon South terrain component missing");
  var src=EditorSceneManager.OpenScene("Assets/Scenes/SweetWater_SourceGrid.unity",OpenSceneMode.Additive);
  var sr=src.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<Terrain>(true)).FirstOrDefault();
  if(!sr)throw new Exception("Sweet Water terrain component missing");
  tr.heightmapPixelError=sr.heightmapPixelError;
  tr.basemapDistance=sr.basemapDistance;
  tr.detailObjectDistance=sr.detailObjectDistance;
  tr.detailObjectDensity=sr.detailObjectDensity;
  tr.drawInstanced=sr.drawInstanced;
  tr.shadowCastingMode=sr.shadowCastingMode;
  tr.reflectionProbeUsage=sr.reflectionProbeUsage;
  tr.allowAutoConnect=sr.allowAutoConnect;
  tr.groupingID=sr.groupingID;
  EditorSceneManager.CloseScene(src,true);
  EditorSceneManager.MarkSceneDirty(target);
  if(!EditorSceneManager.SaveScene(target))throw new IOException("Save Dragon South renderer failed");
  Directory.CreateDirectory("Validation/EdgeGrid20260923");
  File.WriteAllText("Validation/EdgeGrid20260923/dragon_south_renderer_match.txt",
   "heightmapPixelError="+tr.heightmapPixelError+"\n"+
   "basemapDistance="+tr.basemapDistance+"\n"+
   "detailObjectDistance="+tr.detailObjectDistance+"\n"+
   "drawInstanced="+tr.drawInstanced+"\n");
  Debug.Log("DRAGON_SOUTH_RENDERER_MATCHED_TO_SWEET_WATER");
 }
}
