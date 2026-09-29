using System;using System.IO;using System.Linq;using System.Collections.Generic;using System.Security.Cryptography;using System.Text;using UnityEditor;using UnityEditor.SceneManagement;using UnityEngine;using UnityEngine.SceneManagement;
internal class CommandScript:IRunCommand{
 const string ScenePath="Assets/Scenes/Enroth_Linked_OpenWorld.unity";
 public void Execute(ExecutionResult result){
  var sc=EditorSceneManager.OpenScene(ScenePath,OpenSceneMode.Single);
  string pre=TransformHash(sc); int disabled=0, unified=0;
  var mat=AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/SourceGridTerrain/UnifiedEnrothWater.mat");
  foreach(var root in sc.GetRootGameObjects()){
   foreach(var region in root.GetComponentsInChildren<Transform>(true).Where(t=>t.parent!=null && t.name.Contains("LINKED")).ToArray()){
    var rs=region.GetComponentsInChildren<MeshRenderer>(true);
    var internalR=rs.FirstOrDefault(r=>r.name=="Internal Water - Smooth");
    bool keepInternal=internalR && internalR.enabled && internalR.gameObject.activeInHierarchy;
    foreach(var r in rs){
      string n=r.name;
      bool top=n.StartsWith("Water - Smoothed",StringComparison.OrdinalIgnoreCase)||n=="Internal Water - Smooth"||n=="Ocean + Central Lake Water";
      if(top && mat && r.sharedMaterial!=mat){r.sharedMaterial=mat;unified++;}
      if(keepInternal && n.StartsWith("Water - Smoothed",StringComparison.OrdinalIgnoreCase) && r.enabled){r.enabled=false;disabled++;}
    }
   }
  }
  string post=TransformHash(sc);
  if(pre!=post)throw new Exception("Transform hash changed");
  EditorSceneManager.MarkSceneDirty(sc);if(!EditorSceneManager.SaveScene(sc,ScenePath))throw new Exception("save failed");
  Capture(sc);
  Directory.CreateDirectory("Validation/LinkedWaterAudit_20260920");
  File.WriteAllText("Validation/LinkedWaterAudit_20260920/fix_result.txt",$"disabledLegacy={disabled}\nunifiedChanged={unified}\npreHash={pre}\npostHash={post}\ntransformHashMatch={pre==post}\n");
  result.Log($"LINKED_WATER_DUP_FIX disabled={disabled} unified={unified} hashMatch={pre==post}");
 }
 string TransformHash(Scene sc){
  var sb=new StringBuilder();
  foreach(var t in sc.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<Transform>(true)).OrderBy(t=>PathOf(t),StringComparer.Ordinal)){
   var p=t.localPosition;var q=t.localRotation;var s=t.localScale;
   sb.Append(PathOf(t)).Append('|').Append(p.x.ToString("R")).Append(',').Append(p.y.ToString("R")).Append(',').Append(p.z.ToString("R")).Append('|')
   .Append(q.x.ToString("R")).Append(',').Append(q.y.ToString("R")).Append(',').Append(q.z.ToString("R")).Append(',').Append(q.w.ToString("R")).Append('|')
   .Append(s.x.ToString("R")).Append(',').Append(s.y.ToString("R")).Append(',').Append(s.z.ToString("R")).Append('|').Append(t.gameObject.activeSelf).Append('\n');
  }
  using(var sha=SHA256.Create()){return BitConverter.ToString(sha.ComputeHash(Encoding.UTF8.GetBytes(sb.ToString()))).Replace("-","");}
 }
 string PathOf(Transform t){var s=t.GetSiblingIndex()+":"+t.name;while(t.parent!=null){t=t.parent;s=t.GetSiblingIndex()+":"+t.name+"/"+s;}return s;}
 void Capture(Scene sc){
  var go=new GameObject("TEMP_LINKED_WATER_VERIFY_CAMERA");SceneManager.MoveGameObjectToScene(go,sc);
  var cam=go.AddComponent<Camera>();cam.orthographic=true;cam.orthographicSize=1050f;cam.transform.position=new Vector3(-256f,2400f,256f);cam.transform.rotation=Quaternion.Euler(90f,0,0);cam.clearFlags=CameraClearFlags.SolidColor;cam.backgroundColor=new Color(.14f,.16f,.17f);cam.nearClipPlane=.1f;cam.farClipPlane=4000f;cam.depthTextureMode|=DepthTextureMode.Depth;
  var lgo=new GameObject("TEMP_LINKED_WATER_VERIFY_LIGHT");SceneManager.MoveGameObjectToScene(lgo,sc);var l=lgo.AddComponent<Light>();l.type=LightType.Directional;l.intensity=1.1f;lgo.transform.rotation=Quaternion.Euler(55,-35,0);
  var rt=new RenderTexture(1400,850,24);cam.targetTexture=rt;cam.Render();RenderTexture.active=rt;var tex=new Texture2D(rt.width,rt.height,TextureFormat.RGB24,false);tex.ReadPixels(new Rect(0,0,rt.width,rt.height),0,0);tex.Apply();
  File.WriteAllBytes("C:/MMUnityPort/Validation/LinkedWaterAudit_20260920/after_duplicate_fix.png",tex.EncodeToPNG());
  RenderTexture.active=null;cam.targetTexture=null;UnityEngine.Object.DestroyImmediate(rt);UnityEngine.Object.DestroyImmediate(tex);UnityEngine.Object.DestroyImmediate(go);UnityEngine.Object.DestroyImmediate(lgo);
 }
}