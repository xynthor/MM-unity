using System;using System.IO;using System.Linq;using System.Collections.Generic;using UnityEngine;using UnityEditor.SceneManagement;using UnityEngine.SceneManagement;
internal class CommandScript:IRunCommand{
 const string Out="Validation/CanalAudit_20260920";
 public void Execute(ExecutionResult result){
  string scene=File.ReadAllText(Out+"/morph_next.txt").Trim();
  var s=EditorSceneManager.OpenScene("Assets/Scenes/"+scene+".unity",OpenSceneMode.Single);
  var terr=s.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<Terrain>(true)).FirstOrDefault();
  if(!terr){result.Log(scene+" no terrain");return;}
  var td=terr.terrainData; var t0=terr.transform.position;
  var rows=File.ReadAllLines(Out+"/candidate_clusters.tsv").Select(l=>l.Split('\t')).Where(p=>p.Length>=10&&p[1]==scene).ToArray();
  var lines=new List<string>{"id,scene,cx,cz,waterY,centerH,depth,lowFrac_r5,lowFrac_r10,lowFrac_r20,ringHighFrac_r5_10,borderDist,nearRoad,nearBuilding,nearWaterNamed"};
  foreach(var p in rows){
    int id=int.Parse(p[0]); float cx=float.Parse(p[2],System.Globalization.CultureInfo.InvariantCulture),cz=float.Parse(p[3],System.Globalization.CultureInfo.InvariantCulture),wy=float.Parse(p[8],System.Globalization.CultureInfo.InvariantCulture);
    Func<float,float,float> H=(x,z)=>terr.SampleHeight(new Vector3(x,0,z))+t0.y;
    float ch=H(cx,cz);
    Func<float,float> lowFrac=(r)=>{
      int n=0,l=0; float step=1f;
      for(float z=cz-r;z<=cz+r;z+=step)for(float x=cx-r;x<=cx+r;x+=step){if((x-cx)*(x-cx)+(z-cz)*(z-cz)>r*r)continue;n++;if(H(x,z)<wy-.02f)l++;}
      return n>0?(float)l/n:0f;};
    int rn=0,rh=0; for(float z=cz-10;z<=cz+10;z+=1f)for(float x=cx-10;x<=cx+10;x+=1f){float d=Mathf.Sqrt((x-cx)*(x-cx)+(z-cz)*(z-cz));if(d<5||d>10)continue;rn++;if(H(x,z)>wy+.25f)rh++;}
    float border=Mathf.Min(Mathf.Min(cx-t0.x,t0.x+td.size.x-cx),Mathf.Min(cz-t0.z,t0.z+td.size.z-cz));
    int road=0,bld=0,water=0;
    foreach(var tr in s.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<Transform>(true))){
      var pos=tr.position; if(Mathf.Abs(pos.x-cx)>25||Mathf.Abs(pos.z-cz)>25) continue;
      string n=tr.name.ToLowerInvariant();
      if(n.Contains("road")||n.Contains("bridge")||n.Contains("path")) road++;
      if(n.Contains("house")||n.Contains("building")||n.Contains("castle")||n.Contains("tower")||n.Contains("inn")||n.Contains("temple")||n.Contains("shop")) bld++;
      if(n.Contains("water")||n.Contains("river")||n.Contains("canal")||n.Contains("lake")||n.Contains("sea")) water++;
    }
    lines.Add(string.Join(",",new[]{id.ToString(),scene,cx.ToString("F2"),cz.ToString("F2"),wy.ToString("F3"),ch.ToString("F3"),(wy-ch).ToString("F3"),lowFrac(5).ToString("F3"),lowFrac(10).ToString("F3"),lowFrac(20).ToString("F3"),(rn>0?(float)rh/rn:0f).ToString("F3"),border.ToString("F2"),road.ToString(),bld.ToString(),water.ToString()}));
  }
  File.WriteAllLines(Out+"/"+scene+"_morph.csv",lines); result.Log(scene+" morph="+rows.Length);
 }
}