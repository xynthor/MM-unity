using UnityEngine;
using UnityEditor;
using System;
using System.IO;
using System.Collections.Generic;
[InitializeOnLoad]
internal static class MMNorthBoundaryDerivativeAudit20260926 {
 const string V="Validation/EdgeGrid20260923/";
 static readonly string[] N={"SweetWater","Kriegspire","FrozenHighlands","SilverCove"};
 static MMNorthBoundaryDerivativeAudit20260926(){EditorApplication.delayCall+=Run;}
 static void Run(){
  string flag=V+"RUN_NORTH_BOUNDARY_DERIVATIVE_AUDIT.flag";
  if(!File.Exists(flag))return;
  File.Delete(flag);
  var lines=new List<string>();
  try{
   foreach(var n in N){
    var s=AssetDatabase.LoadAssetAtPath<TerrainData>(
      "Assets/World/WorldExtensions/Generated/LinkedSourceTransitions/"+n+"_LinkedNorthProfile.asset");
    var e=AssetDatabase.LoadAssetAtPath<TerrainData>(
      "Assets/World/WorldExtensions/Generated/"+n+"_NorthTerrain.asset");
    if(!s||!e)throw new Exception("Missing "+n);
    var sa=s.GetAlphamaps(0,509,512,3);
    var ea=e.GetAlphamaps(0,0,512,3);
    double meanValueGap=0,meanDerivJump=0;float maxValueGap=0,maxDerivJump=0;
    float dryMaxValueGap=0,dryMaxDeriv=0;int dryWorstX=-1,dryWorstK=-1,dryCount=0;
    int count=0;
    for(int x=0;x<512;x++)for(int k=0;k<s.alphamapLayers;k++){
     float valueGap=Mathf.Abs(sa[2,x,k]-ea[0,x,k]);
     float ds=sa[2,x,k]-sa[1,x,k];
     float de=ea[1,x,k]-ea[0,x,k];
     float dj=Mathf.Abs(ds-de);
     meanValueGap+=valueGap;meanDerivJump+=dj;
     maxValueGap=Mathf.Max(maxValueGap,valueGap);
     maxDerivJump=Mathf.Max(maxDerivJump,dj);count++;
     float ys=-24f+s.GetHeight(x,512),yn=-24f+e.GetHeight(x,0);
     if(ys>.15f&&yn>.15f){
      dryCount++;
      if(valueGap>dryMaxValueGap){dryMaxValueGap=valueGap;dryWorstX=x;dryWorstK=k;}
      dryMaxDeriv=Mathf.Max(dryMaxDeriv,dj);
     }
    }
    var sh=s.GetHeights(0,510,513,3);var eh=e.GetHeights(0,0,513,3);
    double hmean=0;float hmax=0;int hc=0;
    for(int x=0;x<513;x++){
     float ds=(sh[2,x]-sh[1,x])*320f;
     float de=(eh[1,x]-eh[0,x])*320f;
     float dj=Mathf.Abs(ds-de);hmean+=dj;hmax=Mathf.Max(hmax,dj);hc++;
    }
    lines.Add(n+" alphaValueGapMax="+maxValueGap.ToString("F6")+
      " alphaValueGapMean="+(meanValueGap/count).ToString("F6")+
      " alphaDerivativeJumpMax="+maxDerivJump.ToString("F6")+
      " alphaDerivativeJumpMean="+(meanDerivJump/count).ToString("F6")+
      " dryAlphaGapMax="+dryMaxValueGap.ToString("F6")+
      " dryAlphaDerivativeMax="+dryMaxDeriv.ToString("F6")+
      " dryWorstX="+dryWorstX+" dryWorstLayer="+dryWorstK+
      " heightSlopeJumpMax="+hmax.ToString("F5")+
      " heightSlopeJumpMean="+(hmean/hc).ToString("F5"));
   }
  }catch(Exception e){lines.Add("FAIL "+e);}
  File.WriteAllLines(V+"north_boundary_derivative_audit_20260926.txt",lines);
 }
}

// force audit reload
