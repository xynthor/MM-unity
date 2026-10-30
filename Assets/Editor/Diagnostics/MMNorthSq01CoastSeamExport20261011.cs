using System;using System.IO;using System.Linq;using UnityEngine;using UnityEditor;using UnityEditor.SceneManagement;
public static class MMNorthSq01CoastSeamExport20261011{
 public static void Run(){
  var sc=EditorSceneManager.OpenScene("Assets/Scenes/World/Enroth.unity",OpenSceneMode.Single);
  if(sc.isDirty)throw new Exception("Original scene not clean");
  string dir="Validation/NorthSq01CoastSeam20261011";Directory.CreateDirectory(dir);
  string text="";
  foreach(int k in new[]{0,1}){
   float X=-1280+512*k;
   var t=Terrain.activeTerrains.Single(q=>Mathf.Abs(q.transform.position.x-X)<.01f&&Mathf.Abs(q.transform.position.z-768)<.01f);
   var raw=t.terrainData.GetHeights(0,0,513,513);var y=new float[513,513];
   for(int z=0;z<=512;z++)for(int x=0;x<=512;x++)y[z,x]=t.transform.position.y+raw[z,x]*t.terrainData.size.y;
   var bytes=new byte[513*513*4];Buffer.BlockCopy(y,0,bytes,0,bytes.Length);File.WriteAllBytes(dir+"/sq"+k+"_current.f32",bytes);
   text+="SQ"+k+" TerrainData="+AssetDatabase.GetAssetPath(t.terrainData)+" origin="+t.transform.position+" ysize="+t.terrainData.size.y+"\n";
   foreach(int x in k==0?new[]{448,468,488,504,508,512}:new[]{0,4,8,20,32,48,64,88}){
    text+="tile="+k+" x="+x+" worldX="+(t.transform.position.x+x)+" h(z1184,1188,1192,1196,1200)="+
    string.Join(",",new[]{1184,1188,1192,1196,1200}.Select(wz=>y[wz-768,x].ToString("F2")))+"\n";
   }
  }
  File.WriteAllText(dir+"/audit.txt",text);Debug.Log("SQ01_COAST_EXPORTED "+text.Replace("\n",";"));
 }
}
