using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;

// Authority-traced western mainland gets the SAME trees as original Sweet Water.
// Independent from Dragon Isle's original island woodland and canonical source.
public static class MMDragonSouthShoreForest {
 const string Group="Sweet Water Western Shore - Source Forest Continuation";
 const string MaskPath="Validation/EdgeGrid20260923/ReferenceMasks/DragonSouth_SweetWaterWest_MainlandOnly.png";
 static readonly string Log="Validation/EdgeGrid20260923/dragon_south_shore_forest_audit.txt";
 static float Hash01(string name) {
  unchecked{uint h=2166136261;foreach(char c in name){h^=c;h*=16777619;}return(h&0xFFFFFF)/16777215f;}
 }
 static bool MaterialsOK(GameObject o){
  var rs=o.GetComponentsInChildren<Renderer>(true).Where(r=>r.enabled&&r.gameObject.activeInHierarchy).ToArray();
  return rs.Length>0&&rs.All(r=>r.sharedMaterials!=null&&r.sharedMaterials.Length>0&&
   r.sharedMaterials.All(m=>m&&m.shader&&m.shader.isSupported&&m.shader.name!="Hidden/InternalErrorShader"));
 }
 [MenuItem("MMUnity/World/Refine Dragon South Sweet Water Shore Woodland")]
 public static void Apply(){
  var target=EditorSceneManager.OpenScene("Assets/Scenes/Extensions/DragonIsle_South.unity",OpenSceneMode.Single);
  var root=target.GetRootGameObjects().FirstOrDefault(g=>g.name.Contains("Open World"));
  var t=root?root.GetComponentInChildren<Terrain>(true):null;
  if(!t)throw new Exception("Dragon South target terrain missing");
  string file=Path.GetFullPath(MaskPath);
  if(!File.Exists(file))throw new FileNotFoundException("Mainland-only authority coastline not traced",file);
  var tex=new Texture2D(2,2,TextureFormat.RGBA32,false,true);
  if(!tex.LoadImage(File.ReadAllBytes(file))||tex.width!=513||tex.height!=513)
   throw new Exception("Bad authority mainland mask");
  var shore=tex.GetPixels32();UnityEngine.Object.DestroyImmediate(tex);
  var map=MMReferenceEdgeProfiles.Load("DragonIsle_South");

  var old=root.transform.Find(Group);
  if(old)UnityEngine.Object.DestroyImmediate(old.gameObject);
  var group=new GameObject(Group);group.transform.SetParent(root.transform,false);
  var source=EditorSceneManager.OpenScene("Assets/Scenes/Regions/SweetWater.unity",OpenSceneMode.Additive);
  var sr=source.GetRootGameObjects().First(g=>g.name.Contains("Open World"));
  var trees=sr.GetComponentsInChildren<Transform>(true).Where(v=>
    v.name.StartsWith("SourceTree_",StringComparison.OrdinalIgnoreCase) && MaterialsOK(v.gameObject)).ToArray();
  var near=trees.Where(v=>{
    var p=sr.transform.InverseTransformPoint(v.position);
    return p.x>=-255f&&p.x<=-56f;
  }).ToArray();
  if(near.Length<30)throw new Exception("Not enough original Sweet Water trees to continue the forest: "+near.Length);
  int direct=0,additional=0,rejectedCoast=0,rejectedSlope=0,bad=0;
  var occupied=new List<Vector2>();
  var legacyReplacement=new List<Transform>();
  var legacy=root.transform.Find("Reference Woodland - Dragon Isle");
  if(legacy){
   foreach(Transform c in legacy){
    var p=root.transform.InverseTransformPoint(c.position);
    int lx=Mathf.Clamp(Mathf.RoundToInt(p.x+256f),0,512);
    int lz=Mathf.Clamp(Mathf.RoundToInt(p.z+256f),0,512);
    // Replace ONLY old generated Dragon woodland on the authority-supported
    // Sweet Water mainland. Preserve the 149 original island trees in place.
    bool replace=p.x>170f&&lx>=350&&shore[lz*513+lx].r>=128;
    if(replace)legacyReplacement.Add(c);
    else if(p.x>78f)occupied.Add(new Vector2(p.x,p.z));
   }
  }
  bool Place(Transform prefab,float x,float z,bool continuation){
    int ix=Mathf.Clamp(Mathf.RoundToInt(x+256f),0,512);
    int iz=Mathf.Clamp(Mathf.RoundToInt(z+256f),0,512);
    float distance=(map.At(ix,iz).r-128f)*512f/(148f*3f);
    if(ix<350||shore[iz*513+ix].r<128||distance<9f){rejectedCoast++;return false;}
    if(occupied.Any(p=>(p-new Vector2(x,z)).sqrMagnitude<46f))return false;
    Vector3 flat=root.transform.TransformPoint(new Vector3(x,0,z));
    float gy=t.SampleHeight(flat)+t.transform.position.y;
    float slope=t.terrainData.GetSteepness(Mathf.Clamp01(ix/512f),Mathf.Clamp01(iz/512f));
    if(gy<1.35f||gy>65f||slope>28f){rejectedSlope++;return false;}
    var go=UnityEngine.Object.Instantiate(prefab.gameObject);
    go.name="SweetWater_Original_"+prefab.name+"_"+(direct+additional).ToString("D3");
    SceneManager.MoveGameObjectToScene(go,target);
    go.transform.SetParent(group.transform,false);

    go.transform.rotation=prefab.rotation;
    go.transform.localScale=prefab.lossyScale;
    go.transform.position=flat+Vector3.up*gy;
    var renderers=go.GetComponentsInChildren<Renderer>(true).Where(r=>r.enabled&&r.gameObject.activeInHierarchy).ToArray();
    if(renderers.Length==0){UnityEngine.Object.DestroyImmediate(go);bad++;return false;}
    var bounds=renderers[0].bounds;
    foreach(var rr in renderers.Skip(1))bounds.Encapsulate(rr.bounds);
    float span=Mathf.Max(bounds.size.x,Mathf.Max(bounds.size.y,bounds.size.z));
    if(span>24f||!MaterialsOK(go)){UnityEngine.Object.DestroyImmediate(go);bad++;return false;}
    go.transform.position+=Vector3.up*(gy-bounds.min.y-.025f);
    occupied.Add(new Vector2(x,z));
    if(continuation)direct++;else additional++;
    return true;
  }
  foreach(var c in near.OrderBy(v=>sr.transform.InverseTransformPoint(v.position).x)){
   if(direct>=110)break;
   Vector3 p=sr.transform.InverseTransformPoint(c.position);
   // Retain Sweet Water's source-tree north/south order, compressing its
   // western inland distances into the authority mask's actual mainland.
   // Literal edge reflection puts almost every source tree underwater here.
   float x=242f-(p.x+256f)*.30f+(Hash01(c.name+"x")-.5f)*4f;
   float z=p.z+(Hash01(c.name+"z")-.5f)*6f;
   if(x>=-246f&&x<=246f&&z>=-246f&&z<=246f)Place(c,x,z,true);
  }
  var rng=new System.Random(20260925);
  int trials=0;
  while(direct+additional<220&&trials++<60000){
    float x=104f+(float)rng.NextDouble()*141f;
    float z=-238f+(float)rng.NextDouble()*476f;
    var model=near[rng.Next(near.Length)];
    Place(model,x,z,false);
  }
  EditorSceneManager.CloseScene(source,true);
  // Keep the legacy vegetation intact if its source-based replacement fails.
  if(direct<12||direct+additional<200)
   throw new Exception("Source woodland replacement insufficient: mirrored="+direct+
     " total="+(direct+additional)+". Existing island woodland retained.");
  // These are the shorter original generated woodland models. The real
  // Sweet Water canopy is taller and narrower; retain the old plants as
  // understory rather than leaving the mainland visibly bare from above.
  EditorSceneManager.MarkSceneDirty(target);
  if(!EditorSceneManager.SaveScene(target))throw new IOException("Dragon South forest scene save failed");
  Directory.CreateDirectory("Validation/EdgeGrid20260923");
  var stats="SWEET_WATER_WEST_SHORE_TREE_PASS source_models="+near.Length+
    " mirrored="+direct+" supplemental="+additional+" total="+(direct+additional)+
    " rejected_water="+rejectedCoast+" rejected_slope="+rejectedSlope+
    " rejected_material="+bad+" mainland_understory_retained="+legacyReplacement.Count+
    " original_and_understory_legacy_total="+(legacy?legacy.childCount:0)+
    " original_source_scene=UNCHANGED";

  File.WriteAllText(Log,stats+Environment.NewLine);
  AssetDatabase.SaveAssets();
  Debug.Log(stats);
  if(direct+additional<25)
   throw new Exception("Only "+(direct+additional)+" source-style forest trees; inspect "+Log);
 }
}
