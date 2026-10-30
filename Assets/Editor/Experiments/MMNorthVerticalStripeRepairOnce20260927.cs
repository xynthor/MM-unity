using UnityEngine;
using UnityEditor;
using UnityEngine.SceneManagement;
using System;
using System.IO;
using System.Text;
[InitializeOnLoad]
internal static class MMNorthVerticalStripeRepairOnce20260927 {
 const string V="Validation/EdgeGrid20260923/";
 static MMNorthVerticalStripeRepairOnce20260927(){EditorApplication.delayCall+=Run;}
 static void Run(){
  string flag=V+"RUN_NORTH_VERTICAL_STRIPE_REPAIR.flag";
  if(!File.Exists(flag))return;File.Delete(flag);
  var sb=new StringBuilder();
  try{
   if(!File.Exists(@"C:\MMUnityPort\Backups\BeforeNorthVerticalStripeRemoval_20260927\manifest.json"))
    throw new Exception("Restore point missing");
   var sc=SceneManager.GetActiveScene();
   if(sc.path!="Assets/Scenes/World/Enroth.unity"||sc.isDirty)
    throw new Exception("Saved linked scene required");
   var a=AssetDatabase.LoadAssetAtPath<TerrainData>(
    "Assets/World/WorldExtensions/Generated/LinkedSourceTransitions/SweetWater_LinkedNorthProfile.asset");
   var b=AssetDatabase.LoadAssetAtPath<TerrainData>(
    "Assets/World/WorldExtensions/Generated/LinkedSourceTransitions/Kriegspire_LinkedNorthProfile.asset");
   if(!a||!b)throw new Exception("North terrain pair missing");
   var z=293;int sn=Array.FindIndex(a.terrainLayers,l=>l&&l.name=="Realistic_Snow");
   if(sn<0)throw new Exception("Snow layer missing");
   float old=a.GetAlphamaps(511,z,1,1)[0,0,sn];
   float anchor=a.GetAlphamaps(466,z,1,1)[0,0,sn];
   sb.AppendLine("BEFORE source SweetWater z293 snow x466="+anchor+" x511="+old);
   File.WriteAllText(V+"north_cross_tile_alpha_blend_20260926.txt","");
   MMNorthCrossTileAlphaBlend20260926.Apply();
   AssetDatabase.SaveAssets();
   float next=a.GetAlphamaps(511,z,1,1)[0,0,sn];
   float other=b.GetAlphamaps(0,z,1,1)[0,0,sn];
   sb.AppendLine("AFTER source SweetWater z293 snow x511="+next+" Kriegspire x0="+other);
   sb.AppendLine("WROTE correct source/extension alpha seam; delta="+(next-old));
   if(Mathf.Abs(next-other)>.0001f||next>.45f&&anchor<.1f)
    throw new Exception("Post-repair snow seam remains wrong");
   MMReference20260925VisualQAOnce.RunFinalQA();
   sb.AppendLine("QA_RENDERED");
  }catch(Exception ex){sb.AppendLine("FAIL "+ex);Debug.LogError(ex);}
  File.WriteAllText(V+"north_vertical_stripe_repair_20260927.txt",sb.ToString());
 }
}
