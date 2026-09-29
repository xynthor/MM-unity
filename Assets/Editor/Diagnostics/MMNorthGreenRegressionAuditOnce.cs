using UnityEngine;
using UnityEditor;
using UnityEngine.SceneManagement;
using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
[InitializeOnLoad]
internal static class MMNorthGreenRegressionAuditOnce {
 const string V="Validation/EdgeGrid20260923/";
 static MMNorthGreenRegressionAuditOnce(){EditorApplication.delayCall+=Run;}
 static void Run(){
  if(!File.Exists(V+"RUN_NORTH_GREEN_REGRESSION_AUDIT.flag"))return;
  if(EditorApplication.isCompiling||EditorApplication.isUpdating){EditorApplication.delayCall+=Run;return;}
  File.Delete(V+"RUN_NORTH_GREEN_REGRESSION_AUDIT.flag");
  var lines=new List<string>();
  try{
   foreach(string n in new[]{"SweetWater","Kriegspire","FrozenHighlands","SilverCove"}){
    var src=AssetDatabase.LoadAssetAtPath<TerrainData>("Assets/World/"+n+"/Generated/"+n+"Terrain.asset");
    var clone=AssetDatabase.LoadAssetAtPath<TerrainData>("Assets/World/WorldExtensions/Generated/LinkedSourceTransitions/"+n+"_LinkedNorthProfile.asset");
    foreach(var pair in new[]{new{td=src,label="SOURCE"},new{td=clone,label="LINKED_CLONE"}}){
     var t=pair.td;if(!t){lines.Add(n+" "+pair.label+" MISSING");continue;}
     var a=t.GetAlphamaps(0,0,t.alphamapWidth,t.alphamapHeight);
     int layers=t.alphamapLayers,sz=t.alphamapWidth;
     double[] mean=new double[layers];int[] dom=new int[layers];
     for(int z=0;z<t.alphamapHeight;z+=4)for(int x=0;x<sz;x+=4){
      int max=0;for(int k=0;k<layers;k++){mean[k]+=a[z,x,k];if(a[z,x,k]>a[z,x,max])max=k;}
      dom[max]++;
     }
     int npx=(t.alphamapHeight/4)*(sz/4);
     lines.Add(n+" "+pair.label+" asset="+AssetDatabase.GetAssetPath(t)+
      " layers="+layers+" px="+sz+" total="+npx);
     for(int k=0;k<layers;k++){
      var l=t.terrainLayers[k];
      lines.Add("  "+k+" "+(l?l.name:"NULL")+" layerAsset="+AssetDatabase.GetAssetPath(l)+
       " diffuse="+(l&&l.diffuseTexture?AssetDatabase.GetAssetPath(l.diffuseTexture):"NONE")+
       " meanWeight="+(mean[k]/npx).ToString("F4")+" dominantPx="+dom[k]);
     }
    }
   }
  }catch(Exception e){lines.Add("FAIL "+e);}
  File.WriteAllLines(V+"north_green_regression_audit_20260925.txt",lines);
  Debug.Log("NORTH_GREEN_REGRESSION_AUDIT "+lines[0]);
 }
}

// post repair color QA reload
