using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using UnityEngine.Rendering;
using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;

public static class MMNorthOceanClip20261007
{
 const string ScenePath="Assets/Scenes/World/Enroth.unity"; const string WaterName="Linked connected sea and existing lowland waters 20260929"; const float X0=-1280f,X1=768f,Z0=768f,Z1=1280f,SeaY=.12f; const string Report="Validation/EdgeGrid20260923/NorthSquareRealism20261007/ocean_clip.txt";
 static readonly string[] ExtPaths={"Assets/World/WorldExtensions/Generated/NorthReference20261001/SweetWater_NorthTerrain.asset","Assets/World/WorldExtensions/Generated/NorthReference20261001/Kriegspire_NorthTerrain.asset","Assets/World/WorldExtensions/Generated/NorthReference20261001/FrozenHighlands_NorthTerrain.asset","Assets/World/WorldExtensions/Generated/NorthReference20261001/SilverCove_NorthTerrain.asset"};
 struct P{public Vector3 v;public P(Vector3 x){v=x;}}
 static Terrain Find(Terrain[] ts,float x,float z){return ts.FirstOrDefault(t=>x>=t.transform.position.x-.01f&&x<=t.transform.position.x+t.terrainData.size.x+.01f&&z>=t.transform.position.z-.01f&&z<=t.transform.position.z+t.terrainData.size.z+.01f);}
 static float Ground(Terrain t,float x,float z){return t.transform.position.y+t.SampleHeight(new Vector3(x,0,z));}
 static List<P> Clip(List<P> poly,int axis,float k,bool ge){var o=new List<P>();if(poly.Count==0)return o;for(int i=0;i<poly.Count;i++){var a=poly[i];var b=poly[(i+1)%poly.Count];float av=axis==0?a.v.x:a.v.z,bv=axis==0?b.v.x:b.v.z;bool ai=ge?av>=k:av<=k,bi=ge?bv>=k:bv<=k;if(ai&&bi)o.Add(b);else if(ai&&!bi){float q=(k-av)/(bv-av);o.Add(new P(Vector3.Lerp(a.v,b.v,q)));}else if(!ai&&bi){float q=(k-av)/(bv-av);o.Add(new P(Vector3.Lerp(a.v,b.v,q)));o.Add(b);}}return o;}
 static void AddPoly(List<P> p,Transform wt,List<Vector3> v,List<int> tr){if(p.Count<3)return;int n=v.Count;for(int i=0;i<p.Count;i++)v.Add(wt.InverseTransformPoint(p[i].v));for(int i=1;i<p.Count-1;i++){if(Vector3.Cross(p[i].v-p[0].v,p[i+1].v-p[0].v).sqrMagnitude<.00000001f)continue;tr.Add(n);tr.Add(n+i);tr.Add(n+i+1);}}
 static void Outside(List<P> tri,Transform wt,List<Vector3> v,List<int> tr){AddPoly(Clip(tri,0,X0,false),wt,v,tr);AddPoly(Clip(tri,0,X1,true),wt,v,tr);var m=Clip(Clip(tri,0,X0,true),0,X1,false);AddPoly(Clip(m,1,Z0,false),wt,v,tr);AddPoly(Clip(m,1,Z1,true),wt,v,tr);}
 static bool Hit(Vector3 a,Vector3 b,Vector3 c){float minx=Mathf.Min(a.x,Mathf.Min(b.x,c.x)),maxx=Mathf.Max(a.x,Mathf.Max(b.x,c.x)),minz=Mathf.Min(a.z,Mathf.Min(b.z,c.z)),maxz=Mathf.Max(a.z,Mathf.Max(b.z,c.z));return maxx>=X0&&minx<=X1&&maxz>=Z0&&minz<=Z1;}
 static bool Wet(Terrain[] ext,float x,float z,float step){float e=step*.5f;for(int dz=-1;dz<=1;dz++)for(int dx=-1;dx<=1;dx++){float px=x+dx*e,pz=z+dz*e;var t=Find(ext,px,pz);if(!t||Ground(t,px,pz)>SeaY-.02f)return false;}return true;}
 [MenuItem("MMUnity/Reference 2026/Clip North Ocean")]
 public static void Run(){
  var s=SceneManager.GetActiveScene();if(s.path!=ScenePath||s.isDirty)throw new Exception("Expected clean active Enroth; do not reload or discard current work");
  Directory.CreateDirectory(Path.GetDirectoryName(Report));var active=Terrain.activeTerrains;var ext=ExtPaths.Select(p=>AssetDatabase.LoadAssetAtPath<TerrainData>(p)).Select(td=>active.First(q=>q.terrainData==td)).ToArray();
  var wt=s.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<Transform>(true)).First(q=>q.name==WaterName);var mf=wt.GetComponent<MeshFilter>();var m=mf.sharedMesh;var ov=m.vertices;var ot=m.triangles;var v=new List<Vector3>(ov.Length+80000);var tr=new List<int>(ot.Length+150000);
  string backup="Backups/NorthAuthored20261007/FinalOceanBefore";Directory.CreateDirectory(backup);foreach(var path in new[]{AssetDatabase.GetAssetPath(m),s.path}){string dst=Path.Combine(backup,Path.GetFileName(path));if(!File.Exists(dst))File.Copy(path,dst);}
  for(int i=0;i<ot.Length;i+=3){Vector3 a=wt.TransformPoint(ov[ot[i]]),b=wt.TransformPoint(ov[ot[i+1]]),c=wt.TransformPoint(ov[ot[i+2]]);if(!Hit(a,b,c)){int n=v.Count;v.Add(ov[ot[i]]);v.Add(ov[ot[i+1]]);v.Add(ov[ot[i+2]]);tr.Add(n);tr.Add(n+1);tr.Add(n+2);}else Outside(new List<P>{new P(a),new P(b),new P(c)},wt,v,tr);}
  const float step=2f;int runs=0,wet=0,cols=(int)((X1-X0)/step);for(float z=Z0;z<Z1;z+=step){int run=-1;for(int ix=0;ix<cols;ix++){float x=X0+ix*step;bool w=Wet(ext,x+1f,z+1f,step);if(w)wet++;if(w&&run<0)run=ix;bool end=run>=0&&(!w||ix==cols-1);if(end){int last=w&&ix==cols-1?ix+1:ix;float ax=X0+run*step,bx=X0+last*step;int n=v.Count;v.Add(wt.InverseTransformPoint(new Vector3(ax,SeaY,z)));v.Add(wt.InverseTransformPoint(new Vector3(ax,SeaY,z+step)));v.Add(wt.InverseTransformPoint(new Vector3(bx,SeaY,z+step)));v.Add(wt.InverseTransformPoint(new Vector3(bx,SeaY,z)));tr.Add(n);tr.Add(n+1);tr.Add(n+2);tr.Add(n);tr.Add(n+2);tr.Add(n+3);run=-1;runs++;}}}
  m.Clear();m.indexFormat=IndexFormat.UInt32;m.SetVertices(v);m.SetTriangles(tr,0);m.RecalculateNormals();m.RecalculateBounds();EditorUtility.SetDirty(m);AssetDatabase.SaveAssets();EditorSceneManager.MarkSceneDirty(s);EditorSceneManager.SaveScene(s);File.WriteAllLines(Report,new[]{"PASS north ocean regenerated","wetCells="+wet,"wetRuns="+runs,"vertices="+m.vertexCount,"triangles="+(m.triangles.Length/3)});Debug.Log("NORTH_OCEAN_CLIP_DONE");
 }
}
