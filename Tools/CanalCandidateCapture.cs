using System;using System.IO;using System.Linq;using UnityEngine;using UnityEditor.SceneManagement;using UnityEngine.SceneManagement;
internal class CommandScript:IRunCommand{
 const string Out="Validation/CanalAudit_20260920";
 public void Execute(ExecutionResult result){
  string scene=File.ReadAllText(Out+"/capture_next.txt").Trim();
  var s=EditorSceneManager.OpenScene("Assets/Scenes/"+scene+".unity",OpenSceneMode.Single);
  var terr=s.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<Terrain>(true)).FirstOrDefault();
  var rows=File.ReadAllLines(Out+"/candidate_clusters.tsv").Select(l=>l.Split('\t')).Where(p=>p.Length>=10&&p[1]==scene).ToArray();
  int n=0;
  foreach(var p in rows){int id=int.Parse(p[0]);float cx=float.Parse(p[2],System.Globalization.CultureInfo.InvariantCulture),cz=float.Parse(p[3],System.Globalization.CultureInfo.InvariantCulture),minx=float.Parse(p[4],System.Globalization.CultureInfo.InvariantCulture),minz=float.Parse(p[5],System.Globalization.CultureInfo.InvariantCulture),maxx=float.Parse(p[6],System.Globalization.CultureInfo.InvariantCulture),maxz=float.Parse(p[7],System.Globalization.CultureInfo.InvariantCulture),wy=float.Parse(p[8],System.Globalization.CultureInfo.InvariantCulture);
   float span=Mathf.Max(maxx-minx,maxz-minz);float ground=terr?terr.SampleHeight(new Vector3(cx,0,cz))+terr.transform.position.y:wy;
   var go=new GameObject("__CanalAuditCamera");var c=go.AddComponent<Camera>();c.farClipPlane=2000;c.clearFlags=CameraClearFlags.Skybox;
   c.orthographic=true;c.orthographicSize=Mathf.Max(18,span*.75f+10);c.transform.position=new Vector3(cx,Mathf.Max(ground,wy)+120,cz);c.transform.rotation=Quaternion.Euler(90,0,0);
   MMSequentialEvidence.Render(c,Out+"/cluster_"+id.ToString("D2")+"_"+scene+"_top.png",1000,1000);
   c.orthographic=false;c.fieldOfView=52;float d=Mathf.Max(24,span*1.1f);c.transform.position=new Vector3(cx+d*.55f,Mathf.Max(ground,wy)+d*.55f,cz-d*.8f);c.transform.LookAt(new Vector3(cx,wy,cz));
   MMSequentialEvidence.Render(c,Out+"/cluster_"+id.ToString("D2")+"_"+scene+"_oblique.png",1000,800);
   UnityEngine.Object.DestroyImmediate(go);n++;
  }
  result.Log(scene+" canal captures="+n);
 }
}