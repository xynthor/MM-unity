using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using System;
using System.IO;
using System.Linq;

public static class MMNorthCoastalDressing20261006
{
 const string ScenePath="Assets/Scenes/World/Enroth.unity";
 const string ForestRoot="North Extension Coastal Forest Supplement 20261006";
 const string Report="Validation/EdgeGrid20260923/NorthExtensionSafe20261006/coastal_forest.txt";
 static readonly string[] ExtPaths={
  "Assets/World/WorldExtensions/Generated/NorthReference20261001/SweetWater_NorthTerrain.asset",
  "Assets/World/WorldExtensions/Generated/NorthReference20261001/Kriegspire_NorthTerrain.asset",
  "Assets/World/WorldExtensions/Generated/NorthReference20261001/FrozenHighlands_NorthTerrain.asset",
  "Assets/World/WorldExtensions/Generated/NorthReference20261001/SilverCove_NorthTerrain.asset"};

 static Terrain Find(Terrain[] ts,float x,float z){return ts.FirstOrDefault(t=>x>=t.transform.position.x-.01f&&x<=t.transform.position.x+t.terrainData.size.x+.01f&&z>=t.transform.position.z-.01f&&z<=t.transform.position.z+t.terrainData.size.z+.01f);}
 static float Ground(Terrain t,float x,float z){return t.transform.position.y+t.SampleHeight(new Vector3(x,0,z));}
 static void GroundObject(Transform tr,float y){
  var rs=tr.GetComponentsInChildren<Renderer>(false);if(rs.Length==0){tr.position=new Vector3(tr.position.x,y,tr.position.z);return;}
  Bounds b=rs[0].bounds;for(int i=1;i<rs.Length;i++)b.Encapsulate(rs[i].bounds);tr.position+=Vector3.up*(y-b.min.y);
 }
 static int AddForestZone(Transform parent,Transform[] templates,Terrain[] ext,float cx,float cz,float rx,float rz,int count,float phase){
  const float golden=2.39996323f;int added=0;
  for(int i=0;i<count;i++){
   float q=(i+.5f)/count,rad=Mathf.Sqrt(q),ang=i*golden+phase;
   float x=cx+Mathf.Cos(ang)*rx*rad,z=cz+Mathf.Sin(ang)*rz*rad;
   if(z<805f)continue;var t=Find(ext,x,z);if(!t)continue;
   float u=Mathf.Clamp01((x-t.transform.position.x)/512f),v=Mathf.Clamp01((z-t.transform.position.z)/512f);
   float y=Ground(t,x,z),s=t.terrainData.GetSteepness(u,v);if(y<2f||s>28f)continue;
   var src=templates[i%templates.Length];var go=UnityEngine.Object.Instantiate(src.gameObject,parent);
   go.name="NorthCoastalForest_"+added+"_"+Mathf.RoundToInt(x)+"_"+Mathf.RoundToInt(z);
   go.transform.position=new Vector3(x,y,z);go.transform.rotation=Quaternion.Euler(0,(i*137.50776f+phase*51f)%360f,0);
   float sc=.95f+.26f*Mathf.PerlinNoise(i*.19f+phase,phase*.37f);go.transform.localScale=src.localScale*sc;GroundObject(go.transform,y);added++;
  }
  return added;
 }

 [MenuItem("MMUnity/Reference 2026/Add Northern Coastal Forest Belts")]
 public static void Run(){
  var scene=EditorSceneManager.OpenScene(ScenePath,OpenSceneMode.Single);if(scene.isDirty)throw new Exception("Enroth must open clean.");
  Directory.CreateDirectory(Path.GetDirectoryName(Report));if(File.Exists(Report))throw new Exception("Coastal forest supplement already applied.");
  if(scene.GetRootGameObjects().Any(g=>g.name==ForestRoot))throw new Exception("Coastal forest root already exists.");
  var active=Terrain.activeTerrains;
  var ext=ExtPaths.Select(p=>AssetDatabase.LoadAssetAtPath<TerrainData>(p)).Select(td=>active.FirstOrDefault(t=>t.terrainData==td)).ToArray();
  if(ext.Any(t=>!t))throw new Exception("North extension terrain missing.");
  var all=scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<Transform>(true)).ToArray();
  var templates=new[]{
   all.FirstOrDefault(t=>t.name=="SourceTree_0134_6tree29"),all.FirstOrDefault(t=>t.name=="SourceTree_0096_6tree28"),
   all.FirstOrDefault(t=>t.name=="SourceTree_0200_6tree28"),all.FirstOrDefault(t=>t.name=="SourceTree_0228_6tree28")}.Where(t=>t!=null).ToArray();
  if(templates.Length<3)throw new Exception("Production conifer templates missing.");
  var root=new GameObject(ForestRoot);int trees=0;
  trees+=AddForestZone(root.transform,templates,ext,-1120,835,145,48,34,.3f);
  trees+=AddForestZone(root.transform,templates,ext,-900,855,155,55,34,1.1f);
  trees+=AddForestZone(root.transform,templates,ext,520,845,150,52,32,2.7f);
  trees+=AddForestZone(root.transform,templates,ext,650,900,82,55,20,3.6f);
  EditorSceneManager.MarkSceneDirty(scene);if(!EditorSceneManager.SaveScene(scene))throw new IOException("Failed to save Enroth.");
  File.WriteAllLines(Report,new[]{"PASS extension-only coastal forest supplement","legacyObjectsModified=0","supplementalForestObjects="+trees,"forestZones=4","sceneDirtyAfterSave="+scene.isDirty});
  Debug.Log("NORTH_COASTAL_FOREST_DONE forests="+trees);
 }
}