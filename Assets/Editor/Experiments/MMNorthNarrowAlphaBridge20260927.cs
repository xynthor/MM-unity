using UnityEngine;
using UnityEditor;
using System;
using System.IO;
// Narrow 24-34 m north/south splat bridge. Removes the one-row material seam
// without recreating the broad horizontal belt from the retired 64m bridge.
public static class MMNorthNarrowAlphaBridge20260927 {
 const string V="Validation/EdgeGrid20260923/";
 const string G="Assets/World/WorldExtensions/Generated/";
 const string Backup=@"C:\MMUnityPort\Backups\BeforeNorthNarrowAlphaBridge_20260927\manifest.json";
 static readonly string[] N={"SweetWater","Kriegspire","FrozenHighlands","SilverCove"};
 static float S(float t){t=Mathf.Clamp01(t);return t*t*(3f-2f*t);}
 static TerrainData D(string n,bool north){
  string p=north?G+n+"_NorthTerrain.asset":G+"LinkedSourceTransitions/"+n+"_LinkedNorthProfile.asset";
  var d=AssetDatabase.LoadAssetAtPath<TerrainData>(p);
  if(!d||d.alphamapResolution!=512||d.alphamapLayers!=7)throw new Exception("Missing "+p);
  return d;
 }
 public static void Apply(){
  if(!File.Exists(Backup))throw new Exception("North narrow bridge backup missing");
  File.WriteAllText(V+"north_narrow_alpha_bridge_20260927.txt","");
  foreach(var n in N){
   var s=D(n,false);var e=D(n,true);
   var sa=s.GetAlphamaps(0,0,512,512);var ea=e.GetAlphamaps(0,0,512,512);
   var sh=s.GetHeights(0,0,513,513);var eh=e.GetHeights(0,0,513,513);
   int changed=0;float maxGap=0,maxDeriv=0;
   for(int x=0;x<512;x++){
    if(-24f+320f*sh[512,x]<.15f||-24f+320f*eh[0,x]<.15f)continue;
    int span=Mathf.RoundToInt(12f+5f*Mathf.PerlinNoise(x*.021f+4.7f,19.3f));
    span=Mathf.Clamp(span,10,17);
    int zs=511-span,ze=span;
    if(-24f+320f*sh[zs,x]<.15f||-24f+320f*eh[ze,x]<.15f)continue;
    float[] A=new float[7],B=new float[7];
    for(int k=0;k<7;k++){
     float asu=0,bsu=0,w=0;
     for(int q=-2;q<=2;q++){
      int xx=Mathf.Clamp(x+q,0,511);
      float ww=q==0?4f:(Mathf.Abs(q)==1?2f:1f);
      asu+=sa[zs,xx,k]*ww;bsu+=ea[ze,xx,k]*ww;w+=ww;
     }
     A[k]=asu/w;B[k]=bsu/w;
    }
    for(int z=zs;z<512;z++){
     float t=(z-zs)/(float)(2*span);
     float u=S(t);
     for(int k=0;k<7;k++)sa[z,x,k]=Mathf.Lerp(A[k],B[k],u);
    }
    for(int z=0;z<=ze;z++){
     float t=(span+z)/(float)(2*span);
     float u=S(t);
     for(int k=0;k<7;k++)ea[z,x,k]=Mathf.Lerp(A[k],B[k],u);
    }
    changed++;
   }
   for(int x=0;x<512;x++){
    if(-24f+320f*sh[512,x]<.15f||-24f+320f*eh[0,x]<.15f)continue;
    var sharedV=new float[7];var mV=new float[7];float scale=1f;
    for(int k=0;k<7;k++){
     sharedV[k]=.5f*(sa[511,x,k]+ea[0,x,k]);
     // Difference of two normalized alpha vectors sums to zero, so this
     // derivative vector preserves per-pixel normalization on both sides.
     mV[k]=(ea[2,x,k]-sa[509,x,k])*.25f;
     float am=Mathf.Abs(mV[k]);
     if(am>.000001f)scale=Mathf.Min(scale,.92f*sharedV[k]/am);
    }
    scale=Mathf.Clamp01(scale);
    for(int k=0;k<7;k++){
     float m=mV[k]*scale,shared=sharedV[k];
     sa[510,x,k]=shared-m;
     sa[511,x,k]=shared;
     ea[0,x,k]=shared;
     ea[1,x,k]=shared+m;
     maxGap=Mathf.Max(maxGap,Mathf.Abs(sa[511,x,k]-ea[0,x,k]));
     float ds=sa[511,x,k]-sa[510,x,k];
     float de=ea[1,x,k]-ea[0,x,k];
     maxDeriv=Mathf.Max(maxDeriv,Mathf.Abs(ds-de));
    }
   }
   int min=n=="SilverCove"?90:330;
   if(changed<min||maxGap>.0001f||maxDeriv>.08f)
    throw new Exception(n+" narrow bridge failed changed="+changed+
      " gap="+maxGap+" derivative="+maxDeriv);
   s.SetAlphamaps(0,0,sa);e.SetAlphamaps(0,0,ea);
   s.SetBaseMapDirty();e.SetBaseMapDirty();
   EditorUtility.SetDirty(s);EditorUtility.SetDirty(e);
   File.AppendAllText(V+"north_narrow_alpha_bridge_20260927.txt",
    n+" changedColumns="+changed+" halfSpanM=10..17"+
    " maxAlphaGap="+maxGap.ToString("F6")+
    " maxAlphaDerivativeJump="+maxDeriv.ToString("F6")+
    " canonicalSourceWrites=0 heightWrites=0\n");
  }
  AssetDatabase.SaveAssets();
 }
}
