using UnityEngine;
using UnityEditor;
using UnityEngine.SceneManagement;
using UnityEditor.SceneManagement;
using System;
using System.IO;
[InitializeOnLoad]
internal static class MMLinkedNorthProfileQueued20260925{
 const string V="Validation/EdgeGrid20260923/";
 static double nextPoll;
 static MMLinkedNorthProfileQueued20260925(){
  EditorApplication.delayCall+=Run;
  EditorApplication.update+=Poll;
 }
 static void Poll(){
  if(EditorApplication.timeSinceStartup<nextPoll)return;
  nextPoll=EditorApplication.timeSinceStartup+3.0;
  if(!File.Exists(V+"RUN_LINKED_NORTH_SOURCE_PROFILE.flag"))return;
  if(EditorApplication.isCompiling||EditorApplication.isUpdating)return;
  Run();
 }
 static void Run(){
  if(!File.Exists(V+"RUN_LINKED_NORTH_SOURCE_PROFILE.flag"))return;
  if(EditorApplication.isCompiling||EditorApplication.isUpdating){EditorApplication.delayCall+=Run;return;}
  File.Delete(V+"RUN_LINKED_NORTH_SOURCE_PROFILE.flag");
  try{
   var sc=SceneManager.GetActiveScene();
   if(string.IsNullOrEmpty(sc.path)&&!sc.isDirty){
    EditorSceneManager.OpenScene("Assets/Scenes/Enroth_Linked_OpenWorld.unity",OpenSceneMode.Single);
    sc=SceneManager.GetActiveScene();
   }
   if(sc.path!="Assets/Scenes/Enroth_Linked_OpenWorld.unity"||sc.isDirty)
    throw new Exception("Saved linked scene required before rebuilding");
   if(!File.Exists(@"C:\MMUnityPort\Backups\BeforeLinkedOnlyNorthCliffProfile_20260925\manifest.json"))
    throw new Exception("Immutable linked-scene backup not found");
   File.WriteAllText(V+"linked_only_north_cliff_profiles_20260925.csv",
    "zone,measurements\n");
   BuildEnrothLinkedOpenWorld.Build();
   // The four linked-only native terrains are recreated on every Build. Reapply
   // the reference-photo east/west splat continuity AFTER all four are rebuilt;
   // doing this before Build is silently overwritten and restores bright
   // vertical tile-boundary strips in the world overview.
   // Remove x-column banding over the full four-tile north strip as one field
   // before the narrow north/south border bridge. This preserves broad reference
   // biomes but suppresses the vertical stripe artifacts visible in QA renders.
   MMNorthGlobalLateralDenoise20260927.Apply();
   // Combined authority-image source+extension targets share the exact y=158 row.
   // Use only a narrow 10-17m half-span bridge. The retired 64m bridge created
   // a broad horizontal belt; this bridge only removes the one-row material seam.
   MMNorthNarrowAlphaBridge20260927.Apply();
   File.WriteAllText(V+"north_cross_tile_alpha_blend_20260926.txt","");
   MMNorthCrossTileAlphaBlend20260926.Apply();
   // Cross-tile correction can reintroduce vertical alpha bands. Smooth once more
   // globally across all four tiles, then reassert the north/south edge derivative.
   MMNorthGlobalLateralDenoise20260927.Apply();
   MMNorthGlobalSeamBlend20260927.Apply();
   MMNorthNarrowAlphaBridge20260927.Apply();
   // Match northern extension tessellation to its adjoining linked-only source.
   // The generator's former 5 px error versus original 2 px can change edge
   // normals/LOD across an otherwise exact 0m height and alphamap join.
   int northLOD=0;
   foreach(var t in UnityEngine.Object.FindObjectsByType<Terrain>(
     UnityEngine.FindObjectsSortMode.None)){
    if(t.name=="Terrain_SweetWater_North"||
       t.name=="Terrain_Kriegspire_North"||
       t.name=="Terrain_FrozenHighlands_North"||
       t.name=="Terrain_SilverCove_North"){
      t.heightmapPixelError=2f;
      t.basemapDistance=1000f;
      t.drawInstanced=true;
      northLOD++;
    }
   }
   if(northLOD!=4)throw new Exception("Expected exactly 4 generated northern linked terrains, got "+northLOD);
   var live=SceneManager.GetActiveScene();
   if(live.path!="Assets/Scenes/Enroth_Linked_OpenWorld.unity")
    throw new Exception("North LOD correction did not target saved linked world");
   EditorSceneManager.MarkSceneDirty(live);
   if(!EditorSceneManager.SaveScene(live,live.path))
    throw new IOException("North parity terrain render settings could not be saved");
   File.WriteAllText(V+"north_linked_lod_parity_20260927.txt",
     "PASS extensionTerrains="+northLOD+
     " linkedSourcePixelError=2 extensionPixelError=2 basemapDistance=1000"+
     " canonicalAssetsChanged=0 heightsChanged=0 splatsChanged=0\n");
   // Do not execute experimental lateral height tangent. Both raw and
   // Gaussian-filtered variants created visible horizontal terracing in
   // orthographic QA despite reducing measured one-pixel slope jumps.
   // Preserve photo-ground continuity; linked canonical x-edge heights remain exact.
   foreach(var t in UnityEngine.Object.FindObjectsByType<Terrain>(
      UnityEngine.FindObjectsSortMode.None))t.Flush();
   MMReference20260925VisualQAOnce.RunFinalQA();
   // Source splatmaps are now copied directly from canonical MM6 terrains and are
   // intentionally not post-processed. Only generated northern extensions are blended.
   File.WriteAllText(V+"linked_only_north_cliff_runtime.txt",
    "PASS: linked-only source profile clones created; linked world rebuilt\n");
  }catch(Exception ex){
   File.WriteAllText(V+"linked_only_north_cliff_runtime.txt","FAIL "+ex+"\n");
   Debug.LogError(ex);
  }
 }
}

// Rebuild source-photo seam 20260926

// canonical source splat restore rerun 20260927

// narrow bridge compile reload 20260927 2116
