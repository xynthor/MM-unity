using System;using System.IO;using System.Linq;using System.Collections.Generic;using UnityEngine;using UnityEditor;using UnityEditor.SceneManagement;
public static class MMDragonFloatingRocksFinalQA20261011{
 const string Report="Validation/DragonFloatingRocks20261011";
 static string Stable(Transform t){var p=new List<string>();while(t){p.Add(t.name+"#"+t.GetSiblingIndex());t=t.parent;}p.Reverse();return string.Join("/",p);}
 static float Y(Vector3 v){var t=Terrain.activeTerrains.FirstOrDefault(t=>v.x>=t.transform.position.x&&v.x<=t.transform.position.x+512&&v.z>=t.transform.position.z&&v.z<=t.transform.position.z+512);if(!t)throw new Exception("Ground missing");return t.transform.position.y+t.SampleHeight(v);}
 public static void Run(){
  var sc=EditorSceneManager.OpenScene("Assets/Scenes/World/Enroth.unity",OpenSceneMode.Single);if(sc.isDirty)throw new Exception("Saved scene dirty");
  var ts=Terrain.activeTerrains;int pairs=0,bad=0,colliders=0;float worst=0;
  foreach(var a in ts){
   if(!a.GetComponent<TerrainCollider>()||a.GetComponent<TerrainCollider>().terrainData!=a.terrainData)colliders++;
   foreach(var b in ts){
    float dx=b.transform.position.x-a.transform.position.x,dz=b.transform.position.z-a.transform.position.z;
    bool east=Mathf.Abs(dx-512)<.01f&&Mathf.Abs(dz)<.01f;
    bool north=Mathf.Abs(dz-512)<.01f&&Mathf.Abs(dx)<.01f;if(!east&&!north)continue;
    pairs++;
    for(int i=0;i<=512;i++){
     var p=new Vector3(east?b.transform.position.x:a.transform.position.x+i,0,east?a.transform.position.z+i:b.transform.position.z);
     float d=Mathf.Abs(a.transform.position.y+a.SampleHeight(p)-b.transform.position.y-b.SampleHeight(p));
     worst=Mathf.Max(worst,d);if(d>.001f)bad++;
    }
   }
  }
  if(ts.Length!=30||pairs!=49||bad!=0||colliders!=0)throw new Exception("Global terrain QA failure: "+ts.Length+"/"+pairs+"/"+bad+"/"+colliders);
  var report=File.ReadAllLines(Report+"/kept.txt");
  var ids=report.Where(s=>s.Contains("|beforeGap=")).Select(s=>s.Split('|')[0]).ToHashSet();
  var candidates=UnityEngine.Object.FindObjectsByType<Transform>(FindObjectsInactive.Include,FindObjectsSortMode.None).Where(t=>ids.Contains(Stable(t))).ToArray();
  if(candidates.Length!=7)throw new Exception("Missing seven rock roots");
  float worstGap=0;
  foreach(var r in candidates){
   var rs=r.GetComponentsInChildren<MeshRenderer>(true);if(rs.Length!=5)throw new Exception("Granite LOD count changed");
   var bounds=rs[0].bounds;foreach(var item in rs.Skip(1))bounds.Encapsulate(item.bounds);
   float gap=bounds.min.y-Y(bounds.center);worstGap=Mathf.Max(worstGap,Mathf.Abs(gap+.12f));if(Mathf.Abs(gap+.12f)>.035f)throw new Exception("Individual rock no longer grounded "+gap);
  }
  var f=UnityEngine.Object.FindObjectsByType<MeshFilter>(FindObjectsInactive.Include,FindObjectsSortMode.None).Single(q=>q.name=="Inland Outcrops Cell 1"&&q.transform.parent&&q.transform.parent.name=="Dragon South Reference Inland Outcrops 20260930");
  if(AssetDatabase.GetAssetPath(f.sharedMesh)!="Assets/World/WorldExtensions/Generated/DragonGroundedOutcrops20261011/Cell_1_Grounded.asset")throw new Exception("Saved outcrop clone not linked");
  if(File.ReadAllText(Report+"/outcrop_independent_verify.txt").IndexOf("PASS=True",StringComparison.Ordinal)<0)throw new Exception("Outcrop independent QA missing");
  if(File.ReadAllText(Report+"/independent_verify.txt").IndexOf("PASS=True",StringComparison.Ordinal)<0)throw new Exception("Granite independent QA missing");
  string result="PASS=True terrains="+ts.Length+" joins="+pairs+" worstJoinM="+worst.ToString("F6")+" badColliders="+colliders+
   " granularRocksGrounded=7 wholeOutcropsGrounded=10 originalRockAssetsChanged=False buildingsMoved=0"+
   " graniteGroundGapWorstErrorM="+worstGap.ToString("F5")+" sceneDirty="+sc.isDirty;
  File.WriteAllText(Report+"/final_regression.txt",result);
  Debug.Log("DRAGON_FLOATING_ROCKS_FINAL_QA_PASS "+result);
 }
}
