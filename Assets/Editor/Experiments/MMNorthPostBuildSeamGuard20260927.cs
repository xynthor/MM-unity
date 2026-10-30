using UnityEngine;
using UnityEditor;
using UnityEngine.SceneManagement;
using System;
using System.IO;
using System.Text;
// Re-import after a large linked rebuild can restore 1px-wide native snow
// strips on the boundaries despite an earlier successful cross-tile pass.
// Verify the STORED TerrainData after all pending AssetDatabase updates.
[InitializeOnLoad]
internal static class MMNorthPostBuildSeamGuard20260927 {
 const string V="Validation/EdgeGrid20260923/";
 const string Flag=V+"RUN_NORTH_POSTBUILD_SEAM_GUARD.flag";
 const string Root="Assets/World/WorldExtensions/Generated/LinkedSourceTransitions/";
 static readonly string[] Names={"SweetWater","Kriegspire","FrozenHighlands"};
 static double due;static int stable,attempt;
 static MMNorthPostBuildSeamGuard20260927(){EditorApplication.update+=Tick;}
 static string Inspect(out bool bad){
  bad=false;var sb=new StringBuilder();
  foreach(var n in Names){
   var d=AssetDatabase.LoadAssetAtPath<TerrainData>(Root+n+"_LinkedNorthProfile.asset");
   if(!d)throw new Exception("Missing linked north source "+n);
   int sn=Array.FindIndex(d.terrainLayers,t=>t&&t.name=="Realistic_Snow");
   if(sn<0)throw new Exception(n+" lacks native snow layer");
   var a=d.GetAlphamaps(460,293,52,1);
   float anchor=a[0,6,sn],boundary=a[0,51,sn];
   bool stripe=anchor<.25f&&boundary>anchor+.35f;
   if(stripe)bad=true;
   sb.AppendLine(n+" z293 snowAnchor="+anchor.ToString("F4")+
     " snowAtSharedEdge="+boundary.ToString("F4")+" stripe="+stripe);
  }
  return sb.ToString();
 }
 static void Tick(){
  if(!File.Exists(Flag)){stable=0;attempt=0;return;}
  if(EditorApplication.timeSinceStartup<due)return;
  due=EditorApplication.timeSinceStartup+4.0;
  if(EditorApplication.isCompiling||EditorApplication.isUpdating)return;
  var sc=SceneManager.GetActiveScene();
  if(sc.path!="Assets/Scenes/World/Enroth.unity"||sc.isDirty)return;
  try{
   bool bad;string audit=Inspect(out bad);
   if(bad){
    if(++attempt>2)throw new Exception("North snow-stripe guard failed after 2 repairs\n"+audit);
    stable=0;
    File.WriteAllText(V+"north_cross_tile_alpha_blend_20260926.txt","");
    MMNorthCrossTileAlphaBlend20260926.Apply();
    AssetDatabase.SaveAssets();
    AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
    File.WriteAllText(V+"north_postbuild_seam_guard_20260927.txt",
      "RECOVERING attempt="+attempt+"\n"+audit);
    return;
   }
   if(++stable<2)return; // verify stored assets twice after import has settled
   MMReference20260925VisualQAOnce.RunFinalQA();
   File.WriteAllText(V+"north_postbuild_seam_guard_20260927.txt",
    "PASS imported linked source terrain snow seams verified across all three east/west joins"+
    " repairAttempts="+attempt+" finalQA=PASS\n"+audit);
   File.Delete(Flag);
   stable=0;attempt=0;
  }catch(Exception e){
   File.WriteAllText(V+"north_postbuild_seam_guard_20260927.txt","FAIL "+e+"\n");
   File.Delete(Flag);Debug.LogError(e);stable=0;attempt=0;
  }
 }
}
