using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;

public static class MMParadiseFreeHavenGreen20261001
{
 const string ScenePath="Assets/Scenes/World/Enroth.unity";
 const string Original="Assets/World/WorldExtensions/Generated/LinkedSurfaceInputs/Terrain/18ab3d932291f3d48ab29d6192d05cc0.asset";
 const string Root="Assets/World/WorldExtensions/Generated/ParadiseGreen20261001";
 const string Out=Root+"/ParadiseWest_Terrain.asset";
 const string V="Validation/EdgeGrid20260923/";
 static float S(float a,float b,float x){float t=Mathf.Clamp01((x-a)/Mathf.Max(.001f,b-a));return t*t*(3f-2f*t);}
 static Color32[] Img(string p){var t=new Texture2D(2,2,TextureFormat.RGBA32,false,true);if(!t.LoadImage(File.ReadAllBytes(p))||t.width!=513||t.height!=513)throw new Exception("Bad image "+p);var a=t.GetPixels32();UnityEngine.Object.DestroyImmediate(t);return a;}
 static int Find(TerrainData d,string contains){return Array.FindIndex(d.terrainLayers,l=>l&&l.name.IndexOf(contains,StringComparison.OrdinalIgnoreCase)>=0);}
 static void Norm(float[,,]a,int z,int x,int L){float s=0;for(int k=0;k<L;k++)s+=a[z,x,k];if(s<.00001f)return;for(int k=0;k<L;k++)a[z,x,k]/=s;}
 public static void Run(){
  var s=EditorSceneManager.OpenScene(ScenePath,OpenSceneMode.Single);
  var parent=s.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<Transform>(true)).FirstOrDefault(t=>t.name=="Paradise Valley West - LINKED EXTENSION");
  if(!parent)throw new Exception("Paradise West parent missing");
  var terr=parent.GetComponentInChildren<Terrain>(true);if(!terr)throw new Exception("Paradise West terrain missing");
  var src=AssetDatabase.LoadAssetAtPath<TerrainData>(Original);if(!src)throw new Exception("Original Paradise West missing");
  if(!AssetDatabase.IsValidFolder(Root))AssetDatabase.CreateFolder("Assets/World/WorldExtensions/Generated","ParadiseGreen20261001");
  var d=AssetDatabase.LoadAssetAtPath<TerrainData>(Out);
  if(!d){d=UnityEngine.Object.Instantiate(src);d.name=src.name;AssetDatabase.CreateAsset(d,Out);}
  if(d.heightmapResolution!=src.heightmapResolution||d.alphamapResolution!=src.alphamapResolution||d.alphamapLayers!=src.alphamapLayers)throw new Exception("Paradise clone incompatible");
  d.SetHeights(0,0,src.GetHeights(0,0,src.heightmapResolution,src.heightmapResolution));
  d.terrainLayers=src.terrainLayers;
  d.SetAlphamaps(0,0,src.GetAlphamaps(0,0,src.alphamapWidth,src.alphamapHeight));
  var photo=Img(V+"ReferenceBiomeCandidates_20260925/ParadiseValley_West_ReferenceBiomes.png");
  var land=Img(V+"ReferenceMasks/ParadiseValley_West.png");
  var a=d.GetAlphamaps(0,0,d.alphamapWidth,d.alphamapHeight);int L=d.alphamapLayers;
  int green=0,light=1,forest=Find(d,"ReferenceWestForestInterior");
  int ochre=Array.FindIndex(d.terrainLayers,l=>l&&l.diffuseTexture&&AssetDatabase.GetAssetPath(l.diffuseTexture).IndexOf("ReferenceWestOchreCape",StringComparison.OrdinalIgnoreCase)>=0);
  if(green<0||light<0||forest<0||ochre<0)throw new Exception($"Paradise expected layers missing green={green} light={light} forest={forest} ochre={ochre}");
  int touched=0,strong=0;double beforeGreen=0,afterGreen=0,beforeOchre=0,afterOchre=0;
  int W=d.alphamapWidth,H=d.alphamapHeight;
  for(int z=0;z<H;z++)for(int x=0;x<W;x++){
   float landW=S(142f,174f,land[z*513+x].r);if(landW<.02f)continue;
   var p=photo[z*513+x];float f=p.g/255f;float canopy=S(.11f,.56f,f);if(canopy<.015f)continue;
   float och=a[z,x,ochre];float reserved=och;
   // Preserve the distinct ochre cape wherever it already has authority.
   float capeProtection=S(.08f,.34f,och);
   float influence=.92f*canopy*landW*(1f-.92f*capeProtection);if(influence<.005f)continue;
   float oldG=a[z,x,green]+a[z,x,light]+a[z,x,forest];
   beforeGreen+=oldG;beforeOchre+=och;
   float available=1f-reserved;
   float targetGreenTotal=available*Mathf.Lerp(.55f,.84f,canopy);
   float currentOther=available-oldG;
   float targetOther=Mathf.Max(0f,available-targetGreenTotal);
   // Retain the existing non-green biome proportions, but compress them to make
   // the green areas read like Free Haven. Free Haven's green split is ~22/78.
   if(currentOther>.00001f){
    float scale=targetOther/currentOther;
    for(int k=0;k<L;k++)if(k!=green&&k!=light&&k!=forest&&k!=ochre)a[z,x,k]*=Mathf.Lerp(1f,scale,influence);
   }
   float tg=targetGreenTotal*.22f,tl=targetGreenTotal*.78f;
   a[z,x,green]=Mathf.Lerp(a[z,x,green],tg,influence);
   a[z,x,light]=Mathf.Lerp(a[z,x,light],tl,influence);
   a[z,x,forest]=Mathf.Lerp(a[z,x,forest],0f,influence*.88f);
   a[z,x,ochre]=och;
   Norm(a,z,x,L);
   afterGreen+=a[z,x,green]+a[z,x,light]+a[z,x,forest];afterOchre+=a[z,x,ochre];
   touched++;if(canopy>.72f)strong++;
  }
  if(touched<12000)throw new Exception("Too little Paradise green reference coverage "+touched);
  d.SetAlphamaps(0,0,a);d.SetBaseMapDirty();EditorUtility.SetDirty(d);
  terr.terrainData=d;var col=terr.GetComponent<TerrainCollider>();if(col)col.terrainData=d;
  EditorSceneManager.MarkSceneDirty(s);if(!EditorSceneManager.SaveScene(s))throw new IOException("Could not save Paradise green scene");
  AssetDatabase.SaveAssets();
  Directory.CreateDirectory(V+"ParadiseGreen20261001");
  File.WriteAllText(V+"ParadiseGreen20261001/apply.txt",$"PASS touched={touched} strongForest={strong} meanGreen={beforeGreen/touched:F3}->{afterGreen/touched:F3} meanOchre={beforeOchre/touched:F3}->{afterOchre/touched:F3} heightWrites=cloneRestoreOnly coastlineWrites=0\n");
  Debug.Log("PARADISE_FREEHAVEN_GREEN_DONE touched="+touched);
 }
}