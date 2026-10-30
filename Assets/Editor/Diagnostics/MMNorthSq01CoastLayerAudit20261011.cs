using System;using System.IO;using System.Linq;using System.Collections.Generic;using UnityEngine;using UnityEditor;using UnityEditor.SceneManagement;
public static class MMNorthSq01CoastLayerAudit20261011
{
 public static void Run(){
  var scene=EditorSceneManager.OpenScene("Assets/Scenes/World/Enroth.unity",OpenSceneMode.Single);
  if(scene.isDirty)throw new Exception("Expected clean saved scene");
  var lines=new List<string>();var ts=new Terrain[2];var maps=new float[2][,,];
  for(int i=0;i<2;i++){
   float px=-1280+512*i;
   ts[i]=Terrain.activeTerrains.Single(t=>Mathf.Abs(t.transform.position.x-px)<.01f&&Mathf.Abs(t.transform.position.z-768)<.01f);
   var t=ts[i];var d=t.terrainData;
   lines.Add("SQ"+i+" terrain="+t.name+" source="+AssetDatabase.GetAssetPath(d)+" material="+(t.materialTemplate?t.materialTemplate.name:"none")+
    " materialShader="+(t.materialTemplate&&t.materialTemplate.shader?t.materialTemplate.shader.name:"builtin")+" alphamap="+d.alphamapWidth+"x"+d.alphamapHeight+
    " layers="+d.terrainLayers.Length);
   foreach(var l in d.terrainLayers.Select((layer,k)=>new {layer,k})){
    string name=l.layer?l.layer.name:"NULL",path=l.layer?AssetDatabase.GetAssetPath(l.layer):"";
    string texture=l.layer&&l.layer.diffuseTexture?l.layer.diffuseTexture.name:"null";
    lines.Add("SQ"+i+"_LAYER "+l.k+" name="+name+" path="+path+" diffuse="+texture+" tileSize="+(l.layer?l.layer.tileSize.ToString("F2"):"null")+" tileOffset="+(l.layer?l.layer.tileOffset.ToString("F2"):"null"));
   }
   var rock=GameObject.Find("North Alpine Rock Surface "+i+" 20261007");
   var renderer=rock?rock.GetComponent<MeshRenderer>():null;
   lines.Add("SQ"+i+"_ROCK object="+(rock?rock.name:"missing")+" mat="+(renderer&&renderer.sharedMaterial?renderer.sharedMaterial.name:"null")+
    " shader="+(renderer&&renderer.sharedMaterial&&renderer.sharedMaterial.shader?renderer.sharedMaterial.shader.name:"null")+
    " tex="+(renderer&&renderer.sharedMaterial&&renderer.sharedMaterial.mainTexture?renderer.sharedMaterial.mainTexture.name:"null")+
    " active="+(rock&&rock.activeSelf));
   if(renderer&&renderer.sharedMaterial){var mat=renderer.sharedMaterial;foreach(string property in new[]{"_Rock","_Snow","_RockNormal"}){var tex=mat.HasProperty(property)?mat.GetTexture(property):null;lines.Add("SQ"+i+"_ROCK_PROPERTY "+property+" texture="+(tex?tex.name:"null")+" scale="+(mat.HasProperty(property)?mat.GetTextureScale(property).ToString("F2"):"null")+" offset="+(mat.HasProperty(property)?mat.GetTextureOffset(property).ToString("F2"):"null"));}}
   maps[i]=d.GetAlphamaps(0,0,d.alphamapWidth,d.alphamapHeight);
  }
  foreach(int wz in new[]{1160,1172,1180,1184,1188,1192,1196,1200,1204,1210,1220,1240}){
   for(int i=0;i<2;i++){
    var d=ts[i].terrainData;int x=i==0?d.alphamapWidth-1:0;
    int z=Mathf.Clamp(Mathf.RoundToInt((wz-768f)/512f*(d.alphamapHeight-1)),0,d.alphamapHeight-1);
    var weight=new List<string>();var layerPaths=new List<string>();
    float sum=0;
    for(int k=0;k<maps[i].GetLength(2);k++){
     float v=maps[i][z,x,k];sum+=v;
     string name=k<d.terrainLayers.Length&&d.terrainLayers[k]?d.terrainLayers[k].name:"MISSING";
     if(v>.01f)weight.Add(k+"_"+name+":"+v.ToString("F3"));
    }
    lines.Add("Z="+wz+" SQ"+i+"_WEST_EAST_EDGE_ALPHA x="+x+" z="+z+" sum="+sum.ToString("F4")+" "+string.Join(";",weight));
    Vector3 n=d.GetInterpolatedNormal(i==0?1f:0f,(wz-768f)/512f);
    lines.Add("Z="+wz+" SQ"+i+"_TERRAIN_NORMAL="+n.ToString("F4"));
   }
  }
  Directory.CreateDirectory("Validation/NorthSq01CoastMaterialSeam20261011");
  File.WriteAllLines("Validation/NorthSq01CoastMaterialSeam20261011/audit.txt",lines);
  Debug.Log("SQ01_LAYER_AUDIT_DONE lines="+lines.Count+" sceneDirty="+scene.isDirty);
 }
}
