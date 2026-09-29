using UnityEngine;
using UnityEditor;
using UnityEngine.SceneManagement;
using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
[InitializeOnLoad]
internal static class MMLinkedSourceNorthProfileAudit20260925 {
 const string V="Validation/EdgeGrid20260923/";
 const string G="Assets/World/WorldExtensions/Generated/";
 static readonly string[] N={"SweetWater","Kriegspire","FrozenHighlands","SilverCove"};
 static MMLinkedSourceNorthProfileAudit20260925(){EditorApplication.delayCall+=Run;}
 static void Run(){
  if(!File.Exists(V+"RUN_LINKED_NORTH_SOURCE_AUDIT.flag"))return;
  if(EditorApplication.isCompiling||EditorApplication.isUpdating){EditorApplication.delayCall+=Run;return;}
  File.Delete(V+"RUN_LINKED_NORTH_SOURCE_AUDIT.flag");
  var lines=new List<string>();
  try{
   var sc=SceneManager.GetActiveScene();
   if(sc.path!="Assets/Scenes/World/Enroth.unity"||sc.isDirty)
    throw new Exception("Saved linked scene is required for linked source QA");
   var ts=sc.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<Terrain>(true)).ToArray();
   if(ts.Length!=30)throw new Exception("Incorrect linked count "+ts.Length);
   foreach(string n in N){
    var src=AssetDatabase.LoadAssetAtPath<TerrainData>("Assets/World/"+n+"/Generated/"+n+"Terrain.asset");
    var ext=AssetDatabase.LoadAssetAtPath<TerrainData>(G+n+"_NorthTerrain.asset");
    string intended=G+"LinkedSourceTransitions/"+n+"_LinkedNorthProfile.asset";
    var t=ts.FirstOrDefault(x=>AssetDatabase.GetAssetPath(x.terrainData)==intended);
    if(!src||!ext||!t)throw new Exception("Linked/source/extension terrain absent "+n);
    var a=src.GetHeights(0,0,513,513);
    var b=t.terrainData.GetHeights(0,0,513,513);
    var e=ext.GetHeights(0,0,513,2);
    float seam=0,source=0,side=0,maxDelta=0,maxJump=0;int changed=0;
    for(int x=0;x<513;x++){
     seam=Mathf.Max(seam,Mathf.Abs(b[512,x]-e[0,x])*320f);
     source=Mathf.Max(source,Mathf.Abs(b[512,x]-a[512,x])*320f);
     maxJump=Mathf.Max(maxJump,Mathf.Abs((b[512,x]-b[511,x])-
       (e[1,x]-e[0,x]))*320f);
    }
    for(int z=0;z<513;z++){
     side=Mathf.Max(side,Mathf.Abs(b[z,0]-a[z,0])*320f);
     side=Mathf.Max(side,Mathf.Abs(b[z,512]-a[z,512])*320f);
     for(int x=0;x<513;x++){
      float diff=Mathf.Abs(b[z,x]-a[z,x])*320f;
      maxDelta=Mathf.Max(maxDelta,diff);
      if(diff>.002f)changed++;
     }
    }
    var collider=t.GetComponent<TerrainCollider>();
    if(!collider||collider.terrainData!=t.terrainData)
     throw new Exception(n+" linked collider uses original heights");
    if(seam>.1f||source>.001f||side>.001f||
      changed<100)
     throw new Exception(n+" linked north QA failed seam="+seam+
       " source="+source+" side="+side+" changed="+changed);
    lines.Add(n+",linkedAsset="+intended+",modifiedVertices="+changed+
      ",maxElevationDeltaM="+maxDelta.ToString("F3")+
      ",maxNorthSlopeJump="+maxJump.ToString("F3")+
      ",edgeErrorM="+seam.ToString("F5")+
      ",canonicalEdgeErrorM="+source.ToString("F6")+
      ",sideErrorM="+side.ToString("F6"));
   }
   var ds=AssetDatabase.LoadAssetAtPath<TerrainData>(
    "Assets/World/DragonIsle/Generated/DragonIsleSouthTerrain.asset");
   if(!ds)throw new Exception("Dragon South missing");
   var pond=ds.GetHeights(458,185,38,46);int wet=0;
   for(int z=0;z<46;z++)for(int x=0;x<38;x++)
    if(-24f+pond[z,x]*320f<=.1f)wet++;
   lines.Add("DRAGON_SOUTH_POND,wetSamples="+wet);
   if(wet!=150)throw new Exception("Dragon South pond reference lost cells "+wet);
   File.WriteAllLines(V+"linked_source_north_profile_QA_20260925.csv",lines);
   File.WriteAllText(V+"linked_source_north_profile_QA_runtime.txt","PASS 30 tiles, four linked north profiles and pond 150/150\n");
  }catch(Exception ex){
   File.WriteAllText(V+"linked_source_north_profile_QA_runtime.txt","FAIL "+ex+"\n");
   Debug.LogError(ex);
  }
 }
}
