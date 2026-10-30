using System;using System.IO;using System.Linq;using System.Collections.Generic;using UnityEngine;using UnityEditor;using UnityEditor.SceneManagement;using UnityEngine.SceneManagement;
public static class MMBatchArchipelagoWaterMaterialAudit20261001{
 static string P(Transform t){string s=t.name;while(t.parent){t=t.parent;s=t.name+"/"+s;}return s;}
 public static void Run(){
  var s=EditorSceneManager.OpenScene("Assets/Scenes/World/Enroth.unity",OpenSceneMode.Single);var rows=new List<string>();
  foreach(var r in s.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<Renderer>(true))){
   if(r.bounds.max.x<740||r.bounds.min.x>1820||r.bounds.max.z<-740||r.bounds.min.z>-1320)continue;
   string nm=P(r.transform);string mats=string.Join(",",r.sharedMaterials.Where(m=>m).Select(m=>$"{m.name}@{AssetDatabase.GetAssetPath(m)}"));
   if(nm.IndexOf("water",StringComparison.OrdinalIgnoreCase)>=0||mats.IndexOf("Water",StringComparison.OrdinalIgnoreCase)>=0)
    rows.Add($"{nm}|enabled={r.enabled}|bounds={r.bounds}|mats={mats}");
  }
  var roots=s.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<Transform>(true)).Where(t=>t.name.IndexOf("Archipelago of the Ancients",StringComparison.OrdinalIgnoreCase)>=0).ToArray();
  foreach(var t in roots)rows.Add($"ROOTLIKE|{P(t)}|active={t.gameObject.activeInHierarchy}|children={t.childCount}");
  Directory.CreateDirectory("Validation/EdgeGrid20260923/ArchipelagoExpansion20261001");File.WriteAllLines("Validation/EdgeGrid20260923/ArchipelagoExpansion20261001/water_audit.txt",rows);Debug.Log("ARCH_WATER_AUDIT_DONE "+rows.Count);
 }}