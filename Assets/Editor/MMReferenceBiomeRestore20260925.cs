using UnityEngine;
using UnityEditor;
using UnityEngine.SceneManagement;
using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
public static class MMReferenceBiomeRestore20260925 {
 const string V="Validation/EdgeGrid20260923/";
 const string G="Assets/World/WorldExtensions/Generated/";
 static float S(float a,float b,float v){float t=Mathf.Clamp01((v-a)/(b-a));return t*t*(3f-2f*t);}
 static Color32[] Pixels(string path){
  var tex=new Texture2D(2,2,TextureFormat.RGBA32,false,true);
  if(!tex.LoadImage(File.ReadAllBytes(path))||tex.width!=513||tex.height!=513)
   throw new Exception("Reference photo-derived mask missing or invalid "+path);
  var pix=tex.GetPixels32();UnityEngine.Object.DestroyImmediate(tex);return pix;
 }
 static string[] W(TerrainLayer[] layers)=>layers.Select(l=>l?l.name:"NULL").ToArray();
 static int Index(TerrainData t,string needle){
  int i=Array.FindIndex(t.terrainLayers,l=>l&&l.name.IndexOf(needle,StringComparison.OrdinalIgnoreCase)>=0);
  if(i<0)throw new Exception("Missing terrain layer "+needle);return i;
 }
 static void Normalize(float[] a){
  float sum=a.Sum();if(sum<.00001f)throw new Exception("Zero terrain blend");
  for(int k=0;k<a.Length;k++)a[k]/=sum;
 }
 static float Gauss(float v,float mu,float sigma){
  float t=(v-mu)/sigma;return Mathf.Exp(-.5f*t*t);
 }
 static TerrainData Load(string p){
  var t=AssetDatabase.LoadAssetAtPath<TerrainData>(p);
  if(!t||t.heightmapResolution!=513||t.alphamapResolution!=512)
   throw new Exception("Generated biome asset missing "+p);
  return t;
 }
 static string RepaintReference(string name,string asset){
  var t=Load(asset);
  var photo=Pixels(V+"ReferenceBiomeCandidates_20260925/"+name+"_ReferenceBiomes.png");
  var authority=Pixels(V+"ReferenceMasks/"+name+".png");
  var a=t.GetAlphamaps(0,0,512,512);
  int grass=Index(t,name.StartsWith("Dragon")?(name=="DragonIsle_North"?"Trace_Grass":"ReferenceDragonForestGrass"):"Realistic_Green");
  int sand=Index(t,name.StartsWith("Dragon")?"Trace_Sand":"Realistic_Desert");
  int rock=Index(t,name.StartsWith("Dragon")?"Trace_Rock":"Realistic_Volcanic");
  int dirt=Index(t,name.StartsWith("Dragon")?"Trace_Dirt":"Realistic_Arid");
  int light=name.StartsWith("Dragon")?-1:Index(t,"Realistic_LightGreen");
  float[] target=new float[t.alphamapLayers];int count=0;
  float forestAvg=0,rockAvg=0;
  for(int z=0;z<512;z++)for(int x=0;x<512;x++){
   int at=z*513+x;var p=photo[at];var auth=authority[at];
   float distance=(auth.r-128f)*512f/(148f*3f);
   if(distance<2.5f)continue;
   float forest=p.g/255f,stone=p.r/255f,tan=p.b/255f;
   float influence=0;
   Array.Clear(target,0,target.Length);
   if(name=="ParadiseValley_West"){
    influence=.95f*S(2.5f,21f,distance)*(1f-S(467f,510f,x));
    float south=(1f-S(100f,240f,z));
    float rockStrength=Mathf.Max(stone,.65f*south);
    target[grass]=.28f+.62f*forest*(1f-.60f*rockStrength);
    target[light]=.08f+.19f*forest*(1f-.60f*rockStrength);
    target[sand]=.015f+.10f*tan;
    target[rock]=.11f+.76f*rockStrength;
    target[dirt]=.07f+.34f*tan+.14f*rockStrength;
   }else if(name=="DragonIsle_North"){
    if(x>=354&&z<180)continue; // independent eastern source-aligned mainland
    influence=.83f*S(2.5f,21f,distance)*S(8f,55f,z);
    // North Dragon Isle is primarily the source's tan, bare rocky cape.
    float darkStone=stone*(1f-.65f*tan);
    target[grass]=.045f+.29f*forest;
    target[sand]=.39f+.48f*tan;
    target[rock]=.16f+.40f*darkStone;
    target[dirt]=.19f+.20f*tan;
   }else if(name=="DragonIsle_South"){
    if(x>325||z<220)continue; // never repaint the protected Sweet Water mainland
    influence=.83f*S(2.5f,24f,distance)*S(225f,291f,z)*(1f-S(273f,325f,x));
    target[grass]=.22f+.64f*forest;
    target[sand]=.10f+.39f*tan*(1f-.45f*forest);
    target[rock]=.10f+.39f*stone;
    target[dirt]=.16f+.21f*tan;
   }
   if(influence<.001f)continue;
   Normalize(target);
   float sum=0;
   for(int k=0;k<t.alphamapLayers;k++){
    a[z,x,k]=Mathf.Lerp(a[z,x,k],target[k],influence);sum+=a[z,x,k];
   }
   if(sum<=0f)throw new Exception("Invalid biome repaint "+name+" at "+x+","+z);
   for(int k=0;k<t.alphamapLayers;k++)a[z,x,k]/=sum;
   count++;forestAvg+=forest;rockAvg+=stone;
  }
  if(count<500)throw new Exception("Reference photo map misaligned: "+name+" painted="+count);
  t.SetAlphamaps(0,0,a);EditorUtility.SetDirty(t);
  return name+",repainted="+count+",photo_forest="+(forestAvg/count).ToString("F3")+
    ",photo_rock="+(rockAvg/count).ToString("F3")+",canonical_source_edits=0";
 }
 static float Brush(float x,float z,float cx,float cz,float rx,float rz){
  float dx=(x-cx)/rx,dz=(z-cz)/rz;
  return Mathf.Exp(-2.5f*(dx*dx+dz*dz));
 }
 static string BuildSourceVolcano(){
  var td=Load(G+"Kriegspire_NorthTerrain.asset");
  var auth=Pixels(V+"ReferenceMasks/Kriegspire_North.png");
  var h=td.GetHeights(0,0,513,513);
  const float cx=189f,cz=331f;
  if(auth[331*513+189].r<140||(-24f+h[331,189]*320f)<6f)
   throw new Exception("Northern authority crater position invalid");
  float max=0f;int touched=0;
  for(int z=295;z<=369;z++)for(int x=147;x<=232;x++){
   if(auth[z*513+x].r<140)continue;
   float dx=(x-cx)/25f,dz=(z-cz)/22f;
   float r=Mathf.Sqrt(dx*dx+dz*dz);
   if(r>=1.18f)continue;
   float feather=1f-S(.87f,1.17f,r);
   // The photo shows a dark circular depression with a rubble rim.
   float rim=4.8f*Gauss(r,.64f,.15f);
   float hollow=7.2f*Gauss(r,0f,.29f);
   float delta=(rim-hollow)*feather;
   h[z,x]=Mathf.Clamp01(h[z,x]+delta/320f);
   max=Mathf.Max(max,Mathf.Abs(delta));touched++;
  }
  td.SetHeights(0,0,h);EditorUtility.SetDirty(td);
  return "Kriegspire_North,reference_crater_px=(313,61),terrain_center=(189,331),radius_m=(25,22),"+
         "changed_vertices="+touched+",max_height_adjust_m="+max.ToString("F3");
 }
 static string ExposeSourceVolcanicRock(string name){
  var td=Load(G+name+"_NorthTerrain.asset");
  var auth=Pixels(V+"ReferenceMasks/"+name+"_North.png");
  var a=td.GetAlphamaps(0,0,512,512);
  int stone=Index(td,"Realistic_Volcanic");
  int count=0;
  for(int z=0;z<512;z++)for(int x=0;x<512;x++){
   if(auth[z*513+x].r<139)continue;
   float cover=0;
   if(name=="Kriegspire")cover=Brush(x,z,189,331,35,33);
   if(name=="SweetWater"){
    cover=Mathf.Max(Brush(x,z,476,262,28,32),Brush(x,z,365,215,30,35));
   }
   float influence=.92f*S(.06f,.8f,cover);
   if(influence<.002f)continue;
   float sum=0;
   for(int k=0;k<td.alphamapLayers;k++){
    a[z,x,k]=k==stone?Mathf.Lerp(a[z,x,k],.77f,influence):
      a[z,x,k]*(1f-influence);
    sum+=a[z,x,k];
   }
   for(int k=0;k<td.alphamapLayers;k++)a[z,x,k]/=sum;
   count++;
  }
  td.SetAlphamaps(0,0,a);EditorUtility.SetDirty(td);
  return name+"_North,reference_volcanic_exposure_pixels="+count;
 }
 [MenuItem("MMUnity/Reference 2026/Apply Authority-Derived Regional Biomes")]
 public static void Apply(){
  if(!File.Exists(@"C:\MMUnityPort\Backups\BeforeFullReferenceBiomeAndNorthHeight_20260925\manifest.json"))
   throw new Exception("Generated-terrain backup is required");
  if(SceneManager.GetActiveScene().isDirty)
   throw new Exception("Active scene has unsaved edits; biome application aborted");
  // All read dependencies are checked before painting any generated terrain.
  foreach(string n in new[]{"ParadiseValley_West","DragonIsle_North","DragonIsle_South"}){
   if(!File.Exists(V+"ReferenceBiomeCandidates_20260925/"+n+"_ReferenceBiomes.png"))
    throw new FileNotFoundException("Reference photo candidate unavailable "+n);
   if(!File.Exists(V+"ReferenceMasks/"+n+".png"))
    throw new FileNotFoundException("Authority geometry unavailable "+n);
  }
  var log=new List<string>();
  log.Add(RepaintReference("ParadiseValley_West",G+"ParadiseValley_WestTerrain.asset"));
  log.Add(RepaintReference("DragonIsle_North","Assets/World/DragonIsle/Generated/DragonIsleNorthTerrain.asset"));
  log.Add(RepaintReference("DragonIsle_South","Assets/World/DragonIsle/Generated/DragonIsleSouthTerrain.asset"));
  log.Add(BuildSourceVolcano());
  log.Add(ExposeSourceVolcanicRock("SweetWater"));
  log.Add(ExposeSourceVolcanicRock("Kriegspire"));
  AssetDatabase.SaveAssets();
  File.WriteAllLines(V+"authority_region_biomes_20260925.csv",log);
  Debug.Log("REGION_BIOME_AUTHORITY_REBUILD_COMPLETE "+string.Join(" | ",log));
 }
}
[InitializeOnLoad]
internal static class MMReferenceBiomeQueued20260925 {
 const string V="Validation/EdgeGrid20260923/";
 static MMReferenceBiomeQueued20260925(){EditorApplication.delayCall+=Run;}
 static void Run(){
  if(!File.Exists(V+"RUN_AUTHORITY_BIOME_RECONSTRUCTION.flag"))return;
  File.Delete(V+"RUN_AUTHORITY_BIOME_RECONSTRUCTION.flag");
  try{
   MMReferenceBiomeRestore20260925.Apply();
   File.WriteAllText(V+"authority_biome_reconstruction_runtime.txt","PASS saved_generated_terrain=true\n");
  }catch(Exception ex){
   File.WriteAllText(V+"authority_biome_reconstruction_runtime.txt","FAIL "+ex+"\n");
   Debug.LogError(ex);
  }
 }
}

// BIOME_GATE_20260925
