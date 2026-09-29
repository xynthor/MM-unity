using UnityEngine;
using UnityEditor;
using System;
using System.IO;
using System.Text;
[InitializeOnLoad]
internal static class MMParadiseWestMaskMismatchOnce20260927 {
 const string V="Validation/EdgeGrid20260923/";
 static MMParadiseWestMaskMismatchOnce20260927(){EditorApplication.delayCall+=Run;}
 static void Run(){
  var flag=V+"RUN_PARADISE_WEST_MASK_DIAG.flag";
  if(!File.Exists(flag))return;File.Delete(flag);
  var sb=new StringBuilder();Texture2D refMask=null;
  try{
   var terrain=AssetDatabase.LoadAssetAtPath<TerrainData>(
    "Assets/World/WorldExtensions/Generated/ParadiseValley_WestTerrain.asset");
   if(!terrain)throw new Exception("Missing generated Paradise West TerrainData");
   refMask=new Texture2D(2,2,TextureFormat.RGBA32,false,true);
   if(!refMask.LoadImage(File.ReadAllBytes(V+"ReferenceMasks/ParadiseValley_West.png"))||
      refMask.width!=513||refMask.height!=513)throw new Exception("Mask invalid");
   var px=refMask.GetPixels32();var h=terrain.GetHeights(0,0,513,513);
   var colors=new Color32[513*513];
   int[,] falseDry=new int[2,2],falseWet=new int[2,2],landGood=new int[2,2],waterGood=new int[2,2];
   int minX=513,minZ=513,maxX=-1,maxZ=-1;
   for(int z=0;z<513;z++)for(int x=0;x<513;x++){
    var index=z*513+x;byte v=px[index].r;
    float y=-24f+h[z,x]*320f;
    int i=x<256?0:1,j=z<256?0:1;
    bool maskLand=v>=145,maskWater=v<=113,actualLand=y>=.6f,actualWater=y<=-.2f;
    Color32 color=new Color32(98,98,98,255);
    if(maskWater&&actualLand){
     falseDry[i,j]++;color=new Color32(248,45,35,255);
     minX=Math.Min(x,minX);minZ=Math.Min(z,minZ);maxX=Math.Max(x,maxX);maxZ=Math.Max(z,maxZ);
    }else if(maskLand&&actualWater){
     falseWet[i,j]++;color=new Color32(35,80,250,255);
    }else if(maskLand&&actualLand){landGood[i,j]++;color=new Color32(62,130,64,255);}
    else if(maskWater&&actualWater){waterGood[i,j]++;color=new Color32(22,40,59,255);}
    colors[index]=color;
   }
   for(int j=0;j<2;j++)for(int i=0;i<2;i++)
    sb.AppendLine("quadrant x="+i+" z="+j+" falseDry="+falseDry[i,j]+
      " falseWet="+falseWet[i,j]+" landGood="+landGood[i,j]+" waterGood="+waterGood[i,j]);
   sb.AppendLine("falseDryBounds x="+minX+".."+maxX+" z="+minZ+".."+maxZ);
   int allDry=0,allWet=0;
   foreach(var v in falseDry)allDry+=v;
   foreach(var v in falseWet)allWet+=v;
   sb.AppendLine("TOTAL falseDry="+allDry+" falseWet="+allWet);
   var east=AssetDatabase.LoadAssetAtPath<TerrainData>(
     "Assets/World/ParadiseValley/Generated/ParadiseValleyTerrain.asset");
   if(east){
    var eh=east.GetHeights(0,0,1,513);
    foreach(int z in new[]{0,40,80,120,160,200,240,256,320,400,512})
     sb.AppendLine("EAST_JOIN z="+z+
       " westHeightM="+(-24f+320f*h[z,512]).ToString("F2")+
       " canonicalEastHeightM="+(-24f+320f*eh[z,0]).ToString("F2")+
       " maskLandR="+px[z*513+512].r);
   }
   var outTex=new Texture2D(513,513,TextureFormat.RGB24,false,true);
   outTex.SetPixels32(colors);outTex.Apply();
   File.WriteAllBytes("Preview/ParadiseWest_CoastlineMaskQA_20260927.png",outTex.EncodeToPNG());
   UnityEngine.Object.DestroyImmediate(outTex);
  }catch(Exception e){sb.AppendLine("FAIL "+e);}
  finally{
   if(refMask)UnityEngine.Object.DestroyImmediate(refMask);
   File.WriteAllText(V+"paradise_west_mask_mismatch_20260927.txt",sb.ToString());
  }
 }
}
