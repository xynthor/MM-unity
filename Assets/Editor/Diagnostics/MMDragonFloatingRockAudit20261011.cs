using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;

public static class MMDragonFloatingRockAudit20261011 {
 const string Scene="Assets/Scenes/World/Enroth.unity";
 const string Report="Validation/DragonFloatingRocks20261011";
 static string PathOf(Transform t){
  var stack=new Stack<string>();
  while(t){stack.Push(t.name);t=t.parent;}
  return string.Join("/",stack);
 }
 static string S(string v)=>"\""+(v??"").Replace("\"","\"\"").Replace("\n"," ").Replace("\r"," ")+"\"";
 static bool ContainsAny(string s,params string[] q)=>q.Any(v=>s.IndexOf(v,StringComparison.OrdinalIgnoreCase)>=0);
 static Terrain GroundTerrain(Vector3 p){
  return Terrain.activeTerrains.FirstOrDefault(t=>p.x>=t.transform.position.x-.001f&&p.x<=t.transform.position.x+t.terrainData.size.x+.001f&&
             p.z>=t.transform.position.z-.001f&&p.z<=t.transform.position.z+t.terrainData.size.z+.001f);
 }
 static Bounds BoundsOf(Transform t){
  var rs=t.GetComponentsInChildren<Renderer>(true).Where(r=>r.enabled && !(r is ParticleSystemRenderer)).ToArray();
  if(rs.Length==0)return new Bounds(t.position,Vector3.zero);
  var b=rs[0].bounds;for(int i=1;i<rs.Length;i++)b.Encapsulate(rs[i].bounds);
  return b;
 }
 public static void Run(){
  var scene=EditorSceneManager.OpenScene(Scene,OpenSceneMode.Single);if(scene.isDirty)throw new Exception("Scene dirty on open");
  if(Terrain.activeTerrains.Length!=30)throw new Exception("Unexpected terrain count "+Terrain.activeTerrains.Length);
  var rows=new List<string>{"classification,gapM,groundY,minRendererY,centerX,centerZ,boundsX,boundsZ,path,prefab,meshPath,materialNames,active,rendererCount"};
  int all=0,possible=0,building=0;var objs=UnityEngine.Object.FindObjectsByType<Transform>(FindObjectsInactive.Include,FindObjectsSortMode.None);
  foreach(var tr in objs){
   var p=tr.position;
   if(p.x< -1792||p.x> -1280||p.z<256||p.z>1280)continue;
   if(tr.GetComponent<Terrain>()||tr.GetComponent<TerrainCollider>())continue;
   var renderers=tr.GetComponentsInChildren<MeshRenderer>(true);
   if(renderers.Length==0)continue;
   if(tr.parent && tr.parent.GetComponentInParent<MeshRenderer>())continue;
   // Only top-level prop instances, not their internal mesh-part renderers.
   if(tr.GetComponent<MeshRenderer>()==null && renderers.Length<1)continue;
   var path=PathOf(tr);var meshName=string.Join(";",renderers.Select(r=>r.GetComponent<MeshFilter>()?r.GetComponent<MeshFilter>().sharedMesh?r.GetComponent<MeshFilter>().sharedMesh.name:"":"").Distinct());
   var mats=string.Join(";",renderers.SelectMany(r=>r.sharedMaterials).Where(m=>m).Select(m=>m.name).Distinct());
   var prefab=PrefabUtility.GetCorrespondingObjectFromSource(tr.gameObject);
   var prefabPath=prefab?AssetDatabase.GetAssetPath(prefab):"";
   bool rock=ContainsAny(tr.name+" "+path+" "+meshName+" "+prefabPath, "rock","boulder","granite","stone","outcrop","cliff");
   bool built=ContainsAny(path+" "+prefabPath+" "+meshName,
    "building","house","castle","town","ruin","tower","temple","dungeon","citadel","fort","structure","wall","column","pillar","arch","roof","floor","door","gate","bridge","stair","king","dragon shrine","obelisk","monument","altar","portal","statue","entrance","interior","city","village","foundation");
   if(!rock&&!built)continue;
   var b=BoundsOf(tr);
   var ground=GroundTerrain(b.center);
   if(!ground)continue;
   float gy=ground.transform.position.y+ground.SampleHeight(b.center);
   var gap=b.min.y-gy;
   string kind=built?"PROTECTED_BUILDING":rock?"NATURAL_ROCK_CANDIDATE":"OTHER";
   all++;if(built)building++;if(rock&&!built&&gap>.5f)possible++;
   rows.Add(string.Join(",",new[]{
    S(kind),gap.ToString("F3",System.Globalization.CultureInfo.InvariantCulture),
    gy.ToString("F3",System.Globalization.CultureInfo.InvariantCulture),
    b.min.y.ToString("F3",System.Globalization.CultureInfo.InvariantCulture),
    b.center.x.ToString("F2",System.Globalization.CultureInfo.InvariantCulture),
    b.center.z.ToString("F2",System.Globalization.CultureInfo.InvariantCulture),
    b.size.x.ToString("F2",System.Globalization.CultureInfo.InvariantCulture),
    b.size.z.ToString("F2",System.Globalization.CultureInfo.InvariantCulture),
    S(path),S(prefabPath),S(meshName),S(mats),S(tr.gameObject.activeInHierarchy.ToString()),renderers.Length.ToString()
   }));
  }
  Directory.CreateDirectory(Report);File.WriteAllLines(Report+"/all_rock_and_building_candidates.csv",rows);
  Debug.Log("DRAGON_FLOATING_ROCK_AUDIT_PASS sourceOnly=True sceneDirty="+scene.isDirty+" listed="+all+" protectedBuilding="+building+" rockPositiveGap="+possible+" report="+Report);
 }
}
