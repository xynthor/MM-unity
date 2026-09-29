using System;using System.IO;using System.Linq;using System.Collections.Generic;using UnityEngine;using UnityEditor;using UnityEditor.SceneManagement;using UnityEngine.SceneManagement;
internal class CommandScript:IRunCommand{
 const float W=.12f;
 float C(float oldW,float n){return oldW<W?Mathf.Min(n,W-.001f):Mathf.Max(n,W+.001f);}
 public void Execute(ExecutionResult result){
  for(int i=0;i<SceneManager.sceneCount;i++)if(SceneManager.GetSceneAt(i).isDirty)throw new Exception("Unsaved scene");
  var s=EditorSceneManager.OpenScene("Assets/Scenes/Enroth_Linked_OpenWorld.unity",OpenSceneMode.Single);var ts=s.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<Terrain>(true)).ToArray();var touched=new HashSet<TerrainData>();
  for(int iter=0;iter<20;iter++){
   foreach(var a in ts)foreach(var b in ts){if(a==b)continue;var pa=a.transform.position;var pb=b.transform.position;var sz=a.terrainData.size;bool east=Mathf.Abs(pa.x+sz.x-pb.x)<.01f&&Mathf.Abs(pa.z-pb.z)<.01f;bool north=Mathf.Abs(pa.z+sz.z-pb.z)<.01f&&Mathf.Abs(pa.x-pb.x)<.01f;if(!east&&!north)continue;var ad=a.terrainData;var bd=b.terrainData;int n=ad.heightmapResolution;var ah=ad.GetHeights(0,0,n,n);var bh=bd.GetHeights(0,0,n,n);for(int i=0;i<n;i++){int ax=east?n-1:i,az=east?i:n-1,bx=east?0:i,bz=east?i:0;float wa=pa.y+ah[az,ax]*ad.size.y,wb=pb.y+bh[bz,bx]*bd.size.y;float target=(wa+wb)*.5f;ah[az,ax]=(C(wa,target)-pa.y)/ad.size.y;bh[bz,bx]=(C(wb,target)-pb.y)/bd.size.y;}ad.SetHeights(0,0,ah);bd.SetHeights(0,0,bh);touched.Add(ad);touched.Add(bd);}
  }
  foreach(var td in touched){EditorUtility.SetDirty(td);AssetDatabase.SaveAssetIfDirty(td);}
  float max=0,wet=0;int dis=0;foreach(var a in ts)foreach(var b in ts){if(a==b)continue;var pa=a.transform.position;var pb=b.transform.position;var sz=a.terrainData.size;bool east=Mathf.Abs(pa.x+sz.x-pb.x)<.01f&&Mathf.Abs(pa.z-pb.z)<.01f;bool north=Mathf.Abs(pa.z+sz.z-pb.z)<.01f&&Mathf.Abs(pa.x-pb.x)<.01f;if(!east&&!north)continue;for(int i=0;i<=512;i++){float u=i/512f;float wa=pa.y+a.terrainData.GetInterpolatedHeight(east?1:u,east?u:1),wb=pb.y+b.terrainData.GetInterpolatedHeight(east?0:u,east?u:0);float g=Mathf.Abs(wa-wb);max=Mathf.Max(max,g);if(wa<W&&wb<W)wet=Mathf.Max(wet,g);if((wa<W)!=(wb<W))dis++;}}
  Directory.CreateDirectory("Validation/FinalWorld");File.WriteAllText("Validation/FinalWorld/seam_converge.txt","iterations=20\nterrain_assets_touched="+touched.Count+"\nmax_gap_after_m="+max+"\nwet_wet_max_gap_after_m="+wet+"\nwet_dry_disagreements="+dis+"\n");
  if(max>.0101f||dis!=0)throw new Exception("Convergence failed max="+max+" dis="+dis);result.Log("Seam convergence PASS max="+max+"m disagreements="+dis);
 }
}