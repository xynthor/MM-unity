using UnityEngine;
using UnityEditor;
using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
[InitializeOnLoad]
internal static class MMNorthLateralHeightSeamAudit20260926 {
 const string V="Validation/EdgeGrid20260923/";
 static readonly string[] N={"SweetWater","Kriegspire","FrozenHighlands","SilverCove"};
 static MMNorthLateralHeightSeamAudit20260926(){EditorApplication.delayCall+=Run;}
 static void Run(){
  if(!File.Exists(V+"RUN_NORTH_LATERAL_HEIGHT_AUDIT.flag"))return;
  if(EditorApplication.isCompiling||EditorApplication.isUpdating){EditorApplication.delayCall+=Run;return;}
  File.Delete(V+"RUN_NORTH_LATERAL_HEIGHT_AUDIT.flag");
  var lines=new List<string>();
  try{
   for(int row=0;row<2;row++){
    bool north=row==1;
    var maps=N.Select(n=>AssetDatabase.LoadAssetAtPath<TerrainData>(
      north?"Assets/World/WorldExtensions/Generated/"+n+"_NorthTerrain.asset":
      "Assets/World/WorldExtensions/Generated/LinkedSourceTransitions/"+
      n+"_LinkedNorthProfile.asset")).ToArray();
    foreach(var map in maps)if(!map)throw new Exception("Missing terrain in row "+row);
    for(int i=0;i<3;i++){
     var a=maps[i].GetHeights(511,0,2,513);
     var b=maps[i+1].GetHeights(0,0,2,513);
     var dif=new List<float>();var hot=new List<string>();float maxH=0,maxSl=0;
     for(int z=4;z<=508;z++){
      float ya=-24+320*a[z,1],yb=-24+320*b[z,0];
      if(Mathf.Min(ya,yb)<.5f)continue;
      float h=Mathf.Abs(ya-yb);
      float left=(a[z,1]-a[z,0])*320f,right=(b[z,1]-b[z,0])*320f;
      float sl=Mathf.Abs(left-right);
      dif.Add(sl);maxH=Mathf.Max(maxH,h);maxSl=Mathf.Max(maxSl,sl);
      if(sl>.5f)hot.Add("z="+z+" leftSlope="+left.ToString("F3")+
       " rightSlope="+right.ToString("F3")+" jump="+sl.ToString("F3"));
     }
     dif.Sort();int cnt=dif.Count;
     lines.Add((north?"NORTH":"LINKED")+" "+N[i]+"-"+N[i+1]+
       " dry="+cnt+" maxHeightGapM="+maxH.ToString("F4")+
       " maxSlopeJump="+maxSl.ToString("F4")+
       " p95SlopeJump="+(cnt>0?dif[(int)((cnt-1)*.95f)]:0).ToString("F4")+
       " over0.2="+dif.Count(x=>x>.2f));
     lines.AddRange(hot.Take(8).Select(x=>"  "+x));
    }
   }
  }catch(Exception ex){lines.Add("ERROR "+ex);}
  File.WriteAllLines(V+"north_lateral_height_audit_20260926.txt",lines);
 }
}
