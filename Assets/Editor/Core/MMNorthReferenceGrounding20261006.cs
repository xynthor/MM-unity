using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;

public static class MMNorthReferenceGrounding20261006
{
 static Terrain FindTerrain(Terrain[] ts,float x,float z){return ts.FirstOrDefault(t=>x>=t.transform.position.x-.01f&&x<=t.transform.position.x+t.terrainData.size.x+.01f&&z>=t.transform.position.z-.01f&&z<=t.transform.position.z+t.terrainData.size.z+.01f);}
 static float Ground(Terrain t,float x,float z){return t.transform.position.y+t.SampleHeight(new Vector3(x,0,z));}

 [MenuItem("MMUnity/Reference 2026/Ground Northern Vegetation After Topography")]
 public static void Run(){
  var scene=EditorSceneManager.OpenScene("Assets/Scenes/World/Enroth.unity",OpenSceneMode.Single);
  var ts=Terrain.activeTerrains;
  var transforms=scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<Transform>(true)).ToArray();
  int treeMoved=0,ecoMoved=0,waterSkipped=0;float treeMax=0f,ecoMax=0f;
  var moved=new List<string>();

  foreach(var tr in transforms){
   bool tree=tr.name.IndexOf("SourceTree",StringComparison.OrdinalIgnoreCase)>=0;
   bool eco=tr.name.StartsWith("Eco_",StringComparison.OrdinalIgnoreCase);
   if(!tree&&!eco)continue;
   var p=tr.position;if(p.x<-1280||p.x>768||p.z<620||p.z>1280)continue;
   var terr=FindTerrain(ts,p.x,p.z);if(!terr)continue;
   float gy=Ground(terr,p.x,p.z);
   if(gy<=.15f){waterSkipped++;continue;}
   var rs=tr.GetComponentsInChildren<Renderer>(false);if(rs.Length==0)continue;
   Bounds b=rs[0].bounds;for(int k=1;k<rs.Length;k++)b.Encapsulate(rs[k].bounds);
   float gap=b.min.y-gy;
   if(Mathf.Abs(gap)<=.75f)continue;
   tr.position=new Vector3(p.x,p.y-gap,p.z);
   if(tree){treeMoved++;treeMax=Mathf.Max(treeMax,Mathf.Abs(gap));}
   if(eco){ecoMoved++;ecoMax=Mathf.Max(ecoMax,Mathf.Abs(gap));}
   if(moved.Count<40)moved.Add((tree?"TREE ":"ECO ")+tr.name+" gap="+gap.ToString("F3"));
  }

  EditorSceneManager.MarkSceneDirty(scene);
  if(!EditorSceneManager.SaveScene(scene))throw new IOException("Failed to save Enroth after north grounding");
  Directory.CreateDirectory("Validation/EdgeGrid20260923/NorthReference20261006");
  var lines=new List<string>{
   "treeMoved="+treeMoved,
   "treeMaxAbsMoveM="+treeMax.ToString("F3"),
   "ecoMoved="+ecoMoved,
   "ecoMaxAbsMoveM="+ecoMax.ToString("F3"),
   "waterSkipped="+waterSkipped,
   "sceneDirtyAfterSave="+scene.isDirty
  };
  lines.AddRange(moved);
  File.WriteAllLines("Validation/EdgeGrid20260923/NorthReference20261006/grounding.txt",lines);
  Debug.Log("NORTH_REFERENCE_GROUNDING_20261006_DONE "+string.Join(" | ",lines.Take(6)));
 }
}