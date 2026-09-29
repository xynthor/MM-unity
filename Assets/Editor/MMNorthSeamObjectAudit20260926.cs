using UnityEngine;
using UnityEditor;
using UnityEngine.SceneManagement;
using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
[InitializeOnLoad]
internal static class MMNorthSeamObjectAudit20260926 {
 const string V="Validation/EdgeGrid20260923/";
 static MMNorthSeamObjectAudit20260926(){EditorApplication.delayCall+=Run;}
 static void Run(){
  string flag=V+"RUN_NORTH_SEAM_OBJECT_AUDIT.flag";
  if(!File.Exists(flag))return;
  File.Delete(flag);
  var lines=new List<string>();
  try{
   var sc=SceneManager.GetActiveScene();
   foreach(var r in sc.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<Renderer>(true))){
    if(r is Terrain)continue;
    var b=r.bounds;
    bool crosses=b.min.z<=770f&&b.max.z>=766f;
    bool broad=b.size.x>350f||b.size.z>350f;
    if(crosses||broad){
     var p=r.transform.position;
     lines.Add(r.name+" type="+r.GetType().Name+
       " pos=("+p.x.ToString("F1")+","+p.y.ToString("F1")+","+p.z.ToString("F1")+")"+
       " boundsCenter=("+b.center.x.ToString("F1")+","+b.center.y.ToString("F1")+","+b.center.z.ToString("F1")+")"+
       " size=("+b.size.x.ToString("F1")+","+b.size.y.ToString("F1")+","+b.size.z.ToString("F1")+")"+
       " material="+(r.sharedMaterial?r.sharedMaterial.name:"NONE")+
       " enabled="+r.enabled+" active="+r.gameObject.activeInHierarchy+
       " chain="+string.Join("/",r.GetComponentsInParent<Transform>(true).Select(t=>t.name).Reverse()));
    }
   }
  }catch(Exception e){lines.Add("FAIL "+e);}
  File.WriteAllLines(V+"north_seam_object_audit_20260926.txt",lines);
 }
}
