using UnityEngine;
using UnityEditor;
using System;
using System.IO;
// Deterministically restore generated north splatmaps from the last clean
// pre-experiment extension snapshot. Heights, coastline, objects and canonical assets unchanged.
public static class MMNorthBaselineAlphaReset20260927 {
 const string V="Validation/EdgeGrid20260923/";
 const string Backup=@"C:\MMUnityPort\Backups\BeforeNorthAndWestVisualContinuity_20260926\Assets\World\WorldExtensions\Generated\";
 public static void Apply(string n,TerrainData ext){
  string raw=Backup+n+"_NorthTerrain.asset";
  if(!File.Exists(raw))throw new Exception("Missing clean north alpha baseline "+raw);
  string temp="Assets/TempNorthBaselineAlpha_"+n+"_20260927.asset";
  string full=Path.GetFullPath(temp);
  if(File.Exists(full))throw new Exception("Scratch alpha baseline collision "+temp);
  File.Copy(raw,full);
  try{
   AssetDatabase.ImportAsset(temp,ImportAssetOptions.ForceSynchronousImport);
   var b=AssetDatabase.LoadAssetAtPath<TerrainData>(temp);
   if(!b||b.alphamapResolution!=ext.alphamapResolution||
      b.alphamapLayers!=ext.alphamapLayers)
    throw new Exception(n+" incompatible clean alpha baseline");
   var a=b.GetAlphamaps(0,0,b.alphamapWidth,b.alphamapHeight);
   ext.SetAlphamaps(0,0,a);
   ext.SetBaseMapDirty();
   EditorUtility.SetDirty(ext);
   File.AppendAllText(V+"north_baseline_alpha_reset_20260927.txt",
    n+" resetPixels="+(b.alphamapWidth*b.alphamapHeight)+
    " layers="+b.alphamapLayers+
    " baseline=BeforeNorthAndWestVisualContinuity_20260926"+
    " heightWrites=0 canonicalWrites=0\n");
  }finally{
   AssetDatabase.DeleteAsset(temp);
   if(File.Exists(full))File.Delete(full);
   if(File.Exists(full+".meta"))File.Delete(full+".meta");
  }
 }
}