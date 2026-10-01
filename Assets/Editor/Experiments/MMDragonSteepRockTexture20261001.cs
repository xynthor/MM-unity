using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;

public static class MMDragonSteepRockTexture20261001
{
 const string ScenePath="Assets/Scenes/World/Enroth.unity";
 const string TerrainPath="Assets/World/WorldExtensions/Generated/LinkedSurfaceInputs/Terrain/15adc642c6365764291071478f2e9405.asset";
 const string Backup="Backups/BeforeDragonSteepRockTexture_20261001/manifest.json";
 const string BaselineSha="e051fb855b0e407d3113906c2eeac2c0f7f7d49b9e6305df00e24fb40f28b16b";
 static string Sha(string p){using(var h=SHA256.Create())return BitConverter.ToString(h.ComputeHash(File.ReadAllBytes(p))).Replace("-","").ToLowerInvariant();}
 static float S(float a,float b,float x){float t=Mathf.Clamp01((x-a)/(b-a));return t*t*(3f-2f*t);}
 public static void Run(){
  if(!File.Exists(Backup))throw new Exception("Backup manifest missing");
  if(Sha(TerrainPath)!=BaselineSha)throw new Exception("Dragon South TerrainData changed since backup");
  var sc=EditorSceneManager.OpenScene(ScenePath,OpenSceneMode.Single);
  var root=sc.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<Transform>(true)).Single(t=>t.name=="Dragon Isle South - LINKED REFERENCE");
  var t=root.GetComponentInChildren<Terrain>(true);var td=t.terrainData;if(AssetDatabase.GetAssetPath(td)!=TerrainPath||td.alphamapLayers!=14)throw new Exception("Unexpected Dragon South terrain");
  int rock=Array.FindIndex(td.terrainLayers,x=>x&&x.name=="Trace_Rock");
  int sand=Array.FindIndex(td.terrainLayers,x=>x&&x.diffuseTexture&&AssetDatabase.GetAssetPath(x.diffuseTexture).EndsWith("/sand.png",StringComparison.OrdinalIgnoreCase));
  if(rock!=3||sand!=1)throw new Exception($"Unexpected rock/sand slots rock={rock} sand={sand}");
  var a=td.GetAlphamaps(0,0,512,512);int touched=0,domBefore=0,domAfter=0;double moved=0;float borderDelta=0;
  var beforeRock=new float[512,512];for(int y=0;y<512;y++)for(int x=0;x<512;x++)beforeRock[y,x]=a[y,x,rock];
  for(int y=0;y<512;y++)for(int x=0;x<512;x++){
    float nx=(x+.5f)/512f,nz=(y+.5f)/512f;float edge=Mathf.Min(Mathf.Min(nx,1f-nx),Mathf.Min(nz,1f-nz))*512f;if(edge<12f)continue;
    float h=t.transform.position.y+td.GetInterpolatedHeight(nx,nz);if(h<.45f)continue;
    float slope=td.GetSteepness(nx,nz);if(slope<30f)continue;
    int best=0;for(int k=1;k<td.alphamapLayers;k++)if(a[y,x,k]>a[y,x,best])best=k;if(best==rock)domBefore++;
    float q=S(28f,58f,slope);float desired=Mathf.Max(a[y,x,rock],Mathf.Lerp(.34f,.62f,q));
    float add=Mathf.Min(a[y,x,sand],Mathf.Max(0f,desired-a[y,x,rock])*.82f);
    if(add<.0005f)continue;
    a[y,x,sand]-=add;a[y,x,rock]+=add;touched++;moved+=add;
    best=0;for(int k=1;k<td.alphamapLayers;k++)if(a[y,x,k]>a[y,x,best])best=k;if(best==rock)domAfter++;
  }
  for(int y=0;y<512;y++)for(int x=0;x<512;x++){float edge=Mathf.Min(Mathf.Min((x+.5f),511.5f-x),Mathf.Min((y+.5f),511.5f-y));if(edge<12f)borderDelta=Mathf.Max(borderDelta,Mathf.Abs(a[y,x,rock]-beforeRock[y,x]));}
  if(touched<3500||moved<500||borderDelta>1e-7)throw new Exception($"Guard failed touched={touched} moved={moved} borderDelta={borderDelta}");
  td.SetAlphamaps(0,0,a);EditorUtility.SetDirty(td);AssetDatabase.SaveAssets();
  Directory.CreateDirectory("Validation/EdgeGrid20260923/WestDragon20261001");
  string report=$"PASS steepRockPixels={touched} sandToRockAlpha={moved:F1} dominantRockSamplesBefore={domBefore} dominantRockSamplesAfter={domAfter} borderRockDelta={borderDelta:F8} rockLayer={rock} sandLayer={sand} heightWrites=0 terrainLayerWrites=0 sceneWrites=0";
  File.WriteAllText("Validation/EdgeGrid20260923/WestDragon20261001/dragon_steep_rock_texture.txt",report+"\n");Debug.Log(report);
 }
}