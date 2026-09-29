using UnityEngine;
using UnityEditor;
using System;
using System.IO;
using System.Text;
[InitializeOnLoad]
internal static class MMParadiseCapeHeightCandidate20260927 {
 const string V="Validation/EdgeGrid20260923/";
 static MMParadiseCapeHeightCandidate20260927(){EditorApplication.delayCall+=Run;}
 static void Run(){
  string flag=V+"RUN_PARADISE_CAPE_HEIGHT_CANDIDATE.flag";
  if(!File.Exists(flag))return;File.Delete(flag);
  var log=new StringBuilder();
  var d=AssetDatabase.LoadAssetAtPath<TerrainData>(
   "Assets/World/WorldExtensions/Generated/ParadiseValley_WestTerrain.asset");
  Texture2D t=null;
  try{
   if(!d)throw new Exception("Missing Paradise West generated terrain");
   t=new Texture2D(2,2,TextureFormat.RGBA32,false,true);
   if(!t.LoadImage(File.ReadAllBytes(V+"ReferenceMasks/ParadiseValley_West.png"))||
      t.width!=513||t.height!=513)throw new Exception("Reference mask corrupt");
   var mask=t.GetPixels32();var h=d.GetHeights(0,0,513,513);
   int feasible=0,borderConflict=0;float biggestLift=0;
   var paint=new Color32[513*513];
   for(int z=0;z<513;z++)for(int x=0;x<513;x++){
    int i=z*513+x;float y=-24f+320f*h[z,x];
    byte m=mask[i].r;
    bool falseWet=m>=145&&y<-.2f;
    bool inner=x>38&&x<469&&z>38&&z<474;
    bool strong=m>=166;
    bool good=falseWet&&inner&&strong;
    if(good){paint[i]=new Color32(246,57,38,255);
     feasible++;biggestLift=Mathf.Max(biggestLift,.6f-y);
    }else if(falseWet){paint[i]=new Color32(56,90,221,255);borderConflict++;}
    else if(m>=145&&y>.6f)paint[i]=new Color32(76,133,65,255);
    else if(m<=113&&y<-.2f)paint[i]=new Color32(21,43,65,255);
    else paint[i]=new Color32(92,92,89,255);
   }
   log.AppendLine("FEASIBLE_INTERIOR_STRONG_REFERENCE="+feasible+
    " REST_NEAR_PROTECTED_BORDERS_OR_LOW_CONFIDENCE="+borderConflict+
    " deepestInteriorLiftM="+biggestLift.ToString("F2"));
   foreach(int z in new[]{0,16,32,48,64,80,96,112,128,144,160,192})
   foreach(int x in new[]{352,384,416,448,464,480,496,512}){
    int i=z*513+x;
    log.AppendLine("z="+z+" x="+x+" refR="+mask[i].r+
      " terrainY="+(-24f+320f*h[z,x]).ToString("F2"));
   }
   var image=new Texture2D(513,513,TextureFormat.RGBA32,false,true);
   image.SetPixels32(paint);image.Apply();
   File.WriteAllBytes("Preview/ParadiseWest_ConstrainedCoastCandidate_20260927.png",
    image.EncodeToPNG());
   UnityEngine.Object.DestroyImmediate(image);
  }catch(Exception e){log.AppendLine("FAIL "+e);}
  finally{
   if(t)UnityEngine.Object.DestroyImmediate(t);
   File.WriteAllText(V+"paradise_cape_height_candidate_20260927.txt",log.ToString());
  }
 }
}
