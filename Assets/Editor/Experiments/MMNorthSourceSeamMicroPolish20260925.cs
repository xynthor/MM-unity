using UnityEngine;
using UnityEditor;
using UnityEngine.SceneManagement;
using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
// Constrained micro-polish: preserve ALL original source heights, extension
// boundary elevation and first slope, coast authority, and inland mountains.
public static class MMNorthSourceSeamMicroPolish20260925 {
 const string V="Validation/EdgeGrid20260923/";
 const string G="Assets/World/WorldExtensions/Generated/";
 const string Backup=@"C:\MMUnityPort\Backups\BeforeNorthSourceSeamMicroPolish_20260925\manifest.json";
 static readonly string[] Names={"SweetWater","Kriegspire","FrozenHighlands","SilverCove"};
 static float S(float a,float b,float v){
  float u=Mathf.Clamp01((v-a)/(b-a));return u*u*(3f-2f*u);
 }
 static byte[] LandMask(string name){
  var texture=new Texture2D(2,2,TextureFormat.RGBA32,false,true);
  string path=V+"ReferenceMasks/"+name+"_North.png";
  if(!texture.LoadImage(File.ReadAllBytes(path))||texture.width!=513||texture.height!=513)
   throw new Exception("Reference mask invalid: "+path);
  var pix=texture.GetPixels32();UnityEngine.Object.DestroyImmediate(texture);
  return pix.Select(c=>c.r).ToArray();
 }
 [MenuItem("MMUnity/Reference 2026/Micro-polish North Source Seam Pits")]
 public static void Apply(){
  if(!File.Exists(Backup))throw new Exception("Verified current-terrain backup missing");
  if(SceneManager.GetActiveScene().path!="Assets/Scenes/World/Enroth.unity" ||
    SceneManager.GetActiveScene().isDirty)throw new Exception("Saved linked scene required");
  var terrains=new List<TerrainData>();var candidate=new List<float[,]>();
  var lines=new List<string>();const float H=320f;
  foreach(string n in Names){
   var td=AssetDatabase.LoadAssetAtPath<TerrainData>(G+n+"_NorthTerrain.asset");
   if(!td||td.heightmapResolution!=513)throw new Exception("Missing terrain "+n);
   var src=AssetDatabase.LoadAssetAtPath<TerrainData>(
     "Assets/World/"+n+"/Generated/"+n+"Terrain.asset");
   if(!src)throw new Exception("Protected source not available "+n);
   var h=td.GetHeights(0,0,513,513);var orig=src.GetHeights(0,511,513,2);
   var land=LandMask(n);int touched=0,columns=0,excluded=0;
   float maxBefore=0,maxAfter=0,maxMove=0,border=0,slope=0;
   for(int x=8;x<=504;x++){
    float lo=h[1,x],hi=h[64,x];
    if((hi-lo)*H<6f||(-24f+hi*H)<3f||land[16*513+x]<146){
     excluded++;continue;
    }
    float min=999f;
    for(int q=2;q<=24;q++)min=Mathf.Min(min,h[q,x]);
    float pit=Mathf.Max(0f,(Mathf.Min(lo,hi)-min)*H);
    maxBefore=Mathf.Max(maxBefore,pit);
    if(pit<1.1f)continue;
    float xFade=S(8f,32f,x)*S(8f,32f,512f-x);
    float floor=lo-.75f/H;
    int local=0;
    for(int q=2;q<=32;q++){
     float zFade=1f-S(20f,32f,q);
     float previous=h[q,x];
     h[q,x]+=Mathf.Max(0f,floor-previous)*xFade*zFade;
     float delta=Mathf.Abs(h[q,x]-previous)*H;
     if(delta>.0005f){touched++;local++;}
     maxMove=Mathf.Max(maxMove,delta);
    }
    if(local>0)columns++;
   }
   for(int x=0;x<513;x++){
    border=Mathf.Max(border,Mathf.Abs(h[0,x]-orig[1,x])*H);
    slope=Mathf.Max(slope,Mathf.Abs((h[1,x]-h[0,x])-
      (orig[1,x]-orig[0,x]))*H);
    if(x<8||x>504)continue;
    float lo=h[1,x],hi=h[64,x];
    if((hi-lo)*H<6f||(-24f+hi*H)<3f||land[16*513+x]<146)continue;
    float min=999f;
    for(int q=2;q<=24;q++)min=Mathf.Min(min,h[q,x]);
    maxAfter=Mathf.Max(maxAfter,Mathf.Max(0f,(Mathf.Min(lo,hi)-min)*H));
   }
   if(border>.1f||slope>.1f||maxMove>3f)
    throw new Exception(n+" rejected source edge="+border+" slope="+slope+" move="+maxMove);
   terrains.Add(td);candidate.Add(h);
   lines.Add(n+"_North columns="+columns+" vertices="+touched+" excluded="+excluded+
    " inland_extra_pit_before_m="+maxBefore.ToString("F3")+
    " inland_extra_pit_after_m="+maxAfter.ToString("F3")+
    " largest_adjustment_m="+maxMove.ToString("F3")+
    " original_edge_error_m="+border.ToString("F6")+
    " original_slope_error="+slope.ToString("F6"));
  }
  for(int i=0;i<terrains.Count;i++){
   terrains[i].SetHeights(0,0,candidate[i]);EditorUtility.SetDirty(terrains[i]);
  }
  AssetDatabase.SaveAssets();
  Directory.CreateDirectory(V);
  File.WriteAllLines(V+"north_source_seam_micro_polish_20260925.csv",lines);
  Debug.Log("NORTH_SOURCE_SEAM_MICROPOLISH "+string.Join(" | ",lines));
 }
}
