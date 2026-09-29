using System;using System.IO;using System.Linq;using System.Collections.Generic;using UnityEngine;using UnityEditor;using UnityEditor.SceneManagement;using UnityEngine.SceneManagement;
internal class CommandScript:IRunCommand{
 const float Water=.12f;
 float ClampSide(float oldW,float newW){if(oldW<Water)return Mathf.Min(newW,Water-.001f);return Mathf.Max(newW,Water+.001f);}
 public void Execute(ExecutionResult result){
  for(int i=0;i<SceneManager.sceneCount;i++)if(SceneManager.GetSceneAt(i).isDirty)throw new Exception("Unsaved scene");
  var s=EditorSceneManager.OpenScene("Assets/Scenes/World/Enroth.unity",OpenSceneMode.Single);
  var ts=s.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<Terrain>(true)).ToArray();
  var touched=new HashSet<TerrainData>();int pairs=0,samples=0;float beforeMax=0;
  foreach(var a in ts)foreach(var b in ts){if(a==b)continue;var pa=a.transform.position;var pb=b.transform.position;var sa=a.terrainData.size;bool east=Mathf.Abs(pa.x+sa.x-pb.x)<.01f&&Mathf.Abs(pa.z-pb.z)<.01f;bool north=Mathf.Abs(pa.z+sa.z-pb.z)<.01f&&Mathf.Abs(pa.x-pb.x)<.01f;if(!east&&!north)continue;
    var ad=a.terrainData;var bd=b.terrainData;int n=ad.heightmapResolution;if(n!=bd.heightmapResolution)throw new Exception("Resolution mismatch");
    var ah=ad.GetHeights(0,0,n,n);var bh=bd.GetHeights(0,0,n,n);bool changed=false;pairs++;
    for(int i=0;i<n;i++){int ax=east?n-1:i,az=east?i:n-1,bx=east?0:i,bz=east?i:0;float wa=pa.y+ah[az,ax]*ad.size.y,wb=pb.y+bh[bz,bx]*bd.size.y;float gap=Mathf.Abs(wa-wb);beforeMax=Mathf.Max(beforeMax,gap);if(gap<=.001f)continue;float target=(wa+wb)*.5f;float ca=target-wa,cb=target-wb;
      for(int d=0;d<4;d++){float f=1f-d/4f;int iax=east?n-1-d:i,iaz=east?i:n-1-d,ibx=east?d:i,ibz=east?i:d;float oaw=pa.y+ah[iaz,iax]*ad.size.y,obw=pb.y+bh[ibz,ibx]*bd.size.y;float naw=ClampSide(oaw,oaw+ca*f),nbw=ClampSide(obw,obw+cb*f);ah[iaz,iax]=(naw-pa.y)/ad.size.y;bh[ibz,ibx]=(nbw-pb.y)/bd.size.y;}
      changed=true;samples++;
    }
    if(changed){ad.SetHeights(0,0,ah);bd.SetHeights(0,0,bh);EditorUtility.SetDirty(ad);EditorUtility.SetDirty(bd);touched.Add(ad);touched.Add(bd);}
  }
  foreach(var td in touched)AssetDatabase.SaveAssetIfDirty(td);
  float afterMax=0,wetMax=0;int disagree=0;
  foreach(var a in ts)foreach(var b in ts){if(a==b)continue;var pa=a.transform.position;var pb=b.transform.position;var sa=a.terrainData.size;bool east=Mathf.Abs(pa.x+sa.x-pb.x)<.01f&&Mathf.Abs(pa.z-pb.z)<.01f;bool north=Mathf.Abs(pa.z+sa.z-pb.z)<.01f&&Mathf.Abs(pa.x-pb.x)<.01f;if(!east&&!north)continue;for(int i=0;i<=512;i++){float u=i/512f;float wa=pa.y+a.terrainData.GetInterpolatedHeight(east?1:u,east?u:1),wb=pb.y+b.terrainData.GetInterpolatedHeight(east?0:u,east?u:0);float g=Mathf.Abs(wa-wb);afterMax=Mathf.Max(afterMax,g);if(wa<Water&&wb<Water)wetMax=Mathf.Max(wetMax,g);if((wa<Water)!=(wb<Water))disagree++;}}
  Directory.CreateDirectory("Validation/FinalWorld");File.WriteAllText("Validation/FinalWorld/seam_fix.txt","pairs="+pairs+"\ncorrected_edge_samples="+samples+"\nterrain_assets_touched="+touched.Count+"\nmax_gap_before_m="+beforeMax+"\nmax_gap_after_m="+afterMax+"\nwet_wet_max_gap_after_m="+wetMax+"\nwet_dry_disagreements="+disagree+"\n");
  if(afterMax>.0101f||disagree!=0)throw new Exception("Seam verification failed max="+afterMax+" disagreements="+disagree);
  result.Log("Seams fixed max "+beforeMax+" -> "+afterMax+"m; wet/dry disagreements=0");
 }
}