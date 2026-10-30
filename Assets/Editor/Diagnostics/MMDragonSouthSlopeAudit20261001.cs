using System;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
public static class MMDragonSouthSlopeAudit20261001
{
 public static void Run(){
  var s=EditorSceneManager.OpenScene("Assets/Scenes/World/Enroth.unity",OpenSceneMode.Single);
  var r=s.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<Transform>(true)).Single(t=>t.name=="Dragon Isle South - LINKED REFERENCE");
  var t=r.GetComponentInChildren<Terrain>(true);var td=t.terrainData;var a=td.GetAlphamaps(0,0,td.alphamapWidth,td.alphamapHeight);
  int rock=Array.FindIndex(td.terrainLayers,x=>x&&x.name=="Trace_Rock");if(rock<0)throw new Exception("Trace_Rock missing");
  int sand=Array.FindIndex(td.terrainLayers,x=>x&&x.diffuseTexture&&AssetDatabase.GetAssetPath(x.diffuseTexture).EndsWith("/sand.png",StringComparison.OrdinalIgnoreCase));if(sand<0)throw new Exception("sand missing");
  var rows=new System.Collections.Generic.List<string>();
  foreach(var bin in new[]{new Vector2(10,20),new Vector2(20,30),new Vector2(30,45),new Vector2(45,90)}){
   long n=0,domRock=0;double rw=0,sw=0;float minH=999,maxH=-999;
   for(int y=0;y<td.alphamapHeight;y++)for(int x=0;x<td.alphamapWidth;x++){
    float nx=(x+.5f)/td.alphamapWidth,nz=(y+.5f)/td.alphamapHeight;float slope=td.GetSteepness(nx,nz);if(slope<bin.x||slope>=bin.y)continue;
    float h=t.transform.position.y+td.GetInterpolatedHeight(nx,nz);if(h<.25f)continue;n++;rw+=a[y,x,rock];sw+=a[y,x,sand];minH=Mathf.Min(minH,h);maxH=Mathf.Max(maxH,h);
    int b=0;for(int k=1;k<td.alphamapLayers;k++)if(a[y,x,k]>a[y,x,b])b=k;if(b==rock)domRock++;
   }
   rows.Add($"slope={bin.x:F0}-{bin.y:F0} n={n} meanRock={(n>0?rw/n:0):F3} meanSand={(n>0?sw/n:0):F3} dominantRock={domRock} heightRange={minH:F2}..{maxH:F2}");
  }
  Directory.CreateDirectory("Validation/EdgeGrid20260923/WestDragon20261001");File.WriteAllLines("Validation/EdgeGrid20260923/WestDragon20261001/dragon_slope_audit.txt",rows);Debug.Log(string.Join("\n",rows));
 }
}