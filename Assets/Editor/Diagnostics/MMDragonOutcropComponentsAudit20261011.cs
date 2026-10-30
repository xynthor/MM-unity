using System;using System.IO;using System.Linq;using System.Collections.Generic;using System.Globalization;using UnityEngine;using UnityEditor;using UnityEditor.SceneManagement;
public static class MMDragonOutcropComponentsAudit20261011{
 const string Output="Validation/DragonFloatingRocks20261011/outcrop_components.csv";
 static Terrain Ground(Vector3 p)=>Terrain.activeTerrains.FirstOrDefault(t=>p.x>=t.transform.position.x&&p.x<=t.transform.position.x+512&&p.z>=t.transform.position.z&&p.z<=t.transform.position.z+512);
 static int Find(int[] pa,int v){while(pa[v]!=v){pa[v]=pa[pa[v]];v=pa[v];}return v;}
 static void Union(int[] pa,int a,int b){a=Find(pa,a);b=Find(pa,b);if(a!=b)pa[b]=a;}
 public static void Run(){
  var s=EditorSceneManager.OpenScene("Assets/Scenes/World/Enroth.unity",OpenSceneMode.Single);if(s.isDirty)throw new Exception("Dirty source");
  var all=UnityEngine.Object.FindObjectsByType<MeshFilter>(FindObjectsInactive.Include,FindObjectsSortMode.None);
  var rows=new List<string>{"name,asset,component,vertices,triangles,lowestY,highestY,minGap,medianGap,maxGap,base10PctGap,centerX,centerZ,boundsX,boundsZ"};
  foreach(var f in all){
   if(!f.sharedMesh||!f.name.StartsWith("Inland Outcrops Cell "))continue;
   if(!f.transform.parent||f.transform.parent.name!="Dragon South Reference Inland Outcrops 20260930")continue;
   var m=f.sharedMesh;var v=m.vertices;var tri=m.triangles;int[] parents=Enumerable.Range(0,v.Length).ToArray();
   for(int i=0;i<tri.Length;i+=3){Union(parents,tri[i],tri[i+1]);Union(parents,tri[i],tri[i+2]);}
   var parts=new Dictionary<int,List<int>>();for(int i=0;i<v.Length;i++){int k=Find(parents,i);if(!parts.ContainsKey(k))parts[k]=new List<int>();parts[k].Add(i);}
   int ci=0;foreach(var part in parts.Values.OrderBy(p=>p.Min())){
    var world=part.Select(k=>f.transform.TransformPoint(v[k])).ToArray();
    float minx=world.Min(p=>p.x),maxx=world.Max(p=>p.x),minz=world.Min(p=>p.z),maxz=world.Max(p=>p.z);
    float miny=world.Min(p=>p.y),maxy=world.Max(p=>p.y);
    var gaps=new List<float>();
    foreach(var p in world){
     var t=Ground(p);if(!t)continue;
     gaps.Add(p.y-(t.transform.position.y+t.SampleHeight(p)));
    }
    if(gaps.Count<part.Count*.7f)throw new Exception("Many outcrop vertices outside linked terrain");
    var sorted=gaps.OrderBy(x=>x).ToArray();
    var lowY=world.OrderBy(p=>p.y).Take(Math.Max(2,world.Length/10)).ToArray();
    var baseGap=lowY.Select(p=>{var t=Ground(p);return p.y-(t.transform.position.y+t.SampleHeight(p));}).Min();
    rows.Add(string.Join(",",new[]{f.name,AssetDatabase.GetAssetPath(m),ci.ToString(),part.Count.ToString(),
      (tri.Length/3).ToString(),miny.ToString("F2",CultureInfo.InvariantCulture),maxy.ToString("F2",CultureInfo.InvariantCulture),
      sorted.First().ToString("F3",CultureInfo.InvariantCulture),sorted[sorted.Length/2].ToString("F3",CultureInfo.InvariantCulture),
      sorted.Last().ToString("F3",CultureInfo.InvariantCulture),baseGap.ToString("F3",CultureInfo.InvariantCulture),
      ((minx+maxx)/2).ToString("F2",CultureInfo.InvariantCulture),((minz+maxz)/2).ToString("F2",CultureInfo.InvariantCulture),
      (maxx-minx).ToString("F2",CultureInfo.InvariantCulture),(maxz-minz).ToString("F2",CultureInfo.InvariantCulture)}));
    ci++;
   }
  }
  Directory.CreateDirectory(Path.GetDirectoryName(Output));File.WriteAllLines(Output,rows);
  Debug.Log("DRAGON_OUTCROP_COMPONENT_AUDIT_PASS components="+(rows.Count-1)+" sceneDirty="+s.isDirty);
 }
}
