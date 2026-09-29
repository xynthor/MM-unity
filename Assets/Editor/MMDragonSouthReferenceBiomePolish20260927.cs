using UnityEngine;
using UnityEditor;
using UnityEngine.SceneManagement;
using System;
using System.IO;
// Repaint ONLY generated Dragon Isle South from the authority-photo biome raster.
// Preserve Sweet Water mainland, tile borders, heights, pond and canonical scenes.
public static class MMDragonSouthReferenceBiomePolish20260927 {
 const string V="Validation/EdgeGrid20260923/";
 const string A="Assets/World/DragonIsle/Generated/DragonIsleSouthTerrain.asset";
 const string B=@"C:\MMUnityPort\Backups\BeforeDragonParadiseBiomePolish_20260927\manifest.json";
 static float S(float a,float b,float x){float t=Mathf.Clamp01((x-a)/Mathf.Max(.001f,b-a));return t*t*(3f-2f*t);}
 static Color32[] Load(string p){
  var t=new Texture2D(2,2,TextureFormat.RGBA32,false,true);
  if(!File.Exists(p)||!t.LoadImage(File.ReadAllBytes(p))||t.width!=513||t.height!=513)
   throw new Exception("Dragon reference raster missing "+p);
  var d=t.GetPixels32();UnityEngine.Object.DestroyImmediate(t);return d;
 }
 public static void Apply(){
  if(!File.Exists(B))throw new Exception("Dragon/Paradise polish rollback missing");
  var td=AssetDatabase.LoadAssetAtPath<TerrainData>(A);
  if(!td||td.alphamapResolution!=512||td.heightmapResolution!=513||
     (td.alphamapLayers!=8&&td.alphamapLayers!=12))
   throw new Exception("Dragon South generated terrain unexpected");
  var biome=Load(V+"ReferenceBiomeCandidates_20260925/DragonIsle_South_ReferenceBiomes.png");
  var land=Load(V+"ReferenceMasks/DragonIsle_South.png");
  var a=td.GetAlphamaps(0,0,512,512);var h=td.GetHeights(0,0,513,513);
  int grass=0,sand=1,dirt=2,rock=3,snow=4,ash=5;
  int changed=0,protectedMainland=0,protectedCoast=0;double g0=0,g1=0,d0=0,d1=0;
  for(int z=220;z<500;z++)for(int x=12;x<326;x++){
   float y=-24f+320f*h[z,x];if(y<.2f){protectedCoast++;continue;}
   float auth=land[z*513+x].r;
   float dist=(auth-128f)*512f/(148f*3f);
   if(dist<5f){protectedCoast++;continue;}
   var p=biome[z*513+x];
   float pr=p.r/255f,pf=p.g/255f,po=p.b/255f;
   float conf=Mathf.Max(pr,Mathf.Max(pf,po));
   if(conf<.05f)continue;
   float dry=Mathf.Clamp01(.72f*pr+.86f*po);
   float[] target=new float[a.GetLength(2)];
   target[grass]=.055f+.41f*pf*(1f-.62f*dry);
   target[sand]=.27f+.31f*po;
   target[dirt]=.19f+.22f*po+.05f*pf;
   target[rock]=.13f+.35f*pr;
   target[snow]=0f;target[ash]=.025f+.08f*pr;
   float sum=0;for(int k=0;k<6;k++)sum+=target[k];
   for(int k=0;k<6;k++)target[k]/=Mathf.Max(.00001f,sum);
   float fade=S(220f,270f,z)*(1f-S(286f,326f,x))*S(12f,36f,x)*(1f-S(468f,500f,z));
   float w=.88f*fade*S(.05f,.42f,conf)*S(5f,24f,dist);
   if(w<.002f)continue;
   g0+=a[z,x,grass];d0+=a[z,x,dirt]+a[z,x,sand]+a[z,x,rock];
   float extra=0;for(int k=6;k<a.GetLength(2);k++)extra+=a[z,x,k];
   for(int k=0;k<a.GetLength(2);k++){
    float goal=k<6?target[k]*(1f-extra):a[z,x,k];
    a[z,x,k]=Mathf.Lerp(a[z,x,k],goal,w);
   }
   float norm=0;for(int k=0;k<a.GetLength(2);k++)norm+=a[z,x,k];
   if(norm>.00001f)for(int k=0;k<a.GetLength(2);k++)a[z,x,k]/=norm;
   g1+=a[z,x,grass];d1+=a[z,x,dirt]+a[z,x,sand]+a[z,x,rock];
   changed++;
  }
  // Explicitly confirm protected source-aligned eastern mainland and all tile borders unchanged.
  var original=td.GetAlphamaps(0,0,512,512);float border=0,mainland=0;
  for(int z=0;z<512;z++)for(int k=0;k<a.GetLength(2);k++){
   border=Mathf.Max(border,Mathf.Abs(original[z,0,k]-a[z,0,k]));
   border=Mathf.Max(border,Mathf.Abs(original[z,511,k]-a[z,511,k]));
   if(z<220)mainland=Mathf.Max(mainland,Mathf.Abs(original[z,420,k]-a[z,420,k]));
  }
  for(int x=0;x<512;x++)for(int k=0;k<a.GetLength(2);k++){
   border=Mathf.Max(border,Mathf.Abs(original[0,x,k]-a[0,x,k]));
   border=Mathf.Max(border,Mathf.Abs(original[511,x,k]-a[511,x,k]));
  }
  if(changed<8000||border>.000001f||mainland>.000001f)
   throw new Exception("Dragon South biome polish unsafe changed="+changed+" border="+border+" mainland="+mainland);
  td.SetAlphamaps(0,0,a);td.SetBaseMapDirty();EditorUtility.SetDirty(td);AssetDatabase.SaveAssets();
  File.WriteAllText(V+"dragon_south_reference_biome_polish_20260927.txt",
   "PASS paintedPixels="+changed+
   " meanGrassBefore="+(g0/changed).ToString("F3")+
   " meanGrassAfter="+(g1/changed).ToString("F3")+
   " meanDryBefore="+(d0/changed).ToString("F3")+
   " meanDryAfter="+(d1/changed).ToString("F3")+
   " borderAlphaError="+border.ToString("F6")+
   " protectedMainlandError="+mainland.ToString("F6")+
   " heightWrites=0 pondWrites=0 canonicalWrites=0\n");
 }
}
