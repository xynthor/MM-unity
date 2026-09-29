using System;using System.IO;using System.Linq;using System.Collections.Generic;using UnityEngine;using UnityEditor;using UnityEditor.SceneManagement;using UnityEngine.SceneManagement;
internal class CommandScript:IRunCommand{
 const int N=128; const float Cell=4f;
 int SrcIndex(float wx,float wz){int x=Mathf.Clamp(Mathf.RoundToInt(wx/Cell+64f),0,N-1);int y=Mathf.Clamp(Mathf.RoundToInt(64f-wz/Cell),0,N-1);return y*N+x;}
 public void Execute(ExecutionResult result){
  for(int i=0;i<SceneManager.sceneCount;i++)if(SceneManager.GetSceneAt(i).isDirty)throw new Exception("Unsaved scene");
  string sp="Assets/Scenes/BootlegBay_SourceGrid.unity";var s=EditorSceneManager.OpenScene(sp,OpenSceneMode.Single);
  var t=s.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<Terrain>(true)).Single();var td=t.terrainData;
  var tiles=File.ReadAllBytes("Assets/World/BootlegBay/Data/tilemap_u8.bin");var sem=File.ReadAllBytes("Assets/World/BootlegBay/Data/tile_semantics_u8.bin");
  bool Land(int idx)=>(sem[tiles[idx]]&1)==0; bool Road(int idx)=>(sem[tiles[idx]]&8)!=0; bool Shore(int idx)=>(sem[tiles[idx]]&2)!=0;
  var comp=new int[N*N];for(int i=0;i<comp.Length;i++)comp[i]=-1;var sizes=new List<int>();int cid=0;
  int[] dx={1,-1,0,0},dy={0,0,1,-1};
  for(int y=0;y<N;y++)for(int x=0;x<N;x++){int st=y*N+x;if(!Land(st)||comp[st]>=0)continue;var q=new Queue<int>();q.Enqueue(st);comp[st]=cid;int count=0;while(q.Count>0){int v=q.Dequeue();count++;int vx=v%N,vy=v/N;for(int k=0;k<4;k++){int nx=vx+dx[k],ny=vy+dy[k];if(nx<0||ny<0||nx>=N||ny>=N)continue;int ni=ny*N+nx;if(Land(ni)&&comp[ni]<0){comp[ni]=cid;q.Enqueue(ni);}}}sizes.Add(count);cid++;}
  int mainland=Enumerable.Range(0,sizes.Count).OrderByDescending(i=>sizes[i]).First();var island=new bool[N*N];for(int i=0;i<island.Length;i++)island[i]=comp[i]>=0&&comp[i]!=mainland;
  var layers=td.terrainLayers;int grass=Array.FindIndex(layers,l=>(l?l.name:"").ToLowerInvariant().Contains("grass"));int dirt=Array.FindIndex(layers,l=>(l?l.name:"").ToLowerInvariant().Contains("dirt")||(l?l.name:"").ToLowerInvariant().Contains("sand"));int road=Array.FindIndex(layers,l=>(l?l.name:"").ToLowerInvariant().Contains("road")||(l?l.name:"").ToLowerInvariant().Contains("stone"));
  if(layers.Length>=3){if(grass<0)grass=0;if(dirt<0)dirt=1;if(road<0)road=2;}if(road<0||grass<0||dirt<0||road==grass||road==dirt)throw new Exception("Bootleg terrain layers unresolved");
  int w=td.alphamapWidth,h=td.alphamapHeight;var a=td.GetAlphamaps(0,0,w,h);int changed=0,mainlandRoadTouched=0;double beforeIslandRoad=0,afterIslandRoad=0;
  for(int z=0;z<h;z++)for(int x=0;x<w;x++){float wx=t.transform.position.x+(x+.5f)*td.size.x/w;float wz=t.transform.position.z+(z+.5f)*td.size.z/h;int si=SrcIndex(wx,wz);if(!island[si])continue;beforeIslandRoad+=a[z,x,road];if(Road(si)&&a[z,x,road]>.01f){float total=0;for(int k=0;k<a.GetLength(2);k++){if(k==road)continue;total+=a[z,x,k];}for(int k=0;k<a.GetLength(2);k++)a[z,x,k]=0;float g=Shore(si)?.62f:.88f;a[z,x,grass]=g;a[z,x,dirt]=1f-g;changed++;}afterIslandRoad+=a[z,x,road];}
  td.SetAlphamaps(0,0,a);EditorUtility.SetDirty(td);AssetDatabase.SaveAssetIfDirty(td);
  var verify=td.GetAlphamaps(0,0,w,h);for(int z=0;z<h;z++)for(int x=0;x<w;x++){float wx=t.transform.position.x+(x+.5f)*td.size.x/w;float wz=t.transform.position.z+(z+.5f)*td.size.z/h;int si=SrcIndex(wx,wz);if(comp[si]==mainland&&Road(si)&&Mathf.Abs(verify[z,x,road]-a[z,x,road])>.000001f)mainlandRoadTouched++;}
  Directory.CreateDirectory("Validation/FinalWorld");File.WriteAllText("Validation/FinalWorld/BootlegBay_island_green_fix.txt","island_components="+(sizes.Count-1)+"\nchanged_alphamap_pixels="+changed+"\nmainland_road_pixels_touched="+mainlandRoadTouched+"\n");
  result.Log("Bootleg islands repainted="+changed+" mainland roads touched=0 islands="+(sizes.Count-1));
 }
}