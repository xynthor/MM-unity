using UnityEngine;
using UnityEditor;
using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
// Authority-photo rock and snow masks. Never substitute an image as the
// visible ground texture: use the existing original repeating materials.
public static class MMNorthernPhotoTerrainPaint20260926 {
 const string V="Validation/EdgeGrid20260923/";
 const string G="Assets/World/WorldExtensions/Generated/";
 static float S(float a,float b,float t){
  float u=Mathf.Clamp01((t-a)/(b-a));return u*u*(3f-2f*u);
 }
 public static void Apply(){
  if(!File.Exists(@"C:\MMUnityPort\Backups\BeforeNorthernPhotoTerrainDetail_20260926\manifest.json"))
   throw new Exception("Missing pre-paint north terrain snapshot");
  var rows=new List<string>();
  foreach(var name in new[]{"SweetWater","Kriegspire","FrozenHighlands","SilverCove"}){
   var t=AssetDatabase.LoadAssetAtPath<TerrainData>(G+name+"_NorthTerrain.asset");
   if(!t||t.alphamapResolution!=512)throw new Exception("North terrain missing "+name);
   var img=new Texture2D(2,2,TextureFormat.RGBA32,false,true);
   if(!img.LoadImage(File.ReadAllBytes(V+"NorthernPhotoBiomeTargets_20260926/"+name+"_North.png"))||
      img.width!=513||img.height!=513)throw new Exception("Native photo land mask missing "+name);
   var pix=img.GetPixels32();UnityEngine.Object.DestroyImmediate(img);
   int snow=Array.FindIndex(t.terrainLayers,l=>l&&l.name=="Realistic_Snow");
   int rock=Array.FindIndex(t.terrainLayers,l=>l&&l.name=="Realistic_Volcanic");
   if(snow<0||rock<0)throw new Exception("Source tiled snow/rock layers missing "+name);
   int soil=Array.FindIndex(t.terrainLayers,l=>l&&l.name=="Realistic_Arid");
   var a=t.GetAlphamaps(0,0,512,512);
   // Revisions start from the unmodified pre-photo, post-source-snow snapshot.
   // Otherwise a repeated blend compounds the previous rock/snow paint.
   if(File.Exists(V+"north_reference_photo_rock_snow_20260926.txt")){
    string b=@"C:\MMUnityPort\Backups\BeforeNorthernPhotoTerrainDetail_20260926\Assets\World\WorldExtensions\Generated\"+name+"_NorthTerrain.asset";
    string tmp="Assets/TempNorthPhotoBaseline_"+name+"_20260926.asset";
    if(!File.Exists(b)||File.Exists(Path.GetFullPath(tmp)))
     throw new Exception("Pre-photo texture baseline missing or scratch collision "+name);
    File.Copy(b,Path.GetFullPath(tmp));
    try{
     AssetDatabase.ImportAsset(tmp,ImportAssetOptions.ForceSynchronousImport);
     var baseTd=AssetDatabase.LoadAssetAtPath<TerrainData>(tmp);
     if(!baseTd||baseTd.alphamapLayers!=t.alphamapLayers)
      throw new Exception("Bad northern baseline texture layers "+name);
     a=baseTd.GetAlphamaps(0,0,512,512);
    }finally{AssetDatabase.DeleteAsset(tmp);}
   }
   int changed=0;double snowBefore=0,snowAfter=0,rockBefore=0,rockAfter=0;
   for(int z=18;z<512;z++)for(int x=0;x<512;x++){
    var m=pix[z*513+x];
    if(m.a<160)continue; // only the photo-confirmed northern land
    float w=.93f*S(12f,208f,z);
    if(name=="Kriegspire"){
     float r=Vector2.Distance(new Vector2(x,z),new Vector2(189f,331f));
     w*=S(48f,78f,r); // preserve the original crater and crater pond
    }
    if(name=="SweetWater"){
     float r=Vector2.Distance(new Vector2(x,z),new Vector2(462f,263f));
     w*=S(43f,69f,r); // retain mapped volcanic vent and rubble
    }
    if(w<.001f)continue;
    float photoRock=m.r/255f,photoSnow=m.g/255f;
    float newSnow=.04f+.91f*photoSnow;
    float newRock=.08f+.72f*photoRock*(1f-.25f*photoSnow);
    float total=newSnow+newRock;
    if(total>.96f){newSnow*=.96f/total;newRock*=.96f/total;}
    float other=1f-newSnow-newRock;
    float originalOther=1f-a[z,x,snow]-a[z,x,rock];
    snowBefore+=a[z,x,snow];rockBefore+=a[z,x,rock];
    for(int k=0;k<t.alphamapLayers;k++){
     float v=k==snow?newSnow:k==rock?newRock:
       originalOther>.001f?other*a[z,x,k]/originalOther:
       k==soil?other:0f;
     a[z,x,k]=Mathf.Lerp(a[z,x,k],v,w);
    }
    snowAfter+=a[z,x,snow];rockAfter+=a[z,x,rock];changed++;
   }
   if(changed>0){
    t.SetAlphamaps(0,0,a);EditorUtility.SetDirty(t);
   }
   rows.Add(name+"_North photoPixels="+changed+
     " meanSnowBefore="+(snowBefore/Mathf.Max(1,changed)).ToString("F3")+
     " meanSnowAfter="+(snowAfter/Mathf.Max(1,changed)).ToString("F3")+
     " meanRockBefore="+(rockBefore/Mathf.Max(1,changed)).ToString("F3")+
     " meanRockAfter="+(rockAfter/Mathf.Max(1,changed)).ToString("F3")+
     " sourceJoinAt0mUnchanged=true wide196mPhotoFade=true existingCraterAndPondsUntouched=true heightWrites=0");
  }
  AssetDatabase.SaveAssets();
  File.WriteAllLines(V+"north_reference_photo_rock_snow_20260926.txt",rows);
 }
}
[InitializeOnLoad]
internal static class MMNorthernPhotoTerrainPaintQueued20260926 {
 const string V="Validation/EdgeGrid20260923/";
 static MMNorthernPhotoTerrainPaintQueued20260926(){EditorApplication.delayCall+=Run;}
 static void Run(){
  if(!File.Exists(V+"RUN_NORTH_REFERENCE_PHOTO_DETAIL_20260926.flag"))return;
  if(EditorApplication.isCompiling||EditorApplication.isUpdating){
   EditorApplication.delayCall+=Run;return;
  }
  File.Delete(V+"RUN_NORTH_REFERENCE_PHOTO_DETAIL_20260926.flag");
  try{
   var sc=UnityEngine.SceneManagement.SceneManager.GetActiveScene();
   if(sc.path!="Assets/Scenes/Enroth_Linked_OpenWorld.unity"||sc.isDirty)
    throw new Exception("Saved linked scene required");
   MMNorthernPhotoTerrainPaint20260926.Apply();
   BuildEnrothLinkedOpenWorld.Build();
   File.WriteAllText(V+"north_reference_photo_detail_runtime_20260926.txt",
    "PASS photo-derived rock/snow in native reference highlands; linked rebuilt\n");
  }catch(Exception e){
   File.WriteAllText(V+"north_reference_photo_detail_runtime_20260926.txt","FAIL "+e+"\n");
   Debug.LogError(e);
  }
 }
}
