using UnityEngine;
using UnityEditor;
using System;
using System.IO;
// Smoothed north/south material bridge.  Uses horizontally filtered anchor rows
// so the blend cannot create one-column vertical bars.  Linked-world only.
public static class MMNorthSouthAlphaBridge20260926 {
 const string V="Validation/EdgeGrid20260923/";
 const string G="Assets/World/WorldExtensions/Generated/";
 const string Backup=@"C:\MMUnityPort\Backups\BeforeSmoothedNorthMaterialBridge_20260927\manifest.json";
 static readonly string[] N={"SweetWater","Kriegspire","FrozenHighlands","SilverCove"};
 const int Span=64;
 static float S(float t){t=Mathf.Clamp01(t);return t*t*(3f-2f*t);}
 static float PhotoBlend(float t,int x){
  // Continuously bend the dark volcanic/snow transition using the authority
  // photo's irregular scale, with identical first derivatives at the join.
  float noise=Mathf.PerlinNoise(x*.019f+23f,t*3.7f+11f)-.5f;
  return S(t+.32f*noise*4f*t*(1f-t));
 }
 static TerrainData Get(string n,bool north){
  string p=north?G+n+"_NorthTerrain.asset":
    G+"LinkedSourceTransitions/"+n+"_LinkedNorthProfile.asset";
  var d=AssetDatabase.LoadAssetAtPath<TerrainData>(p);
  if(!d||d.alphamapResolution!=512||d.alphamapLayers!=7)
   throw new Exception("Missing bridge terrain "+p);
  return d;
 }
 public static void Apply(){
  if(!File.Exists(Backup))throw new Exception("Smoothed bridge backup missing");
  File.WriteAllText(V+"north_south_alpha_bridge_20260926.txt","");
  foreach(var n in N){
   var s=Get(n,false);var e=Get(n,true);
   var sa=s.GetAlphamaps(0,0,512,512);var ea=e.GetAlphamaps(0,0,512,512);
   var sh=s.GetHeights(0,0,513,513);var eh=e.GetHeights(0,0,513,513);
   var A=new float[512,7];var B=new float[512,7];
   bool[] dry=new bool[512];int dryCols=0;
   int zs=511-Span,ze=Span;
   // Horizontal Gaussian anchor filtering prevents barcode artifacts.
   for(int x=0;x<512;x++){
    if(-24f+320f*sh[512,x]<.15f||-24f+320f*eh[0,x]<.15f||
       -24f+320f*sh[zs,x]<.15f||-24f+320f*eh[ze,x]<.15f)continue;
    dry[x]=true;dryCols++;
    for(int k=0;k<7;k++){
     float asu=0f,bsu=0f,w=0f;
     for(int q=-6;q<=6;q++){ 
      int xx=Mathf.Clamp(x+q,0,511);
      if(-24f+320f*sh[zs,xx]<.15f||-24f+320f*eh[ze,xx]<.15f)continue;
      float ww=Mathf.Exp(-(q*q)/(2f*9f));
      asu+=sa[zs,xx,k]*ww;bsu+=ea[ze,xx,k]*ww;w+=ww;
     }
     A[x,k]=w>.001f?asu/w:sa[zs,x,k];
     B[x,k]=w>.001f?bsu/w:ea[ze,x,k];
    }
   }
   int changed=0;
   for(int x=0;x<512;x++){
    if(!dry[x])continue;
    // Keep exact direct anchor character but suppress high-frequency x-column noise.
    for(int z=zs;z<=511;z++){
     float u=PhotoBlend((z-zs)/(float)(2*Span),x);
     for(int k=0;k<7;k++)sa[z,x,k]=Mathf.Lerp(A[x,k],B[x,k],u);
    }
    for(int z=0;z<=ze;z++){
     float u=PhotoBlend((Span+z)/(float)(2*Span),x);
     for(int k=0;k<7;k++)ea[z,x,k]=Mathf.Lerp(A[x,k],B[x,k],u);
    }
    changed++;
   }
   // Exact shared row for all dry-land columns.
   for(int x=0;x<512;x++){
    if(-24f+320f*sh[512,x]<.15f||-24f+320f*eh[0,x]<.15f)continue;
    for(int k=0;k<7;k++){
     float shared=.5f*(sa[511,x,k]+ea[0,x,k]);
     sa[511,x,k]=shared;ea[0,x,k]=shared;
    }
   }
   float maxGap=0f,maxDerivative=0f,maxLateral=0f;
   for(int x=0;x<512;x++)for(int k=0;k<7;k++){
    maxGap=Mathf.Max(maxGap,Mathf.Abs(sa[511,x,k]-ea[0,x,k]));
    if(dry[x]){
     float ds=sa[511,x,k]-sa[510,x,k];
     float de=ea[1,x,k]-ea[0,x,k];
     maxDerivative=Mathf.Max(maxDerivative,Mathf.Abs(ds-de));
    }
    if(x>0&&dry[x]&&dry[x-1])
     maxLateral=Mathf.Max(maxLateral,Mathf.Abs(sa[511,x,k]-sa[511,x-1,k]));
   }
   int min=n=="SilverCove"?110:360;
   if(changed<min||maxGap>.0001f||maxDerivative>.01f||maxLateral>.18f)
    throw new Exception(n+" smooth bridge failed changed="+changed+
      " gap="+maxGap+" derivative="+maxDerivative+" lateral="+maxLateral);
   s.SetAlphamaps(0,0,sa);e.SetAlphamaps(0,0,ea);
   s.SetBaseMapDirty();e.SetBaseMapDirty();
   EditorUtility.SetDirty(s);EditorUtility.SetDirty(e);
   File.AppendAllText(V+"north_south_alpha_bridge_20260926.txt",
    n+" changedColumns="+changed+" dryColumns="+dryCols+" jaggedPhotoBandM="+Span+
    " maxAlphaGap="+maxGap.ToString("F6")+
    " maxAlphaDerivativeJump="+maxDerivative.ToString("F6")+
    " maxLateralStep="+maxLateral.ToString("F6")+
    " canonicalSourceWrites=0 heightWrites=0\n");
  }
  AssetDatabase.SaveAssets();
 }
}
