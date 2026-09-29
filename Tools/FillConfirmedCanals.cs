using System;using System.IO;using System.Linq;using System.Collections.Generic;using UnityEngine;using UnityEngine.Rendering;using UnityEditor;using UnityEditor.SceneManagement;
internal class CommandScript:IRunCommand{
 const string Out="Validation/CanalApproval_20260920", Backup="Backups/CanalWater_20260920";
 struct P{public int x,z;public P(int X,int Z){x=X;z=Z;}}
 struct Spec{public string scene;public float x,z;public string tag;public Spec(string S,float X,float Z,string T){scene=S;x=X;z=Z;tag=T;}}
 bool Inside(Vector2 p,Vector2 a,Vector2 b,Vector2 c){float d1=(p.x-b.x)*(a.y-b.y)-(a.x-b.x)*(p.y-b.y);float d2=(p.x-c.x)*(b.y-c.y)-(b.x-c.x)*(p.y-c.y);float d3=(p.x-a.x)*(c.y-a.y)-(c.x-a.x)*(p.y-a.y);bool neg=d1<0||d2<0||d3<0,pos=d1>0||d2>0||d3>0;return !(neg&&pos);}
 bool ProtectedName(string n){n=n.ToLowerInvariant();return n.Contains("road")||n.Contains("bridge")||n.Contains("house")||n.Contains("building")||n.Contains("castle")||n.Contains("tower")||n.Contains("temple")||n.Contains("shop")||n.Contains("inn");}
 public void Execute(ExecutionResult result){
  Directory.CreateDirectory(Out);Directory.CreateDirectory(Backup);
  var specs=new[]{
   new Spec("FreeHaven",25.1f,-5.7f,"FreeHaven"),
   new Spec("MireOfTheDamned",29.7f,130.6f,"Mire"),
   new Spec("FrozenHighlands",-158.56f,21.78f,"WhiteCap")
  };
  var summary=new List<string>{"scene|mesh|waterY|cells|vertsAdded|trisAdded|minx|maxx|minz|maxz|protectedOverlap|terrainChanged"};
  foreach(var sp in specs){
   var s=EditorSceneManager.OpenScene("Assets/Scenes/"+sp.scene+".unity",OpenSceneMode.Single);
   var terr=s.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<Terrain>(true)).Single();
   var td=terr.terrainData;int N=td.heightmapResolution-1;float sx=td.size.x/N,sz=td.size.z/N;
   var wr=s.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<MeshRenderer>(true)).First(x=>x.name=="Internal Water - Smooth");
   var mf=wr.GetComponent<MeshFilter>();var m=mf.sharedMesh;if(!m)throw new Exception(sp.scene+" water mesh missing");if(m.subMeshCount!=1)throw new Exception(sp.scene+" subMeshCount="+m.subMeshCount);
   float wy=wr.bounds.center.y;var oldv=m.vertices;var covered=new bool[N,N];
   var ix=m.GetIndices(0);
   for(int i=0;i+2<ix.Length;i+=3){Vector3 aw=wr.transform.TransformPoint(oldv[ix[i]]),bw=wr.transform.TransformPoint(oldv[ix[i+1]]),cw=wr.transform.TransformPoint(oldv[ix[i+2]]);Vector2 a=new Vector2(aw.x,aw.z),b=new Vector2(bw.x,bw.z),c=new Vector2(cw.x,cw.z);int x0=Mathf.Max(0,Mathf.FloorToInt((Mathf.Min(a.x,Mathf.Min(b.x,c.x))-terr.transform.position.x)/sx)-1),x1=Mathf.Min(N-1,Mathf.CeilToInt((Mathf.Max(a.x,Mathf.Max(b.x,c.x))-terr.transform.position.x)/sx)+1),z0=Mathf.Max(0,Mathf.FloorToInt((Mathf.Min(a.y,Mathf.Min(b.y,c.y))-terr.transform.position.z)/sz)-1),z1=Mathf.Min(N-1,Mathf.CeilToInt((Mathf.Max(a.y,Mathf.Max(b.y,c.y))-terr.transform.position.z)/sz)+1);for(int z=z0;z<=z1;z++)for(int x=x0;x<=x1;x++){Vector2 p=new Vector2(terr.transform.position.x+(x+.5f)*sx,terr.transform.position.z+(z+.5f)*sz);if(Inside(p,a,b,c))covered[x,z]=true;}}
   var low=new bool[N,N];for(int z=0;z<N;z++)for(int x=0;x<N;x++){float h=terr.transform.position.y+td.GetInterpolatedHeight((x+.5f)/N,(z+.5f)/N);low[x,z]=h<wy-.02f&&!covered[x,z];}
   int tx=Mathf.Clamp(Mathf.FloorToInt((sp.x-terr.transform.position.x)/sx),0,N-1),tz=Mathf.Clamp(Mathf.FloorToInt((sp.z-terr.transform.position.z)/sz),0,N-1);
   P seed=new P(-1,-1);float best=999999f;for(int z=Mathf.Max(0,tz-25);z<=Mathf.Min(N-1,tz+25);z++)for(int x=Mathf.Max(0,tx-25);x<=Mathf.Min(N-1,tx+25);x++)if(low[x,z]){float d=(x-tx)*(x-tx)+(z-tz)*(z-tz);if(d<best){best=d;seed=new P(x,z);}}
   if(seed.x<0)throw new Exception(sp.scene+" no low dry canal cell near target");
   var seen=new bool[N,N];var q=new Queue<P>();var comp=new List<P>();int[] dx={1,-1,0,0},dz={0,0,1,-1};q.Enqueue(seed);seen[seed.x,seed.z]=true;while(q.Count>0){var p=q.Dequeue();comp.Add(p);for(int k=0;k<4;k++){int nx=p.x+dx[k],nz=p.z+dz[k];if(nx>=0&&nz>=0&&nx<N&&nz<N&&low[nx,nz]&&!seen[nx,nz]){seen[nx,nz]=true;q.Enqueue(new P(nx,nz));}}}
   if(comp.Count<3||comp.Count>4000)throw new Exception(sp.scene+" suspicious component size "+comp.Count);
   var protectedRs=s.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<Renderer>(true)).Where(r=>r.enabled&&r.gameObject.activeInHierarchy&&ProtectedName(r.name)).ToArray();
   int overlap=0;foreach(var p in comp){float wx=terr.transform.position.x+(p.x+.5f)*sx,wz=terr.transform.position.z+(p.z+.5f)*sz;foreach(var rr in protectedRs){var b=rr.bounds;if(wx>=b.min.x&&wx<=b.max.x&&wz>=b.min.z&&wz<=b.max.z&&wy>=b.min.y-.2f&&wy<=b.max.y+.2f){overlap++;break;}}}
   if(overlap>0)throw new Exception(sp.scene+" protected road/building overlap cells="+overlap);
   string ap=AssetDatabase.GetAssetPath(m);string bak=Backup+"/"+sp.scene+"_InternalWaterSmooth.asset";if(!File.Exists(bak))File.Copy(ap,bak);
   var verts=new List<Vector3>(oldv);var tris=new List<int>(ix);var oldNormals=m.normals;var normals=(oldNormals!=null&&oldNormals.Length==oldv.Length)?new List<Vector3>(oldNormals):null;var oldUv=m.uv;var uvs=(oldUv!=null&&oldUv.Length==oldv.Length)?new List<Vector2>(oldUv):null;
   foreach(var p in comp){float x0=terr.transform.position.x+p.x*sx,x1=x0+sx,z0=terr.transform.position.z+p.z*sz,z1=z0+sz;int b=verts.Count;Vector3 w0=new Vector3(x0,wy,z0),w1=new Vector3(x1,wy,z0),w2=new Vector3(x1,wy,z1),w3=new Vector3(x0,wy,z1);verts.Add(wr.transform.InverseTransformPoint(w0));verts.Add(wr.transform.InverseTransformPoint(w1));verts.Add(wr.transform.InverseTransformPoint(w2));verts.Add(wr.transform.InverseTransformPoint(w3));tris.Add(b);tris.Add(b+2);tris.Add(b+1);tris.Add(b);tris.Add(b+3);tris.Add(b+2);if(normals!=null){normals.Add(Vector3.up);normals.Add(Vector3.up);normals.Add(Vector3.up);normals.Add(Vector3.up);}if(uvs!=null){uvs.Add(new Vector2(x0*.02f,z0*.02f));uvs.Add(new Vector2(x1*.02f,z0*.02f));uvs.Add(new Vector2(x1*.02f,z1*.02f));uvs.Add(new Vector2(x0*.02f,z1*.02f));}}
   if(verts.Count>65535)m.indexFormat=IndexFormat.UInt32;m.SetVertices(verts);m.SetTriangles(tris,0,true);if(normals!=null)m.SetNormals(normals);if(uvs!=null)m.SetUVs(0,uvs);m.RecalculateBounds();EditorUtility.SetDirty(m);AssetDatabase.SaveAssetIfDirty(m);
   int minx=comp.Min(p=>p.x),maxx=comp.Max(p=>p.x),minz=comp.Min(p=>p.z),maxz=comp.Max(p=>p.z);
   summary.Add(sp.scene+"|"+ap+"|"+wy.ToString("F3")+"|"+comp.Count+"|"+(comp.Count*4)+"|"+(comp.Count*2)+"|"+(terr.transform.position.x+minx*sx).ToString("F2")+"|"+(terr.transform.position.x+(maxx+1)*sx).ToString("F2")+"|"+(terr.transform.position.z+minz*sz).ToString("F2")+"|"+(terr.transform.position.z+(maxz+1)*sz).ToString("F2")+"|0|0");
  }
  File.WriteAllLines(Out+"/CANAL_FILL_RESULT.txt",summary);result.Log("Confirmed canals filled: 3 regions; terrain untouched");
 }
}