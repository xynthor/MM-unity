using UnityEngine;
using UnityEditor;
using System;
using System.IO;
[InitializeOnLoad]
internal static class MMNorthDryLandReconnectQueued20260925 {
 const string V="Validation/EdgeGrid20260923/";
 static MMNorthDryLandReconnectQueued20260925(){EditorApplication.delayCall+=Run;}
 static void Run(){
  if(!File.Exists(V+"RUN_NORTH_DRY_LAND_RECONNECT.flag"))return;
  if(EditorApplication.isUpdating||EditorApplication.isCompiling){
   EditorApplication.delayCall+=Run;return;
  }
  File.Delete(V+"RUN_NORTH_DRY_LAND_RECONNECT.flag");
  try{
   MMNorthDryLandReconnect20260925.Apply();
   BuildEnrothLinkedOpenWorld.Build();
   File.WriteAllText(V+"north_dry_land_reconnect_runtime.txt",
    "PASS generated northern lowland dry-land join and rebuilt linked world\n");
  }catch(Exception ex){
   File.WriteAllText(V+"north_dry_land_reconnect_runtime.txt","FAIL "+ex+"\n");
   Debug.LogError(ex);
  }
 }
}
