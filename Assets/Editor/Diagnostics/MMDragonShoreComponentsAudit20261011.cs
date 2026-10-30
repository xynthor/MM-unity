using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
public static class MMDragonShoreComponentsAudit20261011 {
 const string Scene="Assets/Scenes/World/Enroth.unity";
 const string Report="Validation/DragonFloatingRocks20261011/shore_components_review.csv";
 static readonly CultureInfo CI=CultureInfo.InvariantCulture;
 static int Root(int[] parents,int i){while(parents[i]!=i){parents[i]=parents[parents[i]];i=parents[i];}return i;}
 static void Join(int[] parents,int a,int b){int x=Root(parents,a),y=Root(parents,b);if(x!=y)parents[y]=x;}
 static Terrain TerrainAt(Vector3 p)=>Terrain.activeTerrains.FirstOrDefault(t=>p.x>=t.transform.position.x&&p.x<=t.transform.position.x+512&&p.z>=t.transform.position.z&&p.z<=t.transform.position.z+512);
 static string Ancestor(Transform t) {var names=new List<string>();for(;t;t=t.parent){names.Add(t.name);if(names.Count>12)break;}return string.Join(" / ",names);}
 public static void Run(){
  var scene=EditorSceneManager.OpenScene(Scene,OpenSceneMode.Single);if(scene.isDirty)throw new Exception("Scene must be saved/clean");
  var filters=UnityEngine.Object.FindObjectsByType<MeshFilter>(FindObjectsInactive.Include,FindObjectsSortMode.None);
  var rows=new List<string>{"object,asset,region,component,vertices,minClearanceM,base10MinM,base10MedianM,medianClearanceM,maxClearanceM,centerX,centerZ,widthX,widthZ,centerY"};
  int count=0,floatParts=0,meshCount=0;int beyond=0;
  foreach(var f in filters.OrderBy(f=>f.name)){
   if(!f.name.StartsWith("Shore Rocks Cell ",StringComparison.Ordinal)||!f.transform.parent||!f.transform.parent.name.Contains("Dragon",StringComparison.OrdinalIgnoreCase))continue;
   var m=f.sharedMesh;if(!m)continue;
   meshCount++;
   var verts=m.vertices;var tri=m.triangles;var par=Enumerable.Range(0,verts.Length).ToArray();
   for(int i=0;i<tri.Length;i+=3){Join(par,tri[i],tri[i+1]);Join(par,tri[i],tri[i+2]);}
   var groups=new Dictionary<int,List<int>>();
   for(int i=0;i<verts.Length;i++){int r=Root(par,i);if(!groups.ContainsKey(r))groups.Add(r,new List<int>());groups[r].Add(i);}
   int ci=0;
   foreach(var part in groups.Values.OrderBy(g=>g.Min())){
    var points=part.Select(k=>f.transform.TransformPoint(verts[k])).ToArray();
    var sample=new List<(float gap,Vector3 point)>();
    foreach(var v in points){
      var t=TerrainAt(v);if(!t){beyond++;continue;}
      sample.Add((v.y-t.transform.position.y-t.SampleHeight(v),v));
    }
    if(sample.Count<part.Count*.8f)throw new Exception("Too many out-of-terrain vertices in "+f.name);
    var gaps=sample.Select(a=>a.gap).OrderBy(x=>x).ToArray();if(gaps.Length==0)continue;
    var basePoint=points.OrderBy(p=>p.y).Take(Math.Max(2,part.Count/10)).ToArray();
    var lows=basePoint.Select(p=>{var t=TerrainAt(p);return p.y-t.transform.position.y-t.SampleHeight(p);}).OrderBy(x=>x).ToArray();
    float xMin=points.Min(p=>p.x),xMax=points.Max(p=>p.x),zMin=points.Min(p=>p.z),zMax=points.Max(p=>p.z);
    float med=gaps[gaps.Length/2],baseMed=lows[lows.Length/2];
    if(gaps[0]>.8f)floatParts++;
    rows.Add(string.Join(",",new[]{f.name,AssetDatabase.GetAssetPath(m),f.transform.parent.name,ci.ToString(),part.Count.ToString(),
     gaps[0].ToString("F3",CI),lows[0].ToString("F3",CI),baseMed.ToString("F3",CI),med.ToString("F3",CI),
     gaps[gaps.Length-1].ToString("F3",CI),((xMin+xMax)/2).ToString("F2",CI),((zMin+zMax)/2).ToString("F2",CI),
     (xMax-xMin).ToString("F2",CI),(zMax-zMin).ToString("F2",CI),points.Average(p=>p.y).ToString("F2",CI)}));
    count++;ci++;
   }
  }
  Directory.CreateDirectory(Path.GetDirectoryName(Report));File.WriteAllLines(Report,rows);
  Debug.Log("DRAGON_SHORE_COMPONENTS_AUDIT_PASS meshes="+meshCount+" components="+count+" minClearanceAbove0.8="+floatParts+" outsideVertices="+beyond+" sceneDirty="+scene.isDirty);
 }
}
