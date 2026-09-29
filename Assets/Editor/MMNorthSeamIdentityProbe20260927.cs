using UnityEngine;
using UnityEditor;
using UnityEngine.SceneManagement;
using System;
using System.IO;
using System.Linq;
using System.Text;
// Read-only probe: distinguish true material differences from splatmap ecotone.
[InitializeOnLoad]
public static class MMNorthSeamIdentityProbe20260927 {
 const string Root="Assets/World/WorldExtensions/Generated/";
 const string Dir="Validation/EdgeGrid20260923/";
 static MMNorthSeamIdentityProbe20260927(){EditorApplication.delayCall+=Run;}
 static void Run(){
  string flag=Dir+"RUN_NORTH_SEAM_IDENTITY_PROBE.flag";
  if(!File.Exists(flag))return;
  if(EditorApplication.isCompiling||EditorApplication.isUpdating){
   EditorApplication.delayCall+=Run;return;
  }
  File.Delete(flag);
  var outp=new StringBuilder();
  try{
   var sc=SceneManager.GetActiveScene();
   if(sc.path!="Assets/Scenes/Enroth_Linked_OpenWorld.unity"||sc.isDirty)
    throw new Exception("Saved linked scene required");
   var names=new[]{"SweetWater","Kriegspire","FrozenHighlands","SilverCove"};
   foreach(var n in names){
    var s=AssetDatabase.LoadAssetAtPath<TerrainData>(Root+"LinkedSourceTransitions/"+n+"_LinkedNorthProfile.asset");
    var e=AssetDatabase.LoadAssetAtPath<TerrainData>(Root+n+"_NorthTerrain.asset");
    if(!s||!e||s.alphamapLayers!=e.alphamapLayers)
     throw new Exception("Missing/incompatible north terrain "+n);
    outp.AppendLine("TERRAIN "+n+" source="+AssetDatabase.GetAssetPath(s)+" extension="+AssetDatabase.GetAssetPath(e));
    for(int k=0;k<s.alphamapLayers;k++){
     var sl=s.terrainLayers[k];var el=e.terrainLayers[k];
     if(!sl||!el)throw new Exception("Null terrain layer "+n+" "+k);
     var sp=AssetDatabase.GetAssetPath(sl);
     var ep=AssetDatabase.GetAssetPath(el);
     var st=sl.diffuseTexture?AssetDatabase.GetAssetPath(sl.diffuseTexture):"NULL";
     var et=el.diffuseTexture?AssetDatabase.GetAssetPath(el.diffuseTexture):"NULL";
     outp.AppendLine("layer "+k+" name="+sl.name+"/"+el.name+
      " sameAsset="+(sl==el)+" sameDiffuse="+(st==et)+
      " source="+sp+" extension="+ep+" sourceTexture="+st+
      " extensionTexture="+et+" tile="+sl.tileSize+"/"+el.tileSize+
      " offset="+sl.tileOffset+"/"+el.tileOffset);
    }
    var sa=s.GetAlphamaps(0,0,512,512);var ea=e.GetAlphamaps(0,0,512,512);
    var sh=s.GetHeights(0,0,513,513);var eh=e.GetHeights(0,0,513,513);
    int snow=Array.FindIndex(s.terrainLayers,l=>l&&l.name=="Realistic_Snow");
    int rock=Array.FindIndex(s.terrainLayers,l=>l&&l.name=="Realistic_Volcanic");
    if(snow<0||rock<0)throw new Exception("Missing expected layers "+n);
    float borderMax=0;int borderLand=0;
    for(int x=0;x<512;x++){
     if(-24f+320f*sh[511,x]<.2f||-24f+320f*eh[0,x]<.2f)continue;
     borderLand++;
     for(int k=0;k<s.alphamapLayers;k++)
      borderMax=Mathf.Max(borderMax,Mathf.Abs(sa[511,x,k]-ea[0,x,k]));
    }
    outp.AppendLine("sharedBorder landSamples="+borderLand+
     " maxAbsAlphaGap="+borderMax.ToString("F6"));
    foreach(int off in new[]{-96,-48,-16,-4,-1,0,4,16,48,96}){
     bool extension=off>=0;int z=extension?off:512+off;
     var a=extension?ea:sa;var h=extension?eh:sh;
     double sn=0,ro=0;int count=0;
     for(int x=0;x<512;x++){
      if(-24f+320f*h[z,x]<.2f)continue;
      sn+=a[z,x,snow];ro+=a[z,x,rock];count++;
     }
     outp.AppendLine("offset_m="+off+" landSamples="+count+
      " meanSnow="+(sn/Math.Max(1,count)).ToString("F4")+
      " meanRock="+(ro/Math.Max(1,count)).ToString("F4"));
    }
   }
   File.WriteAllText(Dir+"north_seam_identity_probe_20260927.txt",
    "PASS read-only material and sampled border audit\n"+outp);
  }catch(Exception ex){
   File.WriteAllText(Dir+"north_seam_identity_probe_20260927.txt",
    "FAIL "+ex+"\n"+outp);
   Debug.LogException(ex);
  }
 }
}
