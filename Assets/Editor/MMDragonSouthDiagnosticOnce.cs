using UnityEngine;
using UnityEditor;
using UnityEngine.SceneManagement;
using System;
using System.IO;
using System.Text;
[InitializeOnLoad]
internal static class MMDragonSouthDiagnosticOnce {
 const string Root=@"C:\MMUnityPort\";
 const string Flag=Root+@"Validation\EdgeGrid20260923\RUN_DRAGON_SOUTH_DIAGNOSTIC.flag";
 const string Output=Root+@"Validation\EdgeGrid20260923\dragon_south_pond_and_cliff_live_qa.txt";
 const string Backup=Root+@"Backups\BeforeDragonSouthAlphaCorrection_20260925\DragonIsleSouthTerrain.asset";
 const string Temp="Assets/TempDragonSouthVisualQA_20260925.asset";
 const string D="Assets/World/DragonIsle/Generated/DragonIsleSouthTerrain.asset";
 const string S="Assets/World/SweetWater/Generated/SweetWaterTerrain.asset";
 static MMDragonSouthDiagnosticOnce(){EditorApplication.delayCall+=Run;}
 static void Run(){
  if(!File.Exists(Flag))return;
  File.Delete(Flag);
  var sb=new StringBuilder(); bool created=false;
  try {
   var d=AssetDatabase.LoadAssetAtPath<TerrainData>(D);
   var s=AssetDatabase.LoadAssetAtPath<TerrainData>(S);
   if(!d||!s)throw new Exception("Missing source/current terrain");
   if(!File.Exists(Backup))throw new Exception("Missing verified prejoin backup");
   if(File.Exists(Path.GetFullPath(Temp)))throw new Exception("Temp asset already exists");
   File.Copy(Backup,Path.GetFullPath(Temp));created=true;
   AssetDatabase.ImportAsset(Temp,ImportAssetOptions.ForceSynchronousImport);
   var b=AssetDatabase.LoadAssetAtPath<TerrainData>(Temp);
   if(!b)throw new Exception("Backup terrain import failed");
   var dh=d.GetHeights(0,0,513,513); var sh=s.GetHeights(0,0,513,513);
   var bh=b.GetHeights(0,0,513,513);
   sb.AppendLine("ACTIVE_SCENE="+SceneManager.GetActiveScene().path);
   sb.AppendLine("SCENE_DIRTY="+SceneManager.GetActiveScene().isDirty);
   float seamMax=0,pondMax=0;int changed=0;
   for(int z=0;z<513;z++)seamMax=Mathf.Max(seamMax,Mathf.Abs(dh[z,512]-sh[z,0])*320f);
   sb.AppendLine("SWEET_WATER_SEAM_MAX_M="+seamMax.ToString("F6"));
   foreach(var box in new[]{new[]{450,510,160,260},new[]{458,495,185,230},new[]{465,495,195,230}}){
    int originalWet=0,currentWet=0,lostWet=0,gainedWet=0;float maxDiff=0;
    for(int z=box[2];z<=box[3];z++)for(int x=box[0];x<=box[1];x++){
     var by=-24f+bh[z,x]*320f;var cy=-24f+dh[z,x]*320f;
     bool was=by<=.1f,now=cy<=.1f;
     if(was)originalWet++;if(now)currentWet++;
     if(was&&!now)lostWet++;if(!was&&now)gainedWet++;
     maxDiff=Mathf.Max(maxDiff,Mathf.Abs(by-cy));
    }
    sb.AppendLine("POND x="+box[0]+".."+box[1]+" z="+box[2]+".."+box[3]+
      " oldWet="+originalWet+" currentWet="+currentWet+" lostWet="+lostWet+
      " gainedWet="+gainedWet+" heightMaxDiffM="+maxDiff.ToString("F4"));
   }
   var mask=new Texture2D(2,2);
   mask.LoadImage(File.ReadAllBytes(Root+@"Validation\EdgeGrid20260923\ReferenceMasks\DragonSouth_SweetWaterWest_MainlandOnly.png"));
   if(mask.width!=513||mask.height!=513)throw new Exception("Unexpected mask size");
   var pix=mask.GetPixels32(); UnityEngine.Object.DestroyImmediate(mask);
   float ymin=999,ymax=-999,ysum=0,dropMax=0,dropSum=0;int shore=0,cliff3=0,cliff6=0;
   for(int z=8;z<505;z++)for(int x=8;x<505;x++){
    if(pix[z*513+x].r<128)continue;
    if(pix[z*513+x-1].r>=128&&pix[z*513+x+1].r>=128&&
       pix[(z-1)*513+x].r>=128&&pix[(z+1)*513+x].r>=128)continue;
    float y=-24f+dh[z,x]*320f;
    float inland=-24f+dh[z,Mathf.Min(512,x+8)]*320f;
    float delta=Mathf.Abs(inland-y);dropMax=Mathf.Max(dropMax,delta);dropSum+=delta;
    if(delta>3)cliff3++;if(delta>6)cliff6++;
    ymin=Mathf.Min(ymin,y);ymax=Mathf.Max(ymax,y);ysum+=y;shore++;
   }
   sb.AppendLine("SHORE_MASK_SAMPLES="+shore+" heightMinM="+ymin.ToString("F2")+
     " heightMeanM="+(ysum/Mathf.Max(1,shore)).ToString("F2")+
     " heightMaxM="+ymax.ToString("F2"));
   sb.AppendLine("SHORE_INLAND_EAST_8M_ABS_DIFF_MAX="+dropMax.ToString("F2")+
     " MEAN="+(dropSum/Mathf.Max(1,shore)).ToString("F2")+
     " GT3M="+cliff3+" GT6M="+cliff6);
   sb.AppendLine("SOURCE_TERRAIN_ASSET_LEFT_UNCHANGED=true");
  }catch(Exception ex){sb.AppendLine("DIAGNOSTIC_ERROR="+ex);}
  finally{
   if(created){AssetDatabase.DeleteAsset(Temp);}
   File.WriteAllText(Output,sb.ToString());
   Debug.Log("DRAGON_SOUTH_DIAGNOSTIC_WRITTEN "+Output);
  }
 }
}
