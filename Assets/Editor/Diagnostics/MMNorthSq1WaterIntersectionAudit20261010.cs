using System;using System.IO;using System.Linq;using System.Collections.Generic;
using UnityEngine;using UnityEditor;using UnityEditor.SceneManagement;
public static class MMNorthSq1WaterIntersectionAudit20261010
{
 static float H(Terrain t,Vector3 p)=>t.transform.position.y+t.SampleHeight(p);
 public static void Run(){
  var sc=EditorSceneManager.OpenScene("Assets/Scenes/World/Enroth.unity",OpenSceneMode.Single);
  if(sc.isDirty)throw new Exception("Saved scene dirty; audit read-only");
  var t=Terrain.activeTerrains.Single(q=>Mathf.Abs(q.transform.position.x+768)<.1f&&Mathf.Abs(q.transform.position.z-768)<.1f);
  var lines=new List<string>();var rows=new List<string>{"meshName,assetPath,objectPath,vertexIndex,x,z,waterY,groundY,groundMinusWater"};
  int total=0,buried=0,trisTotal=0,trisHidden=0,trisPart=0;
  foreach(var mr in UnityEngine.Object.FindObjectsByType<MeshRenderer>()){
   if(!mr.enabled||!mr.gameObject.activeInHierarchy||!mr.sharedMaterial||!mr.sharedMaterial.shader.name.Contains("Water"))continue;
   if(mr.bounds.max.x < -768||mr.bounds.min.x > -256||mr.bounds.max.z < 768||mr.bounds.min.z >1280)continue;
   var mf=mr.GetComponent<MeshFilter>();if(!mf||!mf.sharedMesh)continue;
   var m=mf.sharedMesh;var v=m.vertices;var water=new float[v.Length];var inTile=new bool[v.Length];int n=0,b=0;float max=0;
   for(int i=0;i<v.Length;i++){
    var p=mf.transform.TransformPoint(v[i]);
    if(p.x < -768||p.x > -256||p.z<768||p.z>1280)continue;
    inTile[i]=true;water[i]=H(t,p)-p.y;
    n++;if(water[i]>.30f){b++;max=Mathf.Max(max,water[i]);}
    rows.Add(mr.name.Replace(",",";")+","+AssetDatabase.GetAssetPath(m).Replace(",",";")+","+PathOf(mr.transform).Replace(",",";")+","+i+","+
     p.x.ToString("F5",System.Globalization.CultureInfo.InvariantCulture)+","+p.z.ToString("F5",System.Globalization.CultureInfo.InvariantCulture)+","+
     p.y.ToString("F5",System.Globalization.CultureInfo.InvariantCulture)+","+H(t,p).ToString("F5",System.Globalization.CultureInfo.InvariantCulture)+","+water[i].ToString("F5",System.Globalization.CultureInfo.InvariantCulture));
   }
   int ntris=0,hidden=0,partial=0;
   for(int k=0;k<m.subMeshCount;k++){
    if(m.GetTopology(k)!=UnityEngine.MeshTopology.Triangles)continue;
    var tri=m.GetTriangles(k);
    for(int i=0;i+2<tri.Length;i+=3){
     int a=tri[i],b0=tri[i+1],c=tri[i+2];
     if(!inTile[a]||!inTile[b0]||!inTile[c])continue;
     ntris++;int submerged=(water[a]>.30f?1:0)+(water[b0]>.30f?1:0)+(water[c]>.30f?1:0);
     if(submerged==3)hidden++;else if(submerged>0)partial++;
    }
   }
   total+=n;buried+=b;trisTotal+=ntris;trisHidden+=hidden;trisPart+=partial;
   lines.Add("WATER_OBJECT="+PathOf(mr.transform)+" mesh="+AssetDatabase.GetAssetPath(m)+" material="+mr.sharedMaterial.name+" shader="+mr.sharedMaterial.shader.name+
    " verticesInSq1="+n+" buriedVerts="+b+" maxBurial="+max.ToString("F3")+" inTileTriangles="+ntris+" hiddenTriangles="+hidden+" partialTriangles="+partial+
    " totalVertices="+v.Length+" totalTriangles="+m.triangles.Length/3);
  }
  lines.Add("TOTAL vertices="+total+" buried="+buried+" inTileTriangles="+trisTotal+" hiddenTriangles="+trisHidden+" partialTriangles="+trisPart+" sceneDirty="+sc.isDirty);
  string dir="Validation/NorthSq1WaterIntersection20261010";Directory.CreateDirectory(dir);
  File.WriteAllLines(dir+"/audit.txt",lines);File.WriteAllLines(dir+"/vertices.csv",rows);
  Debug.Log("SQ1_WATER_INTERSECTION_AUDIT "+lines.Last());
 }
 static string PathOf(Transform t){var q=new List<string>();for(var p=t;p;p=p.parent)q.Add(p.name);q.Reverse();return string.Join("/",q);}
}
