using UnityEngine;
using UnityEditor;
using System;
using System.IO;
using System.Collections.Generic;

public static class MMWorldExtensionInternalSeamLock {
 const string G="Assets/World/WorldExtensions/Generated/";
 const float H=320f;
 static float Smooth(float t){t=Mathf.Clamp01(t);return t*t*(3f-2f*t);}
 static TerrainData T(string n){
  var t=AssetDatabase.LoadAssetAtPath<TerrainData>(G+n+"Terrain.asset");
  if(!t)throw new Exception("Missing extension terrain "+n);
  return t;
 }
 static string SymmetricVertical(string left,string right,int band){
  var a=T(left);var b=T(right);var ah=a.GetHeights(0,0,513,513);var bh=b.GetHeights(0,0,513,513);
  float before=0f,after=0f,maxMove=0f;
  for(int z=0;z<513;z++){
   float av=ah[z,512],bv=bh[z,0];before=Mathf.Max(before,Mathf.Abs(av-bv)*H);
   float mid=(av+bv)*.5f,da=mid-av,db=mid-bv;
   for(int q=0;q<=band;q++){
    float f=1f-Smooth(q/(float)band);
    int ax=512-q,bx=q;
    float oa=ah[z,ax],ob=bh[z,bx];
    ah[z,ax]+=da*f;bh[z,bx]+=db*f;
    maxMove=Mathf.Max(maxMove,Mathf.Max(Mathf.Abs(ah[z,ax]-oa),Mathf.Abs(bh[z,bx]-ob))*H);
   }
   after=Mathf.Max(after,Mathf.Abs(ah[z,512]-bh[z,0])*H);
  }
  a.SetHeights(0,0,ah);b.SetHeights(0,0,bh);EditorUtility.SetDirty(a);EditorUtility.SetDirty(b);
  return left+"|"+right+",mode=symmetric,before_m="+before.ToString("F5")+",after_m="+after.ToString("F5")+",max_move_m="+maxMove.ToString("F4");
 }
 static string CopyRightToLeft(string left,string right,int band){
  var a=T(left);var b=T(right);var ah=a.GetHeights(0,0,513,513);var bh=b.GetHeights(0,0,513,513);
  float before=0f,after=0f,maxMove=0f;
  for(int z=0;z<513;z++){
   float target=bh[z,0],delta=target-ah[z,512];before=Mathf.Max(before,Mathf.Abs(delta)*H);
   for(int q=0;q<=band;q++){
    int x=512-q;float old=ah[z,x];ah[z,x]+=delta*(1f-Smooth(q/(float)band));
    maxMove=Mathf.Max(maxMove,Mathf.Abs(ah[z,x]-old)*H);
   }
   after=Mathf.Max(after,Mathf.Abs(ah[z,512]-bh[z,0])*H);
  }
  a.SetHeights(0,0,ah);EditorUtility.SetDirty(a);
  return left+"|"+right+",mode=left_only,before_m="+before.ToString("F5")+",after_m="+after.ToString("F5")+",max_move_m="+maxMove.ToString("F4");
 }
 public static void Apply(){
  var rows=new List<string>();
  rows.Add(SymmetricVertical("FrozenHighlands_North","SilverCove_North",8));
  rows.Add(CopyRightToLeft("Southwest_Ocean","HermitsIsle_South",8));
  AssetDatabase.SaveAssets();
  Directory.CreateDirectory("Validation/EdgeGrid20260923");
  File.WriteAllLines("Validation/EdgeGrid20260923/internal_extension_seam_lock.csv",rows);
  Debug.Log("INTERNAL_EXTENSION_SEAM_LOCK_DONE "+string.Join(" | ",rows));
 }
}
