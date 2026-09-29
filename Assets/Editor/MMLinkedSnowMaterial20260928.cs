using UnityEngine;
using UnityEditor;
using UnityEngine.SceneManagement;
using System;
using System.IO;
using System.Net;
using System.Linq;

// Explicit MCP entry point; CC0 source files and derived assets remain linked-only.
public static class MMLinkedSnowMaterial20260928 {
 const string Root="Assets/World/WorldExtensions/Generated/LinkedSnow02";
 public static string Manifest(){using(var c=new WebClient()){c.Headers.Add("User-Agent","MMUnityPort / Powered by Poly Haven");return c.DownloadString("https://api.polyhaven.com/files/snow_02");}}
 static void Download(string suffix){
  string name="snow_02_"+suffix+"_2k.jpg",path=Root+"/"+name;
  byte[] data;
  if(File.Exists(path))data=File.ReadAllBytes(path);
  else using(var client=new WebClient()){
   client.Headers.Add("User-Agent","MMUnityPort material importer (Powered by Poly Haven)");
   data=client.DownloadData("https://dl.polyhaven.org/file/ph-assets/Textures/jpg/2k/snow_02/"+name);
   if(data.Length<1000)throw new Exception("Invalid snow texture download");
  }
  string expected=suffix=="diff"?"5541e5601951b071691238ddc70632c0":suffix=="nor_gl"?"ef128fbdf1b31932c792eb6472c84574":"8b9787b4c7b1d7390a4b4a1ae67728b1";
  using(var hash=System.Security.Cryptography.MD5.Create())if(BitConverter.ToString(hash.ComputeHash(data)).Replace("-","").ToLowerInvariant()!=expected)throw new Exception("Official snow manifest hash mismatch: "+name);
  if(!File.Exists(path))File.WriteAllBytes(path,data);
 }
 public static void Apply(Scene scene){
  if(scene.name!="Enroth")throw new Exception("Linked world required");
  if(!AssetDatabase.IsValidFolder(Root))AssetDatabase.CreateFolder("Assets/World/WorldExtensions/Generated","LinkedSnow02");
  foreach(var suffix in new[]{"diff","nor_gl","rough"})Download(suffix);
  foreach(var suffix in new[]{"diff","nor_gl","rough"}){
   string path=Root+"/snow_02_"+suffix+"_2k.jpg";AssetDatabase.ImportAsset(path,ImportAssetOptions.ForceSynchronousImport);
   var importer=(TextureImporter)AssetImporter.GetAtPath(path);importer.maxTextureSize=2048;importer.mipmapEnabled=true;importer.anisoLevel=8;
   importer.textureType=suffix=="nor_gl"?TextureImporterType.NormalMap:TextureImporterType.Default;
   importer.sRGBTexture=suffix=="diff";importer.isReadable=suffix!="nor_gl";importer.textureCompression=TextureImporterCompression.CompressedHQ;importer.SaveAndReimport();
  }
  var diff=AssetDatabase.LoadAssetAtPath<Texture2D>(Root+"/snow_02_diff_2k.jpg");
  var rough=AssetDatabase.LoadAssetAtPath<Texture2D>(Root+"/snow_02_rough_2k.jpg");
  var normal=AssetDatabase.LoadAssetAtPath<Texture2D>(Root+"/snow_02_nor_gl_2k.jpg");
  MMSnowRepetitionBake20260928.Bake();
  string packed=Root+"/Snow02_SeamlessAlbedoSmoothness.png";
  if(!File.Exists(packed)){
   var pixels=diff.GetPixels32();var r=rough.GetPixels32();if(pixels.Length!=r.Length)throw new Exception("Snow map dimensions differ");
   for(int i=0;i<pixels.Length;i++)pixels[i].a=(byte)(255-r[i].r);
   var image=new Texture2D(diff.width,diff.height,TextureFormat.RGBA32,false);image.SetPixels32(pixels);image.Apply();File.WriteAllBytes(packed,image.EncodeToPNG());UnityEngine.Object.DestroyImmediate(image);
  }
  AssetDatabase.ImportAsset(packed,ImportAssetOptions.ForceSynchronousImport);
  var pi=(TextureImporter)AssetImporter.GetAtPath(packed);pi.sRGBTexture=true;pi.alphaSource=TextureImporterAlphaSource.FromInput;pi.alphaIsTransparency=false;pi.maxTextureSize=2048;pi.anisoLevel=8;pi.mipmapEnabled=true;pi.SaveAndReimport();
  var texture=AssetDatabase.LoadAssetAtPath<Texture2D>(packed);int layers=0;
  string normalPath=Root+"/Snow02_SeamlessNormal.png";AssetDatabase.ImportAsset(normalPath,ImportAssetOptions.ForceSynchronousImport);
  // Fixed-camera isolation showed minified normal detail produced diagonal
  // moire even with one uniform snow layer. Retain the scan and filter it.
  var ni=(TextureImporter)AssetImporter.GetAtPath(normalPath);ni.textureType=TextureImporterType.NormalMap;ni.sRGBTexture=false;ni.maxTextureSize=2048;ni.anisoLevel=8;ni.filterMode=FilterMode.Trilinear;ni.mipMapBias=1.5f;ni.SaveAndReimport();normal=AssetDatabase.LoadAssetAtPath<Texture2D>(normalPath);
  foreach(var layer in scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<Terrain>(true)).SelectMany(t=>t.terrainData.terrainLayers).Where(l=>l&&l.name=="Realistic_Snow").Distinct()){
   if(!AssetDatabase.GetAssetPath(layer).StartsWith("Assets/World/WorldExtensions/Generated/LinkedSurfaceInputs/Layers/"))throw new Exception("Refusing shared source layer");
   layer.diffuseTexture=texture;layer.normalMapTexture=normal;layer.normalScale=.4f;layer.tileSize=new Vector2(8,8);layer.tileOffset=Vector2.zero;layer.metallic=0;layer.smoothness=.08f;EditorUtility.SetDirty(layer);layers++;
  }
  foreach(var t in scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<Terrain>(true))){t.terrainData.SetBaseMapDirty();t.Flush();}
  AssetDatabase.SaveAssets();
  File.WriteAllText("Validation/EdgeGrid20260923/Takeover_Snow02_Provenance.txt","Snow 02 by Rob Tuytel / Poly Haven\nhttps://polyhaven.com/a/snow_02\nLicense CC0 https://polyhaven.com/license\nDownloaded official diffuse, OpenGL normal and roughness 2K JPG maps via Unity MCP;MD5 verified against official manifest.\nSource scale2m within8m periodic atlas;normal scale .4;albedo alpha packs 1-roughness. Deterministic coupled rotations/offsets and smooth cell blending reduce repetition.\nGenerated linked TerrainLayers only;canonical writes0;layers="+layers+"\n");
 }
}
