using UnityEngine;
using UnityEditor;
using UnityEngine.SceneManagement;
using UnityEngine.Experimental.Rendering;
using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;

public static class MMLinkedFoliageCoverage20260928 {
 const string Root="Assets/World/WorldExtensions/Generated/LinkedFoliageCoverage";
 static void Folder(string path){if(AssetDatabase.IsValidFolder(path))return;int n=path.LastIndexOf('/');Folder(path.Substring(0,n));AssetDatabase.CreateFolder(path.Substring(0,n),path.Substring(n+1));}
 static Texture2D Texture(Texture2D source,float cutoff){
  string path=Root+"/Textures/"+AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(source))+"_"+Mathf.RoundToInt(cutoff*1000)+".png";
  var ready=AssetDatabase.LoadAssetAtPath<Texture2D>(path);if(ready)return ready;
  var rt=RenderTexture.GetTemporary(source.width,source.height,0,RenderTextureFormat.ARGB32,RenderTextureReadWrite.Linear);
  var prior=RenderTexture.active;
  var image=new Texture2D(source.width,source.height,TextureFormat.RGBA32,false,true);
  try {
   Graphics.Blit(source,rt);RenderTexture.active=rt;
   image.ReadPixels(new Rect(0,0,source.width,source.height),0,0);image.Apply();
   File.WriteAllBytes(path,image.EncodeToPNG());
  } finally {RenderTexture.active=prior;RenderTexture.ReleaseTemporary(rt);UnityEngine.Object.DestroyImmediate(image);}
  AssetDatabase.ImportAsset(path,ImportAssetOptions.ForceSynchronousImport);
  var imp=(TextureImporter)AssetImporter.GetAtPath(path);
  imp.textureType=TextureImporterType.Default;imp.alphaSource=TextureImporterAlphaSource.FromInput;
  imp.alphaIsTransparency=true;imp.mipmapEnabled=true;imp.mipMapsPreserveCoverage=true;
  imp.alphaTestReferenceValue=cutoff;imp.sRGBTexture=AssetDatabase.GetAssetPath(source).ToLowerInvariant().Contains(".srgb.")||GraphicsFormatUtility.IsSRGBFormat(source.graphicsFormat);
  imp.maxTextureSize=Mathf.Max(source.width,source.height);imp.textureCompression=TextureImporterCompression.CompressedHQ;
  imp.anisoLevel=4;imp.SaveAndReimport();
  return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
 }
 public static void Apply(Scene scene){
  if(scene.name!="Enroth_Linked_OpenWorld")throw new Exception("Linked scene required");
  if(!File.Exists("Backups/BeforeLinkedRuntimeAndFoliage_20260928/manifest.json"))throw new Exception("Foliage rollback missing");
  Folder(Root+"/Textures");Folder(Root+"/Materials");
  var cache=new Dictionary<Material,Material>();var groups=new HashSet<LODGroup>();int renderers=0;
  foreach(var renderer in scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<MeshRenderer>(true))){
   var mats=renderer.sharedMaterials;bool changed=false;
   for(int i=0;i<mats.Length;i++){
    var src=mats[i];if(!src||!src.HasProperty("_Mode")||src.GetFloat("_Mode")!=1||!(src.mainTexture is Texture2D))continue;
    string mp=AssetDatabase.GetAssetPath(src),tp=AssetDatabase.GetAssetPath(src.mainTexture);
    string key=(mp+" "+tp).ToLowerInvariant();
    if(!(key.Contains("tree")||key.Contains("pine")||key.Contains("searsia")||key.Contains("vegetation")))continue;
    if(mp.StartsWith(Root)){var existingGroup=renderer.GetComponentInParent<LODGroup>();if(existingGroup)groups.Add(existingGroup);continue;}
    Material variant;
    if(!cache.TryGetValue(src,out variant)){
     string path=Root+"/Materials/"+AssetDatabase.AssetPathToGUID(mp)+".mat";
     variant=AssetDatabase.LoadAssetAtPath<Material>(path);
     if(!variant){variant=new Material(src);AssetDatabase.CreateAsset(variant,path);}else EditorUtility.CopySerialized(src,variant);
     variant.name=src.name+" Coverage";variant.mainTexture=Texture((Texture2D)src.mainTexture,src.GetFloat("_Cutoff"));
     variant.enableInstancing=true;EditorUtility.SetDirty(variant);cache.Add(src,variant);
    }
    mats[i]=variant;changed=true;
   }
   if(changed){renderer.sharedMaterials=mats;EditorUtility.SetDirty(renderer);renderers++;var group=renderer.GetComponentInParent<LODGroup>();if(group)groups.Add(group);}
  }
  int lods=0;
  foreach(var group in groups){var levels=group.GetLODs();if(levels.Length<2)continue;int last=levels.Length-1;
   if(levels[last].screenRelativeTransitionHeight>.004f){levels[last].screenRelativeTransitionHeight=.004f;group.SetLODs(levels);EditorUtility.SetDirty(group);lods++;}}
  AssetDatabase.SaveAssets();UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(scene);
  File.WriteAllText("Validation/EdgeGrid20260923/Takeover_FoliageCoverage_Result.txt","renderers="+renderers+" materialVariants="+cache.Count+" extendedBillboardGroups="+lods+" placementWrites=0 canonicalWrites=0\n");
  Debug.Log("LINKED_FOLIAGE_COVERAGE renderers="+renderers+" materials="+cache.Count+" lodGroups="+lods);
 }
}
