using UnityEngine;
using UnityEditor;
using System;
using System.IO;
[InitializeOnLoad]
internal static class MMDragonPondGuardRestore20260925 {
 const string V="Validation/EdgeGrid20260923/";
 const string Backup=@"C:\MMUnityPort\Backups\BeforeDragonSouthAlphaCorrection_20260925\DragonIsleSouthTerrain.asset";
 const string Temp="Assets/TempDragonSouthPondGuard_20260925.asset";
 static MMDragonPondGuardRestore20260925(){EditorApplication.delayCall+=Run;}
 static float S(float a,float b,float v){float t=Mathf.Clamp01((v-a)/(b-a));return t*t*(3f-2f*t);}
 static void Run(){
  if(!File.Exists(V+"RUN_DRAGON_POND_GUARD_RESTORE.flag"))return;
  File.Delete(V+"RUN_DRAGON_POND_GUARD_RESTORE.flag");
  bool imported=false;
  try{
   if(!File.Exists(Backup)||File.Exists(Path.GetFullPath(Temp)))
    throw new Exception("Verified pond backup missing or scratch already exists");
   File.Copy(Backup,Path.GetFullPath(Temp));imported=true;
   AssetDatabase.ImportAsset(Temp,ImportAssetOptions.ForceSynchronousImport);
   var original=AssetDatabase.LoadAssetAtPath<TerrainData>(Temp);
   var current=AssetDatabase.LoadAssetAtPath<TerrainData>("Assets/World/DragonIsle/Generated/DragonIsleSouthTerrain.asset");
   var sweet=AssetDatabase.LoadAssetAtPath<TerrainData>("Assets/World/SweetWater/Generated/SweetWaterTerrain.asset");
   if(!original||!current||!sweet)throw new Exception("Pond/current/source terrain missing");
   var old=original.GetHeights(0,0,513,513);var h=current.GetHeights(0,0,513,513);
   var sh=sweet.GetHeights(0,0,513,513);
   int before=0,after=0,oldWet=0,changed=0;float maxDelta=0;
   for(int z=185;z<=230;z++)for(int x=458;x<=495;x++){
    if(-24+old[z,x]*320f<=.1f)oldWet++;
    if(-24+h[z,x]*320f<=.1f)before++;
   }
   for(int z=160;z<=260;z++)for(int x=448;x<=509;x++){
    float strength=S(448f,459f,x)*(1f-S(497f,509f,x))*
                   S(160f,178f,z)*(1f-S(246f,260f,z));
    float prior=h[z,x];
    float target=Mathf.Lerp(prior,old[z,x],strength);
    maxDelta=Mathf.Max(maxDelta,Mathf.Abs(target-prior)*320f);
    if(Mathf.Abs(target-prior)*320f>.002f)changed++;
    h[z,x]=target;
   }
   for(int z=185;z<=230;z++)for(int x=458;x<=495;x++)
    if(-24+h[z,x]*320f<=.1f)after++;
   float maxEdge=0f;
   for(int z=0;z<513;z++)
    maxEdge=Mathf.Max(maxEdge,Mathf.Abs(h[z,512]-sh[z,0])*320f);
   if(after!=oldWet||maxEdge>.02f||maxDelta>9f)
    throw new Exception("Pond preservation failed old="+oldWet+" after="+after+" edge="+maxEdge+" correction="+maxDelta);
   current.SetHeights(0,0,h);EditorUtility.SetDirty(current);AssetDatabase.SaveAssets();
   string report="PASS source_pond_wet="+oldWet+" before_wet="+before+
    " restored_wet="+after+" vertices_changed="+changed+
    " max_height_correction_m="+maxDelta.ToString("F3")+
    " source_seam_max_m="+maxEdge.ToString("F6");
   File.WriteAllText(V+"pond_guard_restored_20260925.txt",report+"\n");
   Debug.Log(report);
  }catch(Exception ex){
   File.WriteAllText(V+"pond_guard_restored_20260925.txt","FAIL "+ex+"\n");
   Debug.LogError(ex);
  }finally{
   if(imported)AssetDatabase.DeleteAsset(Temp);
  }
 }
}
