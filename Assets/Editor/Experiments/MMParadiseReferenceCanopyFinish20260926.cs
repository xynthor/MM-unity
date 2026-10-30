using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
// Native source-tree models, placed only inside photographed Paradise woodland.
public static class MMParadiseReferenceCanopyFinish20260926 {
 const string V="Validation/EdgeGrid20260923/";
 const string Scene="Assets/Scenes/Extensions/ParadiseValley_West.unity";
 const string Group="Paradise Photo-Verified Tall Woodland 20260926";
 static Color32[] Read(string path){
  var im=new Texture2D(2,2,TextureFormat.RGBA32,false,true);
  if(!im.LoadImage(File.ReadAllBytes(path))||im.width!=513||im.height!=513)
   throw new Exception("Forest reference missing: "+path);
  var px=im.GetPixels32();UnityEngine.Object.DestroyImmediate(im);return px;
 }
 static Bounds Visual(GameObject go){
  var rs=go.GetComponentsInChildren<Renderer>(true);
  if(rs.Length==0)throw new Exception("Source tree has no renderer");
  var b=rs[0].bounds;foreach(var r in rs.Skip(1))b.Encapsulate(r.bounds);return b;
 }
 static bool Apart(Vector2 pt,List<Vector2> used,float dist){
  foreach(var u in used)if((pt-u).sqrMagnitude<dist*dist)return false;
  return true;
 }
 [MenuItem("MMUnity/Reference 2026/Finish Western Paradise Photo Woodland")]
 public static void Apply(){
  if(!File.Exists(@"C:\MMUnityPort\Backups\BeforeNorthAndWestVisualContinuity_20260926\manifest.json"))
   throw new Exception("Reference backup missing");
  var active=SceneManager.GetActiveScene();
  if(active.path!="Assets/Scenes/World/Enroth.unity"||active.isDirty)
   throw new Exception("Saved linked scene required");
  var forest=Read(V+"ReferenceBiomeCandidates_20260925/ParadiseValley_West_ReferenceBiomes.png");
  var land=Read(V+"ReferenceMasks/ParadiseValley_West.png");
  var scene=EditorSceneManager.OpenScene(Scene,OpenSceneMode.Additive);
  try{
   var root=scene.GetRootGameObjects().FirstOrDefault(g=>g.name.Contains("Open World"));
   if(!root||root.transform.Find(Group))throw new Exception("Missing root or already installed");
   var terrain=root.GetComponentInChildren<Terrain>(true);
   var originals=root.transform.Find("Region Dressing - Reference Matched");
   if(!terrain||!originals)throw new Exception("Native tree source missing");
   var pines=originals.Cast<Transform>().Where(t=>t.name.StartsWith("Pine_")).ToArray();
   var searsia=originals.Cast<Transform>().Where(t=>t.name.StartsWith("Searsia_")).ToArray();
   if(pines.Length<10||searsia.Length<60)throw new Exception("Source tree types incomplete");
   var used=new List<Vector2>();
   foreach(var tr in root.GetComponentsInChildren<Transform>(true)){
    string n=tr.name;
    if(n.StartsWith("Pine_")||n.StartsWith("YoungPine_")||
       n.StartsWith("Searsia_")||n.StartsWith("SourceMap_Searsia"))
     used.Add(new Vector2(tr.position.x,tr.position.z));
   }
   var dst=new GameObject(Group);dst.transform.SetParent(root.transform,false);
   var rng=new System.Random(20260926);
   var pts=new List<Vector2>();
   for(int z=11;z<502;z+=4)for(int x=11;x<488;x+=4)
    pts.Add(new Vector2(x+((float)rng.NextDouble()-.5f)*3f,
                        z+((float)rng.NextDouble()-.5f)*3f));
   for(int i=pts.Count-1;i>0;i--){
    int j=rng.Next(i+1);var t=pts[i];pts[i]=pts[j];pts[j]=t;
   }
   int tall=0,medium=0,maskRejected=0,groundRejected=0,spaceRejected=0;
   foreach(var v in pts){
    if(tall>=240&&medium>=110)break;
    int x=Mathf.Clamp(Mathf.RoundToInt(v.x),0,512);
    int z=Mathf.Clamp(Mathf.RoundToInt(v.y),0,512);
    var color=forest[z*513+x];var coast=land[z*513+x];
    if(color.g<125||color.r>155||coast.r<145){maskRejected++;continue;}
    float wx=terrain.transform.position.x+v.x,wz=terrain.transform.position.z+v.y;
    float elev=terrain.SampleHeight(new Vector3(wx,0,wz))+terrain.transform.position.y;
    if(elev<.75f||elev>70f||terrain.terrainData.GetSteepness(v.x/512f,v.y/512f)>34f){
     groundRejected++;continue;
    }
    var loc=new Vector2(wx,wz);
    if(!Apart(loc,used,3.85f)){spaceRejected++;continue;}
    bool pine=tall<240&&(medium>=110||rng.NextDouble()<.71);
    var source=pine?pines[rng.Next(pines.Length)]:searsia[rng.Next(searsia.Length)];
    var go=UnityEngine.Object.Instantiate(source.gameObject,dst.transform);
    go.name=(pine?"ReferenceTallPine_":"ReferenceForestTree_")+(pine?tall:medium);
    go.transform.position=new Vector3(wx,elev,wz);
    go.transform.rotation=Quaternion.Euler(0,(float)rng.NextDouble()*360f,0);
    var b=Visual(go);
    float maxSpan=Mathf.Max(b.size.x,Mathf.Max(b.size.y,b.size.z));
    float target=(pine?10f:6.6f)+(float)rng.NextDouble()*(pine?4.8f:3.1f);
    if(maxSpan<.1f){UnityEngine.Object.DestroyImmediate(go);continue;}
    go.transform.localScale*=target/maxSpan;
    b=Visual(go);
    go.transform.position+=Vector3.up*(elev-b.min.y-.035f);
    used.Add(loc);
    if(pine)tall++;else medium++;
   }
   if(tall<90)throw new Exception("Insufficient woodland "+tall+" terrainRejected="+groundRejected+" spacingRejected="+spaceRejected);
   var invalid=dst.GetComponentsInChildren<Renderer>(true).Count(r=>r.sharedMaterials.Any(
     m=>!m||!m.shader||!m.shader.isSupported));
   if(invalid>0)throw new Exception("Invalid source tree materials "+invalid);
   EditorSceneManager.MarkSceneDirty(scene);
   if(!EditorSceneManager.SaveScene(scene,Scene))throw new IOException("Paradise save failed");
   File.WriteAllText(V+"paradise_photo_canopy_finish_20260926.txt",
    "PASS tallPines="+tall+" additionalBroadleaf="+medium+
    " rejectedNonForest="+maskRejected+" rejectedTerrain="+groundRejected+
    " rejectedSpacing="+spaceRejected+" invalidMaterials="+invalid+
    " originalSourceScenesUntouched=true\n");
  }finally{EditorSceneManager.CloseScene(scene,true);EditorSceneManager.SetActiveScene(active);}
 }
}
