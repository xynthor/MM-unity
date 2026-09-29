using UnityEngine;
using UnityEditor;
using UnityEngine.SceneManagement;
using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
[InitializeOnLoad]
internal static class MMNorthC1TransitionDryRun20260925{
 const string V="Validation/EdgeGrid20260923/";
 const string G="Assets/World/WorldExtensions/Generated/";
 static readonly string[] Names={"SweetWater","Kriegspire","FrozenHighlands","SilverCove"};
 static MMNorthC1TransitionDryRun20260925(){EditorApplication.delayCall+=Run;}
 static float S(float a,float b,float v){float t=Mathf.Clamp01((v-a)/(b-a));return t*t*(3f-2f*t);}
 static float H(float a,float b,float da,float db,float t,int span){
  float t2=t*t,t3=t2*t;
  return (2*t3-3*t2+1f)*a+(t3-2*t2+t)*da*span+
         (-2*t3+3*t2)*b+(t3-t2)*db*span;
 }
 static void Run(){
  if(!File.Exists(V+"RUN_NORTH_C1_DRYRUN.flag"))return;
  File.Delete(V+"RUN_NORTH_C1_DRYRUN.flag");
  var lines=new List<string>();
  try{
   var sc=SceneManager.GetActiveScene();
   if(sc.path!="Assets/Scenes/World/Enroth.unity"||sc.isDirty)
    throw new Exception("Saved linked scene required");
   var linked=sc.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<Terrain>(true)).ToArray();
   foreach(string n in Names){
    var original=AssetDatabase.LoadAssetAtPath<TerrainData>("Assets/World/"+n+"/Generated/"+n+"Terrain.asset");
    var ext=AssetDatabase.LoadAssetAtPath<TerrainData>(G+n+"_NorthTerrain.asset");
    var clone=AssetDatabase.LoadAssetAtPath<TerrainData>(G+"LinkedSourceTransitions/"+n+"_LinkedNorthProfile.asset");
    var active=n=="SilverCove"?original:clone;
    if(!active||!ext||!original)throw new Exception("Missing terrain "+n);
    var t=linked.FirstOrDefault(q=>q.terrainData==active);
    if(!t)throw new Exception("Linked source terrain missing "+n);
    var h=active.GetHeights(0,0,513,513);var e=ext.GetHeights(0,0,513,2);
    var img=new Texture2D(2,2);img.LoadImage(File.ReadAllBytes(V+"ReferenceMasks/"+n+"_North.png"));
    var mask=img.GetPixels32();UnityEngine.Object.DestroyImmediate(img);
    int band=n=="SilverCove"?112:72;
    var severity=new float[513];
    for(int x=8;x<=504;x++){
     float edge=-24f+h[512,x]*320f,inside=-24f+h[512-band,x]*320f;
     if(mask[16*513+x].r<147||edge<.15f||inside<3f)continue;
     float jump=Mathf.Abs((h[512,x]-h[511,x]-(e[1,x]-e[0,x]))*320f);
     severity[x]=S(.18f,.9f,jump)*S(143f,156f,mask[16*513+x].r);
    }
    int selected=0,changed=0,seaRisk=0;float maxMove=0,maxAfter=0,maxBefore=0;
    var extremes=new List<string>();
    for(int x=8;x<=504;x++){
     float total=0,v=0;
     for(int dx=-8;dx<=8;dx++){float k=9-Mathf.Abs(dx);total+=k;v+=k*severity[x+dx];}
     float w=S(.04f,.34f,v/total)*S(8f,28f,x)*S(8f,28f,512f-x);
     float prevJump=Mathf.Abs((h[512,x]-h[511,x]-(e[1,x]-e[0,x]))*320f);
     maxBefore=Mathf.Max(maxBefore,prevJump);
     if(w<.01f)continue;selected++;
     int begin=512-band;
     float a=h[begin,x],b=h[512,x];
     float da=(h[begin+1,x]-h[begin-1,x])*.5f,db=e[1,x]-e[0,x];
     float lo=Mathf.Min(a,b)-.2f/320f,hi=Mathf.Max(a,b)+.5f/320f;
     float last=h[511,x];
     for(int z=begin+1;z<512;z++){
      float t0=(z-begin)/(float)band;
      float candidate=Mathf.Clamp(H(a,b,da,db,t0,band),lo,hi);
      float after=Mathf.Lerp(h[z,x],candidate,w);
      float mov=Mathf.Abs(after-h[z,x])*320f;
      if(mov>.003f)changed++;
      maxMove=Mathf.Max(maxMove,mov);
      if(-24f+after*320f<-.10f && -24f+h[z,x]*320f>.15f)seaRisk++;
      if(z==511)last=after;
     }
     float jump=Mathf.Abs((h[512,x]-last-db)*320f);
     maxAfter=Mathf.Max(maxAfter,jump);
     if(jump>.35f)extremes.Add("  x="+x+" oldJump="+prevJump.ToString("F2")+
       " predictedJump="+jump.ToString("F2")+" weight="+w.ToString("F2")+
       " edgeY="+(-24f+h[512,x]*320f).ToString("F2"));
    }
    int modelCount=0,buildings=0;var sample=new List<string>();
    var root=t.transform.parent;
    if(root){
     float north=t.transform.position.z+512-band;
     foreach(var r in root.GetComponentsInChildren<MeshRenderer>(true)){
      if(r.bounds.center.z<north||r.bounds.center.z>north+band+4f)continue;
      if(r.name.StartsWith("Water",StringComparison.OrdinalIgnoreCase))continue;
      modelCount++;
      string lower=r.name.ToLowerInvariant();
      if(lower.Contains("house")||lower.Contains("building")||lower.Contains("road")||
         lower.Contains("bridge")||lower.Contains("wall")){
       buildings++;if(sample.Count<8)sample.Add(r.name);
      }
     }
    }
    lines.Add(n+",band_m="+band+",eligibleColumns="+selected+
     ",wouldChangeVertices="+changed+",largestLinkedHeightMoveM="+maxMove.ToString("F3")+
     ",maxExistingSlopeJumpMPerM="+maxBefore.ToString("F3")+
     ",maxPredictedSelectedSlopeJump="+maxAfter.ToString("F3")+
     ",newlyFloodedCellCandidates="+seaRisk+",northernStripRenderers="+modelCount+
     ",possibleBuildingOrRoadRenderers="+buildings);
    lines.AddRange(extremes.Take(12));
    if(sample.Count>0)lines.Add("  POSSIBLE_STRUCTURES: "+string.Join(";",sample));
   }
  }catch(Exception ex){lines.Add("FAILED "+ex);}
  File.WriteAllLines(V+"north_c1_transition_dryrun_20260925.txt",lines);
 }
}

// retrigger full post C1 QA 20260925
