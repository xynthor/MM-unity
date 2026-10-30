using System;using System.IO;using System.Linq;using System.Collections.Generic;using System.Globalization;
using UnityEngine;using UnityEditor;using UnityEditor.SceneManagement;
public static class MMNorthSmallBoulderFootAudit20261011{
 static readonly CultureInfo C=CultureInfo.InvariantCulture;
 static Terrain Ground(Vector3 p)=>Terrain.activeTerrains.FirstOrDefault(t=>p.x>=t.transform.position.x-.001f&&p.x<=t.transform.position.x+512.001f&&p.z>=t.transform.position.z-.001f&&p.z<=t.transform.position.z+512.001f);
 static string F(float x)=>x.ToString("F4",C);
 public static void Run(){
  var s=EditorSceneManager.OpenScene("Assets/Scenes/World/Enroth.unity",OpenSceneMode.Single);
  if(s.isDirty)throw new Exception("Scene must be clean");
  var rows=new List<string>{"name,active,vertexCount,mesh,collider,minVertexGap,medianVertexGap,maxVertexGap,low10MinGap,low10MedianGap,low10MaxGap,centerBoundsGap,centerX,centerZ,centerGroundY,boundsMinY,lowestVertexX,lowestVertexZ"};
  foreach(var t in UnityEngine.Object.FindObjectsByType<Transform>(FindObjectsInactive.Include,FindObjectsSortMode.None).Where(t=>t.name.StartsWith("AuthoredBoulder_")&&t.parent&&t.parent.name=="North Production Boulder Dressing 20261007").OrderBy(t=>t.name)){
   var m=t.GetComponent<MeshFilter>()?t.GetComponent<MeshFilter>().sharedMesh:null;
   var r=t.GetComponent<MeshRenderer>();if(!m||!r)throw new Exception("Missing boulder geometry at "+t.name);
   var verts=m.vertices;var points=verts.Select(v=>t.TransformPoint(v)).ToArray();
   var gaps=new List<(float g,float y,Vector3 p)>();
   foreach(var p in points){var ground=Ground(p);if(!ground)throw new Exception("Boulder outside terrain "+t.name);gaps.Add((p.y-(ground.transform.position.y+ground.SampleHeight(p)),p.y,p));}
   var sorted=gaps.OrderBy(z=>z.g).ToArray();int n=Math.Max(2,gaps.Count/10);
   var lows=gaps.OrderBy(v=>v.y).Take(n).OrderBy(v=>v.g).ToArray();
   var b=r.bounds;var mid=Ground(b.center);float centerY=mid.transform.position.y+mid.SampleHeight(b.center);
   var low=points.OrderBy(p=>p.y).First();
   rows.Add(string.Join(",",new[]{t.name,t.gameObject.activeInHierarchy.ToString(),verts.Length.ToString(),m.name,(t.GetComponent<MeshCollider>()!=null).ToString(),
    F(sorted[0].g),F(sorted[sorted.Length/2].g),F(sorted.Last().g),F(lows[0].g),F(lows[lows.Length/2].g),F(lows.Last().g),
    F(b.min.y-centerY),F(b.center.x),F(b.center.z),F(centerY),F(b.min.y),F(low.x),F(low.z)}));
  }
  Directory.CreateDirectory("Validation/DragonFloatingRocks20261011");
  File.WriteAllLines("Validation/DragonFloatingRocks20261011/north_boulder_foot_audit.csv",rows);
  Debug.Log("NORTH_BOULDER_FOOT_AUDIT_PASS candidates="+(rows.Count-1)+" savedSceneDirty="+s.isDirty);
 }
}
