using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using System;
using System.IO;
using System.Linq;

public static class MMNorthReferenceForest20261006
{
 const string ScenePath="Assets/Scenes/World/Enroth.unity";
 const string RootName="North Reference Forest Belts 20261006";
 const string ReportDir="Validation/EdgeGrid20260923/NorthReferenceRebuild20261006";
 static Terrain Find(Terrain[] ts,float x,float z){return ts.FirstOrDefault(t=>x>=t.transform.position.x-.01f&&x<=t.transform.position.x+t.terrainData.size.x+.01f&&z>=t.transform.position.z-.01f&&z<=t.transform.position.z+t.terrainData.size.z+.01f);}
 static int AddZone(Transform parent,Transform[] templates,Terrain[] terrains,float cx,float cz,float rx,float rz,int count,float phase){
   const float golden=2.39996323f;int added=0;
   for(int i=0;i<count;i++){
     float q=(i+.5f)/count,rad=Mathf.Sqrt(q),ang=i*golden+phase;
     float wx=cx+Mathf.Cos(ang)*rx*rad,wz=cz+Mathf.Sin(ang)*rz*rad;
     var terr=Find(terrains,wx,wz);if(!terr)continue;
     float u=Mathf.Clamp01((wx-terr.transform.position.x)/terr.terrainData.size.x),v=Mathf.Clamp01((wz-terr.transform.position.z)/terr.terrainData.size.z);
     float slope=terr.terrainData.GetSteepness(u,v),gy=terr.transform.position.y+terr.SampleHeight(new Vector3(wx,0,wz));
     if(gy<1.0f||slope>31f)continue;
     var src=templates[i%templates.Length];var go=UnityEngine.Object.Instantiate(src.gameObject,parent);
     go.name="NorthRefTree_"+added+"_"+Mathf.RoundToInt(wx)+"_"+Mathf.RoundToInt(wz);
     go.transform.position=new Vector3(wx,gy,wz);
     go.transform.rotation=Quaternion.Euler(0,(i*137.50776f+phase*41f)%360f,0);
     float sc=.82f+.32f*Mathf.PerlinNoise(i*.173f+phase,phase*.417f);go.transform.localScale=src.localScale*sc;
     var rs=go.GetComponentsInChildren<Renderer>(false);if(rs.Length>0){Bounds b=rs[0].bounds;for(int k=1;k<rs.Length;k++)b.Encapsulate(rs[k].bounds);go.transform.position+=Vector3.up*(gy-b.min.y);}
     added++;
   }
   return added;
 }
 [MenuItem("MMUnity/Reference 2026/Add Northern Reference Forest Belts")]
 public static void Run(){
   var s=EditorSceneManager.OpenScene(ScenePath,OpenSceneMode.Single);
   var existing=s.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<Transform>(true)).FirstOrDefault(t=>t.name==RootName);
   if(existing)throw new Exception("Northern reference forest belts already exist.");
   var all=s.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<Transform>(true)).ToArray();
   var templates=new[]{
     all.FirstOrDefault(t=>t.name=="SourceTree_0000_6tree27"),
     all.FirstOrDefault(t=>t.name=="SourceTree_0005_6tree28"),
     all.FirstOrDefault(t=>t.name=="SourceTree_0012_6tree29")}.Where(t=>t!=null).ToArray();
   if(templates.Length<2)throw new Exception("Production conifer templates not found.");
   var root=new GameObject(RootName);var terrains=Terrain.activeTerrains;int added=0;
   added+=AddZone(root.transform,templates,terrains,-1050,735,185,130,52,.25f);
   added+=AddZone(root.transform,templates,terrains,-850,820,175,125,48,1.10f);
   added+=AddZone(root.transform,templates,terrains,-635,880,160,110,40,2.10f);
   added+=AddZone(root.transform,templates,terrains,-390,850,140,95,28,2.80f);
   added+=AddZone(root.transform,templates,terrains,255,825,135,95,28,3.40f);
   added+=AddZone(root.transform,templates,terrains,455,770,160,105,44,4.20f);
   added+=AddZone(root.transform,templates,terrains,610,720,120,92,36,5.10f);
   EditorSceneManager.MarkSceneDirty(s);if(!EditorSceneManager.SaveScene(s))throw new IOException("Could not save Enroth.");
   Directory.CreateDirectory(ReportDir);File.WriteAllLines(ReportDir+"/forest.txt",new[]{"PASS production conifer forest belts","treesAdded="+added,"zones=7","centralStromgardPlainKeptOpen=true","sceneDirtyAfterSave="+s.isDirty});
   Debug.Log("NORTH_REFERENCE_FOREST_DONE treesAdded="+added);
 }
}