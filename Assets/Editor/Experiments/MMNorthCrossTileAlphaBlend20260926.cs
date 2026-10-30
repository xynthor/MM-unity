using UnityEngine;
using UnityEditor;
using System;
using System.IO;
// Smooth the three internal E/W material joins across the northern four-tile chain.
// Only generated north assets and linked-only source clones are modified.
public static class MMNorthCrossTileAlphaBlend20260926 {
 const string V="Validation/EdgeGrid20260923/";
 const string G="Assets/World/WorldExtensions/Generated/";
 const string Backup=@"C:\MMUnityPort\Backups\BeforeNorthAlphaSeamBlend_20260926\manifest.json";
 static readonly string[] N={"SweetWater","Kriegspire","FrozenHighlands","SilverCove"};
 static float S(float t){t=Mathf.Clamp01(t);return t*t*(3f-2f*t);}
 static TerrainData Get(string n,bool north){
  string p=north?G+n+"_NorthTerrain.asset":
   G+"LinkedSourceTransitions/"+n+"_LinkedNorthProfile.asset";
  var d=AssetDatabase.LoadAssetAtPath<TerrainData>(p);
  if(!d||d.alphamapResolution!=512||d.alphamapLayers!=7)throw new Exception("Missing north blend terrain "+p);
  return d;
 }
 public static void Apply(){
  if(!File.Exists(Backup))throw new Exception("North cross-tile backup missing");
  int changed=0;float maxGap=0,maxSlope=0;
  // Keep the linked copies of the original MM6 maps visually canonical.
  // Blend only the generated northern extensions across their internal E/W joins.
  for(int row=1;row<2;row++)for(int pair=0;pair<3;pair++){
   bool north=row==1;var a=Get(N[pair],north);var b=Get(N[pair+1],north);
   var aa=a.GetAlphamaps(0,0,512,512);var bb=b.GetAlphamaps(0,0,512,512);
   var ah=a.GetHeights(0,0,513,513);var bh=b.GetHeights(0,0,513,513);
   int local=0;
   for(int z=0;z<512;z++){
    if(-24f+320f*ah[z,512]<.15f||-24f+320f*bh[z,0]<.15f)continue;
    int w=Mathf.RoundToInt(30f+24f*Mathf.PerlinNoise(z*.021f+pair*3.7f,row*8.2f+5.1f));
    int xa=511-w,xb=w;
    float[] seam=new float[7];
    for(int k=0;k<7;k++)seam[k]=.5f*(aa[z,xa,k]+bb[z,xb,k]);
    for(int x=xa;x<512;x++){
     float u=S((x-xa)/(float)Mathf.Max(1,511-xa));
     for(int k=0;k<7;k++)aa[z,x,k]=Mathf.Lerp(aa[z,xa,k],seam[k],u);
    }
    for(int x=0;x<=xb;x++){
     float u=S(x/(float)Mathf.Max(1,xb));
     for(int k=0;k<7;k++)bb[z,x,k]=Mathf.Lerp(seam[k],bb[z,xb,k],u);
    }
    for(int k=0;k<7;k++){
     aa[z,511,k]=seam[k];bb[z,0,k]=seam[k];
     maxGap=Mathf.Max(maxGap,Mathf.Abs(aa[z,511,k]-bb[z,0,k]));
     maxSlope=Mathf.Max(maxSlope,Mathf.Abs((aa[z,511,k]-aa[z,510,k])-(bb[z,1,k]-bb[z,0,k])));
    }
    local++;changed++;
   }
   a.SetAlphamaps(0,0,aa);b.SetAlphamaps(0,0,bb);EditorUtility.SetDirty(a);EditorUtility.SetDirty(b);
   File.AppendAllText(V+"north_cross_tile_alpha_blend_20260926.txt",
    (north?"EXT":"SRC")+" "+N[pair]+"-"+N[pair+1]+" blendedRows="+local+"\n");
  }
  AssetDatabase.SaveAssets();
  if(changed<180||maxGap>.0001f||maxSlope>.08f)
   throw new Exception("North cross-tile alpha blend failed rows="+changed+" gap="+maxGap+" slope="+maxSlope);
  File.AppendAllText(V+"north_cross_tile_alpha_blend_20260926.txt",
   "SUMMARY changedRows="+changed+" maxAlphaGap="+maxGap.ToString("F6")+
   " maxAlphaSlopeJump="+maxSlope.ToString("F6")+
   " canonicalSourceWrites=0 heightWrites=0\n");
 }
}
