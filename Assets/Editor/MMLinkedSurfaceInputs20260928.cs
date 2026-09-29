using UnityEngine;
using UnityEditor;
using UnityEngine.SceneManagement;
using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;

// Reproducible final linked-world surface pass. Imported and upstream generated
// TerrainData and TerrainLayers are read only. Every visible tile gets one clone.
public static class MMLinkedSurfaceInputs20260928 {
 const string Root="Assets/World/WorldExtensions/Generated/LinkedSurfaceInputs";
 static readonly Dictionary<string,string> Sources=new Dictionary<string,string>();
 static void Folder(string p){if(AssetDatabase.IsValidFolder(p))return;int n=p.LastIndexOf('/');Folder(p.Substring(0,n));AssetDatabase.CreateFolder(p.Substring(0,n),p.Substring(n+1));}
 static Texture2D Opaque(Texture2D source){
  if(!source)return null;
  string path=AssetDatabase.GetAssetPath(source);
  var imp=AssetImporter.GetAtPath(path) as TextureImporter;
  // Unity sample CH maps pack height in alpha. That channel is not a
  // smoothness map and must not enter the terrain Standard shader as one.
  if(!path.EndsWith("_CH.png",StringComparison.OrdinalIgnoreCase)||imp==null||!imp.DoesSourceTextureHaveAlpha())return source;
  string dest=Root+"/Textures/"+source.name+"_Opaque.png";
  if(!AssetDatabase.LoadAssetAtPath<Texture2D>(dest)){
   if(!AssetDatabase.CopyAsset(path,dest))throw new Exception("Cannot clone "+path);
   var ci=(TextureImporter)AssetImporter.GetAtPath(dest);ci.alphaSource=TextureImporterAlphaSource.None;ci.SaveAndReimport();
  }
  return AssetDatabase.LoadAssetAtPath<Texture2D>(dest);
 }
 static TerrainLayer Layer(TerrainLayer src){
  string guid=AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(src));
  string path=Root+"/Layers/"+guid+".terrainlayer";
  var dst=AssetDatabase.LoadAssetAtPath<TerrainLayer>(path);
  if(!dst){dst=new TerrainLayer();AssetDatabase.CreateAsset(dst,path);}
  EditorUtility.CopySerialized(src,dst);dst.name=src.name;
  dst.diffuseTexture=Opaque(src.diffuseTexture);
  if(src.diffuseTexture&&AssetDatabase.GetAssetPath(src.diffuseTexture).EndsWith("dry_soil_CH.png"))
   dst.normalMapTexture=AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/EnvironmentAssets/UnitySamples/dry_soil_NOH.png");
  EditorUtility.SetDirty(dst);return dst;
 }
 static float[,,] Normalize(float[,,] a){
  int n=a.GetLength(2);var fractions=new float[n];var bytes=new int[n];
  for(int z=0;z<a.GetLength(0);z++)for(int x=0;x<a.GetLength(1);x++){
   float sum=0;for(int k=0;k<n;k++)sum+=a[z,x,k];
   if(float.IsNaN(sum)||sum<=0)throw new Exception("Invalid material weight total");
   int used=0;
   for(int k=0;k<n;k++){float f=255*a[z,x,k]/sum;bytes[k]=Mathf.FloorToInt(f);fractions[k]=f-bytes[k];used+=bytes[k];}
   for(int q=used;q<255;q++){int best=0;for(int k=1;k<n;k++)if(fractions[k]>fractions[best])best=k;bytes[best]++;fractions[best]=-1;}
   for(int k=0;k<n;k++)a[z,x,k]=bytes[k]/255f;
  }
  return a;
 }
 public static void Apply(Scene scene){
  if(File.Exists("Validation/EdgeGrid20260923/MANUAL_LINKED_WORLD_PROTECTED.txt"))throw new Exception("Current linked terrain contains protected manual corrections. Regeneration from upstream sources is blocked; normalize current weights in place instead.");
  if(scene.path!="Assets/Scenes/World/Enroth.unity"&&scene.name!="Enroth")throw new Exception("Linked world only");
  if(!File.Exists("Backups/BeforeLinkedSurfaceInputs_20260928_0010/manifest.json"))throw new Exception("Surface rollback missing");
  Folder(Root+"/Terrain");Folder(Root+"/Layers");Folder(Root+"/Textures");
  string mapPath="Validation/EdgeGrid20260923/Takeover_SurfaceSources.tsv";
  if(File.Exists(mapPath))foreach(var line in File.ReadAllLines(mapPath)){var f=line.Split('\t');if(f.Length==2)Sources[f[0]]=f[1];}
  int count=0;float heightError=0;var report=new List<string>();
  foreach(var t in scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<Terrain>(true))){
   var source=t.terrainData;string sourcePath=AssetDatabase.GetAssetPath(source);
   if(sourcePath.StartsWith(Root+"/Terrain/")){
    string original;if(!Sources.TryGetValue(sourcePath,out original))throw new Exception("Missing immutable source mapping: "+sourcePath);
    sourcePath=original;source=AssetDatabase.LoadAssetAtPath<TerrainData>(original);
   }
   string target=Root+"/Terrain/"+AssetDatabase.AssetPathToGUID(sourcePath)+".asset";
   var dst=AssetDatabase.LoadAssetAtPath<TerrainData>(target);
   if(!dst){dst=UnityEngine.Object.Instantiate(source);AssetDatabase.CreateAsset(dst,target);}
   else if(dst.heightmapResolution!=source.heightmapResolution||dst.alphamapResolution!=source.alphamapResolution||dst.detailResolution!=source.detailResolution)
    throw new Exception("Source terrain resolution changed; explicitly rebuild its linked copy: "+sourcePath);
   dst.name=source.name;
   dst.size=source.size;dst.treePrototypes=source.treePrototypes;dst.treeInstances=source.treeInstances;
   dst.detailPrototypes=source.detailPrototypes;
   if(source.detailResolution>0)
    for(int layer=0;layer<source.detailPrototypes.Length;layer++)dst.SetDetailLayer(0,0,layer,source.GetDetailLayer(0,0,source.detailWidth,source.detailHeight,layer));
   dst.SetHoles(0,0,source.GetHoles(0,0,source.holesResolution,source.holesResolution));
   var heights=source.GetHeights(0,0,source.heightmapResolution,source.heightmapResolution);
   dst.SetHeights(0,0,heights);
   dst.terrainLayers=source.terrainLayers.Select(Layer).ToArray();
   dst.SetAlphamaps(0,0,Normalize(source.GetAlphamaps(0,0,source.alphamapWidth,source.alphamapHeight)));
   dst.SyncTexture(TerrainData.AlphamapTextureName);
   foreach(var control in dst.alphamapTextures)EditorUtility.SetDirty(control);
   var verify=dst.GetHeights(0,0,dst.heightmapResolution,dst.heightmapResolution);
   for(int z=0;z<heights.GetLength(0);z++)for(int x=0;x<heights.GetLength(1);x++)heightError=Mathf.Max(heightError,Mathf.Abs(heights[z,x]-verify[z,x])*source.size.y);
   t.terrainData=dst;var collider=t.GetComponent<TerrainCollider>();if(collider)collider.terrainData=dst;
   EditorUtility.SetDirty(dst);EditorUtility.SetDirty(t);if(collider)EditorUtility.SetDirty(collider);
   dst.SetBaseMapDirty();t.Flush();Sources[target]=sourcePath;report.Add(target+"\t"+sourcePath);count++;
  }
  if(count!=30||heightError>.00001f)throw new Exception("Surface clone geometry mismatch "+count+" / "+heightError);
  File.WriteAllLines(mapPath,report);AssetDatabase.SaveAssets();
  Debug.Log("LINKED_SURFACE_INPUTS tiles="+count+" maxHeightError="+heightError+" canonicalWrites=0 packedHeightAlphaRemoved=true soilNormalMatched=true");
 }
}
