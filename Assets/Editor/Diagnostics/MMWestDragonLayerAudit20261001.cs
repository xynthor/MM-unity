using System;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
public static class MMWestDragonLayerAudit20261001
{
 public static void Run(){
  var s=EditorSceneManager.OpenScene("Assets/Scenes/World/Enroth.unity",OpenSceneMode.Single);
  string[] names={"Paradise Valley West - LINKED EXTENSION","Dragon Isle South - LINKED REFERENCE"};
  var all=s.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<Transform>(true)).ToArray();
  var rows=new System.Collections.Generic.List<string>();
  foreach(var n in names){
   var r=all.Single(t=>t.name==n);var t=r.GetComponentInChildren<Terrain>(true);var td=t.terrainData;
   rows.Add("=== "+n+" ===");rows.Add("terrain="+td.name+" asset="+AssetDatabase.GetAssetPath(td)+" alpha="+td.alphamapWidth+"x"+td.alphamapHeight+" layers="+td.alphamapLayers);
   var a=td.GetAlphamaps(0,0,td.alphamapWidth,td.alphamapHeight);
   for(int i=0;i<td.terrainLayers.Length;i++){
    var l=td.terrainLayers[i];double sum=0;float mx=0;int dom=0;
    for(int y=0;y<td.alphamapHeight;y++)for(int x=0;x<td.alphamapWidth;x++){float v=a[y,x,i];sum+=v;mx=Mathf.Max(mx,v);int b=0;for(int k=1;k<td.alphamapLayers;k++)if(a[y,x,k]>a[y,x,b])b=k;if(b==i)dom++;}
    rows.Add(i+" name="+(l?l.name:"NULL")+" asset="+AssetDatabase.GetAssetPath(l)+" diffuse="+(l&&l.diffuseTexture?AssetDatabase.GetAssetPath(l.diffuseTexture):"NULL")+" tile="+(l?l.tileSize.ToString():"")+" mean="+(sum/(td.alphamapWidth*td.alphamapHeight)).ToString("F4")+" max="+mx.ToString("F3")+" dominant="+dom);
   }
  }
  Directory.CreateDirectory("Validation/EdgeGrid20260923/WestDragon20261001");File.WriteAllLines("Validation/EdgeGrid20260923/WestDragon20261001/layer_audit.txt",rows);Debug.Log(string.Join("\n",rows));
 }
}