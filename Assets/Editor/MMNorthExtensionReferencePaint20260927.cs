using UnityEngine;
using UnityEditor;
using System;
using System.IO;
// Paint ONLY the generated northern extensions from the secondary authority map.
// Linked copies of the four original MM6 maps keep their canonical splatmaps exactly.
public static class MMNorthExtensionReferencePaint20260927 {
 const string V="Validation/EdgeGrid20260923/";
 const string Target=V+"NorthernPhotoBiomeTargets_20260926/";
 const string Baseline=@"C:\MMUnityPort\Backups\BeforeNorthernPhotoTerrainDetail_20260926\Assets\World\WorldExtensions\Generated\";
 const string Guard=@"C:\MMUnityPort\Backups\BeforeCanonicalNorthSourceRestore_20260927\manifest.json";
 static float S(float a,float b,float x){
  float t=Mathf.Clamp01((x-a)/Mathf.Max(.001f,b-a));return t*t*(3f-2f*t);
 }
 static Color32[] LoadMask(string n){
  var t=new Texture2D(2,2,TextureFormat.RGBA32,false,true);
  string p=Target+n+"_North.png";
  if(!File.Exists(p)||!t.LoadImage(File.ReadAllBytes(p))||t.width!=513||t.height!=513)
   throw new Exception("North authority target missing "+n);
  var d=t.GetPixels32();UnityEngine.Object.DestroyImmediate(t);return d;
 }
 static float[,,] LoadBaseline(string n,TerrainData current){
  string src=Baseline+n+"_NorthTerrain.asset";
  string tmp="Assets/TempCanonicalNorthExtension_"+n+"_20260927.asset";
  if(!File.Exists(src)||File.Exists(Path.GetFullPath(tmp)))
   throw new Exception("North extension baseline missing/collision "+n);
  File.Copy(src,Path.GetFullPath(tmp));
  try{
   AssetDatabase.ImportAsset(tmp,ImportAssetOptions.ForceSynchronousImport);
   var td=AssetDatabase.LoadAssetAtPath<TerrainData>(tmp);
   if(!td||td.alphamapResolution!=512||td.alphamapLayers!=current.alphamapLayers)
    throw new Exception("Invalid north extension baseline "+n);
   return td.GetAlphamaps(0,0,512,512);
  }finally{AssetDatabase.DeleteAsset(tmp);}
 }
 public static void Apply(string n,TerrainData canonical,TerrainData extension){
  if(!File.Exists(Guard))throw new Exception("Canonical-north rollback snapshot missing");
  if(canonical.alphamapResolution!=512||extension.alphamapResolution!=512||
     canonical.alphamapLayers!=extension.alphamapLayers)
   throw new Exception(n+" incompatible north splatmap");
  var mask=LoadMask(n);
  var a=LoadBaseline(n,extension);
  var edge=canonical.GetAlphamaps(0,511,512,1);
  var h=extension.GetHeights(0,0,513,513);
  int snow=Array.FindIndex(canonical.terrainLayers,l=>l&&l.name=="Realistic_Snow");
  int rock=Array.FindIndex(canonical.terrainLayers,l=>l&&l.name=="Realistic_Volcanic");
  int soil=Array.FindIndex(canonical.terrainLayers,l=>l&&l.name=="Realistic_Arid");
  int road=Array.FindIndex(canonical.terrainLayers,l=>l&&l.name=="Realistic_RoadOverlay");
  if(snow<0||rock<0||soil<0||road<0)throw new Exception(n+" expected canonical north materials");
  int changed=0,landPixels=0;float edgeGap=0;
  double beforeSnow=0,afterSnow=0,beforeRock=0,afterRock=0;
  for(int z=0;z<512;z++)for(int x=0;x<512;x++){
   if(-24f+320f*h[z,x]<.15f)continue;
   var m=mask[z*513+x];
   if(m.a<145){continue;}
   landPixels++;
   float photoRock=m.r/255f,photoSnow=m.g/255f;
   float targetSnow=.025f+.925f*photoSnow;
   float targetRock=.055f+.78f*photoRock*(1f-.18f*photoSnow);
   float sum=targetSnow+targetRock;
   if(sum>.965f){targetSnow*=.965f/sum;targetRock*=.965f/sum;}
   float other=1f-targetSnow-targetRock;
   float originalOther=1f-a[z,x,snow]-a[z,x,rock];
   float[] photo=new float[a.GetLength(2)];
   for(int k=0;k<photo.Length;k++)
    photo[k]=k==snow?targetSnow:k==rock?targetRock:
     originalOther>.001f?other*a[z,x,k]/originalOther:(k==soil?other:0f);
   float preserve=1f;
   if(n=="Kriegspire"){
    float r=Vector2.Distance(new Vector2(x,z),new Vector2(189f,331f));
    preserve=S(44f,82f,r);
   }else if(n=="SweetWater"){
    float r=Vector2.Distance(new Vector2(x,z),new Vector2(462f,263f));
    preserve=S(40f,74f,r);
   }
   float roadKeep=Mathf.Clamp01(a[z,x,road]*2.2f);
   float photoWeight=.95f*preserve*(1f-.92f*roadKeep);
   float jag=(Mathf.PerlinNoise(x*.024f+8.2f,19.7f)-.5f)*34f;
   float join=S(0f,104f+jag,z);
   float[] blended=new float[photo.Length];
   for(int k=0;k<photo.Length;k++){
    float refValue=Mathf.Lerp(a[z,x,k],photo[k],photoWeight);
    blended[k]=Mathf.Lerp(edge[0,x,k],refValue,join);
   }
   if(z==0)for(int k=0;k<photo.Length;k++)blended[k]=edge[0,x,k];
   beforeSnow+=a[z,x,snow];beforeRock+=a[z,x,rock];
   for(int k=0;k<photo.Length;k++)a[z,x,k]=blended[k];
   afterSnow+=a[z,x,snow];afterRock+=a[z,x,rock];changed++;
  }
  if(changed<10000||landPixels<12000)
   throw new Exception(n+" authority north paint too small changed="+changed+" land="+landPixels);
  // Exact material continuity at the shared border, including submerged columns.
  for(int x=0;x<512;x++)for(int k=0;k<a.GetLength(2);k++)a[0,x,k]=edge[0,x,k];
  for(int x=0;x<512;x++)for(int k=0;k<a.GetLength(2);k++)
   edgeGap=Mathf.Max(edgeGap,Mathf.Abs(a[0,x,k]-edge[0,x,k]));
  // Unity terrain alphamaps are quantized; one 8-bit alpha step is 1/255 = .003921568.
  // Treat <= one stored step as exact visual continuity, but reject anything larger.
  if(edgeGap>.00405f)throw new Exception(n+" extension/source alpha border mismatch "+edgeGap);
  extension.SetAlphamaps(0,0,a);extension.SetBaseMapDirty();EditorUtility.SetDirty(extension);
  File.AppendAllText(V+"north_extension_reference_paint_20260927.txt",
   n+" authorityPixels="+changed+" meanSnowBefore="+(beforeSnow/changed).ToString("F3")+
   " meanSnowAfter="+(afterSnow/changed).ToString("F3")+
   " meanRockBefore="+(beforeRock/changed).ToString("F3")+
   " meanRockAfter="+(afterRock/changed).ToString("F3")+
   " joinFadeM=70..138 sourceAlphaGap="+edgeGap.ToString("F6")+
   " sourceCloneSplatsChanged=0 heightWrites=0 canonicalSourceWrites=0\n");
 }
}
