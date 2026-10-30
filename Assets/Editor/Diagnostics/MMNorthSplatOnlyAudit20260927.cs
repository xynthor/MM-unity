using UnityEngine;
using UnityEditor;
using System;
using System.IO;
using System.Collections.Generic;
[InitializeOnLoad]
internal static class MMNorthSplatOnlyAudit20260927 {
 const string V="Validation/EdgeGrid20260923/";
 static readonly string[] N={"SweetWater","Kriegspire","FrozenHighlands","SilverCove"};
 static MMNorthSplatOnlyAudit20260927(){EditorApplication.delayCall+=Run;}
 static void Run(){
  string flag=V+"RUN_NORTH_SPLAT_ONLY_20260927.flag";
  if(!File.Exists(flag))return;File.Delete(flag);
  try{
   var im=new Texture2D(2048,1024,TextureFormat.RGB24,false,true);
   var report=new List<string>();
   foreach(var row in new[]{0,1})for(int col=0;col<4;col++){
    var n=N[col];
    var path=row==0?
      "Assets/World/WorldExtensions/Generated/LinkedSourceTransitions/"+n+"_LinkedNorthProfile.asset":
      "Assets/World/WorldExtensions/Generated/"+n+"_NorthTerrain.asset";
    var data=AssetDatabase.LoadAssetAtPath<TerrainData>(path);
    if(!data)throw new Exception("Missing "+path);
    var a=data.GetAlphamaps(0,0,512,512);
    double[] mean=new double[7];var c=new Color32[512*512];
    int snow=Array.FindIndex(data.terrainLayers,l=>l&&l.name=="Realistic_Snow");
    int rock=Array.FindIndex(data.terrainLayers,l=>l&&l.name=="Realistic_Volcanic");
    int green=Array.FindIndex(data.terrainLayers,l=>l&&l.name=="Realistic_Green");
    int light=Array.FindIndex(data.terrainLayers,l=>l&&l.name=="Realistic_LightGreen");
    int soil=Array.FindIndex(data.terrainLayers,l=>l&&l.name=="Realistic_Arid");
    if(snow<0||rock<0||green<0||light<0||soil<0)throw new Exception("Material missing "+n);
    double edgeSnow=0,edgeRock=0,edgeForest=0;
    for(int z=0;z<512;z++)for(int x=0;x<512;x++){
     var s=a[z,x,snow];var v=a[z,x,rock];var g=a[z,x,green]+a[z,x,light];var b=a[z,x,soil];
     var rgb=new Color(.97f*s+.28f*v+.19f*g+.65f*b,
       .98f*s+.24f*v+.41f*g+.49f*b,.99f*s+.24f*v+.22f*g+.32f*b);
     c[z*512+x]=rgb;
     if(z==(row==0?511:0)){edgeSnow+=s;edgeRock+=v;edgeForest+=g;}
    }
    im.SetPixels32(col*512,row*512,512,512,c);
    report.Add(n+" "+(row==0?"SOURCE":"NORTH")+" seamSnow="+(edgeSnow/512).ToString("F3")+
      " seamRock="+(edgeRock/512).ToString("F3")+" seamForest="+(edgeForest/512).ToString("F3"));
   }
   im.Apply();
   File.WriteAllBytes("Preview/Enroth_North_SplatOnly_20260927.png",im.EncodeToPNG());
   UnityEngine.Object.DestroyImmediate(im);
   File.WriteAllLines(V+"north_splat_only_20260927.txt",report);
  }catch(Exception e){File.WriteAllText(V+"north_splat_only_20260927.txt","FAIL "+e);}
 }
}

// post vertical stripe repair verification
