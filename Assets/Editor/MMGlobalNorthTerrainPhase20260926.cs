using UnityEngine;
using UnityEditor;
using System;
using System.IO;
// Give every linked-source/north-extension TerrainLayer the same GLOBAL
// world-space texture phase. Canonical source layers remain untouched.
public static class MMGlobalNorthTerrainPhase20260926 {
 const string V="Validation/EdgeGrid20260923/";
 const string Root="Assets/World/WorldExtensions/Generated/GlobalNorthPhaseLayers";
 static void Folder(){
  if(!AssetDatabase.IsValidFolder(Root))
   AssetDatabase.CreateFolder("Assets/World/WorldExtensions/Generated","GlobalNorthPhaseLayers");
 }
 static TerrainLayer Clone(string n,string row,int k,TerrainLayer src,float wx,float wz){
  Folder();
  string p=Root+"/"+n+"_"+row+"_"+k+".terrainlayer";
  var dst=AssetDatabase.LoadAssetAtPath<TerrainLayer>(p);
  if(!dst){dst=new TerrainLayer();AssetDatabase.CreateAsset(dst,p);}
  EditorUtility.CopySerialized(src,dst);dst.name=src.name;
  // Match the reference snow surface on BOTH linked-world sides of the join.
  // Canonical source TerrainLayers remain untouched; only generated phase clones change.
  var expectedDiffuse=src.diffuseTexture;
  if(src.name=="Realistic_Snow"){
   expectedDiffuse=AssetDatabase.LoadAssetAtPath<Texture2D>(
    "Assets/Materials/VisualRefinement/Snow_FineGrain.png");
   if(!expectedDiffuse)throw new Exception("Fine reference snow texture missing");
   dst.diffuseTexture=expectedDiffuse;
  }
  dst.tileOffset=new Vector2(src.tileOffset.x+Mathf.Repeat(wx,src.tileSize.x),
                            src.tileOffset.y+Mathf.Repeat(wz,src.tileSize.y));
  if(dst.diffuseTexture!=expectedDiffuse||dst.normalMapTexture!=src.normalMapTexture||
     dst.tileSize!=src.tileSize)throw new Exception(n+" phase clone altered material "+k);
  EditorUtility.SetDirty(dst);return dst;
 }
 static int Col(string n){
  if(n=="SweetWater")return 0;if(n=="Kriegspire")return 1;
  if(n=="FrozenHighlands")return 2;if(n=="SilverCove")return 3;
  throw new Exception("Unknown north phase region "+n);
 }
 public static void Apply(string n,TerrainData canonical,TerrainData linked,TerrainData north){
  if(!File.Exists(@"C:\MMUnityPort\Backups\BeforeGlobalNorthTexturePhase_20260926\manifest.json"))
   throw new Exception("Global phase backup missing");
  int col=Col(n);float wx=(col-2)*512f-256f;
  float sourceZ=256f,northZ=768f;
  var s=new TerrainLayer[canonical.terrainLayers.Length];
  var e=new TerrainLayer[canonical.terrainLayers.Length];
  for(int k=0;k<s.Length;k++){
   s[k]=Clone(n,"Source",k,canonical.terrainLayers[k],wx,sourceZ);
   e[k]=Clone(n,"North",k,canonical.terrainLayers[k],wx,northZ);
  }
  linked.terrainLayers=s;north.terrainLayers=e;
  linked.SetBaseMapDirty();north.SetBaseMapDirty();
  EditorUtility.SetDirty(linked);EditorUtility.SetDirty(north);AssetDatabase.SaveAssets();
  float err=0f;
  for(int k=0;k<s.Length;k++){
   float tx=canonical.terrainLayers[k].tileSize.x,ty=canonical.terrainLayers[k].tileSize.y;
   float srcX=Mathf.Repeat(s[k].tileOffset.x,tx);
   float expX=Mathf.Repeat(canonical.terrainLayers[k].tileOffset.x+wx,tx);
   float srcZ=Mathf.Repeat(s[k].tileOffset.y,ty);
   float expSrcZ=Mathf.Repeat(canonical.terrainLayers[k].tileOffset.y+sourceZ,ty);
   float northX=Mathf.Repeat(e[k].tileOffset.x,tx);
   float northPhaseZ=Mathf.Repeat(e[k].tileOffset.y,ty);
   float expNorthZ=Mathf.Repeat(canonical.terrainLayers[k].tileOffset.y+northZ,ty);
   err=Mathf.Max(err,Mathf.Abs(srcX-expX));
   err=Mathf.Max(err,Mathf.Abs(northX-expX));
   err=Mathf.Max(err,Mathf.Abs(srcZ-expSrcZ));
   err=Mathf.Max(err,Mathf.Abs(northPhaseZ-expNorthZ));
  }
  if(err>.001f)throw new Exception(n+" global terrain phase mismatch "+err);
  File.AppendAllText(V+"global_north_texture_phase_20260926.txt",
   n+" linkedSourceWorldOrigin=("+wx.ToString("F0")+","+sourceZ.ToString("F0")+")"+
   " extensionWorldOrigin=("+wx.ToString("F0")+","+northZ.ToString("F0")+")"+
   " layers="+s.Length+" maxPhaseErrorM="+err.ToString("F6")+
   " linkedSnowFineGrainBothRows=true otherCanonicalDiffuseNormalExact=true canonicalLayerWrites=0\n");
 }
}
